using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Roles.Unit.JackOLantern;

namespace TownOfHostY.Roles.Madmate;
public sealed class JO : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(JO),
            player => new JO(player),
            CustomRoles.jO,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Madmate,
            (int)Options.offsetId.UnitMix + 100,//使用しない
            null,
            "オー",
            countType:CountTypes.Crew,
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public JO(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        roleDetectCooldown = OptionjORoleDetectCooldown.GetFloat();
        canDetectNeutral = OptionjOCanDetectNeutral.GetBool();
        sendRoleMaxCount = OptionjOSendRoleCount.GetInt();
        canVent = OptionjOCanVent.GetBool();

        sendRoleLimit = sendRoleMaxCount;
    }

    static float roleDetectCooldown;
    static int sendRoleMaxCount;
    static bool canDetectNeutral;
    static bool canVent;

    int sendRoleLimit;
    HashSet<byte> DetectPlayers = new(15);
    HashSet<byte> DidntDetectPlayers = new(15);

    public override void Add()
    {
        Player.AddDoubleTrigger();
        DetectPlayers = new(15);
        DidntDetectPlayers = new(15);

        // インポスターからマッドを白に塗り替える
        foreach (var impostor in Main.AllPlayerControls.Where(pc=> pc.Is(CustomRoleTypes.Impostor)))
        {
            NameColorManager.Add(impostor.PlayerId, Player.PlayerId, "#ffffff");
        }
        // インポスター視点マッドに★を付ける
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public float CalculateKillCooldown() => roleDetectCooldown;
    public bool CanUseImpostorVentButton() => canVent;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = 0.1f;
    }

    // 検知済みのプレイヤーであるか
    bool DetectedPlayer(byte playerId) => DetectPlayers.Contains(playerId);
    // 検知無効のプレイヤーであるか
    bool DidntDetectPlayer(byte playerId) => DidntDetectPlayers.Contains(playerId);


    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.CanKill) return;

        var (killer, target) = info.AttemptTuple;
        info.DoKill = false;

        // 検知済みのプレイヤーなら省略
        if (DetectedPlayer(target.PlayerId)) return;
        if (DidntDetectPlayer(target.PlayerId)) return;

        Logger.Info($"{killer.GetNameWithRole()}：役職検知→{target.GetNameWithRole()}", "jO");

        // 検知できるかチェック
        if (target.Is(CustomRoles.Lantern))
        {
            Logger.Info($"{killer.GetNameWithRole()}：役職検知(ランタン)キャンセル", "jO");
            DidntDetect(target.PlayerId);
            return;
        }
        else if (target.Is(CustomRoleTypes.Neutral) && !canDetectNeutral)
        {
            Logger.Info($"{killer.GetNameWithRole()}：役職検知Ⓝキャンセル", "jO");
            DidntDetect(target.PlayerId);
            return;
        }

        // 検知、リスト登録
        DetectPlayers.Add(target.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()}：リスト登録", "jO");
        // 表示更新
        Utils.NotifyRoles(SpecifySeer: Player);

        // キルクールリセット
        killer.SetKillCooldown();
    }

    // 検知時、無効な対象の時
    void DidntDetect(byte targetId)
    {
        // 検知無効、リスト登録
        DidntDetectPlayers.Add(targetId);
        // 表示更新
        Utils.NotifyRoles(SpecifySeer: Player);
    }

    public override bool OnCheckShapeshift(PlayerControl target, ref bool animate)
    {
        // 変身アニメーションを起こさない
        animate = false;

        // 回数制限,自身以外の変身かどうか
        var shapeshifting = !Is(target);
        if (sendRoleLimit <= 0 || !shapeshifting) return false;

        // 対象が検知済みのプレイヤーであるか
        if (!DetectedPlayer(target.PlayerId))
        {
            Logger.Info($"{Player.GetNameWithRole()} : {target.GetNameWithRole()}はまだ検知していません", "jO");
            return false;
        }
        // 対象が検知不可能なプレイヤーであるか
        if (DidntDetectPlayer(target.PlayerId))
        {
            Logger.Info($"{Player.GetNameWithRole()} : {target.GetNameWithRole()}は検知×対象です(送信キャンセル)", "jO");
            return false;
        }

        // ジャックに送信する
        SentPlayer.Add(target.PlayerId);
        
        sendRoleLimit--;
        Logger.Info($"{Player.GetNameWithRole()} : 役職送信→{target.GetNameWithRole()}/残り{sendRoleLimit}回", "jO");

        // 表示更新
        jack.KillFlash();
        Utils.NotifyRoles(SpecifySeer: Player);
        Utils.NotifyRoles(SpecifySeer: jack);

        return false;
    }

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref string roleText)
    {
        // 検知無効マーク
        if (DidntDetectPlayer(seen.PlayerId))
        {
            enabled = true;
            roleText = Utils.ColorString(Utils.GetRoleColor(CustomRoles.JackOLantern), "×");
        }
        // 検知済み役職表示
        if (DetectedPlayer(seen.PlayerId)) enabled = true;
        // 検知済みかつ送信済み
        if (SentPlayer.Contains(seen.PlayerId))
        {
            roleText = Utils.ColorString(Utils.GetRoleColor(CustomRoles.JackOLantern), "◆") + roleText;
        }
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!seer.Is(CustomRoleTypes.Impostor) ||
            seen.GetRoleClass() is not JO jO)
        {
            return string.Empty;
        }
        // インポスターから見たマッドへの★
        return Utils.ColorString(Palette.ImpostorRed, "★");
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("jODetect");
        return true;
    }
    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(sendRoleLimit > 0 ? RoleInfo.RoleColor : Color.gray, $"[{sendRoleLimit}]");
}

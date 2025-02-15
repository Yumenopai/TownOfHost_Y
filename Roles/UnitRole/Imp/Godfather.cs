using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Roles.Unit.GodfatherAndJanitor;

namespace TownOfHostY.Roles.Impostor;
public sealed class Godfather : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Godfather),
            player => new Godfather(player),
            CustomRoles.Godfather,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 100,//使用しない
            null,
            "ゴッドファーザー"
        );
    public Godfather(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        GodfatherKillCooldown = OptionGodfatherKillCooldown.GetFloat();
        LockDistance = OptionGodfatherLockDistance.GetFloat();
        JanitorSeeSelectedTiming = OptionJanitorSeeSelectedTiming.GetBool();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public override void OnDestroy()
    {
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
    }

    private static float GodfatherKillCooldown;
    private static float LockDistance;
    private static bool JanitorSeeSelectedTiming;
    // ジャニターが近くにいる時のマーク表示
    private static bool canLockKill;

    public override void Add()
    {
        godfather = Player;
        Logger.Info($"{Player.GetNameWithRole()} : Godfather登録", "G&J");

        JanitorTarget.Clear();
        canLockKill = false;
    }
    public float CalculateKillCooldown() => GodfatherKillCooldown;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        //ジャニターが死亡している場合killをそのまま行う
        if (!janitor.IsAlive()) return;

        // ゴッドファーザーとジャニターの距離
        var janitorDist = Vector2.Distance(killer.transform.position, janitor.transform.position);
        Logger.Info($"{Player.GetNameWithRole()}～Janitor距離 : {janitorDist}", "G&J");
        // 設定距離に満たない場合は通常のキルをそのまま行う
        if (!canLockKill) return;

        /* ジャニターターゲットの設定*/
        // キルしない
        info.CanKill = false;

        // ジャニターターゲットの追加
        JanitorTarget.Add(target.PlayerId);

        // ターゲットの足止め
        Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
        target.MarkDirtySettings();
        Logger.Info($"{target.GetNameWithRole()} : ターゲット足止め", "G&J");

        // ターゲットにキルフラッシュ
        target.KillFlash();

        // ジャニターへのキルフラッシュ通知
        if (JanitorSeeSelectedTiming)
        {
            janitor.KillFlash();
        }
        // ジャニター視点の矢印表示追加
        if (Janitor.TrackTarget)
        {
            TargetArrow.Add(janitor.PlayerId, target.PlayerId);
        }

        // 自身のキルクールリセット
        killer.SetKillCooldown();

        // バニラの表示更新:ターゲットの名前色、マーク表示更新の為全員をまわす
        Utils.NotifyRoles(ForceLoop: true);
    }

    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __)
    {
        // ターゲットがいない場合は処理しない
        if (JanitorTarget.Count <= 0) return;

        // ジャニターによるキルが行われずに残ったターゲットは会議前にキル
        foreach (var targetId in JanitorTarget)
        {
            var target = Utils.GetPlayerById(targetId);

            // ターゲットが生存している場合のみキルを実行
            if (target.IsAlive())
            {
                target.SetRealKiller(Player);
                target.RpcMurderPlayer(target, true);

                // ターゲットの足止め解除
                Main.AllPlayerSpeed[target.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
                target.MarkDirtySettings();
                Logger.Info($"{target.GetNameWithRole()} : ジャニター未掃除のターゲット死亡", "G&J");
            }
        }

        // ターゲットをリセット
        JanitorTarget.Clear();
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask || !Player.IsAlive()) return;

        var pc = janitor;
        if (pc.inVent) return;
        bool isChange = false;
        // ゴッドファーザーとジャニターの距離
        var janitorDist = Vector2.Distance(godfather.transform.position, janitor.transform.position);

        // 設定距離範囲内か
        bool canDist = janitorDist <= LockDistance;
        // 変更されているか
        isChange = canLockKill != canDist;

        if (isChange)
        {
            canLockKill = canDist;
            Utils.NotifyRoles(SpecifySeer: Player);
        }
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("GodfatherLock");
        return canLockKill;
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref string roleText)
    {
        // 相方の役職名を表示させる
        if (seen.Is(CustomRoles.Janitor)) enabled = true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seen != seer) return string.Empty;
        string mark = canLockKill ? "⊥" : "";
        return Utils.ColorString(RoleInfo.RoleColor, mark);
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        // seenが省略の場合seer
        seen ??= seer;

        // ターゲットでない
        if (!JanitorTarget.Contains(seen.PlayerId)) return string.Empty;

        // ジャニター対象へのマーク
        return Utils.ColorString(Palette.ImpostorRed, "⊥");
    }

    // ターゲットは全視点に名前が紫色になる
    public static string OverrideNameColorByJanitorTarget(PlayerControl target, string colorCode)
    {
        if (!CustomRoles.GodfatherAndJanitor.IsEnable()) return colorCode;

        if (JanitorTarget.Contains(target.PlayerId))
        {
            colorCode = "#cc00cc";
        }

        return colorCode;
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Madmate;
public sealed class MadConnecter : RoleBase, IKiller, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadConnecter),
            player => new MadConnecter(player),
            CustomRoles.MadConnecter,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Madmate,
            (int)Options.offsetId.MadY + 800,
            SetupOptionItem,
            "マッドコネクター",
            isDesyncImpostor: true
        );
    public MadConnecter(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CanVent = OptionCanVent.GetBool();
        KnowImpostorTasks = OptionKnowImpostor.GetInt();
        KnowImpostorRoleTasks = OptionKnowImpostorRole.GetInt();
        KnowDeadBodyArrowTasks = OptionKnowDeadBodyArrow.GetInt();

        int count = KnowImpostorTasks;
        if (count < KnowImpostorRoleTasks) count = KnowImpostorRoleTasks;
        if (count < KnowDeadBodyArrowTasks) count = KnowDeadBodyArrowTasks;
        // タスクの最大数
        if (count != 0) maxTask = count;

        MadList = new();
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.SuffixOthers.Add(GetSuffixOthers);
    }

    static OptionItem OptionCanVent;
    static OptionItem OptionKnowImpostor;
    static OptionItem OptionKnowImpostorRole;
    static OptionItem OptionKnowDeadBodyArrow;
    static bool CanVent;
    static int KnowImpostorTasks;
    static int KnowImpostorRoleTasks;
    static int KnowDeadBodyArrowTasks;

    static HashSet<MadConnecter> MadList;
    static int maxTask = 99;

    HashSet<byte> ConnectImpostorId;
    bool[] IsSet;
    enum SetNumber
    {
        knowImpostor = 0,
        knowDeadBodyArrow,
    }

    enum OptionName
    {
        MadConnectorKnowImpostorsTasks,
        MadConnectorKnowImpostorRoleTasks,
        MadConnectorKnowDeadBodyArrowTasks,
    }   

    private static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, true, false);
        OptionKnowImpostor = IntegerOptionItem.Create(RoleInfo, 11, OptionName.MadConnectorKnowImpostorsTasks, new(0, 30, 1), 2, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionKnowImpostorRole = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MadConnectorKnowImpostorRoleTasks, new(0, 30, 1), 4, false, OptionKnowImpostor)
            .SetValueFormat(OptionFormat.Pieces);
        OptionKnowDeadBodyArrow = IntegerOptionItem.Create(RoleInfo, 13, OptionName.MadConnectorKnowDeadBodyArrowTasks, new(0, 30, 1), 0, false)
            .SetValueFormat(OptionFormat.Pieces);

        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public float CalculateKillCooldown() => 0.01f;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(Options.AddOnRoleOptions[(CustomRoles.MadConnecter, CustomRoles.AddLight)].GetBool());
    }
    public override void Add()
    {
        MadList.Add(this);
        ConnectImpostorId = new();
        IsSet = new[] { false, false };

        if (maxTask == 99) maxTask = 1;
        VentEnterTask.Add(Player, maxTask, useVent: CanVent);
    }
    public bool CheckKillFlash(MurderInfo info) => IsSet[(int)SetNumber.knowDeadBodyArrow];

    // TargetDeadArrowで更新を行うか
    public static bool IsEnableDeadArrow()
    {
        return CustomRoles.MadConnecter.IsEnable() && KnowDeadBodyArrowTasks > 0;
    }

    // インポスターが誰か分かるタスク数に達しているか
    private bool KnowsImpostor()
        => KnowImpostorTasks != 0 &&
        VentEnterTask.NowTaskCountNow(Player.PlayerId) >= KnowImpostorTasks;
    // インポスターの役職が分かるタスク数に達しているか
    private bool KnowsImpostorRole()
        => KnowImpostorRoleTasks != 0 &&
        VentEnterTask.NowTaskCountNow(Player.PlayerId) >= KnowImpostorRoleTasks;
    // 死体への矢印が表示されるタスク数に達しているか
    private bool KnowDeadBodyArrow()
        => KnowDeadBodyArrowTasks != 0 &&
        VentEnterTask.NowTaskCountNow(Player.PlayerId) >= KnowDeadBodyArrowTasks;

    // 互いにコネクトしたインポスターであるか
    private bool ConnectsImpostor(byte impostorId)
        => ConnectImpostorId.Contains(impostorId);

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        // ガード持ちに関わらず能力発動する直接キル役職
        (var killer, var target) = info.AttemptTuple;
        info.DoKill = false;

        // 視認前、またはインポスターでない、または既に繋がっている場合は関係ない
        if (!KnowsImpostor() || !target.Is(CustomRoleTypes.Impostor) || ConnectsImpostor(target.PlayerId)) return;

        // インポスターとコネクト
        ConnectImpostorId.Add(target.PlayerId);
        // マッドにはパリン
        killer.RpcProtectedMurderPlayer(target);
        // 相方インポスターにはキルフラッシュ
        target.KillFlash();

        Logger.Info($"{killer.GetNameWithRole()} : {target.GetNameWithRole()}とコネクト", "MadConnecter");

        // 互いに矢印追加
        TargetArrow.Add(killer.PlayerId, target.PlayerId);
        TargetArrow.Add(target.PlayerId, killer.PlayerId);

        // 表示更新
        Utils.NotifyRoles();
    }

    // タスク完了ごとに呼ばれる
    public static void OnCompleteVentTask(PlayerControl pc)
    {
        // 全員通る
        if (pc.GetRoleClass() is not MadConnecter mad) return;

        // インポスターが誰か分かる
        if (mad.KnowsImpostor() && !mad.IsSet[(int)SetNumber.knowImpostor])
        {
            mad.IsSet[(int)SetNumber.knowImpostor] = true;
            foreach (var impostor in Main.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor)))
            {
                // インポスターの名前を赤くする
                NameColorManager.Add(pc.PlayerId, impostor.PlayerId, impostor.GetRoleColorCode());
            }
            // 表示更新
            Utils.NotifyRoles(SpecifySeer: pc);
        }

        // 死体への矢印が表示される
        if (mad.KnowDeadBodyArrow() && !mad.IsSet[(int)SetNumber.knowDeadBodyArrow])
        {
            mad.IsSet[(int)SetNumber.knowDeadBodyArrow] = true;

            // 死体への矢印が表示できるようにする
            TargetDeadArrow.AddSeer(mad.Player.PlayerId);
        }
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("MadConnecterConnect");
        return true;
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref string roleText)
    {
        // 相方の役職名を表示させる
        if (KnowsImpostor() && KnowsImpostorRole() && seen.Is(CustomRoleTypes.Impostor)) enabled = true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //seerが自分でない、
        if (!Is(seer)) return string.Empty;

        // コネクトしたインポスターへの丸印表示
        if (ConnectImpostorId.Contains(seen.PlayerId))
        {
            // コネクト済みなら丸印を付ける
            return Utils.ColorString(Palette.ImpostorRed, "●");
        }
        // 相手につけるのはここまで
        if (!Is(seen)) return string.Empty;

        // タスク完了による能力解放マーク
        var mark = new StringBuilder();
        if (KnowsImpostor()) mark.Append(Utils.ColorString(Palette.ImpostorRed, "Ｉ"));
        if (KnowsImpostorRole()) mark.Append(Utils.ColorString(Palette.Purple, "Ｒ"));
        if (KnowDeadBodyArrow()) mark.Append(Utils.ColorString(Palette.CrewmateBlue, "Ｄ"));
        return mark.ToString();
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!seer.Is(CustomRoleTypes.Impostor) ||
            seen.GetRoleClass() is not MadConnecter madConnecter ||
            !madConnecter.ConnectsImpostor(seer.PlayerId))
        {
            return string.Empty;
        }
        // インポスターから見たマッドへの★
        return Utils.ColorString(Palette.ImpostorRed, "★");
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //seerおよびseenが自分である場合以外、または会議中は関係なし
        if (!Is(seer) || !Is(seen) || isForMeeting) return string.Empty;

        string arrow = "";
        // インポスターへの矢印表示
        if (ConnectImpostorId.Count > 0)
        {
            foreach (var targetId in ConnectImpostorId)
            {
                // マッドからコネクトインポスターへの矢印
                arrow += TargetArrow.GetArrows(Player, targetId);
            }
            // 矢印表示があれば
            if (arrow.Length >= 0)
            {
                // 色を付ける
                arrow = Utils.ColorString(Palette.ImpostorRed, arrow);
            }
        }
        return arrow;
    }
    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer != seen || isForMeeting) return string.Empty;
        // インポスターから見たマッドへの矢印
        return GetImpostorArrows(seer.PlayerId);
    }
    private static string GetImpostorArrows(byte seerId)
    {
        var arrow = "";
        MadList.RemoveWhere(mad => !mad.Player.IsAlive());

        HashSet<byte> targets = new(15);

        foreach (var mad in MadList)
        {
            if (mad.ConnectImpostorId.Count <= 0) continue;

            targets.Add(mad.Player.PlayerId);
        }
        // インポスターから見たマッドへの矢印
        arrow += TargetArrow.GetArrows(seerId, targets.ToArray());

        return arrow.Length > 0 ? Utils.ColorString(Palette.ImpostorRed, arrow) : "";
    }
}
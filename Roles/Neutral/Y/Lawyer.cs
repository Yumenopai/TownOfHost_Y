using System.Collections.Generic;
using System.Linq;
using Hazel;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Neutral;
public sealed class Lawyer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Lawyer),
            player => new Lawyer(player),
            CustomRoles.Lawyer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuY + 500,
            SetupOptionItem,
            "弁護士",
            "#daa520",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public Lawyer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        KnowTargetRole = OptionKnowTargetRole.GetBool();
        TargetKnows = OptionTargetKnows.GetBool();
        PursuerGuardNum = OptionPursuerGuardNum.GetInt();

        Lawyers.Add(this);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);

        Target = null;
        Pursuers = false;
    }
    public override void OnDestroy()
    {
        Lawyers.Remove(this);

        if (Lawyers.Count <= 0)
        {
            CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
            CustomRoleManager.OnMurderPlayerOthers.Remove(OnMurderPlayerOthers);
        }
    }

    private static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionKnowTargetRole;
    private static OptionItem OptionTargetKnows;
    private static OptionItem OptionPursuerGuardNum;
    enum OptionName
    {
        LawyerTargetKnows,
        LawyerKnowTargetRole,
        PursuerGuardNum
    }
    private static bool HasImpostorVision;
    private static bool KnowTargetRole;
    private static bool TargetKnows;
    private static int PursuerGuardNum;

    private static HashSet<Lawyer> Lawyers = new(15);
    private PlayerControl Target = null;
    private bool Pursuers = false;

    private int GuardCount = 0;

    private static void SetupOptionItem()
    {
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.ImpostorVision, false, false);
        OptionKnowTargetRole = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LawyerKnowTargetRole, false, false);
        OptionTargetKnows = BooleanOptionItem.Create(RoleInfo, 12, OptionName.LawyerTargetKnows, false, false);
        OptionPursuerGuardNum = IntegerOptionItem.Create(RoleInfo, 13, OptionName.PursuerGuardNum, new(0, 20, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        Pursuers = false;
        //ターゲット割り当て
        if (AmongUsClient.Instance.AmHost)
        {
            List<PlayerControl> targetList = new();
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (Player == target) continue;
                if ((target.Is(CustomRoleTypes.Impostor)
                    || target.IsNeutralKiller())
                    && !target.Is(CustomRoles.Lovers)
                ) targetList.Add(target);
            }
            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            Target = SelectedTarget;
            SendRPC(Player.PlayerId, SelectedTarget.PlayerId, "SetTarget");
            Logger.Info($"{Player.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Lawyer");
        }

        GuardCount = PursuerGuardNum;
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
    private void SendRPC(byte LawyerId, byte targetId = byte.MaxValue, string Progress = "")
    {
        switch (Progress)
        {
            case "SetTarget":
                var sender = CreateSender(CustomRPC.SetLawyerTarget);
                sender.Writer.Write(LawyerId);
                sender.Writer.Write(targetId);
                break;
            case "":
                if (!AmongUsClient.Instance.AmHost) return;
                var Rsender = CreateSender(CustomRPC.SetRemoveLawyerTarget);
                Rsender.Writer.Write(LawyerId);
                break;
        }
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType == CustomRPC.SetLawyerTarget)
        {
            byte LawyerId = reader.ReadByte();
            byte TargetId = reader.ReadByte();
            if (Player == null || Player.PlayerId != LawyerId) return;

            Target = Utils.GetPlayerById(TargetId);
        }
        else if (rpcType == CustomRPC.SetRemoveLawyerTarget)
        {
            byte LawyerId = reader.ReadByte();
            if (Player == null || Player.PlayerId != LawyerId) return;
            Target = null;
        }
    }
    public bool IsPursuer() => Pursuers;

    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        var target = info.AttemptTarget;

        foreach (var lawyer in Lawyers.ToArray())
        {
            if (lawyer.Target == target)
            {
                lawyer.ChangeRole();
                break;
            }
        }
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (KnowTargetRole && Target != null && seen.PlayerId == Target.PlayerId)
            enabled = true;
    }

    private bool CanUseGuard() => Player.IsAlive() && GuardCount > 0;
    public override string GetProgressText(bool comms = false)
    {
        if (!Pursuers) return string.Empty;
        return Utils.ColorString(CanUseGuard() ? Color.yellow : Color.gray, $"〔{GuardCount}〕");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer == Player && seen == Target)
            return Utils.ColorString(RoleInfo.RoleColor, "§");
        return string.Empty;
    }
    public string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (TargetKnows && seer == Target && seen == Target && Player.IsAlive())
            return Utils.ColorString(RoleInfo.RoleColor, "§");
        return string.Empty;
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;

        if (!Pursuers || GuardCount <= 0) return true;
        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        killer.SetKillCooldown();
        GuardCount--;
        Utils.NotifyRoles(SpecifySeer: target);
        info.CanKill = false;
        return true;
    }

    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (Player == null) return;
        if (Target != null && Target.PlayerId == exiled.PlayerId && Player.IsAlive())
            ChangeRole();
    }

    public static void ChangeRoleByTarget(PlayerControl target)
    {
        foreach (var lawyer in Lawyers)
        {
            if (lawyer.Target != target) continue;

            lawyer.ChangeRole();
            break;
        }
    }
    public void ChangeRole()
    {
        Pursuers = true;
        Target = null;
        SendRPC(Player.PlayerId);
        Utils.NotifyRoles();
    }


    public static void EndGameCheck()
    {
        foreach (var pc in Main.AllPlayerControls.Where(c => c.GetCustomRole() == CustomRoles.Lawyer))
        {
            var role = (Lawyer)pc.GetRoleClass();
            if (!role.IsPursuer())
            {
                // 弁護士
                // 勝者に依頼人が含まれている時
                if (role.Target != null && CustomWinnerHolder.WinnerIds.Contains(role.Target.PlayerId))
                {
                    // 弁護士が生きている時 リセットして単独勝利
                    if (pc.IsAlive())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lawyer);
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    }
                    // 弁護士が死んでいる時 勝者と共に追加勝利
                    else
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Lawyer);
                    }
                }
            }
            else
            {
                // 追跡者
                // 追跡者が生き残った場合ここで追加勝利
                if (pc.IsAlive())
                {
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Lawyer);
                }
            }
        }
    }
}
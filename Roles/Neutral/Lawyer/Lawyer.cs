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
    public static bool HasImpostorVision;
    private static bool KnowTargetRole;
    private static bool TargetKnows;
    public static int PursuerGuardNum;

    private static HashSet<Lawyer> Lawyers = new(15);
    private PlayerControl Target = null;
    public static CustomRoles[] AdditionalRoles => [CustomRoles.Pursuer];

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
        //ターゲット割り当て
        if (AmongUsClient.Instance.AmHost)
        {
            var rand = IRandom.Instance;
            var targetList = GetTargetList(false);
            if (targetList.Count == 0) targetList = GetTargetList(true);
            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            Target = SelectedTarget;
            SendRPC(Player.PlayerId, SelectedTarget.PlayerId, "SetTarget");
            Logger.Info($"{Player.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Lawyer");
        }
    }
    private List<PlayerControl> GetTargetList(bool includeLovers)
    {
        List<PlayerControl> targetList = new();
        foreach (var target in Main.AllPlayerControls)
        {
            if (Player == target) continue;
            if ((target.Is(CustomRoleTypes.Impostor) || target.IsNeutralKiller()) &&
                (includeLovers || !target.Is(CustomRoles.Lovers))
            ) targetList.Add(target);
        }
        return targetList;
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
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref string roleText)
    {
        if (KnowTargetRole && Target != null && seen.PlayerId == Target.PlayerId)
            enabled = true;
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
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (Player == null) return;
        if (Target != null && Target.PlayerId == exiled.PlayerId && Player.IsAlive())
        {
            _ = new LateTask(() => ChangeRole(), 0.5f, "LawyerChangeRoleByExile");
        }
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
        SendRPC(Player.PlayerId);
        Player.RpcSetCustomRole(CustomRoles.Pursuer);
        Utils.NotifyRoles();
    }
    public static void EndGameCheck()
    {
        foreach (var pc in Main.AllPlayerControls.Where(c => c.GetCustomRole() == CustomRoles.Lawyer))
        {
            var role = (Lawyer)pc.GetRoleClass();

            // 勝者に依頼人が含まれている時
            if (role.Target != null &&
                (CustomWinnerHolder.WinnerIds.Contains(role.Target.PlayerId) ||
                 CustomWinnerHolder.WinnerRoles.Contains(role.Target.GetCustomRole())))
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
    }
}
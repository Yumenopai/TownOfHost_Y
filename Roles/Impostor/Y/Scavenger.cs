using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class Scavenger : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Scavenger),
            player => new Scavenger(player),
            CustomRoles.Scavenger,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 500,
            SetUpOptionItem,
            "スカベンジャー"
        );
    public Scavenger(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        IgnoreBait = OptionIgnoreBait.GetBool();
        IsEatUpKillLimit = OptionIsEatUpKillLimit.GetBool();
        LimitMaxCount = OptionLimitMaxCount.GetInt();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionIgnoreBait;
    private static OptionItem OptionIsEatUpKillLimit;
    private static OptionItem OptionLimitMaxCount;
    enum OptionName
    {
        ScavengerIgnoreBait,
        ScavengerIsEatUpKillLimit,
        ScavengerLimitMaxCount,
    }
    private static float KillCooldown;
    public static bool IgnoreBait;
    private static bool IsEatUpKillLimit;
    private static int LimitMaxCount;

    int limitCount = 15;

    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionIsEatUpKillLimit = BooleanOptionItem.Create(RoleInfo, 20, OptionName.ScavengerIsEatUpKillLimit, false, false);
        OptionLimitMaxCount = IntegerOptionItem.Create(RoleInfo, 21, OptionName.ScavengerLimitMaxCount, new(1, 15, 1), 2, false).SetParent(OptionIsEatUpKillLimit)
            .SetValueFormat(OptionFormat.Pieces);
        OptionIgnoreBait = BooleanOptionItem.Create(RoleInfo, 11, OptionName.ScavengerIgnoreBait, false, false);
    }
    public override void Add()
    {
        if (IsEatUpKillLimit)
        {
            limitCount = LimitMaxCount;
            Player.AddDoubleTrigger();
        }
    }
    public float CalculateKillCooldown() => KillCooldown;
    public override string GetProgressText(bool comms = false)
    {
        if (!IsEatUpKillLimit) return string.Empty;

        return Utils.ColorString(limitCount > 0 ? Palette.ImpostorRed : Color.gray, $"[{limitCount}]");
    }

    // Limit-ON
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide) return;
        if (!IsEatUpKillLimit || limitCount <= 0) return;
        if (!info.CanKill) return;

        (var killer, var target) = info.AttemptTuple;
        info.CanKill = killer.CheckDoubleTrigger(target, () =>
        {
            if (!killer.IsAlive() || !target.IsAlive()) return;

            var killerId = killer.PlayerId;
            var targetId = target.PlayerId;

            target.SetRealKiller(killer);
            killer.RpcMurderPlayer(target, true);
            limitCount--;
            Logger.Info($"{killer.GetNameWithRole()}：喰らいつくし 残り{limitCount}回", "Scavenger");

            Utils.NotifyRoles(SpecifySeer: killer);
            EatUpKill(killer, target);
        });
    }
    // Limit-OFF
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        if (!Is(info.AttemptKiller) || info.IsSuicide) return;
        if (IsEatUpKillLimit) return;

        EatUpKill(killer, target);
    }

    private static void EatUpKill(PlayerControl killer, PlayerControl target)
    {
        if (!IgnoreBait && target.Is(CustomRoles.Bait))
        {
            Logger.Info($"{target.GetNameWithRole()}：ベイトキルなので通報される", "Scavenger");
        }
        else //ベイトじゃない又はベイト無効など
        {
            if (target.Is(CustomRoles.Bait)) Bait.BaitKillPlayer = null; //ベイトマーク取り消し
            ReportDeadBodyPatch.CannotReportByDeadBodyList.Add(target.PlayerId);
            Logger.Info($"{target.GetNameWithRole()}：通報できない死体", "Scavenger");
        }
    }

}

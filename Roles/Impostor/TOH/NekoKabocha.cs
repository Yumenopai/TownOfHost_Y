using AmongUs.GameOptions;
using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;

public sealed class NekoKabocha : RoleBase, IImpostor//, INekomata
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(NekoKabocha),
            player => new NekoKabocha(player),
            CustomRoles.NekoKabocha,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpTOH + 1700,
            SetupOptionItems,
            "ネコカボチャ",
            introSound: () => PlayerControl.LocalPlayer.KillSfx
        );
    public NekoKabocha(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        impostorsGetRevenged = Options.RevengeImpostorByImpostor.GetBool();
        madmatesGetRevenged = Options.RevengeMadByImpostor.GetBool();
        revengeOnExile = optionRevengeOnExile.Bool;
    }

    private static BooleanOptionItem optionRevengeOnExile;
    private static void SetupOptionItems()
    {
        optionRevengeOnExile = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NekoKabochaRevengeOnExile, false, false);
    }
    private enum OptionName { NekoKabochaRevengeOnExile }

    private static bool impostorsGetRevenged;
    private static bool madmatesGetRevenged;
    public static bool revengeOnExile;
    private static readonly LogHandler logger = Logger.Handler(nameof(NekoKabocha));

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        // 普通のキルじゃない．もしくはキルを行わない時はreturn
        if (info.IsAccident || info.IsSuicide || !info.CanKill || !info.DoKill || info.IsMeeting)
        {
            return;
        }
        // 殺してきた人を殺し返す
        logger.Info("ネコカボチャの仕返し");
        var killer = info.AttemptKiller;
        if (!IsCandidate(killer))
        {
            logger.Info("キラーは仕返し対象ではないので仕返しされません");
            return;
        }
        killer.SetRealKiller(Player);
        PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Revenge;
        Player.RpcMurderPlayer(killer);
    }
    //public bool DoRevenge(CustomDeathReason deathReason) => revengeOnExile && deathReason == CustomDeathReason.Vote;
    public bool IsCandidate(PlayerControl player)
    {
        return player.GetCustomRole().GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => impostorsGetRevenged,
            CustomRoleTypes.Madmate => madmatesGetRevenged,
            _ => true,
        };
    }
}

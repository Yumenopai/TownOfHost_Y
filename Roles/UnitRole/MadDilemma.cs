using AmongUs.GameOptions;
using Hazel;
using InnerNet;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Madmate;
public sealed class MadDilemma : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadDilemma),
            player => new MadDilemma(player),
            CustomRoles.MadDilemma,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            (int)Options.offsetId.UnitMix + 0,
            null,
            "マッドジレンマ",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadDilemma(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {}
}

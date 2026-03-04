using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Vanilla;

public sealed class Detective : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Detective),
            player => new Detective(player),
            RoleTypes.Detective,
            "#8cffff"
        );
    public Detective(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Roles.Impostor.GotFather_Janitor;

namespace TownOfHostY.Roles.Impostor;
public sealed class GotFather : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GotFather),
            player => new GotFather(player),
            CustomRoles.GotFather,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 300,
            null,
            "ゴットファーザー"
        );
    public GotFather(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        GotFatherKillCooldown = OptionGotFatherKillCooldown.GetFloat();

    }
    private static float GotFatherKillCooldown;
    public float CalculateKillCooldown() => GotFatherKillCooldown;
}

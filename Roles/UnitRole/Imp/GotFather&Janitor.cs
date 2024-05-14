using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class GotFather_Janitor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GotFather_Janitor),
            player => new GotFather_Janitor(player),
            CustomRoles.GotFather_Janitor,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Unit,
            (int)Options.offsetId.UnitMix + 0,
            SetupOptionItem,
            "ゴットファーザー&ジャニター",
            assignInfo: new RoleAssignInfo(CustomRoles.GotFather_Janitor, CustomRoleTypes.Unit)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.GotFather, CustomRoles.Janitor }
            }
        );
    public GotFather_Janitor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    enum OptionName
    {
    }
    private static void SetupOptionItem()
    {
    }
}

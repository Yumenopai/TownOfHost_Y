using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
namespace TownOfHostY.Roles.Impostor;
public sealed class GotFather_Janitor : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GotFather_Janitor),
            player => new GotFather_Janitor(player),
            CustomRoles.GotFather_Janitor,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 100,
            SetupOptionItem,
            "ゴットファーザー&ジャニター",
            tab: TabGroup.UnitRoles,
            assignInfo: new RoleAssignInfo(CustomRoles.GotFather_Janitor, CustomRoleTypes.Impostor)
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
    public static OptionItem OptionGotFatherKillCooldown;
    public static OptionItem OptionCleanCooldown;
    public static OptionItem OptionLookJanitor;
    enum OptionName
    {
        GotFatherKillCooldown,
        JanitorCleanCooldown,
        LookJanitor,
    }
    private static void SetupOptionItem()
    {
        OptionGotFatherKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.GotFatherKillCooldown, new(5.0f, 900f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCleanCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.JanitorCleanCooldown, new(5.0f, 900f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionLookJanitor = FloatOptionItem.Create(RoleInfo, 12, OptionName.LookJanitor, new(1.0f, 20f, 0.5f), 10f, false)
        .SetValueFormat(OptionFormat.Multiplier);
    }
}

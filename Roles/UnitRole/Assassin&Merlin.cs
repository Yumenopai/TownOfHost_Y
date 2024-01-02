using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class AssassinAndMerlin : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(AssassinAndMerlin),
            player => new AssassinAndMerlin(player),
            CustomRoles.AssassinAndMerlin,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Unit,
            (int)Options.offsetId.UnitSpecial + 0,
            //(int)Options.offsetId.UnitMix + 0,
            //SetupOptionItem,
            null,
            "アサシン&マーリン",
            "#ffffff",
            assignInfo: new RoleAssignInfo(CustomRoles.AssassinAndMerlin, CustomRoleTypes.Unit)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.Assassin, CustomRoles.Merlin }
            }
        );
    public AssassinAndMerlin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    //public static OptionItem OptionTaskTrigger;
    //public static OptionItem OptionChallengeMaxCount;
    //public static OptionItem OptionResetAddonChangeCrew;
    enum OptionName
    {
        CounselorTaskTrigger,
        CounselorChallengeMaxCount,
    }
    private static void SetupOptionItem()
    {
        //OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 10, OptionName.CounselorTaskTrigger, new(0, 20, 1), 5, false)
        //    .SetValueFormat(OptionFormat.Pieces);
        //OptionChallengeMaxCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CounselorChallengeMaxCount, new(1, 15, 1), 3, false)
        //    .SetValueFormat(OptionFormat.Times);
        //OptionResetAddonChangeCrew = BooleanOptionItem.Create(RoleInfo, 12, OptionName.MadDilemmaResetAddonChangeCrew, false, false);

    }
}

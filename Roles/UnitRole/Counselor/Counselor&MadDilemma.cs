using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Unit;
public sealed class CounselorAndMadDilemma : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CounselorAndMadDilemma),
            player => new CounselorAndMadDilemma(player),
            CustomRoles.CounselorAndMadDilemma,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Unit,
            (int)Options.offsetId.UnitMix + 0,
            SetupOptionItem,
            "カウンセラー&マッドジレンマ",
            "#ffffff",
            assignInfo: new RoleAssignInfo(CustomRoles.CounselorAndMadDilemma, CustomRoleTypes.Unit)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.Counselor, CustomRoles.MadDilemma }
            }
        );
    public CounselorAndMadDilemma(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public static OptionItem OptionTaskTrigger;
    public static OptionItem OptionChallengeMaxCount;
    //public static OptionItem OptionResetAddonChangeCrew;
    enum OptionName
    {
        CounselorTaskTrigger,
        CounselorChallengeMaxCount,
        //MadDilemmaResetAddonChangeCrew,
    }
    private static void SetupOptionItem()
    {
        Dictionary<string, string> counselorDic = new() { { "%counselor%", Utils.ColorString(Utils.GetRoleColor(CustomRoles.Counselor), Utils.GetRoleName(CustomRoles.Counselor)) } };

        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 10, OptionName.CounselorTaskTrigger, new(0, 20, 1), 5, false).SetReplacementDictionary(counselorDic)
            .SetValueFormat(OptionFormat.Pieces);
        OptionChallengeMaxCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CounselorChallengeMaxCount, new(1, 15, 1), 3, false).SetReplacementDictionary(counselorDic)
            .SetValueFormat(OptionFormat.Times);
        //OptionResetAddonChangeCrew = BooleanOptionItem.Create(RoleInfo, 12, OptionName.MadDilemmaResetAddonChangeCrew, false, false);

        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, CustomRoles.MadDilemma, RoleInfo.Tab, RoleInfo.RoleName, true);
    }
}

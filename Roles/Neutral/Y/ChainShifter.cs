using System.Linq;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.AddOns.Common;
using static TownOfHostY.Roles.Neutral.SchrodingerCat;
using UnityEngine;

namespace TownOfHostY.Roles.Neutral;

public sealed class ChainShifter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ChainShifter),
            player => new ChainShifter(player),
            CustomRoles.ChainShifter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuY + 1100,
            SetupOptionItem,
            "チェインシフター",
            "#666666",
            assignInfo: new RoleAssignInfo(CustomRoles.ChainShifter, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public ChainShifter(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        ShiftTime = OptionShiftTime.GetFloat();
        ShiftDistance = OptionShiftDistance.GetFloat();
        ShiftInactiveTime = OptionShiftInactiveTime.GetFloat();
        ShiftedRole = ChangeRoles[OptionShiftedRole.GetValue()];
        ShiftWhenKilled = OptionShiftWhenKilled.GetBool();

        ChainShifterAddon.Init();
    }

    private static OptionItem OptionShiftTime;
    private static OptionItem OptionShiftDistance;
    private static OptionItem OptionShiftInactiveTime;
    private static OptionItem OptionShiftedRole;
    private static OptionItem OptionShiftWhenKilled;

    public static float ShiftTime;
    public static float ShiftDistance;
    public static float ShiftInactiveTime;
    public static CustomRoles ShiftedRole = CustomRoles.Opportunist;
    public static bool ShiftWhenKilled;

    public static CustomRoles[] AdditionalRoles => [ShiftedRole];
    enum OptionName
    {
        ChainShifterShiftTime,
        ChainShifterShiftDistance,
        ChainShifterShiftInactiveTime,
        ChainShifterShiftedRole,
        ChainShifterShiftWhenKilled,
    }
    public static readonly CustomRoles[] ChangeRoles =
    {
            CustomRoles.Opportunist, CustomRoles.Jester, CustomRoles.Crewmate, CustomRoles.God,
    };
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        OptionShiftTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.ChainShifterShiftTime, new(1f, 10f, 1f), 4f, false)
           .SetValueFormat(OptionFormat.Seconds);
        OptionShiftDistance = FloatOptionItem.Create(RoleInfo, 11, OptionName.ChainShifterShiftDistance, new(0.5f, 2f, 0.1f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionShiftInactiveTime = FloatOptionItem.Create(RoleInfo, 12, OptionName.ChainShifterShiftInactiveTime, new(10f, 60f, 2.5f), 10f, false)
           .SetValueFormat(OptionFormat.Seconds);
        OptionShiftedRole = StringOptionItem.Create(RoleInfo, 13, OptionName.ChainShifterShiftedRole, cRolesString, 1, false);
        OptionShiftWhenKilled = BooleanOptionItem.Create(RoleInfo, 14, OptionName.ChainShifterShiftWhenKilled, false, false);
    }
    public override void Add()
    {
        Player.RpcSetCustomRole(CustomRoles.ChainShifterAddon);
    }

    public override void OverrideShowMainRoleText(ref Color roleColor, ref string roleText)
    {
        roleText = "";
    }
}

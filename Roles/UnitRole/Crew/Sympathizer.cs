using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Utils;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Sympathizer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Sympathizer),
            player => new Sympathizer(player),
            CustomRoles.Sympathizer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.UnitCrew + 0,
            SetupOptionItem,
            "共鳴者",
            "#f08080",
            tab: TabGroup.UnitRoles,
            introSound: () => DestroyableSingleton<HudManager>.Instance.TaskUpdateSound,
            assignInfo: new RoleAssignInfo(CustomRoles.Sympathizer, CustomRoleTypes.Crewmate)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.Sympathizer, CustomRoles.Sympathizer }
            }
        );
    public Sympathizer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        SympaCheckedTasks = OptionSympaCheckedTasks.GetInt();
    }

    private static OptionItem OptionSympaCheckedTasks;
    enum OptionName
    {
        SympaCheckedTasks,
    }

    private static int SympaCheckedTasks;

    private static void SetupOptionItem()
    {
        OptionSympaCheckedTasks = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SympaCheckedTasks, new(1, 20, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer.Is(CustomRoles.Sympathizer) && seen.Is(CustomRoles.Sympathizer)
            && seer.GetPlayerTaskState().CompletedTasksCount >= SympaCheckedTasks
            && seen.GetPlayerTaskState().CompletedTasksCount >= SympaCheckedTasks)
            return ColorString(RoleInfo.RoleColor, "◎");

        return string.Empty;
    }
}
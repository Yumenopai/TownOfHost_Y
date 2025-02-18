using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using static TownOfHostY.Utils;

namespace TownOfHostY.Roles.Crewmate;
public sealed class TaskManager : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(TaskManager),
            player => new TaskManager(player),
            CustomRoles.TaskManager,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 100,
            SetupOptionItem,
            "タスクマネージャー",
            "#80ffdd",
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public TaskManager(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        SeeNowtask = OptionSeeNowtask.GetBool();
    }

    private static OptionItem OptionSeeNowtask;
    enum OptionName
    {
        TaskmanagerSeeNowtask,
    }
    private static bool SeeNowtask;

    private static void SetupOptionItem()
    {
        OptionSeeNowtask = BooleanOptionItem.Create(RoleInfo, 10, OptionName.TaskmanagerSeeNowtask, true, false);
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        // タスクステート取得
        (int completetask, int alltask) = GetTasksState();

        // 全体タスク数が見えるか
        bool canSee = (isForMeeting || !Player.IsAlive() || SeeNowtask) && !IsActive(SystemTypes.Comms);
        // 表示する現在のタスク数
        string nowtask = canSee ? completetask.ToString() : "?";

        return ColorString(RoleInfo.RoleColor, $"({nowtask}/{alltask})");
    }
}
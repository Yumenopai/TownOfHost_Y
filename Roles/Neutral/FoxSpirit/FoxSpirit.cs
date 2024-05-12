using System.Linq;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral;

public sealed class FoxSpirit : RoleBase, ISystemTypeUpdateHook
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(FoxSpirit),
            player => new FoxSpirit(player),
            CustomRoles.FoxSpirit,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuFox + 0,
            SetupOptionItem,
            "妖狐",
            "#ad6ce0",
            countType: CountTypes.None,
            introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
            assignInfo: new(CustomRoles.FoxSpirit, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1),
                IsInitiallyAssignableCallBack = () => (MapNames)Main.NormalOptions.MapId is not MapNames.Polus and not MapNames.Fungle
            }
        );
    public FoxSpirit(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        KilledAfterFinishTask = OptionKilledAfterFinishTask.GetBool();
        IgnoreGhostTask = OptionIgnoreGhostTask.GetBool();
    }
    private static OptionItem OptionTaskCount;
    private static OptionItem OptionKilledAfterFinishTask;
    private static OptionItem OptionIgnoreGhostTask;
    private bool KilledAfterFinishTask;
    public static bool IgnoreGhostTask;
    private enum OptionName
    {
        FoxSpiritTaskCount,
        FoxSpiritKilledAfterFinishTask,
        FoxSpiritIgnoreGhostTask,
    }

    private static void SetupOptionItem()
    {
        OptionTaskCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.FoxSpiritTaskCount, new(1, 50, 1), 10, false)
                .SetValueFormat(OptionFormat.Pieces);
        OptionKilledAfterFinishTask = BooleanOptionItem.Create(RoleInfo, 11, OptionName.FoxSpiritKilledAfterFinishTask, true, false);
        OptionIgnoreGhostTask = BooleanOptionItem.Create(RoleInfo, 12, OptionName.FoxSpiritIgnoreGhostTask, true, false);
    }
    public static (bool, int, int) TaskData => (false, 0, OptionTaskCount.GetInt());
    public static bool CheckWin()
    {
        return Main.AllAlivePlayerControls.ToArray()
                .Any(p => p.Is(CustomRoles.FoxSpirit) && p.GetPlayerTaskState().IsTaskFinished);
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;
        // タスク完了していたらキルされる
        if (KilledAfterFinishTask && IsTaskFinished) return true;

        killer.RpcProtectedMurderPlayer(target); //常にバリアする、互いに分かる
        target.RpcProtectedMurderPlayer(target);
        Logger.Info($"{target.GetNameWithRole()}：ガード", "FoxSpirit");

        info.CanKill = false;
        return true;
    }

    // O2
    bool ISystemTypeUpdateHook.UpdateLifeSuppSystem(LifeSuppSystemType switchSystem, byte amount)
    {
        return false;
    }
    // リアクター
    bool ISystemTypeUpdateHook.UpdateReactorSystem(ReactorSystemType switchSystem, byte amount)
    {
        return false;
    }
    bool ISystemTypeUpdateHook.UpdateHeliSabotageSystem(HeliSabotageSystem switchSystem, byte amount)
    {
        return false;
    }
}

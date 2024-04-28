using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Class;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Madmate;

public sealed class MadGuesser : VoteGuesser, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadGuesser),
            player => new MadGuesser(player),
            CustomRoles.MadGuesser,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            (int)Options.offsetId.MadSpecial + 0,
            SetupOptionItem,
            "マッドゲッサー",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadGuesser(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
        )
    {
        CanVent = OptionCanVent.GetBool();
        CanAlsoBeExposedToImpostor = OptionCanAlsoBeExposedToImpostor.GetBool();
        NumOfGuess = OptionNumOfGuess.GetInt();
        MultipleInMeeting = OptionMultipleInMeeting.GetBool();
        HideMisfire = OptionHideMisfire.GetBool();
        TaskTrigger = OptionTaskTrigger.GetInt();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    private static OptionItem OptionCanVent;
    private static OptionItem OptionCanAlsoBeExposedToImpostor;
    private static OptionItem OptionNumOfGuess;
    private static OptionItem OptionMultipleInMeeting;
    private static OptionItem OptionHideMisfire;
    /// <summary>能力発動タスク数</summary>
    private static OptionItem OptionTaskTrigger;
    private static Options.OverrideTasksData Tasks;
    enum OptionName
    {
        CanVent,
        MadSnitchCanAlsoBeExposedToImpostor,
        GuesserNumOfGuess,
        GuesserMultipleInMeeting,
        GuesserHideMisfire,
        MadSnitchTaskTrigger,
    }
    private static bool CanVent;
    private static bool CanAlsoBeExposedToImpostor;
    private static int TaskTrigger;

    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanVent, false, false);
        OptionCanAlsoBeExposedToImpostor = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MadSnitchCanAlsoBeExposedToImpostor, false, false);
        OptionNumOfGuess = IntegerOptionItem.Create(RoleInfo, 13, OptionName.GuesserNumOfGuess, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionMultipleInMeeting = BooleanOptionItem.Create(RoleInfo, 14, OptionName.GuesserMultipleInMeeting, false, false);
        OptionHideMisfire = BooleanOptionItem.Create(RoleInfo, 15, OptionName.GuesserHideMisfire, false, false);
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 16, OptionName.MadSnitchTaskTrigger, new(0, 99, 1), 1, false).SetValueFormat(OptionFormat.Pieces);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 30, RoleInfo.RoleName, RoleInfo.Tab);
    }

    private bool KnowsImpostor()
    {
        return MyTaskState.HasCompletedEnoughCountOfTasks(TaskTrigger);
    }
    private void CheckAndAddNameColorToImpostors()
    {
        if (!KnowsImpostor()) return;

        foreach (var impostor in Main.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor)))
        {
            NameColorManager.Add(Player.PlayerId, impostor.PlayerId, impostor.GetRoleColorCode());
        }
    }
    public override void Add()
    {
        CheckAndAddNameColorToImpostors();
    }
    public override bool OnCompleteTask()
    {
        CheckAndAddNameColorToImpostors();
        return true;
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (
            // オプションが無効
            !CanAlsoBeExposedToImpostor ||
            // インポスター→MadGuesserではない
            !seer.Is(CustomRoleTypes.Impostor) ||
            seen.GetRoleClass() is not MadGuesser madGuesser ||
            // マッドスニッチがまだインポスターを知らない
            !madGuesser.KnowsImpostor())
        {
            return string.Empty;
        }

        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.MadGuesser), "★");
    }
    public override void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
    {
        if (seen != Player) return;

        var taskState = Player.GetPlayerTaskState();
        if (taskState == null || !taskState.hasTasks) return;

        Color TextColor = Color.white;
        var TaskCompleteColor = RoleInfo.RoleColor.ShadeColor(0.5f); //タスク完了後の色
        var NonCompleteColor = Color.white; //カウントされない人外は白色

        TextColor = KnowsImpostor() ? TaskCompleteColor : NonCompleteColor;
        int KnowTasksCount = taskState.AllTasksCount > TaskTrigger ? TaskTrigger : taskState.AllTasksCount;

        text = Utils.ColorString(TextColor, $"({taskState.CompletedTasksCount}/{KnowTasksCount})");
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Rabbit : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Rabbit),
            player => new Rabbit(player),
            CustomRoles.Rabbit,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1600,
            SetupOptionItem,
            "ラビット",
            "#88d2ff"
        );
    public Rabbit(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => IsFinish(player) ? HasTask.ForRecompute : HasTask.True
    )
    {
        TaskTrigger = OptionTaskTrigger.GetInt();
        NumLongTasks = OptionNumLongTasks.GetInt();
        NumShortTasks = OptionNumShortTasks.GetInt();

        if (Main.NormalOptions.NumLongTasks < NumLongTasks)
        {
            NumLongTasks = Main.NormalOptions.NumLongTasks;
        }
        if (Main.NormalOptions.NumShortTasks < NumShortTasks)
        {
            NumShortTasks = Main.NormalOptions.NumShortTasks;
        }
        taskFinish = new();
    }

    private static OptionItem OptionTaskTrigger;
    private static OptionItem OptionNumLongTasks;
    private static OptionItem OptionNumShortTasks;
    enum OptionName
    {
        RabbitRedistributionLongTasks,
        RabbitRedistributionShortTasks
    }
    private static int TaskTrigger;
    private static int NumLongTasks;
    private static int NumShortTasks;
    private static List<PlayerControl> taskFinish = new();
    public static (bool, int, int) TaskData => (false, NumLongTasks, NumShortTasks);

    private static void SetupOptionItem()
    {
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 10, GeneralOption.TaskTrigger, new(0, 20, 1), 10, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionNumLongTasks = IntegerOptionItem.Create(RoleInfo, 11, OptionName.RabbitRedistributionLongTasks, new(0, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionNumShortTasks = IntegerOptionItem.Create(RoleInfo, 12, OptionName.RabbitRedistributionShortTasks, new(0, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
    }

    string showArrow = string.Empty;
    public static bool IsFinish(PlayerControl pc) => taskFinish.Contains(pc);
    public override void Add()
    {
        showArrow = string.Empty;
    }

    public override bool OnCompleteTask()
    {
        if (!Player.IsAlive()) return true;

        if (!IsFinish(Player)) //一巡目タスク未コンプ
        {
            if (!(MyTaskState.CompletedTasksCount >= TaskTrigger || IsTaskFinished))
            {
                return true;
            }
            if (IsTaskFinished) taskFinish.Add(Player);
        }

        var Impostors = Main.AllAlivePlayerControls.Where(pc=>pc.Is(CustomRoleTypes.Impostor)).ToArray();
        if (Impostors.Length == 0) return true;
        var target = Impostors[IRandom.Instance.Next(Impostors.Length)];

        //対象の方角ベクトルを取る
        var dir = target.transform.position - Player.transform.position;
        int index;
        if (dir.magnitude < 2)
        {
            //近い時はドット表示
            index = 8;
        }
        else
        {
            //-22.5～22.5度を0とするindexに変換
            // 下が0度、左側が+180まで右側が-180まで
            // 180度足すことで上が0度の時計回り
            // 45度単位のindexにするため45/2を加算
            var angle = Vector3.SignedAngle(Vector3.down, dir, Vector3.back) + 180 + 22.5;
            index = ((int)(angle / 45)) % 8;
        }
        showArrow = Arrows[index];
        Logger.Info($"{Player.GetNameWithRole()} target:{target.GetNameWithRole()}[{Arrows[index]}]", "Rabbit");

        _ = new LateTask(() =>
        {
            showArrow = string.Empty;
            Utils.NotifyRoles(SpecifySeer: Player);
        }, 5f, "Rabbit showArrow Empty");

        if (IsTaskFinished) //タスク全完了時にリセット
        {
            MyTaskState.AllTasksCount += NumLongTasks + NumShortTasks;
            Player.Data.RpcSetTasks(Array.Empty<byte>()); //タスクを再配布
            Player.SyncSettings();
        }
        return true;
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || isForMeeting) return string.Empty;

        return showArrow.Length == 0 ? string.Empty : showArrow.Color(RoleInfo.RoleColor);
    }

    static readonly string[] Arrows = {
            "↑",
            "↗",
            "→",
            "↘",
            "↓",
            "↙",
            "←",
            "↖",
            "・"
        };
}

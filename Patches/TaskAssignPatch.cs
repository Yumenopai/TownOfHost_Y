using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.AddOns.Crewmate;
using TownOfHostY.Roles.Crewmate;
using System.Linq;
using TownOfHostY.Roles.Neutral;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.AddTasksFromList))]
    class AddTasksFromListPatch
    {
        public static void Prefix(ShipStatus __instance,
            [HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            if (!Options.DisableTasks.GetBool()) return;
            List<NormalPlayerTask> disabledTasks = new();
            for (var i = 0; i < unusedTasks.Count; i++)
            {
                var task = unusedTasks[i];
                if (task.TaskType == TaskTypes.SwipeCard && Options.DisableSwipeCard.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.SubmitScan && Options.DisableSubmitScan.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.UnlockSafe && Options.DisableUnlockSafe.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.UploadData && Options.DisableUploadData.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.StartReactor && Options.DisableStartReactor.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.ResetBreakers && Options.DisableResetBreaker.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.RewindTapes && Options.DisableRewindTapes.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.VentCleaning && Options.DisableVentCleaning.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.BuildSandcastle && Options.DisableBuildSandcastle.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.TestFrisbee && Options.DisableTestFrisbee.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.WaterPlants && Options.DisableWaterPlants.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.CatchFish && Options.DisableCatchFish.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.HelpCritter && Options.DisableHelpCritter.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.TuneRadio && Options.DisableTuneRadio.GetBool()) disabledTasks.Add(task);
                if (task.TaskType == TaskTypes.AssembleArtifact && Options.DisableAssembleArtifact.GetBool()) disabledTasks.Add(task);
            }
            foreach (var task in disabledTasks)
            {               
                unusedTasks.Remove(task);
            }
        }
    }

    [HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.RpcSetTasks))]
    class RpcSetTasksPatch
    {
       
        public static bool SkipPatch = false;
        public static Il2CppSystem.Collections.Generic.Dictionary<byte, Il2CppStructArray<byte>> taskIds = new();

        public static void Prefix(NetworkedPlayerInfo __instance,
        [HarmonyArgument(0)] ref Il2CppStructArray<byte> taskTypeIds)
        {
           
            if (SkipPatch) return;


            if (Main.RealOptionsData == null)
            {               
                return;
            }

            var pc = Utils.GetPlayerById(__instance.PlayerId);
            CustomRoles? RoleNullable = pc?.GetCustomRole();
            if (RoleNullable == null) return;
            CustomRoles role = RoleNullable.Value;

            // デフォルトのタスク数
            bool hasCommonTasks = true;
            int NumLongTasks = Main.NormalOptions.NumLongTasks;
            int NumShortTasks = Main.NormalOptions.NumShortTasks;

            if (Options.OverrideTasksData.AllData.TryGetValue(role, out var data) && data.doOverride.GetBool())
            {
                hasCommonTasks = data.assignCommonTasks.GetBool();
                NumLongTasks = data.numLongTasks.GetInt();
                NumShortTasks = data.numShortTasks.GetInt();
            }

            if (pc.Is(CustomRoles.VentManager))
                (hasCommonTasks, NumLongTasks, NumShortTasks) = VentManager.TaskData;
            if (pc.Is(CustomRoles.FoxSpirit))
                (hasCommonTasks, NumLongTasks, NumShortTasks) = FoxSpirit.TaskData;
            if (pc.Is(CustomRoles.Workhorse))
                (hasCommonTasks, NumLongTasks, NumShortTasks) = Workhorse.TaskData;
            if (pc.Is(CustomRoles.Rabbit) && Rabbit.IsFinish(pc))
                (hasCommonTasks, NumLongTasks, NumShortTasks) = Rabbit.TaskData;

            if (taskTypeIds.Count == 0) hasCommonTasks = false;
            if (!hasCommonTasks && NumLongTasks == 0 && NumShortTasks == 0) NumShortTasks = 1;
                       
            if (!pc.Is(CustomRoles.VentManager) && !pc.Is(CustomRoles.FoxSpirit)
                && hasCommonTasks
                && NumLongTasks == Main.NormalOptions.NumLongTasks
                && NumShortTasks == Main.NormalOptions.NumShortTasks)
            {
                taskIds[__instance.PlayerId] = taskTypeIds;
                return;
            }

            Il2CppSystem.Collections.Generic.List<byte> TasksList = new();
            foreach (var num in taskTypeIds)
                TasksList.Add(num);

            int defaultCommonTasksNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumCommonTasks);
            if (hasCommonTasks) TasksList.RemoveRange(defaultCommonTasksNum, TasksList.Count - defaultCommonTasksNum);
            else TasksList.Clear();

            Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new();
            int start2 = 0;
            int start3 = 0;

            Il2CppSystem.Collections.Generic.List<NormalPlayerTask> LongTasks = new();
            foreach (var task in ShipStatus.Instance.LongTasks)
                LongTasks.Add(task);
            Shuffle<NormalPlayerTask>(LongTasks);

            Il2CppSystem.Collections.Generic.List<NormalPlayerTask> ShortTasks = new();
            foreach (var task in ShipStatus.Instance.ShortTasks)
                ShortTasks.Add(task);
            Shuffle<NormalPlayerTask>(ShortTasks);

           
            if (pc.Is(CustomRoles.VentManager) || pc.Is(CustomRoles.FoxSpirit))
            {
                TasksList.Clear();
                ShortTasks.Clear();

                var ventTask = ShipStatus.Instance.ShortTasks.FirstOrDefault(task => task.TaskType == TaskTypes.VentCleaning);
                if (ventTask != null)
                    ShortTasks.Add(ventTask);              
            }

            ShipStatus.Instance.AddTasksFromList(
                ref start2,
                NumLongTasks,
                TasksList,
                usedTaskTypes,
                LongTasks
            );
            ShipStatus.Instance.AddTasksFromList(
                ref start3,
                NumShortTasks,
                TasksList,
                usedTaskTypes,
                ShortTasks
            );

            taskTypeIds = new Il2CppStructArray<byte>(TasksList.Count);
            for (int i = 0; i < TasksList.Count; i++)
            {
                taskTypeIds[i] = TasksList[i];
            }

            taskIds[__instance.PlayerId] = taskTypeIds;
        }

        public static void Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                T obj = list[i];
                int rand = IRandom.Instance.Next(i, list.Count);
                list[i] = list[rand];
                list[rand] = obj;
            }
        }
    }
}
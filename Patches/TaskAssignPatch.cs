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
        // Use ref so we can replace the list instance instead of mutating it in-place,
        // which can cause the original method to crash when it iterates over the list.
        public static void Prefix(ShipStatus __instance,
            [HarmonyArgument(4)] ref Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.DisableTasks.GetBool()) return;

            if (unusedTasks == null)
            {
                Logger.Warn("unusedTasks が null です - タスク無効化をスキップします", "AddTasksFromListPatch");
                return;
            }

            Logger.Info($"AddTasksFromList called. incoming unusedTasks.Count={unusedTasks.Count}", "AddTasksFromListPatch");

            // Build a new Il2Cpp list containing only the tasks we want to keep.
            var filtered = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
            var removedTypes = new List<TaskTypes>();
            for (var i = 0; i < unusedTasks.Count; i++)
            {
                var task = unusedTasks[i];
                if (task == null) continue;

                bool isDisabled = false;
                if (task.TaskType == TaskTypes.SwipeCard && Options.DisableSwipeCard.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.SubmitScan && Options.DisableSubmitScan.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.UnlockSafe && Options.DisableUnlockSafe.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.UploadData && Options.DisableUploadData.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.StartReactor && Options.DisableStartReactor.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.ResetBreakers && Options.DisableResetBreaker.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.RewindTapes && Options.DisableRewindTapes.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.VentCleaning && Options.DisableVentCleaning.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.BuildSandcastle && Options.DisableBuildSandcastle.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.TestFrisbee && Options.DisableTestFrisbee.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.WaterPlants && Options.DisableWaterPlants.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.CatchFish && Options.DisableCatchFish.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.HelpCritter && Options.DisableHelpCritter.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.TuneRadio && Options.DisableTuneRadio.GetBool()) isDisabled = true;
                if (task.TaskType == TaskTypes.AssembleArtifact && Options.DisableAssembleArtifact.GetBool()) isDisabled = true;

                if (isDisabled)
                {
                    removedTypes.Add(task.TaskType);
                }

                if (!isDisabled)
                {
                    filtered.Add(task);
                }
            }

            Logger.Info($"AddTasksFromList: kept={filtered.Count}, removed={removedTypes.Count}", "AddTasksFromListPatch");
            if (removedTypes.Count > 0)
            {
                Logger.Info($"Removed task types: {string.Join(",", removedTypes)}", "AddTasksFromListPatch");
            }

            // Replace the original list reference with the filtered one so the original method
            // iterates over a safe list that doesn't contain disabled tasks.
            unusedTasks = filtered;
        }
    }

    [HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.RpcSetTasks))]
    class RpcSetTasksPatch
    {
        // ★ TOH-K移植: StandardIntroでのタスク再配布のためにtaskIdsをキャッシュする
        public static Il2CppSystem.Collections.Generic.Dictionary<byte, Il2CppStructArray<byte>> taskIds = new();

        public static void Prefix(NetworkedPlayerInfo __instance,
            [HarmonyArgument(0)] ref Il2CppStructArray<byte> taskTypeIds)
        {
            try
            {
                // ゲーム状態チェック
                if (Main.RealOptionsData == null)
                {
                    Logger.Error("CRITICAL: RealOptionsData が null です - ゲーム状態が破損しています", "RpcSetTasksPatch");
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                    return;
                }

                if (__instance == null)
                {
                    Logger.Error("NetworkedPlayerInfo が null です", "RpcSetTasksPatch");
                    return;
                }

                var pc = Utils.GetPlayerById(__instance.PlayerId);
                if (pc == null)
                {
                    Logger.Warn($"PlayerId {__instance.PlayerId} のプレイヤーが見つかりません", "RpcSetTasksPatch");
                    return;
                }

                Logger.Info($"RpcSetTasks called for player {pc.GetNameWithRole()} (id={pc.PlayerId}) incoming taskTypeIds.Count={(taskTypeIds==null?0:taskTypeIds.Count)}", "RpcSetTasksPatch");

                CustomRoles? RoleNullable = pc.GetCustomRole();
                if (RoleNullable == null)
                {
                    Logger.Warn($"{pc.name} のカスタム役職が null です", "RpcSetTasksPatch");
                    return;
                }

                CustomRoles role = RoleNullable.Value;

                // デフォルトのタスク数
                bool hasCommonTasks = true;
                int NumLongTasks = Main.NormalOptions != null ? Main.NormalOptions.NumLongTasks : 0;
                int NumShortTasks = Main.NormalOptions != null ? Main.NormalOptions.NumShortTasks : 0;

                // タスク数オーバーライド
                if (Options.OverrideTasksData.AllData.TryGetValue(role, out var data) && data.doOverride.GetBool())
                {
                    hasCommonTasks = data.assignCommonTasks.GetBool();
                    NumLongTasks = data.numLongTasks.GetInt();
                    NumShortTasks = data.numShortTasks.GetInt();
                    Logger.Info($"OverrideTasks for {pc.name}: hasCommon={hasCommonTasks} long={NumLongTasks} short={NumShortTasks}", "RpcSetTasksPatch");
                }

                // 固有タスク処理
                if (pc.Is(CustomRoles.VentManager))
                    (hasCommonTasks, NumLongTasks, NumShortTasks) = VentManager.TaskData;
                if (pc.Is(CustomRoles.FoxSpirit))
                    (hasCommonTasks, NumLongTasks, NumShortTasks) = FoxSpirit.TaskData;
                if (pc.Is(CustomRoles.Workhorse))
                    (hasCommonTasks, NumLongTasks, NumShortTasks) = Workhorse.TaskData;
                if (pc.Is(CustomRoles.Rabbit) && Rabbit.IsFinish(pc))
                    (hasCommonTasks, NumLongTasks, NumShortTasks) = Rabbit.TaskData;

                if (taskTypeIds == null)
                {
                    Logger.Warn($"{pc.name} の taskTypeIds が null です", "RpcSetTasksPatch");
                    return;
                }

                if (taskTypeIds.Count == 0) hasCommonTasks = false;
                if (!hasCommonTasks && NumLongTasks == 0 && NumShortTasks == 0) NumShortTasks = 1;

                // 割り当て可能なタスクリスト
                Il2CppSystem.Collections.Generic.List<byte> TasksList = new();
                foreach (var num in taskTypeIds) TasksList.Add(num);

                Logger.Info($"Initial TasksList count={TasksList.Count} hasCommonTasks={hasCommonTasks}", "RpcSetTasksPatch");

                // 共通タスク処理
                int defaultCommonTasksNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumCommonTasks);
                if (hasCommonTasks)
                {
                    int removeStart = defaultCommonTasksNum;
                    int removeCount = TasksList.Count - defaultCommonTasksNum;
                    if (removeStart >= 0 && removeStart < TasksList.Count && removeCount > 0)
                        TasksList.RemoveRange(removeStart, removeCount);
                    Logger.Info($"After trimming common tasks TasksList count={TasksList.Count}", "RpcSetTasksPatch");
                }
                else
                {
                    TasksList.Clear();
                    Logger.Info($"Cleared common tasks for {pc.name}", "RpcSetTasksPatch");
                }

                // ShipStatus チェック
                if (ShipStatus.Instance == null)
                {
                    Logger.Error("ShipStatus が null です - タスク割り当てをスキップします", "RpcSetTasksPatch");
                    return;
                }

                // タスク割り当て用のリストを作成
                Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new();
                int start2 = 0, start3 = 0;

                // LongTasks のコピーと null チェック
                var LongTasks = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
                if (ShipStatus.Instance.LongTasks != null)
                {
                    foreach (var task in ShipStatus.Instance.LongTasks)
                    {
                        if (task != null) LongTasks.Add(task);
                    }
                    Shuffle(LongTasks);
                }

                // ShortTasks のコピーと null チェック
                var ShortTasks = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
                if (ShipStatus.Instance.ShortTasks != null)
                {
                    foreach (var task in ShipStatus.Instance.ShortTasks)
                    {
                        if (task != null) ShortTasks.Add(task);
                    }
                    Shuffle(ShortTasks);
                }

                Logger.Info($"LongTasks={LongTasks.Count}, ShortTasks={ShortTasks.Count}", "RpcSetTasksPatch");

                // If no common tasks are available (TasksList empty), but there are Long/Short tasks
                // populate TasksList from available tasks so assignment can proceed without nulls.
                if (TasksList.Count == 0 && (LongTasks.Count > 0 || ShortTasks.Count > 0))
                {
                    foreach (var t in LongTasks)
                    {
                        if (t != null) TasksList.Add((byte)t.TaskType);
                    }
                    foreach (var t in ShortTasks)
                    {
                        if (t != null) TasksList.Add((byte)t.TaskType);
                    }

                    // Remove duplicates while preserving order
                    var seen = new HashSet<byte>();
                    var dedup = new Il2CppSystem.Collections.Generic.List<byte>();
                    foreach (var b in TasksList)
                    {
                        if (seen.Add(b)) dedup.Add(b);
                    }
                    TasksList = dedup;
                    Logger.Info($"Repopulated TasksList from available tasks count={TasksList.Count}", "RpcSetTasksPatch");
                }

                // VentManager / FoxSpirit 固有処理
                if (pc.Is(CustomRoles.VentManager) || pc.Is(CustomRoles.FoxSpirit))
                {
                    TasksList.Clear();
                    ShortTasks.Clear();

                    if (ShipStatus.Instance.ShortTasks != null)
                    {
                        var ventTask = ShipStatus.Instance.ShortTasks.FirstOrDefault(t => t != null && t.TaskType == TaskTypes.VentCleaning);
                        if (ventTask != null) ShortTasks.Add(ventTask);
                    }

                    // Ensure TasksList contains vent task type if needed
                    if (ShortTasks.Count > 0)
                    {
                        bool hasVent = false;
                        for (int _i = 0; _i < TasksList.Count; _i++)
                        {
                            if (TasksList[_i] == (byte)TaskTypes.VentCleaning)
                            {
                                hasVent = true;
                                break;
                            }
                        }

                        if (!hasVent) TasksList.Add((byte)TaskTypes.VentCleaning);
                    }
                    Logger.Info($"VentManager/FoxSpirit special handling TasksList count={TasksList.Count}", "RpcSetTasksPatch");
                }

                // タスク割り当て（null チェック付き）
                if ((LongTasks.Count > 0 || ShortTasks.Count > 0) && TasksList.Count > 0)
                {
                    try
                    {
                        ShipStatus.Instance.AddTasksFromList(ref start2, NumLongTasks, TasksList, usedTaskTypes, LongTasks);
                        ShipStatus.Instance.AddTasksFromList(ref start3, NumShortTasks, TasksList, usedTaskTypes, ShortTasks);
                        Logger.Info($"Performed AddTasksFromList for {pc.name}", "RpcSetTasksPatch");
                    }
                    catch (Il2CppInterop.Runtime.Il2CppException ilEx)
                    {
                        // Il2Cpp exceptions may wrap managed NREs; log and continue without crashing the trampoline
                        Logger.Error($"AddTasksFromList threw Il2CppException: {ilEx.Message}", "RpcSetTasksPatch");
                        Logger.Exception(ilEx, "RpcSetTasksPatch");
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Error($"AddTasksFromList failed: {ex.Message}", "RpcSetTasksPatch");
                        Logger.Exception(ex, "RpcSetTasksPatch");
                    }
                }
                else if (TasksList.Count == 0)
                {
                    Logger.Info($"{pc.name} に割り当て可能なタスクがありませんでした。", "RpcSetTasksPatch");
                }

                // 配列に変換
                taskTypeIds = new Il2CppStructArray<byte>(TasksList.Count);
                for (int i = 0; i < TasksList.Count; i++)
                    taskTypeIds[i] = TasksList[i];

                Logger.Info($"{pc.name} にタスク {TasksList.Count} 個を割り当てました (長: {NumLongTasks}, 短: {NumShortTasks})", "RpcSetTasksPatch");

                // ★ TOH-K移植: StandardモードかつIsroleAssigned前（イントロ前）ならtaskIdsにキャッシュ
                //   StandardIntroHelper.ShowIntroForVanilla内でタスク再配布に使用する
                if (Options.CurrentGameMode == CustomGameMode.Standard && !Main.IsroleAssigned)
                {
                    taskIds[__instance.PlayerId] = taskTypeIds;
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"RpcSetTasksPatch で例外が発生しました: {ex.Message}", "RpcSetTasksPatch");
                Logger.Exception(ex, "RpcSetTasksPatch");
                // ゲーム終了ではなく、元の処理を続行させる
                return;
            }
        }

        public static void Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list)
        {
            if (list == null || list.Count == 0) return;

            for (int i = 0; i < list.Count - 1; i++)
            {
                T obj = list[i];
                int rand = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[rand];
                list[rand] = obj;
            }
        }
    }
}
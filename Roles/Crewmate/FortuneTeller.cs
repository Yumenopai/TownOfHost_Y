using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate
{
    public static class FortuneTeller
    {
        private static readonly int Id = 36400;
        public static List<byte> playerIdList = new();

        public static OptionItem NumOfForecast;
        public static OptionItem ForecastTaskTrigger;
        public static OptionItem CanForecastNoDeadBody;
        public static OptionItem ConfirmCamp;
        public static OptionItem KillerOnly;

        public static Dictionary<byte, PlayerControl> Target = new();
        public static Dictionary<byte, Dictionary<byte, PlayerControl>> TargetResult = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
            NumOfForecast = IntegerOptionItem.Create(Id + 10, "FortuneTellerNumOfForecast", new(1, 99, 1), 2, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.FortuneTeller])
                .SetValueFormat(OptionFormat.Times);
            ForecastTaskTrigger = IntegerOptionItem.Create(Id + 11, "FortuneTellerForecastTaskTrigger", new(0, 99, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.FortuneTeller])
                .SetValueFormat(OptionFormat.Pieces);
            CanForecastNoDeadBody = BooleanOptionItem.Create(Id + 12, "FortuneTellerCanForecastNoDeadBody", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.FortuneTeller]);
            ConfirmCamp = BooleanOptionItem.Create(Id + 13, "FortuneTellerConfirmCamp", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.FortuneTeller]);
            KillerOnly = BooleanOptionItem.Create(Id + 14, "FortuneTellerKillerOnly", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.FortuneTeller]);
        }
        public static void Init()
        {
            playerIdList = new();
            Target = new();
            TargetResult = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
        public static void VoteForecastTarget(this PlayerControl player, byte targetId)
        {
            if (!CanForecastNoDeadBody.GetBool() &&
                GameData.Instance.AllPlayers.ToArray().Where(x => x.IsDead).Count() <= 0) //死体無し
            {
                Logger.Info($"VoteForecastTarget NotForecast NoDeadBody player: {player.name}, targetId: {targetId}", "FortuneTeller");
                return;
            }
            var completedTasks = player.GetPlayerTaskState().CompletedTasksCount;
            if (completedTasks < ForecastTaskTrigger.GetInt()) //占い可能タスク数
            {
                Logger.Info($"VoteForecastTarget NotForecast LessTasks player: {player.name}, targetId: {targetId}, task: {completedTasks}/{ForecastTaskTrigger.GetInt()}", "FortuneTeller");
                return;
            }

            player.SetForecastTarget(targetId);
        }
        private static void SetForecastTarget(this PlayerControl player, byte targetId)
        {
            var target = Utils.GetPlayerById(targetId);
            if (target == null || target.Data.IsDead || target.Data.Disconnected) return;
            if (player.HasForecastResult(target.PlayerId)) return;  //既に占い結果があるときはターゲットにならない

            Target[player.PlayerId] = target;
            Logger.Info($"SetForecastTarget player: {player.name}, target: {target.name}", "FortuneTeller");
        }
        private static bool HasForecastTarget(this PlayerControl player)
        {
            if (!Target.TryGetValue(player.PlayerId, out var target)) return false;
            return target != null;
        }
        public static void ConfirmForecastResult()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null) continue;
                if (player.Is(CustomRoles.FortuneTeller) &&
                    !player.Data.IsDead && !player.Data.Disconnected && player.HasForecastTarget())
                    player.SetForecastResult();
            }
        }
        private static void SetForecastResult(this PlayerControl player)
        {
            if (!Target.TryGetValue(player.PlayerId, out var target))
            {
                Logger.Info($"SetForecastResult NotSet NotHasForecastTarget player: {player.name}", "FortuneTeller");
                return;
            }
            Target.Remove(player.PlayerId);

            if (target == null || target.Data.IsDead || target.Data.Disconnected)
            {
                Logger.Info($"SetForecastResult NotSet TargetNotValid player: {player.name}, target: {target?.name} dead: {target?.Data.IsDead}, disconnected: {target?.Data.Disconnected}", "FortuneTeller");
                return;
            }

            if (!TargetResult.TryGetValue(player.PlayerId, out var resultTarget))
            {
                resultTarget = new();
                TargetResult[player.PlayerId] = resultTarget;
            }
            if (resultTarget.Count >= NumOfForecast.GetInt())
            {
                Logger.Info($"SetForecastResult NotSet ForecastCountOver player: {player.name}, target: {target.name} forecastCount: {resultTarget.Count}, canCount: {NumOfForecast.GetInt()}", "FortuneTeller");
                return;
            }

            resultTarget[target.PlayerId] = target;
            Logger.Info($"SetForecastResult SetTarget player: {player.name}, target: {target.name}", "FortuneTeller");
        }
        public static bool HasForecastResult(this PlayerControl player, byte targetId)
        {
            if (!TargetResult.TryGetValue(player.PlayerId, out var resultTarget)) return false;
            return resultTarget.ContainsKey(targetId);
        }
        public static bool HasForecastResult(this PlayerControl player)
        {
            if (!TargetResult.TryGetValue(player.PlayerId, out var resultTarget)) return false;
            return resultTarget.Count > 0;
        }
        public static string GetCountCanForecast(byte playerId)
        {
            var target = Utils.GetPlayerById(playerId);
            if ((target?.GetPlayerTaskState()?.CompletedTasksCount ?? -1) < ForecastTaskTrigger.GetInt()) return "";

            var count = NumOfForecast.GetInt();
            if (TargetResult.TryGetValue(playerId, out var resultTarget))
                count -= resultTarget.Count;

            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.FortuneTeller), $"[{count}]");
        }
        public static string TargetMark(PlayerControl seer, PlayerControl target)
        {
            if (seer == null || target == null) return "";
            if (!seer.Is(CustomRoles.FortuneTeller)) return ""; //占い師以外処理しない
            if (!seer.HasForecastResult(target.PlayerId)) return "";

            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.FortuneTeller), "★");
        }
        public static bool KnowTargetRoleColor(PlayerControl seer, PlayerControl target, bool isMeeting)
                    => seer.Is(CustomRoles.FortuneTeller) && seer.HasForecastResult(target.PlayerId) &&
                       (target.GetCustomRole().IsImpostor() || target.Is(CustomRoles.Egoist) || target.Is(CustomRoles.Jackal)) &&
                       isMeeting;
        public static bool IsShowTargetRole(PlayerControl seer, PlayerControl target)
        {
            if (!seer.HasForecastResult(target.PlayerId)) return false;
            if (ConfirmCamp.GetBool()) return false;
            if (KillerOnly.GetBool() &&
                !(target.GetCustomRole().IsImpostor() || Utils.IsNeutralKiller(target))) return false;
            return true;
        }
        public static bool IsShowTargetCamp(PlayerControl seer, PlayerControl target, out bool onlyKiller)
        {
            onlyKiller = KillerOnly.GetBool();
            if (!seer.HasForecastResult(target.PlayerId)) return false;
            return !IsShowTargetRole(seer, target);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Crewmate
{
    public static class CandleLighter
    {
        static readonly int Id = 36200;
        static List<byte> playerIdList = new();
        private static OptionItem OpStartVision;
        private static OptionItem OpEndVisionTime;
        private static OptionItem OpTimeMoveEvenDuringMeeting;

        private static float StartVision;
        private static int EndVisionTime;
        private static bool TimeMoveEvenDuringMeeting;

        private static Dictionary<byte, float> ElapsedTime= new();
        private static float UpdateTime;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.CandleLighter);
            OpStartVision = FloatOptionItem.Create(Id + 10, "CandleLighterStartVision", new(0.5f, 5f, 0.1f), 2.0f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.CandleLighter])
                .SetValueFormat(OptionFormat.Multiplier);
            OpEndVisionTime = IntegerOptionItem.Create(Id + 11, "CandleLighterEndVisionTime", new(60, 1200, 60), 480, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.CandleLighter])
                .SetValueFormat(OptionFormat.Seconds);
            OpTimeMoveEvenDuringMeeting = BooleanOptionItem.Create(Id + 12, "TimeMoveMeeting", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.CandleLighter]);
        }
        public static void Init()
        {
            playerIdList = new();
            ElapsedTime = new();

            StartVision = OpStartVision.GetFloat();
            EndVisionTime = OpEndVisionTime.GetInt();
            TimeMoveEvenDuringMeeting = OpTimeMoveEvenDuringMeeting.GetBool();
            UpdateTime = 1.0f;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            ElapsedTime.Add(playerId, EndVisionTime);
        }
        public static bool IsEnable => playerIdList.Count > 0;

        public static void ApplyGameOptions(IGameOptions opt,PlayerControl pc)
        {
            float Vision = StartVision * (ElapsedTime[pc.PlayerId] / EndVisionTime);
            //Vision = Mathf.Clamp(Vision, 0.01f, 5f);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Vision);
            if (Utils.IsActive(SystemTypes.Electrical))
            {
                opt.SetFloat(FloatOptionNames.CrewLightMod, Vision * 5);
            }
        }

        public static void TaskFinish(PlayerControl player, int CompletedTasksCount, int AllTasksCount)
        {
            if (!player.Data.IsDead
                && player.Is(CustomRoles.CandleLighter)
                && ((CompletedTasksCount + 1) >= AllTasksCount))
            {
                ElapsedTime[player.PlayerId] = EndVisionTime;
            }
        }

        public static void FixedUpdate(PlayerControl player)
        {
            if (!player.Is(CustomRoles.CandleLighter)) return;
            if (!GameStates.IsInTask && !TimeMoveEvenDuringMeeting) return;

            UpdateTime -= Time.fixedDeltaTime;
            if (UpdateTime < 0) UpdateTime = 1.0f;

            if (ElapsedTime[player.PlayerId] > 0f)
            {
                ElapsedTime[player.PlayerId] -= Time.fixedDeltaTime; //時間をカウント

                if (UpdateTime == 1.0f)  player.SyncSettings();
            }
        }
    }
}
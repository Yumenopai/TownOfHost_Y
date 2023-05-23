using System.Collections.Generic;
using Hazel;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor
{
    public static class Greedier
    {
        private static readonly int Id = 3300;
        public static List<byte> playerIdList = new();

        private static OptionItem DefaultKillCooldown;
        private static OptionItem OddKillCooldown;
        private static OptionItem EvenKillCooldown;

        private static float DefaultKillCool;
        public static Dictionary<byte, bool> IsOdd = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Greedier);
            DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "DefaultKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Greedier])
                .SetValueFormat(OptionFormat.Seconds);
            OddKillCooldown = FloatOptionItem.Create(Id + 11, "OddKillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Greedier])
                .SetValueFormat(OptionFormat.Seconds);
            EvenKillCooldown = FloatOptionItem.Create(Id + 12, "EvenKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Greedier])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            playerIdList = new();
            IsOdd = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            IsOdd.Add(playerId, false);

            DefaultKillCool = DefaultKillCooldown.GetFloat();
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void SetKillCooldown(byte id)
            => Main.AllPlayerKillCooldown[id] = DefaultKillCool;
        public static void AfterMeetingTasks()
        {
            foreach (var killerId in playerIdList)
            {
                IsOdd[killerId] = true;
                Main.AllPlayerKillCooldown[killerId] = DefaultKillCool;
            }
        }

        public static void OnCheckMurder(PlayerControl killer)
        {
            switch (IsOdd[killer.PlayerId])
            {
                case true:
                    Logger.Info($"{killer?.Data?.PlayerName}:奇数回目のキル", "Greedier");
                    Main.AllPlayerKillCooldown[killer.PlayerId] = EvenKillCooldown.GetFloat();
                    IsOdd[killer.PlayerId] = false;
                    break;
                case false:
                    Logger.Info($"{killer?.Data?.PlayerName}:偶数回目のキル", "Greedier");
                    Main.AllPlayerKillCooldown[killer.PlayerId] = OddKillCooldown.GetFloat();
                    IsOdd[killer.PlayerId] = true;
                    break;
            }
            killer.SyncSettings();//キルクール処理を同期
        }
    }
}
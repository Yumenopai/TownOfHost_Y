using System.Collections.Generic;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor
{
    public static class Ambitioner
    {
        private static readonly int Id = 3400;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        private static OptionItem KillCoolDecreaseRate;

        public static Dictionary<byte, int> KillCount = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Ambitioner);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Ambitioner])
                .SetValueFormat(OptionFormat.Seconds);
            KillCoolDecreaseRate = FloatOptionItem.Create(Id + 11, "KillCoolDecreaseRate", new(0.1f, 1f, 0.1f), 0.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Ambitioner])
                .SetValueFormat(OptionFormat.Multiplier);
        }
        public static void Init()
        {
            playerIdList = new();
            KillCount = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            KillCount.TryAdd(playerId, 0);
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void SetKillCooldown(byte id)
        {
            KillCount[id] = 0;
            Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        }
        public static void OnCheckMurder(PlayerControl killer)
        {
            Logger.Info($"{killer?.Data?.PlayerName}:キルクール減少", "Ambitioner");
            KillCount[killer.PlayerId]++;
            Main.AllPlayerKillCooldown[killer.PlayerId] *= (float)System.Math.Pow(KillCoolDecreaseRate.GetFloat(), KillCount[killer.PlayerId]);
            killer.SyncSettings();//キルクール処理を同期
        }
    }
}
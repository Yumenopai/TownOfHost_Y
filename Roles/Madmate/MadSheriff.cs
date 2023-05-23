using System.Collections.Generic;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Madmate
{
    public static class MadSheriff
    {
        private static readonly int Id = 10600;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        private static OptionItem MisfireKillsTarget;
        public static OptionItem MadSheriffCanVent;
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.MadmateRoles, CustomRoles.MadSheriff);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.MadmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.MadSheriff])
                .SetValueFormat(OptionFormat.Seconds);
            MisfireKillsTarget = BooleanOptionItem.Create(Id + 11, "SheriffMisfireKillsTarget", false, TabGroup.MadmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.MadSheriff]);
            MadSheriffCanVent = BooleanOptionItem.Create(Id + 12, "CanVent", false, TabGroup.MadmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.MadSheriff]);
            SetUpAddOnOptions(Id + 20, CustomRoles.MadSheriff, TabGroup.MadmateRoles);
        }
        public static void Init()
        {
            playerIdList = new();
            CurrentKillCooldown = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CurrentKillCooldown[id];
        public static bool CanUseKillButton(PlayerControl player)
        {
            if (player.Data.IsDead) return false;

            return true;
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
            killer.RpcMurderPlayer(killer);
            if (MisfireKillsTarget.GetBool())
               killer.RpcMurderPlayer(target);
            return false;
        }
    }
}
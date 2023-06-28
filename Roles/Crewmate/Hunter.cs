using System.Collections.Generic;
using Hazel;
using UnityEngine;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Crewmate
{
    public static class Hunter
    {
        private static readonly int Id = 35100;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        private static OptionItem ShotLimitOpt;
        private static OptionItem CanKillAllAlive;
        private static OptionItem KnowTargetIsImpostor;
        private static OptionItem KnowTargetMadIsImpostor;

        public static Dictionary<byte, float> ShotLimit = new();
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static Dictionary<byte, int> isImpostor = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Hunter);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Hunter])
                .SetValueFormat(OptionFormat.Seconds);
            ShotLimitOpt = IntegerOptionItem.Create(Id + 12, "SheriffShotLimit", new(1, 15, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Hunter])
                .SetValueFormat(OptionFormat.Times);
            CanKillAllAlive = BooleanOptionItem.Create(Id + 13, "SheriffCanKillAllAlive", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Hunter]);
            KnowTargetIsImpostor = BooleanOptionItem.Create(Id + 14, "HunterKnowTargetIsImpostor", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Hunter]);
            KnowTargetMadIsImpostor = BooleanOptionItem.Create(Id + 15, "HunterKnowTargetMadIsImpostor", true, TabGroup.CrewmateRoles, false).SetParent(KnowTargetIsImpostor);
        }
        public static void Init()
        {
            playerIdList = new();
            ShotLimit = new();
            CurrentKillCooldown = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            isImpostor.TryAdd(playerId,0);
            ShotLimit.TryAdd(playerId, ShotLimitOpt.GetFloat());
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "Hunter");
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void ResetIsImp()
        {
            foreach (byte HunterId in playerIdList)
            {
                isImpostor[HunterId] = 0;
            }
        }
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetHunterShotLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(ShotLimit[playerId]);
            writer.Write(isImpostor[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte HunterId = reader.ReadByte();
            float Limit = reader.ReadSingle();
            int isImp = reader.ReadInt32();
            if (ShotLimit.ContainsKey(HunterId))
                ShotLimit[HunterId] = Limit;
            else
                ShotLimit.Add(HunterId, ShotLimitOpt.GetFloat());
            isImpostor[HunterId] = isImp;
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? CurrentKillCooldown[id] : 0f;
        public static bool CanUseKillButton(byte playerId)
            => !Main.PlayerStates[playerId].IsDead
            && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
            && ShotLimit[playerId] > 0;

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            ShotLimit[killer.PlayerId]--;
            Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit[killer.PlayerId]}発", "Hunter");

            switch (target.GetCustomRole().GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    isImpostor[killer.PlayerId] = 1; break;
                case CustomRoleTypes.Madmate:
                    if (KnowTargetMadIsImpostor.GetBool()) isImpostor[killer.PlayerId] = 1;
                    else isImpostor[killer.PlayerId] = 0;
                    break;
                case CustomRoleTypes.Neutral:
                    isImpostor[killer.PlayerId] = 2; break;
                default:
                    isImpostor[killer.PlayerId] = 0; break;
            }

            SendRPC(killer.PlayerId);
            Utils.NotifyRoles(SpecifySeer: killer);
        }
        public static string GetShotLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Color.yellow : Color.white, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");

        public static string TargetMark(PlayerControl seer, PlayerControl target)
        {
            var mark = "";
            if (KnowTargetIsImpostor.GetBool() && seer.Is(CustomRoles.Hunter) && isImpostor[seer.PlayerId] == 1 && seer == target)
                mark += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Hunter), "◎");
            if (KnowTargetIsImpostor.GetBool() && seer.Is(CustomRoles.Hunter) && isImpostor[seer.PlayerId] == 2 && seer == target)
                mark += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Hunter), "▽");
            return mark;
        }

    }
}
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using UnityEngine;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class ONDiviner
    {
        private static readonly int Id = 67100;
        public static List<byte> playerIdList = new();
        public static Dictionary<byte, PlayerControl> DivinationTarget = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ONDiviner, CustomGameMode.OneNight);
        }
        public static void Init()
        {
            playerIdList = new();
            DivinationTarget = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            DivinationTarget.Add(playerId, null);

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetONDivinerDivision, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(DivinationTarget[playerId].PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            DivinationTarget[playerId].PlayerId = reader.ReadByte();
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 0.1f;
        public static bool CanUseKillButton(byte playerId)
            => playerIdList.Contains(playerId) && DivinationTarget[playerId] == null;

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            DivinationTarget[killer.PlayerId] = target;
            killer.SetKillCooldown(999f);
            killer.RpcGuardAndKill(target);
            Logger.Info($"{killer.GetNameWithRole()} : 占った", "ONDiviner");

            Utils.NotifyRoles(SpecifySeer: killer);
            SendRPC(killer.PlayerId);
            return;
        }

        public static void SetNotkillTarget()
        {
            if (!IsEnable) return;

            foreach (byte seerId in playerIdList)
            {
                if (DivinationTarget[seerId] == null)
                {
                    List<PlayerControl> targetList = new();
                    var rand = IRandom.Instance;
                    foreach (var p in Main.AllDeadPlayerControls)
                    {
                        targetList.Add(p);
                    }
                    DivinationTarget[seerId] = targetList[rand.Next(targetList.Count)];
                    Logger.Info($"{Utils.GetPlayerById(seerId).GetNameWithRole()}の死亡済占い先：{DivinationTarget[seerId].GetNameWithRole()}", "ONDiviner");
                }
            }
        }

        public static bool IsShowTargetRole(PlayerControl seer, PlayerControl target)
        {
            var IsWatch = false;
            DivinationTarget.Do(x =>
            {
                if (x.Value != null && seer.PlayerId == x.Key && target == x.Value && Utils.GetPlayerById(x.Key).IsAlive())
                    IsWatch = true;
            });
            return IsWatch;
        }
    }
}
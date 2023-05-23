using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class ONWerewolf
    {
        private static readonly int Id = 65000;
        public static List<byte> playerIdList = new();

        public static Dictionary<byte, int> ShotLimit = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ONWerewolf, CustomGameMode.OneNight);
        }
        public static void Init()
        {
            playerIdList = new();
            ShotLimit = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            ShotLimit.TryAdd(playerId, 1);
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "ONWerewolf");
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetONWerewolfShotLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(ShotLimit[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte ONWerewolfId = reader.ReadByte();
            int Limit = reader.ReadInt32();
            if (ShotLimit.ContainsKey(ONWerewolfId))
                ShotLimit[ONWerewolfId] = Limit;
            else
                ShotLimit.Add(ONWerewolfId, 1);
        }
        public static bool CanUseKillButton(byte playerId)
            => playerIdList.Contains(playerId) && ShotLimit[playerId] > 0;

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            Main.ONKillCount++;
            ShotLimit[killer.PlayerId]--;
            Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit[killer.PlayerId]}発", "ONWerewolf");
            SendRPC(killer.PlayerId);
            Main.AllPlayerKillCooldown[killer.PlayerId] = 999f;
            killer.SyncSettings();//キルクール処理を同期
        }
    }

    public static class ONBigWerewolf
    {
        private static readonly int Id = 65100;
        public static List<byte> playerIdList = new();

        public static Dictionary<byte, int> ShotLimit = new();
        public static Dictionary<byte, PlayerControl> DivinationTarget = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ONBigWerewolf, CustomGameMode.OneNight);
        }
        public static void Init()
        {
            playerIdList = new();
            ShotLimit = new();
            DivinationTarget = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            ShotLimit.TryAdd(playerId, 1);
            DivinationTarget.Add(playerId, null);
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "ONBigWerewolf");
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetONBigWerewolfShotLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(ShotLimit[playerId]);
            writer.Write(DivinationTarget[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte ONWerewolfId = reader.ReadByte();
            int Limit = reader.ReadInt32();
            if (ShotLimit.ContainsKey(ONWerewolfId))
                ShotLimit[ONWerewolfId] = Limit;
            else
                ShotLimit.Add(ONWerewolfId, 1);

            DivinationTarget[ONWerewolfId].PlayerId = reader.ReadByte();
        }
        public static bool CanUseKillButton(byte playerId)
            => playerIdList.Contains(playerId) && ShotLimit[playerId] > 0;

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            Main.ONKillCount++;
            ShotLimit[killer.PlayerId]--;
            DivinationTarget[killer.PlayerId] = target;
            Logger.Info($"{killer.GetNameWithRole()} : 大狼が占った", "ONBigWerewolf");
            SendRPC(killer.PlayerId);
            Utils.NotifyRoles(SpecifySeer: killer);
            Main.AllPlayerKillCooldown[killer.PlayerId] = 999f;
            killer.SyncSettings();//キルクール処理を同期
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
                    Logger.Info($"{Utils.GetPlayerById(seerId).GetNameWithRole()}の死亡済占い先：{DivinationTarget[seerId].GetNameWithRole()}", "ONBigWerewolf");
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
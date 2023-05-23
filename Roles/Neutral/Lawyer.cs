using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHost.Roles.Impostor;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Neutral
{
    public static class Lawyer
    {
        private static readonly int Id = 60500;
        public static List<byte> playerIdList = new();

        public static OptionItem HasImpostorVision;
        public static OptionItem KnowTargetRole;
        private static OptionItem TargetKnows;
        public static OptionItem PursuerGuardNum;

        /// <summary>
        /// Key: LawyerのPlayerId, Value: ターゲットのPlayerId
        /// </summary>
        public static Dictionary<byte, byte> Target = new();
        public static Dictionary<byte, int> GuardCount = new();


        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Lawyer);
            HasImpostorVision = BooleanOptionItem.Create(Id + 10, "ImpostorVision", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Lawyer]);
            TargetKnows = BooleanOptionItem.Create(Id + 11, "LawyerTargetKnows", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Lawyer]);
            KnowTargetRole = BooleanOptionItem.Create(Id + 12, "LawyerKnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Lawyer]);
            PursuerGuardNum = IntegerOptionItem.Create(Id + 13, "PursuerGuardNum", new(0, 20, 1), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Lawyer])
                .SetValueFormat(OptionFormat.Times);
        }
        public static void Init()
        {
            playerIdList = new();
            Target = new();
            GuardCount = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

            //ターゲット割り当て
            if (AmongUsClient.Instance.AmHost)
            {
                List<PlayerControl> targetList = new();
                var rand = IRandom.Instance;
                foreach (var target in Main.AllPlayerControls)
                {
                    if (playerId == target.PlayerId) continue;
                    if ((target.Is(CustomRoleTypes.Impostor)
                        || (target.IsNeutralKiller() && !target.Is(CustomRoles.Arsonist) && !target.Is(CustomRoles.PlatonicLover) && !target.Is(CustomRoles.Totocalcio)))
                        && !target.Is(CustomRoles.Lovers)
                    ) targetList.Add(target);
                }
                var SelectedTarget = targetList[rand.Next(targetList.Count)];
                Target.Add(playerId, SelectedTarget.PlayerId);
                SendRPC(playerId, SelectedTarget.PlayerId, "SetTarget");
                Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Lawyer");
            }

            GuardCount.TryAdd(playerId, PursuerGuardNum.GetInt());
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void SendRPC(byte LawyerId, byte targetId = 0x73, string Progress = "")
        {
            MessageWriter writer;
            switch (Progress)
            {
                case "SetTarget":
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLawyerTarget, SendOption.Reliable);
                    writer.Write(LawyerId);
                    writer.Write(targetId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
                case "":
                    if (!AmongUsClient.Instance.AmHost) return;
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemovetLawyerTarget, SendOption.Reliable);
                    writer.Write(LawyerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
                case "Guard":
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPursuerGuardCount, SendOption.Reliable);
                    writer.Write(LawyerId);
                    writer.Write(GuardCount[LawyerId]);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
            }
        }
        public static void ReceiveRPC(MessageReader reader, bool SetTarget, bool Guard = false)
        {
            if (SetTarget)
            {
                byte LawyerId = reader.ReadByte();
                byte TargetId = reader.ReadByte();
                Target[LawyerId] = TargetId;
            }
            else if(Guard)
            {
                byte LawyerId = reader.ReadByte();
                int GuardNum = reader.ReadInt32();
                if (GuardCount.ContainsKey(LawyerId))
                    GuardCount[LawyerId] = GuardNum;
                else
                    GuardCount.Add(LawyerId, PursuerGuardNum.GetInt());

            }
            else
                Target.Remove(reader.ReadByte());
        }
        public static void ChangeRoleByTarget(PlayerControl target)
        {
            byte Lawyer = 0x73;
            Target.Do(x =>
            {
                if (x.Value == target.PlayerId)
                    Lawyer = x.Key;
            });
            Utils.GetPlayerById(Lawyer).RpcSetCustomRole(CustomRoles.Pursuer);
            Target.Remove(Lawyer);
            SendRPC(Lawyer);
            Utils.NotifyRoles();
        }
        public static void ChangeRole(PlayerControl Lawyer)
        {
            Lawyer.RpcSetCustomRole(CustomRoles.Pursuer);
            Target.Remove(Lawyer.PlayerId);
            SendRPC(Lawyer.PlayerId);
        }
        public static string TargetMark(PlayerControl seer, PlayerControl target)
        {
            var mark = "";
            Target.Do(x =>
            {
                if (TargetKnows.GetBool() && seer.PlayerId == x.Value && target.PlayerId == x.Value && Utils.GetPlayerById(x.Key).IsAlive())
                    mark += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "§");
                if (seer.Is(CustomRoles.Lawyer) && seer.PlayerId == x.Key && target.PlayerId == x.Value)
                    mark += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "§");
            });
            return mark;
        }        
        public static bool IsWatchTargetRole(PlayerControl seer, PlayerControl target)
        {
            var IsWatch = false;
            Target.Do(x =>
            {
                if (KnowTargetRole.GetBool() && seer.PlayerId == x.Key && target.PlayerId == x.Value && Utils.GetPlayerById(x.Key).IsAlive())
                    IsWatch = true;
            });
            return IsWatch;
        }
        public static void CheckExileTarget(byte exiledId)
        {
            Target.Do(x =>
            {
                var Lawyer = Utils.GetPlayerById(x.Key);
                if (x.Value == exiledId && Lawyer.IsAlive())
                    Lawyer.RpcSetCustomRole(CustomRoles.Pursuer);
            });
        }

        private static bool CanUseGuard(byte playerId) => !Main.PlayerStates[playerId].IsDead && GuardCount[playerId] > 0;
        public static string GetGuardNum(byte playerId) => Utils.ColorString(CanUseGuard(playerId) ? Color.yellow : Color.white, GuardCount.TryGetValue(playerId, out var GuardNum) ? $"({GuardNum})" : "Invalid");

        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            // 直接キル出来る役職チェック
            if (killer.Is(CustomRoles.Arsonist) || killer.Is(CustomRoles.PlatonicLover) || killer.Is(CustomRoles.Totocalcio) || killer.Is(CustomRoles.MadSheriff)) return true;
            if (GuardCount[target.PlayerId] <= 0) return true;

            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);
            GuardCount[target.PlayerId]--;
            SendRPC(target.PlayerId, Progress:"Guard");

            switch (killer.GetCustomRole())
            {
                case CustomRoles.BountyHunter:
                    if (BountyHunter.GetTarget(killer) == target.PlayerId)
                        BountyHunter.ResetTarget(killer);//ターゲットの選びなおし
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.OnCheckMurder(killer, false);
                    break;
            }

            return false;
        }

        public static void EndGameCheck()
        {
            Target.Do(x =>
            {
                // 勝者に依頼人が含まれている時
                if(CustomWinnerHolder.WinnerIds.Contains(x.Value))
                {
                    byte Lawyer = x.Key;
                    // 弁護士が生きている時 リセットして単独勝利
                    if(Utils.GetPlayerById(Lawyer).IsAlive())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lawyer);
                        CustomWinnerHolder.WinnerIds.Add(Lawyer);
                    }
                    // 弁護士が死んでいる時 勝者と共に追加勝利
                    else
                    {
                        CustomWinnerHolder.WinnerIds.Add(Lawyer);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lawyer);
                    }
                }    
            });

            // 追跡者が生き残った場合ここで追加勝利
            Main.AllPlayerControls
                .Where(p => p.Is(CustomRoles.Pursuer) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Pursuer);
                });
        }

    }
}
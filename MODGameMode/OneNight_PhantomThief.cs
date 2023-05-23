using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using UnityEngine;
using static TownOfHost.Options;
using static TownOfHost.Main;

namespace TownOfHost
{
    public static class ONPhantomThief
    {
        private static readonly int Id = 67200;
        public static List<byte> playerIdList = new();

        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ONPhantomThief, 1, CustomGameMode.OneNight);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

            if (!ResetCamPlayerList.Contains(playerId))
                ResetCamPlayerList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;

        public static void SetKillCooldown(byte id) => AllPlayerKillCooldown[id] = 0.1f;
        public static bool CanUseKillButton(byte playerId)
            => playerIdList.Contains(playerId) && ChangeRolesTarget[playerId] == null;

        public static string GetChangeMark(PlayerControl seer, PlayerControl target = null)
        {
            if (!CurrentGameMode.IsOneNightMode()) return "";
            if (GameStates.IsMeeting || DefaultRole[seer.PlayerId] != CustomRoles.ONPhantomThief) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";

            if (ChangeRolesTarget[seer.PlayerId] == null) return "";

            return Utils.ColorString(Utils.GetRoleColor(ChangeRolesTarget[seer.PlayerId].GetCustomRole()), $" ⇒ {ChangeRolesTarget[seer.PlayerId].GetDisplayRoleName()}");
        }

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (target.Is(CustomRoles.ONPhantomThief) && ChangeRolesTarget[target.PlayerId] != null)
            {
                ChangeRolesTarget[killer.PlayerId] = ChangeRolesTarget[target.PlayerId];
            }
            else if (ChangeRolesTarget.ContainsValue(target))
            {
                foreach (byte seerId in playerIdList)
                {
                    if (ChangeRolesTarget[seerId] == null) continue;

                    //つくる
                }
            }
            else
            {
                ChangeRolesTarget[killer.PlayerId] = target;
            }
            RPC.SendRPCChangeRole(killer.PlayerId);
            killer.SetKillCooldown(999f);
            killer.RpcGuardAndKill(target);

            Logger.Info($"{killer.GetNameWithRole()} : 役職交換予約", "ONPhantomThief");
            Utils.NotifyRoles(SpecifySeer: killer);
            return;
        }
        public static void OnCheckMurderTarget(PlayerControl killer, PlayerControl target)
        {
            if (!IsEnable) return;
            if (ChangeRolesTarget[killer.PlayerId] == null) return;

            if (ChangeRolesTarget[killer.PlayerId].GetCustomRole().IsONImpostor())
            {
                new LateTask(() =>
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                    GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, false);
                }, 0.5f, "ONPhantomThiefKilled");
                Logger.Info($"{target.GetNameWithRole()} : 人狼と交換した怪盗がキルされた", "ONPhantomThief");
            }
            return;
        }

        public static void SetChangeRoles()
        {
            if (!IsEnable || !MeetingStates.FirstMeeting) return;

            foreach (byte seerId in playerIdList)
            {
                if (ChangeRolesTarget[seerId] == null) continue;

                var Seer = Utils.GetPlayerById(seerId);
                var TargetRole = ChangeRolesTarget[seerId].GetCustomRole();
                //先に交換される相手に役職セット
                ChangeRolesTarget[seerId].RpcSetCustomRole(Seer.GetCustomRole());

                //交換する側の役職セット
                Seer.RpcSetCustomRole(TargetRole);
                Logger.Info($"{Seer.GetNameWithRole()} : 役職交換した", "ONPhantomThief");
            }
        }
    }
}
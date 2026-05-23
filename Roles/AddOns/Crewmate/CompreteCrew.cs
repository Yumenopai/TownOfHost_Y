using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.AddOns.Crewmate
{
    public static class CompreteCrew
    {
        private static readonly int Id = (int)Options.offsetId.AddonCrew + 100;
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.CompleteCrew);
            Options.SetUpAddOnOptions(Id + 10, CustomRoles.CompleteCrew, TabGroup.Addons);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

        public static bool CanBeCompreteCrew(PlayerControl pc)
           => pc.IsAlive()
           && !IsThisRole(pc.PlayerId)
           && pc.GetPlayerTaskState().IsTaskFinished
           && pc.Is(CustomRoleTypes.Crewmate);

        public static void OnCompleteTask(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!CustomRoles.CompleteCrew.IsEnable() || playerIdList.Count >= CustomRoles.CompleteCrew.GetCount()) return;
            if (!CanBeCompreteCrew(pc)) return;

            pc.RpcSetCustomRole(CustomRoles.CompleteCrew);
            if (AmongUsClient.Instance.AmHost)
            {
                if (Options.AddOnBuffAssign[CustomRoles.CompleteCrew].GetBool() || Options.AddOnDebuffAssign[CustomRoles.CompleteCrew].GetBool())
                {
                    foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsAddOn()))
                    {
                        if (Options.AddOnRoleOptions.TryGetValue((CustomRoles.CompleteCrew, Addon), out var option) && option.GetBool())
                        {
                            pc.RpcSetCustomRole(Addon);
                        }
                    }
                }
                Add(pc.PlayerId);
                pc.SyncSettings();
                Utils.NotifyRoles();
            }
        }

    }
}
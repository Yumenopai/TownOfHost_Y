using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost.Roles.AddOns
{
    public static class CompreteCrew
    {
        private static readonly int Id = 77700;
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.CompreteCrew, roleSetMode: RoleSettingMode.All);
            Options.SetUpAddOnOptions(Id + 10, CustomRoles.CompreteCrew, TabGroup.Addons);
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
           && !(pc.GetPlayerTaskState().CompletedTasksCount + 1 < pc.GetPlayerTaskState().AllTasksCount)
           && pc.GetCustomRole().IsCrewmate()
           && Utils.HasTasks(pc.Data);

        public static void OnCompleteTask(PlayerControl pc)
        {
            if (!CustomRoles.CompreteCrew.IsEnable() || playerIdList.Count >= CustomRoles.CompreteCrew.GetCount()) return;
            if (!CanBeCompreteCrew(pc)) return;

            pc.RpcSetCustomRole(CustomRoles.CompreteCrew);
            if (AmongUsClient.Instance.AmHost)
            {
                if (Options.AddOnBuffAssign[CustomRoles.CompreteCrew].GetBool() || Options.AddOnDebuffAssign[CustomRoles.CompreteCrew].GetBool())
                {
                    foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsAddOn()))
                    {
                        if (Options.AddOnRoleOptions.TryGetValue((CustomRoles.CompreteCrew, Addon), out var option) && option.GetBool())
                        {
                            pc.RpcSetCustomRole(Addon);
                            SelectRolesPatch.AddonInit(pc, Addon);
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
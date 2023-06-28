using System;
using System.Linq;
using UnityEngine;

namespace TownOfHost.Roles.AddOns
{
    public static class LastImpostor
    {
        private static readonly int Id = 79000;
        public static byte currentId = byte.MaxValue;
        public static OptionItem IsChangeKillCooldown;
        public static OptionItem KillCooldown;
        public static void SetupCustomOption()
        {
            Options.SetupSingleRoleOptions(Id, TabGroup.Addons, CustomRoles.LastImpostor, 1);
            IsChangeKillCooldown = BooleanOptionItem.Create(Id + 11, "IsChangeKillCooldown", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnOnOff[CustomRoles.LastImpostor]);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 60f, 0.5f), 15f, TabGroup.Addons, false).SetParent(IsChangeKillCooldown)
                .SetValueFormat(OptionFormat.Seconds);
            Options.SetUpAddOnOptions(Id + 20, CustomRoles.LastImpostor, TabGroup.Addons);
        }
        public static void Init() => currentId = byte.MaxValue;
        public static void Add(byte id) => currentId = id;
        public static void SetKillCooldown(PlayerControl pc)
        {
            if (currentId == byte.MaxValue || !CanChangeKillColldown(pc)) return;
            Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
        }
        public static bool CanBeLastImpostor(PlayerControl pc)
            => pc.IsAlive()
            && !pc.Is(CustomRoles.LastImpostor)
            && pc.Is(CustomRoleTypes.Impostor)
            && pc.GetCustomRole()
            is not CustomRoles.CatRedLeader
            and not CustomRoles.ONWerewolf
            and not CustomRoles.ONBigWerewolf;

        public static bool CanChangeKillColldown(PlayerControl pc)
            => pc.IsAlive()
            && pc.Is(CustomRoles.LastImpostor)
            && pc.GetCustomRole()
            is not CustomRoles.Vampire
                and not CustomRoles.BountyHunter
                and not CustomRoles.SerialKiller
                and not CustomRoles.Greedier
                and not CustomRoles.Ambitioner
                and not CustomRoles.CatRedLeader;

        public static void SetSubRole()
        {
            //ラストインポスターがすでにいれば処理不要
            if (currentId != byte.MaxValue) return;
            if (!CustomRoles.LastImpostor.IsEnable() || Main.AliveImpostorCount != 1) return;

            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (CanBeLastImpostor(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                    if (Options.AddOnBuffAssign[CustomRoles.LastImpostor].GetBool() || Options.AddOnDebuffAssign[CustomRoles.LastImpostor].GetBool())
                    {
                        foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsAddOn()))
                        {
                            if (Options.AddOnRoleOptions.TryGetValue((CustomRoles.LastImpostor, Addon), out var option) && option.GetBool())
                            {
                                pc.RpcSetCustomRole(Addon);
                            }
                        }
                    }
                    Add(pc.PlayerId);
                    SetKillCooldown(pc);
                    pc.SyncSettings();
                    Utils.NotifyRoles();
                    break;
                }
            }
        }
    }
}
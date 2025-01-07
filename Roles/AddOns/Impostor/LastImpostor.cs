using System;
using System.Linq;
using TownOfHostY.Attributes;
using TownOfHostY.Roles.Core;

using static TownOfHostY.Options;

namespace TownOfHostY.Roles.AddOns.Impostor
{
    public static class LastImpostor
    {
        private static readonly int Id = (int)offsetId.AddonImp + 0;
        public static byte currentId = byte.MaxValue;
        public static OptionItem IsChangeKillCooldown;
        public static OptionItem KillCooldown;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.LastImpostor, new(1, 1, 1));
            IsChangeKillCooldown = BooleanOptionItem.Create(Id + 11, "LastImpostorChangeKillCooldown", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.LastImpostor]);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 60f, 0.5f), 15f, TabGroup.Addons, false).SetParent(IsChangeKillCooldown)
                .SetValueFormat(OptionFormat.Seconds);
            SetUpAddOnOptions(Id + 20, CustomRoles.LastImpostor, TabGroup.Addons);
        }
        [GameModuleInitializer]
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
            is not CustomRoles.CCRedLeader;
            //and not CustomRoles.ONWerewolf
            //and not CustomRoles.ONBigWerewolf;

        public static bool CanChangeKillColldown(PlayerControl pc)
            => pc.IsAlive()
            && pc.Is(CustomRoles.LastImpostor)
            && pc.GetCustomRole()
            is not CustomRoles.Vampire
                and not CustomRoles.BountyHunter
                and not CustomRoles.SerialKiller
                and not CustomRoles.Greedier
                and not CustomRoles.Ambitioner
                and not CustomRoles.GrudgeCharger
                and not CustomRoles.Charger;

        public static void SetSubRole()
        {
            //ラストインポスターがすでにいれば処理不要
            if (currentId != byte.MaxValue) return;
            if (CurrentGameMode == CustomGameMode.HideAndSeek
                || !CustomRoles.LastImpostor.IsPresent() || Main.AliveImpostorCount != 1)
                return;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (CanBeLastImpostor(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                    if (AddOnBuffAssign[CustomRoles.LastImpostor].GetBool() || AddOnDebuffAssign[CustomRoles.LastImpostor].GetBool())
                    {
                        foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsAddOn()))
                        {
                            if (AddOnRoleOptions.TryGetValue((CustomRoles.LastImpostor, Addon), out var option) && option.GetBool())
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
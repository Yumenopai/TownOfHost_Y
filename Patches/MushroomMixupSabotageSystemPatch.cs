using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Roles.Neutral;

namespace TownOfHostY;

[HarmonyPatch(typeof(MushroomMixupSabotageSystem), nameof(MushroomMixupSabotageSystem.UpdateSystem))]
public static class MushroomMixupUpdateSystemPatch
{
    public static bool InSabotage => instance != null && instance.IsActive && NameChanged;
    public static bool NameChanged = false;
    private static MushroomMixupSabotageSystem instance;
    public static List<PlayerControl> TargetPlayers = new();
    public static List<PlayerControl> ChangedPlayers = new();
    public static void Prefix(MushroomMixupSabotageSystem __instance, PlayerControl player, MessageReader msgReader)
    {
        TargetPlayers.Clear();
        ChangedPlayers.Clear();
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            TargetPlayers.Add(pc);
        }

        var name = "<color=#00000000>.";
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            var role = pc.GetRoleClass();
            if (pc.PlayerId != PlayerControl.LocalPlayer.PlayerId &&
                role is IKiller && role is not IImpostor &&
                !pc.Is(CustomRoles.Egoist) &&
                !((pc.Is(CustomRoles.Jackal) || pc.Is(CustomRoles.JSidekick)) && Jackal.CanSeeNameMushroomMixup))
            {
                ChangedPlayers.Add(pc);
                foreach (PlayerControl target in TargetPlayers)
                {
                    if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        _ = new LateTask(() => target.RpcSetNamePrivate(name, seer: pc, force: true), 1f, "MushroomMixupSetName");
                    }
                    else
                    {
                        target.RpcSetNamePrivate(name, seer: pc, force: true);
                    }
                }
            }
        }
        instance = __instance;
        NameChanged = true;
    }
}
[HarmonyPatch(typeof(MushroomMixupSabotageSystem), nameof(MushroomMixupSabotageSystem.Deteriorate))]
public static class MushroomMixupDeterioratePatch
{
    public static void Prefix(MushroomMixupSabotageSystem __instance, float deltaTime)
    {
        if (!__instance.IsActive) return;
        if ((double)__instance.currentSecondsUntilHeal - deltaTime > 0.0) return;

        RestorName();
    }
    public static void RestorName()
    {
        var lateTask = false;
        foreach (var changed in MushroomMixupUpdateSystemPatch.ChangedPlayers.Where(x => x != null))
        {
            foreach (var target in MushroomMixupUpdateSystemPatch.TargetPlayers.Where(x => x != null))
            {
                var name = Main.AllPlayerNames[target.PlayerId];
                if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    _ = new LateTask(() => target.RpcSetNamePrivate(name, seer: changed, force: true), 0.1f, "MushroomMixupRestoreName");
                    lateTask = true;
                }
                else
                {
                    target.RpcSetNamePrivate(name, seer: changed, force: true);
                }
            }
        }

        MushroomMixupUpdateSystemPatch.TargetPlayers.Clear();
        MushroomMixupUpdateSystemPatch.ChangedPlayers.Clear();
        MushroomMixupUpdateSystemPatch.NameChanged = false;

        if (lateTask)
        {
            _ = new LateTask(() => Utils.NotifyRoles(NoCache: true), 0.3f, "MushroomMixupRestoreNotifyRoles");
        }
        else
        {
            Utils.NotifyRoles(NoCache: true);
        }
    }
}


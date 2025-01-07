using System.Linq;
using HarmonyLib;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Patches;

// reference :: TOH-H Patches/SpawnInMinigamePatch.cs
[HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.SpawnAt))]
public static class SpawnInMinigameSpawnAtPatch
{
    public static void Postfix()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            PlayerControl.LocalPlayer.RpcResetAbilityCooldown();
            if (MeetingStates.FirstMeeting && Options.FixFirstKillCooldown.GetBool())
            {
                Logger.Info($"初手キルクール調整", "spawn");
                PlayerControl.LocalPlayer.SetKillCooldown(Main.AllPlayerKillCooldown[PlayerControl.LocalPlayer.PlayerId]);
            }

            if (Options.RandomSpawn_Airship.GetBool())
            {
                new RandomSpawn.AirshipSpawnMap().RandomTeleport(PlayerControl.LocalPlayer);
            }
        }
    }
}
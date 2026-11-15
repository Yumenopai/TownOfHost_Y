//参考：TOH-K
using HarmonyLib;

namespace TownOfHostY;



[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.Serialize))]
class GameDataSerializePatch
{

    public static int SerializeMessageCount;

    public static bool DontTouch;

    public static bool Prefix(NetworkedPlayerInfo __instance, ref bool __result, out string __state)
    {
        __state = null;
        if (AmongUsClient.Instance == null || !GameStates.IsInGame)
        {
            __result = true;
            return true;
        }
        if (DontTouch)
        {
            __instance.ClearDirtyBits();
            __result = false;
            return false;
        }
        if (SerializeMessageCount > 0)
        {
            __result = true;
            MaskName(__instance, out __state);
            return true;
        }

        if (Options.CurrentGameMode != CustomGameMode.Standard
            || !GameStates.IsMeeting
            || ExileController.Instance != null
            || AntiBlackout.IsCached)
        {
            MaskName(__instance, out __state);
            return true;
        }
        __instance.ClearDirtyBits();
        __result = false;
        return false;
    }

    public static void Postfix(NetworkedPlayerInfo __instance, string __state)
    {
        if (__state != null) __instance.DefaultOutfit.PlayerName = __state;
    }

    private static void MaskName(NetworkedPlayerInfo info, out string original)
    {
        original = null;
        if (!AmongUsClient.Instance.AmHost) return;
        var host = PlayerControl.LocalPlayer;
        if (host == null || host.IsAlive()) return;
        if (Camouflage.IsCamouflage || MushroomMixupUpdateSystemPatch.InSabotage) return;
        if (Main.AllPlayerNames == null || !Main.AllPlayerNames.TryGetValue(info.PlayerId, out var realName)) return;
        var outfit = info.DefaultOutfit;
        if (outfit == null || outfit.PlayerName == realName) return;
        original = outfit.PlayerName;
        outfit.PlayerName = realName;
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.MarkDirty))]
class NetworkedPlayerInfoMarkDirtyPatch
{
    public static bool Prefix() => GameDataSerializePatch.DontTouch is false;
}
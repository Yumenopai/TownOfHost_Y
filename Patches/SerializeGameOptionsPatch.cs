using AmongUs.GameOptions;
using HarmonyLib;

namespace TownOfHost_Y.Patches
{
    [HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetKillDistance))]
    internal class KillDistancePatch
    {
        public static bool Prefix(LogicOptions __instance, ref float __result)
        {
            if (__instance == null)
            {
                __result = 0.5f; // フォールバック距離
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GameOptionsFactory), nameof(GameOptionsFactory.ToBytes))]
    internal class SerializeGameOptionsPatch
    {
        // IL2CPP の実際のパラメータ名に合わせて 'data' に変更
        public static bool Prefix(ref IGameOptions data)
        {
            try
            {
                // Do not interfere with vanilla serialization. Allow original method to run.
                return true;
            }
            catch
            {
                // In case of unexpected errors, still allow original to run to avoid breaking flow.
                return true;
            }
        }
    }
}

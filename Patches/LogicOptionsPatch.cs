using HarmonyLib;
using AmongUs.GameOptions;

namespace TownOfHostY.Patches
{
    [HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetKillDistance))]
    class LogicOptionsGetKillDistancePatch
    {
        
        public static bool Prefix(LogicOptions __instance, ref float __result)
        {
            try
            {
                if (__instance == null)
                {
                    __result = 1.0f; 
                    return false; 
                }

                
                try
                {
                    var mgr = GameOptionsManager.Instance;
                    if (mgr != null && mgr.CurrentGameOptions != null)
                    {
                        
                        __result = 1.0f;
                        return false;
                    }
                }
                catch { }

                
                return true;
            }
            catch (System.Exception ex)
            {
                Logger.Exception(ex, "LogicOptionsGetKillDistancePatch");
                __result = 1.0f;
                return false;
            }
        }
    }
}

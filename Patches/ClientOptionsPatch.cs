using HarmonyLib;

using TownOfHostY.Modules.ClientOptions;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    public static class OptionsMenuBehaviourStartPatch
    {
        private static ClientActionItem UnloadMod;
        private static ClientActionItem DumpLog;
        private static ClientActionItem OpenLogFolder;
        private static ClientActionItem IsMuteLobbyBGM;

        public static void Postfix(OptionsMenuBehaviour __instance)
        {
            if (__instance.DisableMouseMovement == null)
            {
                return;
            }

            if (UnloadMod == null || UnloadMod.ToggleButton == null)
            {
                UnloadMod = ClientActionItem.Create("UnloadMod", ModUnloaderScreen.Show, __instance);
            }
            if (DumpLog == null || DumpLog.ToggleButton == null)
            {
                DumpLog = ClientActionItem.Create("DumpLog", Utils.DumpLog, __instance);
            }
            if (OpenLogFolder == null || OpenLogFolder.ToggleButton == null)
            {
                OpenLogFolder = ClientActionItem.Create("OpenLogFolder", Utils.OpenLogFolder, __instance);
            }
            if (IsMuteLobbyBGM == null || IsMuteLobbyBGM.ToggleButton == null)
            {
                IsMuteLobbyBGM = ClientOptionItem.Create("IsMuteLobbyBGM", Main.IsMuteLobbyBGM, __instance);
            }

            if (ModUnloaderScreen.Popup == null)
            {
                ModUnloaderScreen.Init(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
    public static class OptionsMenuBehaviourClosePatch
    {
        public static void Postfix()
        {
            if (ClientActionItem.CustomBackground != null)
            {
                ClientActionItem.CustomBackground.gameObject.SetActive(false);
            }
            ModUnloaderScreen.Hide();
        }
    }
}

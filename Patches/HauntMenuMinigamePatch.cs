using HarmonyLib;

namespace TownOfHostY.Patches;

[HarmonyPatch(typeof(HauntMenuMinigame), nameof(HauntMenuMinigame.SetFilterText))]
public static class HauntMenuMinigameSetFilterTextPatch
{
    public static bool Prefix(HauntMenuMinigame __instance)
    {
        if (__instance.HauntTarget != null && Options.GhostCantSeeOtherRoles.GetBool())
        {
            // 役職表示をカスタムロール名で上書き
            __instance.FilterText.text = RoleText.GetDisplayRoleName(PlayerControl.LocalPlayer, __instance.HauntTarget);
            return false;
        }
        return true;
    }
}
using HarmonyLib;
using Hazel;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Patches.ISystemType;

[HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.UpdateSystem))]
public static class ReactorSystemTypeUpdateSystemPatch
{
    public static bool Prefix(ReactorSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader, ref byte __state /* amount */)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }
        __state = amount;

        if (player.GetRoleClass() is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateReactorSystem(__instance, amount))
        {
            return false;
        }
        return true;
    }
    public static void Postfix(ReactorSystemType __instance, byte __state /* amount */ )
    {
        // サボタージュ発動時
        if (__state == ReactorSystemType.StartCountdown)
        {
            if (((MapNames)Main.NormalOptions.MapId == MapNames.Polus && !Options.SabotageTimeControl_Polus.GetBool())
                || ((MapNames)Main.NormalOptions.MapId == MapNames.Fungle && !Options.SabotageTimeControl_Fungle.GetBool()))
            {
                return;
            }
            var duration = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Polus => Options.PolusReactorTimeLimit.GetFloat(),
                MapNames.Fungle => Options.FungleReactorTimeLimit.GetFloat(),
                _ => float.NaN,
            };
            if (!float.IsNaN(duration))
            {
                __instance.Countdown = duration;
            }
        }
    }
}

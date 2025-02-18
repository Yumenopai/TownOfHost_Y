using AmongUs.GameOptions;
using HarmonyLib;

using TownOfHostY.Roles;
using TownOfHostY.Roles.Core;

namespace TownOfHostY;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
class SelectRolesHASModePatch
{
    public static bool Prefix(RoleManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        //HideAndSeek用
        if (!Options.IsHASMode) return false;

        //CustomRpcSenderとRpcSetRoleReplacerの初期化
        RpcSetRoleReplacer.StartReplace();

        RoleAssignManager.SelectAssignRoles();

        //バニラの役職割り当て
        SelectRolesPatch.AssignRolesNormal(null, 0);

        //MODの役職割り当て
        RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く

        SetColorPatch.IsAntiGlitchDisabled = true;
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.Is(CustomRoleTypes.Impostor))
                pc.RpcSetColor(0);
            else if (pc.Is(CustomRoleTypes.Crewmate))
                pc.RpcSetColor(1);
        }

        var roleTypePlayers = SelectRolesPatch.GetRoleTypePlayers();
        //役職設定処理
        if (roleTypePlayers.TryGetValue(RoleTypes.Crewmate, out var list))
        {
            SelectRolesPatch.AssignCustomRolesFromList(CustomRoles.HASFox, list);
            SelectRolesPatch.AssignCustomRolesFromList(CustomRoles.HASTroll, list);
        }
        foreach (var pair in PlayerState.AllPlayerStates)
        {
            //RPCによる同期
            ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.GetNowMainRole());
        }
        //色設定処理
        SetColorPatch.IsAntiGlitchDisabled = true;

        GameEndChecker.SetPredicateToHideAndSeek();

        Utils.CountAlivePlayers(true);
        Utils.SyncAllSettings();
        SetColorPatch.IsAntiGlitchDisabled = false;

        return false;
    }
}
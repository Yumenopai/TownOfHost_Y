using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;

using TownOfHostY.Modules;
using TownOfHostY.Roles;
using TownOfHostY.Roles.Core;

namespace TownOfHostY;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
class SelectRolesCCModePatch
{
    public static bool Prefix(RoleManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        //CCMode用
        if (!Options.IsCCMode) return false;

        //CustomRpcSenderとRpcSetRoleReplacerの初期化
        RpcSetRoleReplacer.StartReplace();

        RoleAssignManager.SelectAssignRoles();

        Dictionary<RoleTypes, int> roleTypesList = new();
        var assignedNum = 0;
        var assignedNumImpostors = 0;

        if (CatchCat.Option.T_CanUseVent.GetBool())
        { // Engineer Setting
            int CatCount = Main.AllPlayerControls.Count() - 2 - CustomRoles.CCYellowLeader.GetCount();
            roleTypesList.Add(RoleTypes.Engineer, CatCount);
        }
        List<PlayerControl> AllPlayers = new();
        foreach (var pc in Main.AllPlayerControls)
        {
            AllPlayers.Add(pc);
        }

        if (Options.EnableGM.GetBool())
        {
            AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
            PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
            PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SetMainRole(CustomRoles.GM);
        }

        SelectRolesPatch.AssignDesyncRole(CustomRoles.CCYellowLeader, AllPlayers, ref assignedNum, BaseRole: RoleTypes.Impostor);
        SelectRolesPatch.AssignDesyncRole(CustomRoles.CCBlueLeader, AllPlayers, ref assignedNum, BaseRole: RoleTypes.Impostor);

        //バニラの役職割り当て
        SelectRolesPatch.AssignRolesNormal(roleTypesList, assignedNumImpostors);

        //MODの役職割り当て
        RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く

        var roleTypePlayers = SelectRolesPatch.GetRoleTypePlayers();
        //役職設定処理
        //Impostorsを割り当て
        {
            SetColorPatch.IsAntiGlitchDisabled = true;
            if (roleTypePlayers.TryGetValue(RoleTypes.Impostor, out var list))
            {
                foreach (var imp in list)
                {
                    PlayerState.GetByPlayerId(imp.PlayerId).SetMainRole(CustomRoles.CCRedLeader);
                    Logger.Info("役職設定:" + imp?.Data?.PlayerName + " = " + CustomRoles.CCRedLeader.ToString(), "AssignRoles");
                }
            }
        }
        //残りを割り当て
        {
            if (roleTypePlayers.TryGetValue(CatchCat.Option.T_CanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, out var list))
            {
                foreach (var crew in list)
                {
                    PlayerState.GetByPlayerId(crew.PlayerId).SetMainRole(CustomRoles.CCNoCat);
                    Logger.Info("役職設定:" + crew?.Data?.PlayerName + " = " + CustomRoles.CCNoCat.ToString(), "AssignRoles");
                }
            }
            SetColorPatch.IsAntiGlitchDisabled = false;
        }

        foreach (var pair in PlayerState.AllPlayerStates)
        {
            //RPCによる同期
            ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.GetNowMainRole());
        }

        foreach (var pc in Main.AllPlayerControls)
        {
            HudManager.Instance.SetHudActive(true);
            Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更

            CatchCat.Common.Add(pc);
        }
        GameEndChecker.SetPredicateToCatchCat();

        GameOptionsSender.AllSenders.Clear();
        foreach (var pc in Main.AllPlayerControls)
        {
            GameOptionsSender.AllSenders.Add(
                new PlayerGameOptionsSender(pc)
            );
        }

        Utils.CountAlivePlayers(true);
        Utils.SyncAllSettings();
        SetColorPatch.IsAntiGlitchDisabled = false;

        return false;
    }
}
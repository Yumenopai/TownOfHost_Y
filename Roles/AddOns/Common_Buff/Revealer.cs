using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;
using TownOfHostY.Attributes;
using static TownOfHostY.Options;

namespace TownOfHostY.Roles.AddOns.Common;

public static class Revealer
{
    private static readonly int Id = (int)offsetId.AddonBuff + 1300;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Revealer);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Rv");
    private static List<byte> playerIdList = new();
    private static HashSet<byte> SeenIdList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Revealer);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
        SeenIdList = new();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }

    public static void ChangeName(PlayerControl pc)
    {
        if (!pc.Is(CustomRoles.Revealer)) return;

        SeenIdList.Add(pc.PlayerId);

        string exiledRole = Utils.GetRoleName(pc.GetCustomRole());
        string exiledName = pc.GetRealName(true);

        if (pc == PlayerControl.LocalPlayer)
        {
            if (Main.nickName != "")
            {
                exiledName = Main.nickName;
            }
            Main.nickName = string.Format(Translator.GetString("RevealerExiledText"), exiledRole, exiledName);
        }
        else
        {
            pc.RpcSetName(string.Format(Translator.GetString("RevealerExiledText"), exiledRole, exiledName));
        }
        
        _ = new LateTask(() =>
        {
            if (pc == PlayerControl.LocalPlayer)
            {
                Main.nickName = exiledName;
            }
            else
            {
                pc.RpcSetName(exiledName);
            }
        }, 8f, "SetName");
    }

    public static void OverrideDisplayRoleNameAsSeen(PlayerControl seen, bool isMeeting, ref bool enabled, ref string roleText)
    {
        if (!seen.Is(CustomRoles.Revealer) || !SeenIdList.Contains(seen.PlayerId)) return;

        enabled = true;
    }


    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}
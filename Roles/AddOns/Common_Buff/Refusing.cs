using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Options;
using TownOfHostY.Attributes;

namespace TownOfHostY.Roles.AddOns.Common;

public static class Refusing
{
    private static readonly int Id = (int)offsetId.AddonBuff + 1200;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Refusing);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Rf");
    private static List<byte> playerIdList = new();
    private static List<byte> IgnoreExiled = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Refusing);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
        IgnoreExiled = new();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
        if (!IgnoreExiled.Contains(playerId))
            IgnoreExiled.Add(playerId);
    }
    public static NetworkedPlayerInfo VoteChange(NetworkedPlayerInfo Exiled)
    {
        if (Exiled == null || !IgnoreExiled.Contains(Exiled.PlayerId)) return Exiled;

        IgnoreExiled.Remove(Exiled.PlayerId);
        return null;
    }

    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}
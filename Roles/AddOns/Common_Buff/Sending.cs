using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;

using static TownOfHostY.Options;
using static TownOfHostY.Translator;
using static TownOfHostY.Utils;
using TownOfHostY.Attributes;

namespace TownOfHostY.Roles.AddOns.Common;

public static class Sending
{
    private static readonly int Id = (int)offsetId.AddonBuff + 700;
    private static Color RoleColor = GetRoleColor(CustomRoles.Sending);
    public static string SubRoleMark = ColorString(RoleColor, "Sd");
    private static List<byte> playerIdList = new();

    private static PlayerControl ExiledPlayer = null;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Sending);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
        ExiledPlayer = null;
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static void OnExileWrapUp(PlayerControl exiled)
    {
        ExiledPlayer = exiled;
    }
    public static void OnStartMeeting()
    {
        ExiledPlayer = null;
    }
    public static string RealNameChange()
    {
        if (ExiledPlayer == null) return string.Empty;

        var ExiledPlayerName = ExiledPlayer.Data.PlayerName;
        if (ExiledPlayer.Is(CustomRoleTypes.Impostor))
            return ColorString(RoleColor, string.Format(GetString("isImpostor"), ExiledPlayerName));
        else
            return ColorString(RoleColor, string.Format(GetString("isNotImpostor"), ExiledPlayerName));
    }

    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}
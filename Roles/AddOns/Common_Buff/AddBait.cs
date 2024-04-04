using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Options;
using TownOfHostY.Attributes;

namespace TownOfHostY.Roles.AddOns.Common;

public static class AddBait
{
    private static readonly int Id = (int)offsetId.AddonBuff + 1100;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.AddBait);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｂ");
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.AddBait);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static void OnMurderPlayer(MurderInfo info)
    {
        if (info.IsMeeting) return;

        var (killer, target) = info.AttemptTuple;

        if (playerIdList.Contains(target.PlayerId) && !info.IsSuicide)
            _ = new LateTask(() =>
            {
                killer.CmdReportDeadBody(target.Data);
            }, 0.15f, "AddBait Self Report");
    }

    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}
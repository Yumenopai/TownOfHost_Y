using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Options;
using Unity.Services.Authentication.Internal;
using Hazel;
using System.Linq;
using TownOfHostY.Attributes;

namespace TownOfHostY.Roles.AddOns.Common;

public static class Guarding
{
    private static readonly int Id = 80900;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Guarding);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｇ");
    private static List<byte> playerIdList = new();

    /// <summary>
    /// ガード未使用の場合Listにいる
    /// </summary>
    public static List<byte> GuardingList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Guarding);
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
        if (!GuardingList.Contains(playerId))
            GuardingList.Add(playerId);
    }
    /// <summary>
    /// trueはキルが起こる
    /// </summary>
    public static bool OnCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        if (!GuardingList.Contains(target.PlayerId)) return true;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;

        killer.RpcGuardAndKill(target);
        GuardingList.Remove(target.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()}->{target.GetNameWithRole()}:ガード", "Guarding");
        info.CanKill = false;
        return true;
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}
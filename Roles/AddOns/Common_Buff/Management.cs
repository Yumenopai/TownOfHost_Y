using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Options;
using TownOfHostY.Attributes;

namespace TownOfHostY.Roles.AddOns.Common;

public static class Management
{
    private static readonly int Id = (int)offsetId.AddonBuff + 100;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Management);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｍ");
    private static List<byte> playerIdList = new();

    private static OptionItem OptionSeeNowtask;
    public static bool SeeNowtask;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Management);
        OptionSeeNowtask = BooleanOptionItem.Create(Id + 10, "ManagementSeeNowtask", true, TabGroup.Addons, false);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();

        SeeNowtask = OptionSeeNowtask.GetBool();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }

    public static string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (seer != seen) return "";

        // タスクステート取得
        (int completetask, int alltask) = Utils.GetTasksState();

        // 全体タスク数が見えるか
        bool canSee = (isForMeeting || !seer.IsAlive() || SeeNowtask) && !Utils.IsActive(SystemTypes.Comms);
        // 表示する現在のタスク数
        string nowtask = canSee ? completetask.ToString() : "?";

        return Utils.ColorString(Color.cyan, $"({nowtask}/{alltask})");
    }

    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}
using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Unit;
public sealed class JackOLantern : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JackOLantern),
            player => new JackOLantern(player),
            CustomRoles.JackOLantern,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Unit,
            (int)Options.offsetId.UnitSpecial + 0,
            //(int)Options.offsetId.UnitMix + 100,
            null,
            "ジャック・オー・ランタン",
            "#e5a323",
            assignInfo: new RoleAssignInfo(CustomRoles.JackOLantern, CustomRoleTypes.Unit)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = new CustomRoles[3] { CustomRoles.Jack, CustomRoles.jO, CustomRoles.Lantern }
            }
        );
    public JackOLantern(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    // ジャック
    public static PlayerControl jack = null;

    public static OptionItem OptionJackKillCooldown;
    public static OptionItem OptionJackRoleRemoveKillCount;
    public static OptionItem OptionjORoleDetectCooldown;
    public static OptionItem OptionjOCanDetectNeutral;
    public static OptionItem OptionjOSendRoleCount;
    public static OptionItem OptionjOCanVent;
    public static OptionItem OptionLanternVisionDuringFixLight;
    public static OptionItem OptionLanternRoleReceivedExcludeNormalCrewmate;

    public static HashSet<byte> SentPlayer = new(15); //ジャック側で初期化
    enum OptionName
    {
        JackKillCooldown,
        JackRoleRemoveKillCount,
        jORoleDetectCooldown,
        jOCanDetectNeutral,
        jOSendRoleCount,
        jOCanVent,
        LanternVisionDuringFixLight,
        LanternRoleReceivedExcludeNormalCrewmate
    }
    // 直接設置
    public static void SetupRoleOptions()
    {
        TextOptionItem.Create(41, "Head.LimitedTimeRoleH", TabGroup.UnitRoles)
            .SetColor(RoleInfo.RoleColor);
        var spawnOption = IntegerOptionItem.Create(RoleInfo.ConfigId, CustomRoles.JackOLantern.ToString(), new(0, 100, 10), 0, TabGroup.UnitRoles, false)
            .SetColor(RoleInfo.RoleColor)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.Standard) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(RoleInfo.ConfigId + 1, "Maximum", new(1, 1, 1), 1, TabGroup.UnitRoles, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Pair)
            .SetFixValue(true)
            .SetGameMode(CustomGameMode.Standard);

        Options.CustomRoleSpawnChances.Add(RoleInfo.RoleName, spawnOption);
        Options.CustomRoleCounts.Add(RoleInfo.RoleName, countOption);

        Dictionary<string, string> jackDic = new() { { "%jack%", Utils.ColorString(Palette.ImpostorRed, Utils.GetRoleName(CustomRoles.Jack)) } };
        Dictionary<string, string> joDic = new() { { "%jo%", Utils.ColorString(Palette.ImpostorRed, Utils.GetRoleName(CustomRoles.jO)) } };
        Dictionary<string, string> lanternDic = new() { { "%lantern%", Utils.ColorString(RoleInfo.RoleColor, Utils.GetRoleName(CustomRoles.Lantern)) } };

        // ジャック
        OptionJackKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.JackKillCooldown, new(5.0f, 180f, 2.5f), 30f, false).SetReplacementDictionary(jackDic)
            .SetValueFormat(OptionFormat.Seconds);
        OptionJackRoleRemoveKillCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.JackRoleRemoveKillCount, new(0, 15, 1), 2, false).SetReplacementDictionary(jackDic)
            .SetValueFormat(OptionFormat.Pieces);

        // オー
        OptionjORoleDetectCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.jORoleDetectCooldown, new(5.0f, 180f, 2.5f), 45f, false).SetReplacementDictionary(joDic)
            .SetValueFormat(OptionFormat.Seconds);
        OptionjOCanDetectNeutral = BooleanOptionItem.Create(RoleInfo, 13, OptionName.jOCanDetectNeutral, false, false).SetReplacementDictionary(joDic);
        OptionjOSendRoleCount = IntegerOptionItem.Create(RoleInfo, 14, OptionName.jOSendRoleCount, new(0, 15, 1), 2, false).SetReplacementDictionary(joDic)
            .SetValueFormat(OptionFormat.Pieces);
        OptionjOCanVent = BooleanOptionItem.Create(RoleInfo, 15, OptionName.jOCanVent, true, false).SetReplacementDictionary(joDic);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, CustomRoles.jO, RoleInfo.Tab, RoleInfo.RoleName, true);

        // ランタン
        OptionLanternVisionDuringFixLight = IntegerOptionItem.Create(RoleInfo, 17, OptionName.LanternVisionDuringFixLight, new(20, 80, 10), 50, false).SetReplacementDictionary(lanternDic)
            .SetValueFormat(OptionFormat.Percent);
        OptionLanternRoleReceivedExcludeNormalCrewmate = BooleanOptionItem.Create(RoleInfo, 18, OptionName.LanternRoleReceivedExcludeNormalCrewmate, false, false).SetReplacementDictionary(lanternDic);
    }
}

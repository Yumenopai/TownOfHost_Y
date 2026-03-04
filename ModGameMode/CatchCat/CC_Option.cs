using TownOfHostY.Roles.Core;
using TownOfHostY.Modules;
using UnityEngine;
using static Il2CppSystem.Uri;

namespace TownOfHostY.CatchCat;

static class Option
{
    public static readonly int Id = (int)Options.offsetId.GModeCC;
    public static OptionItem IgnoreReport;

    public static OptionItem LeaderIgnoreVent;
    public static OptionItem LeaderKilled;
    public static OptionItem LK_CatCount;
    public static OptionItem LK_OneGuard;

    public static OptionItem WhenColorCatKilled;
    public static OptionItem ColorCatShowSameCamp;
    public static OptionItem TaskCompleteAbility;
    public static OptionItem T_KnowAllLeader;
    public static OptionItem T_OneGuardOwn;
    public static OptionItem T_CanUseVent;
    public static OptionItem T_VentCooldown;
    public static OptionItem T_VentMaxTime;
    public static OptionItem T_OwnLeaderKillcoolDecrease;

    public static OptionItem M_LeaderRemain;
    public static OptionItem M_NeutralCatRemain;
    public static OptionItem M_RemainCatShowName;
    public static OptionItem M_RemainCatShowNameNum;
    public static OptionItem M_ColorCatCount;

    public enum ColorCatKill
    {
        CCatJustKill,
        COtherCatKill,
        CCatOverride,
        CCatAlwaysGuard,
    };

    public static void SetupCustomOption()
    {
        // 共通設定
        TextOptionItem.Create(Id + 1000, "CCCommonSetting", TabGroup.ModMainSettings)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.CatchCat);
        IgnoreReport = StringOptionItem.Create(Id + 1010, "IgnoreReport", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.CatchCat);

        // リーダー設定
        TextOptionItem.Create(Id + 2000, "CCLeaderSetting", TabGroup.ModMainSettings)
            .SetColor(Palette.Orange)
            .SetGameMode(CustomGameMode.CatchCat);
        SetupLeaderRoleOptions(Id + 2100, CustomRoles.CCRedLeader);
        SetupLeaderRoleOptions(Id + 2200, CustomRoles.CCBlueLeader);
        SetupAddLeaderRoleOptions(Id + 2300, CustomRoles.CCYellowLeader);

        LeaderIgnoreVent = StringOptionItem.Create(Id + 2400, "IgnoreLeaderVent", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetHeader(true)
            .SetColor(Palette.Orange)
            .SetGameMode(CustomGameMode.CatchCat);
        LeaderKilled = StringOptionItem.Create(Id + 2500, "CCLeaderKilled", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Palette.Orange)
            .SetGameMode(CustomGameMode.CatchCat);

        LK_CatCount = IntegerOptionItem.Create(Id + 2510, "CCLK_CatCount", new(0, 7, 1), 0, TabGroup.ModMainSettings, false)
            .SetParent(LeaderKilled)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.CatchCat);
        LK_OneGuard = StringOptionItem.Create(Id + 2520, "CCLK_OneGuard", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetParent(LeaderKilled)
            .SetGameMode(CustomGameMode.CatchCat);

        // 猫設定
        TextOptionItem.Create(Id + 3000, "CCCatSetting", TabGroup.ModMainSettings)
            .SetColor(Color.gray)
            .SetGameMode(CustomGameMode.CatchCat);
        WhenColorCatKilled = StringOptionItem.Create(Id + 3100, "CCWhenColorCatKilled", EnumHelper.GetAllNames<ColorCatKill>(), 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.gray)
            .SetGameMode(CustomGameMode.CatchCat);
        ColorCatShowSameCamp = StringOptionItem.Create(Id + 3200, "CCColorCatShowSameCamp", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.gray)
            .SetGameMode(CustomGameMode.CatchCat);

        TaskCompleteAbility = StringOptionItem.Create(Id + 3300, "CCTaskCompleteAbility", new string[] { "Off", "On" }, 1, TabGroup.ModMainSettings, false)
            .SetColor(Color.gray)
            .SetGameMode(CustomGameMode.CatchCat);

        T_KnowAllLeader = IntegerOptionItem.Create(Id + 3310, "CCT_KnowAllLeader", new(0, 100, 10), 0, TabGroup.ModMainSettings, false)
            .SetParent(TaskCompleteAbility)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.CatchCat);
        T_OwnLeaderKillcoolDecrease = IntegerOptionItem.Create(Id + 3320, "CCT_OwnLeaderKillcoolDecrease", new(0, 100, 10), 0, TabGroup.ModMainSettings, false)
            .SetParent(TaskCompleteAbility)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.CatchCat);
        T_OneGuardOwn = IntegerOptionItem.Create(Id + 3330, "CCT_OneGuardOwn", new(0, 100, 10), 0, TabGroup.ModMainSettings, false)
            .SetParent(TaskCompleteAbility)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.CatchCat);
        T_CanUseVent = IntegerOptionItem.Create(Id + 3340, "CCT_CanUseVent", new(0, 100, 10), 0, TabGroup.ModMainSettings, false)
            .SetParent(TaskCompleteAbility)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.CatchCat);

        T_VentCooldown = FloatOptionItem.Create(Id + 3341, "VentCooldown", new(5f, 60f, 2.5f), 20f, TabGroup.ModMainSettings, false)
            .SetParent(T_CanUseVent)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.CatchCat);
        T_VentMaxTime = FloatOptionItem.Create(Id + 3342, "VentMaxTime", new(1f, 10f, 1f), 3f, TabGroup.ModMainSettings, false)
            .SetParent(T_CanUseVent)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.CatchCat);

        // 会議表示
        TextOptionItem.Create(Id + 5000, "CCMeetingDisplay", TabGroup.ModMainSettings)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.CatchCat);
        M_LeaderRemain = StringOptionItem.Create(Id + 5010, "CCM_LeaderRemain", new string[] { "Off", "On" }, 1, TabGroup.ModMainSettings, false)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.CatchCat);
        M_NeutralCatRemain = StringOptionItem.Create(Id + 5020, "CCM_NeutralCatRemain", new string[] { "Off", "On" }, 1, TabGroup.ModMainSettings, false)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.CatchCat);
        M_RemainCatShowName = StringOptionItem.Create(Id + 5030, "CCM_RemainCatShowName", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.CatchCat);
        M_RemainCatShowNameNum = IntegerOptionItem.Create(Id + 5031, "CCM_RemainCatShowNameNum", new(1, 13, 1), 2, TabGroup.ModMainSettings, false)
            .SetParent(M_RemainCatShowName)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.CatchCat);
        M_ColorCatCount = StringOptionItem.Create(Id + 5040, "CCM_ColorCatCount", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.CatchCat);
    }

    private static void SetupLeaderRoleOptions(int id, CustomRoles role)
    {
        // spawnOption は IntegerOptionItem 型で確実に扱う
        IntegerOptionItem spawnOption = IntegerOptionItem.Create(id, role.ToString() + "Fixed", new(100, 100, 1), 100, TabGroup.ModMainSettings, false);
        spawnOption.SetColor(Utils.GetRoleColor(role))
                   .SetValueFormat(OptionFormat.Percent)
                   .SetFixValue(true)
                   .SetGameMode(CustomGameMode.CatchCat);

        IntegerOptionItem countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, TabGroup.ModMainSettings, false);
        countOption.SetParent(spawnOption)
                   .SetValueFormat(OptionFormat.Players)
                   .SetGameMode(CustomGameMode.CatchCat);

        Options.CustomRoleSpawnChances.Add(role, spawnOption);
        Options.CustomRoleCounts.Add(role, countOption);
    }

    private static void SetupAddLeaderRoleOptions(int id, CustomRoles role)
    {
        IntegerOptionItem spawnOption = IntegerOptionItem.Create(id, role.ToString(), new(0, 100, 100), 0, TabGroup.ModMainSettings, false);
        spawnOption.SetColor(Utils.GetRoleColor(role))
                   .SetValueFormat(OptionFormat.Percent)
                   .SetGameMode(CustomGameMode.CatchCat);

        IntegerOptionItem countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, TabGroup.ModMainSettings, false);
        countOption.SetParent(spawnOption)
                   .SetHidden(true)
                   .SetGameMode(CustomGameMode.CatchCat);

        Options.CustomRoleSpawnChances.Add(role, spawnOption);
        Options.CustomRoleCounts.Add(role, countOption);
    }


}

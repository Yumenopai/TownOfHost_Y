using System;
using System.Linq;
using System.Text;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Utils;
using static TownOfHostY.Translator;
using UnityEngine;
using System.Collections.Generic;
using TownOfHostY.Attributes;
using Epic.OnlineServices.Presence;
using static UnityEngine.GraphicsBuffer;
using AmongUs.GameOptions;

namespace TownOfHostY;

static class GameModeUtils
{
    private static readonly int Id = 210000;
    public static OptionItem IgnoreReport;
    public static OptionItem IgnoreVent;
    public static OptionItem LeaderKilled;
    public static OptionItem LeaderKilledTiming;
    public static OptionItem WhenColorCatKilled;

    public static OptionItem M_LeaderRemain;
    public static OptionItem M_NeutralCatRemain;
    public static OptionItem M_RemainCatShowName;
    public static OptionItem M_RemainCatShowNameNum;
    public static OptionItem M_ColorCatCount;

    public static OptionItem T_KnowAllLeader;
    public static OptionItem T_OneGuardOwn;
    public static OptionItem T_CanUseVent;
    public static OptionItem T_VentCooldown;
    public static OptionItem T_VentMaxTime;
    public static OptionItem T_OwnLeaderKillcoolDecrease;

    static LeaderKill NowLeaderKill;
    static ColorCatKill NowColorCatKill;

    private static Dictionary<byte, bool> CanGuard = new();
    private static Dictionary<byte, bool> CanVent = new();
    private static Dictionary<byte, bool[]> IsSet = new();

    enum LeaderKill
    {
        TimingAlways,
        TimingOneCatBeYourCamp,
        TimingAfterUsingOneGuard,
    };
    enum ColorCatKill
    {
        CCatJustKill,
        COtherCatKill,
        CCatOverride,
        CCatAlwaysGuard,
    };

    public static void SetupCustomOption()
    {
        SetupLeaderRoleOptions(Id, CustomRoles.CCRedLeader);
        SetupLeaderRoleOptions(Id + 100, CustomRoles.CCBlueLeader);
        SetupAddLeaderRoleOptions(Id + 200, CustomRoles.CCYellowLeader);

        IgnoreVent = BooleanOptionItem.Create(Id + 1000, "IgnoreVent", false, TabGroup.MainSettings, false)
            .SetHeader(true)
            .SetColor(Color.gray)
            .SetGameMode(CustomGameMode.CatchCat);
        IgnoreReport = BooleanOptionItem.Create(Id + 1001, "IgnoreReport", false, TabGroup.MainSettings, false)
            .SetColor(Color.gray)
            .SetGameMode(CustomGameMode.CatchCat);

        WhenColorCatKilled = StringOptionItem.Create(Id + 1002, "CCWhenColorCatKilled", EnumHelper.GetAllNames<ColorCatKill>(), 0, TabGroup.MainSettings, false)
            .SetHeader(true)
            .SetColor(Color.magenta)
            .SetGameMode(CustomGameMode.CatchCat);
        LeaderKilled = BooleanOptionItem.Create(Id + 1003, "CCLeaderKilled", false, TabGroup.MainSettings, false)
            .SetColor(Color.magenta)
            .SetGameMode(CustomGameMode.CatchCat);
        LeaderKilledTiming = StringOptionItem.Create(Id + 1004, "CCLeaderKilledTiming", EnumHelper.GetAllNames<LeaderKill>(), 0, TabGroup.MainSettings, false).SetParent(LeaderKilled)
            .SetGameMode(CustomGameMode.CatchCat);

        TextOptionItem.Create(Id + 1020, "CCMeetingDisplay", TabGroup.MainSettings)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.CatchCat);
        M_LeaderRemain = BooleanOptionItem.Create(Id + 1021, "CCM_LeaderRemain", true, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.CatchCat);
        M_NeutralCatRemain = BooleanOptionItem.Create(Id + 1022, "CCM_NeutralCatRemain", true, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.CatchCat);
        M_RemainCatShowName = BooleanOptionItem.Create(Id + 1023, "CCM_RemainCatShowName", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.CatchCat);
        M_RemainCatShowNameNum = IntegerOptionItem.Create(Id + 1024, "CCM_RemainCatShowNameNum", new(1, 13, 1), 2, TabGroup.MainSettings, false).SetParent(M_RemainCatShowName)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.CatchCat);
        M_ColorCatCount = BooleanOptionItem.Create(Id + 1025, "CCM_ColorCatCount", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.CatchCat);

        TextOptionItem.Create(Id + 1030, "CCTaskCompleteAbility", TabGroup.MainSettings)
            .SetColor(Color.green)
            .SetGameMode(CustomGameMode.CatchCat);
        T_KnowAllLeader = IntegerOptionItem.Create(Id + 1031, "CCT_KnowAllLeader", new(0, 100, 10), 0, TabGroup.MainSettings, false)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.CatchCat);
        T_OneGuardOwn = IntegerOptionItem.Create(Id + 1032, "CCT_OneGuardOwn", new(0, 100, 10), 0, TabGroup.MainSettings, false)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.CatchCat);
        T_CanUseVent = IntegerOptionItem.Create(Id + 1033, "CCT_CanUseVent", new(0, 100, 10), 0, TabGroup.MainSettings, false)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.CatchCat);
        T_VentCooldown = FloatOptionItem.Create(Id + 1034, "VentCooldown", new(5f, 60f, 2.5f), 20f, TabGroup.MainSettings, false).SetParent(T_CanUseVent)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.CatchCat);
        T_VentMaxTime = FloatOptionItem.Create(Id + 1035, "VentMaxTime", new(1f, 10f, 1f), 3f, TabGroup.MainSettings, false).SetParent(T_CanUseVent)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.CatchCat);
        T_OwnLeaderKillcoolDecrease = IntegerOptionItem.Create(Id + 1036, "CCT_OwnLeaderKillcoolDecrease", new(0, 100, 10), 0, TabGroup.MainSettings, false)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.CatchCat);
    }
    public static void SetupLeaderRoleOptions(int id, CustomRoles role)
    {
        var spawnOption = IntegerOptionItem.Create(id, role.ToString() + "Fixed", new(100, 100, 1), 100, TabGroup.MainSettings, false).SetColor(Utils.GetRoleColor(role))
            .SetValueFormat(OptionFormat.Percent)
            .SetHeader(true)
            .SetFixValue(true)
            .SetGameMode(CustomGameMode.CatchCat) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, TabGroup.MainSettings, false).SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.CatchCat);

        Options.CustomRoleSpawnChances.Add(role, spawnOption);
        Options.CustomRoleCounts.Add(role, countOption);
    }
    public static void SetupAddLeaderRoleOptions(int id, CustomRoles role)
    {
        var spawnOption = IntegerOptionItem.Create(id, role.ToString(), new(0, 100, 100), 0, TabGroup.MainSettings, false).SetColor(Utils.GetRoleColor(role))
            .SetValueFormat(OptionFormat.Percent)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.CatchCat) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, TabGroup.MainSettings, false).SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(CustomGameMode.CatchCat);

        Options.CustomRoleSpawnChances.Add(role, spawnOption);
        Options.CustomRoleCounts.Add(role, countOption);
    }

    [GameModuleInitializer]
    public static void Init()
    {
        CanGuard.Clear();
        CanVent.Clear();
        IsSet.Clear();
    }
    public static void Add(PlayerControl pc)
    {
        NowLeaderKill = (LeaderKill)LeaderKilledTiming.GetValue();
        NowColorCatKill = (ColorCatKill)WhenColorCatKilled.GetValue();

        if (pc.GetCustomRole().IsCCLeaderRoles()) CanGuard.Add(pc.PlayerId, true);
        else
        {
            CanGuard.Add(pc.PlayerId, false);
            CanVent.Add(pc.PlayerId, false);
        }
        IsSet.Add(pc.PlayerId, new bool[] { false, false, false, false });
    }

    public static float CalculateKillCooldown(PlayerControl pc)
    {
        float decrease = 0;
        if (T_OwnLeaderKillcoolDecrease.GetInt() != 0)
        {
            foreach (var cat in Main.AllPlayerControls.Where(p => p.GetCustomRole().IsCCColorCatRoles()))
            {
                if (IsSameCamp_LederCat(pc.GetCustomRole(), cat.GetCustomRole()))
                {
                    var ts = cat.GetPlayerTaskState();
                    var per = ts.CompletedTasksCount / ts.AllTasksCount * 100;
                    if (per >= T_OwnLeaderKillcoolDecrease.GetInt())
                    {
                        Logger.Info($"KillCooldown decrease[{pc.GetRealName()}]", "KillCooldown");
                        decrease -= 1;
                    }
                }
            }
        }
        Logger.Info($"KillCooldown decrease : {decrease} ", "KillCooldown");
        return Options.DefaultKillCooldown - decrease;
    }

    // Leader killed
    public static void OnCheckMurderByLeader(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        var targetRole = target.GetCustomRole();
        if (!targetRole.IsCCLeaderRoles()) return;

        if (LeaderKilled.GetBool())
        {
            switch (NowLeaderKill)
            {
                case LeaderKill.TimingAlways: return;   // 無条件でキルされる

                case LeaderKill.TimingOneCatBeYourCamp:
                    foreach(var pc in Main.AllPlayerControls)
                    {
                        // 同陣営の猫がいるのでキルされる
                        if (IsSameCamp_LederCat(targetRole, pc.GetCustomRole())) return;
                    }
                    break;

                case LeaderKill.TimingAfterUsingOneGuard:
                    if (!CanGuard[target.PlayerId]) return;    // 使用済みなのでキルされる

                    CanGuard[target.PlayerId] = false;
                    break;
            }
        }
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
        info.CanKill = false;
        return;
    }

    // Cat killed
    public static void OnCheckMurderByCat(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        var targetRole = target.GetCustomRole();
        if (!targetRole.IsCCCatRoles()) return;

        if (targetRole.IsCCColorCatRoles() && !CanGuard[target.PlayerId])         // 所属済み猫
        {
            switch (NowColorCatKill)
            {
                case ColorCatKill.CCatJustKill:     // 無条件でキルされる
                    return;

                case ColorCatKill.COtherCatKill:    // 同陣営はキルできない
                    if (IsSameCamp_LederCat(killer.GetCustomRole(), targetRole)) info.DoKill = false;
                    return;

                case ColorCatKill.CCatOverride:     // 同陣営はキルできない
                    if (IsSameCamp_LederCat(killer.GetCustomRole(), targetRole))
                    {
                        info.DoKill = false; return;
                    }
                    break;  // 同陣営でない猫には上書き
            }
        }

        // 互いにパリン
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
        info.CanKill = false;

        // ただのガードなのでここで返す
        if (targetRole.IsCCColorCatRoles() && CanGuard[target.PlayerId])
        {
            CanGuard[target.PlayerId] = false; return;
        }
        if (targetRole.IsCCColorCatRoles() && NowColorCatKill == ColorCatKill.CCatAlwaysGuard) return;

        // 元所属陣営の色を消す
        if (!target.Is(CustomRoles.CCNoCat))
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                if (IsSameCamp_LederCat(pc.GetCustomRole(), targetRole))
                {
                    NameColorManager.Remove(pc.PlayerId, target.PlayerId);
                }
            }
        }

        // 役職変化
        switch (killer.GetCustomRole())
        {
            case CustomRoles.CCRedLeader:
                target.RpcSetCustomRole(CustomRoles.CCRedCat);
                break;
            case CustomRoles.CCBlueLeader:
                target.RpcSetCustomRole(CustomRoles.CCBlueCat);
                break;
            case CustomRoles.CCYellowLeader:
                target.RpcSetCustomRole(CustomRoles.CCYellowCat);
                break;
        }
        NameColorManager.Add(killer.PlayerId, target.PlayerId);
        NameColorManager.Add(target.PlayerId, killer.PlayerId);

        NotifyRoles();
        MarkEveryoneDirtySettings();
        return;
    }

    // Task
    public static void OnCompleteTask(PlayerControl pc, TaskState ts)
    {
        int per = (int)(((float)ts.CompletedTasksCount / ts.AllTasksCount) * 100);
        Logger.Info($"{pc.GetRealName()} 完了タスク: {ts.CompletedTasksCount} / {ts.AllTasksCount} * 100 => {per}%", "OnCompleteTask");

        if (!IsSet[pc.PlayerId][0] && T_KnowAllLeader.GetInt() != 0 && per >= T_KnowAllLeader.GetInt())
        {
            foreach (var leader in Main.AllPlayerControls.Where(p => p.GetCustomRole().IsCCLeaderRoles()))
            {
                NameColorManager.Add(pc.PlayerId, leader.PlayerId);
                Logger.Info($"NameColorManager.Add({pc.GetRealName()}, {leader.GetRealName()})", "OnCompleteTask");
            }
            IsSet[pc.PlayerId][0] = true;
        }

        if (!IsSet[pc.PlayerId][1] && T_OneGuardOwn.GetInt() != 0 && per >= T_OneGuardOwn.GetInt())
        {
            CanGuard[pc.PlayerId] = true;
            Logger.Info($"CanGuard[{pc.GetRealName()}] : true", "OnCompleteTask");
            IsSet[pc.PlayerId][1] = true;
        }

        if (!IsSet[pc.PlayerId][2] && T_CanUseVent.GetInt() != 0 && per >= T_CanUseVent.GetInt())
        {
            CanVent[pc.PlayerId] = true;
            Logger.Info($"CanVent[{pc.GetRealName()}] : true", "OnCompleteTask");
            pc.MarkDirtySettings();
            pc.RpcResetAbilityCooldown();
            IsSet[pc.PlayerId][2] = true;
        }

        if (!IsSet[pc.PlayerId][3] && T_OwnLeaderKillcoolDecrease.GetInt() != 0 && per >= T_OwnLeaderKillcoolDecrease.GetInt())
        {
            Logger.Info($"OwnLeaderKillcoolDecrease[{pc.GetRealName()}] : true", "OnCompleteTask");
            IsSet[pc.PlayerId][3] = true;
        }
    }

    //Vent
    public static bool CanUseVent(PlayerControl pc)
        => pc.IsAlive()
        && CanVent[pc.PlayerId];
    public static void ApplyGameOptions(PlayerControl pc, IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = CanUseVent(pc) ? T_VentCooldown.GetFloat() : 255f;
        AURoleOptions.EngineerInVentMaxTime = T_VentMaxTime.GetFloat();
    }

    public static string GetMark(PlayerControl pc)
    {
        string mark = string.Empty;
        if (IsSet[pc.PlayerId][0])
            mark += ColorString(Color.yellow, "Ｌ");
        if (CanGuard[pc.PlayerId])
            mark += ColorString(Color.cyan, "Ｇ");
        if (IsSet[pc.PlayerId][2])
            mark += ColorString(Color.green, "Ｖ");
        if (IsSet[pc.PlayerId][3])
            mark += ColorString(GetRoleColor(pc.GetCustomRole()), "Ｋ");
        return mark;
    }


    // meeting
    public static void CCMeetingInfomation()
    {
        int[] counts = CountLivingPlayersByPredicates(
            pc => pc.Is(CustomRoles.CCRedLeader),
            pc => pc.Is(CustomRoles.CCBlueLeader),
            pc => pc.Is(CustomRoles.CCYellowLeader),
            pc => pc.Is(CustomRoles.CCNoCat),
            pc => pc.Is(CustomRoles.CCRedCat),
            pc => pc.Is(CustomRoles.CCBlueCat),
            pc => pc.Is(CustomRoles.CCYellowCat)
        );
        int Leader = counts[0] + counts[1] + counts[2];
        int NoCat = counts[3];
        int RedTeam = counts[0] + counts[4];
        int BlueTeam = counts[1] + counts[5];
        int YellowTeam = counts[2] + counts[6];

        string title = $"<color=#f8cd46>{GetString("CCMidwayResultsTitle")}</color>";

        var sb = new StringBuilder();
        bool needNewline = false;
        bool[] isShow = new bool[] { true, true, true, true };
        while (true)
        {
            if (needNewline) sb.Append("\n");

            if (M_LeaderRemain.GetBool() && isShow[0])
            {
                sb.Append(string.Format(GetString("Message.CCMidwayResultsLeader"), Leader));
                needNewline = true; isShow[0] = false; continue;
            }
            if (M_NeutralCatRemain.GetBool() && isShow[1])
            {
                sb.Append(string.Format(GetString("Message.CCMidwayResultsNeutralCat"), NoCat));
                needNewline = true; isShow[1] = false; continue;
            }
            if (M_RemainCatShowName.GetBool() && NoCat <= M_RemainCatShowNameNum.GetInt() && NoCat != 0 && isShow[2])
            {
                if (needNewline) sb.Append("--------------------------------------------------------\n");
                sb.Append(GetString("Message.CCMidwayResultsNeutralCatName"));
                foreach(var pc in Main.AllAlivePlayerControls.Where(p => p.Is(CustomRoles.CCNoCat)))
                {
                    sb.Append($"\n・{pc.GetRealName()}");
                }
                needNewline = true; isShow[2] = false; continue;
            }
            if ((M_ColorCatCount.GetBool() || NoCat == 0) && isShow[3])
            {
                if (needNewline) sb.Append("--------------------------------------------------------\n");
                sb.Append(string.Format(GetString("Message.CCMidwayResultsRedBlue"), RedTeam, BlueTeam));
                if (CustomRoles.CCYellowLeader.IsEnable())
                    sb.Append("\n").Append(string.Format(GetString("Message.CCMidwayResultsYellow"), YellowTeam));
                if (NoCat == 0)
                    sb.Append("\n\n").Append(GetString("Message.CCMidwayResultsSuddenDeath"));
                needNewline = true; isShow[3] = false; continue;
            }
            break;
        }
        SendMessage(sb.ToString(), title: title);
        Logger.Info("リーダー" + Leader + "人生存中。無陣営猫残り" + NoCat + "人", "MidwayResults");
    }

    // /nコマンド
    public static void CCSetting(StringBuilder sb)
    {
        sb.Append(GetString("Leader")).Append(":");
        sb.AppendFormat("\n{0}×{1}", GetRoleName(CustomRoles.CCRedLeader), CustomRoles.CCRedLeader.GetCount());
        sb.AppendFormat("\n{0}×{1}", GetRoleName(CustomRoles.CCBlueLeader), CustomRoles.CCBlueLeader.GetCount());
        if (CustomRoles.CCYellowLeader.IsEnable()) sb.AppendFormat("\n{0}×{1}", GetRoleName(CustomRoles.CCYellowLeader), CustomRoles.CCYellowLeader.GetCount());
        sb.Append("\n--------------------------------------------------------\n");
        sb.Append("<line-height=1.7pic>").Append(GetString("Settings")).Append(":\n<size=80%>");

        sb.Append($" {IgnoreVent.GetName(true)}: {IgnoreVent.GetString()}\n");
        sb.Append($" {IgnoreReport.GetName(true)}: {IgnoreReport.GetString()}\n");
        sb.Append($" {WhenColorCatKilled.GetName(true)}: {WhenColorCatKilled.GetString()}\n");
        sb.Append($" {LeaderKilled.GetName(true)}: {LeaderKilled.GetString()}\n");
        if (LeaderKilled.GetBool())
            sb.Append($" ┗ {LeaderKilledTiming.GetName(true)}: {LeaderKilledTiming.GetString()}\n");

        sb.Append($"\n<size=90%>{GetString("CCMeetingDisplay")}：\n</size>");
        if (M_LeaderRemain.GetBool())
            sb.Append($" ・{M_LeaderRemain.GetName(true)}\n");
        if (M_NeutralCatRemain.GetBool())
            sb.Append($" ・{M_NeutralCatRemain.GetName(true)}\n");
        if (M_RemainCatShowName.GetBool())
            sb.Append($" ・{M_RemainCatShowName.GetName(true)}\n")
                .Append($" 　┗ {M_RemainCatShowNameNum.GetName(true)}: {M_RemainCatShowNameNum.GetString()}\n");
        if (M_ColorCatCount.GetBool())
            sb.Append($" ・{M_ColorCatCount.GetName(true)}\n");

        sb.Append($"\n<size=90%>{GetString("CCTaskCompleteAbility")}：\n</size>");
        for (int i = 10; i <= 100; i += 10)
        {
            if (i == T_KnowAllLeader.GetInt())
                sb.Append($" ・{T_KnowAllLeader.GetName(true)}: {T_KnowAllLeader.GetString()}\n");
            if (i == T_OneGuardOwn.GetInt())
                sb.Append($" ・{T_OneGuardOwn.GetName(true)}: {T_OneGuardOwn.GetString()}\n");
            if (i == T_CanUseVent.GetInt())
                sb.Append($" ・{T_CanUseVent.GetName(true)}: {T_CanUseVent.GetString()}\n")
                    .Append($" 　┣ {T_VentCooldown.GetName(true)}: {T_VentCooldown.GetString()}\n")
                    .Append($" 　┗ {T_VentMaxTime.GetName(true)}: {T_VentMaxTime.GetString()}\n");
            if (i == T_OwnLeaderKillcoolDecrease.GetInt())
                sb.Append($" ・{T_OwnLeaderKillcoolDecrease.GetName(true)}: {T_OwnLeaderKillcoolDecrease.GetString()}\n");
        }
        sb.Append($"</line-height>\n");
        foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && ((x.Id >= 100000 && x.Id < Id) || x.Id >= Id + 10000) && !x.IsHiddenOn(Options.CurrentGameMode)))
        {
            if (Options.NotShowOption(opt.Name)) continue;

            if (opt.Name is "NameChangeMode" && Options.GetNameChangeModes() != NameChange.None)
                sb.Append($"\n【{opt.GetName(true)}】 ：{opt.GetString()}\n");
            else
                sb.Append($"\n【{opt.GetName(true)}】\n");

            sb.Append("<size=65%><line-height=1.5pic>");
            ShowChildrenSettings(opt, ref sb);
            sb.Append("</size></line-height>");
        }
    }

    // /h nコマンド
    public static void CCSettingHelp(byte PlayerId)
    {
        SendMessage(GetString("CCInfoStart") + ":", PlayerId);

        var sb = new StringBuilder();
        sb.Append(GetString("CCInfo1LeaderHeader")).Append(GetString("CCInfo1MakeCat")).Append("<color=#000080>");
        if (LeaderKilled.GetBool())
        {
            sb.Append(GetString("CCInfo1LeaderEachOther"));
            if((LeaderKill)LeaderKilledTiming.GetValue() == LeaderKill.TimingOneCatBeYourCamp)
                sb.Append(GetString("CCInfo1LeaderKillOneCat"));
            if ((LeaderKill)LeaderKilledTiming.GetValue() == LeaderKill.TimingAfterUsingOneGuard)
                sb.Append(GetString("CCInfo1LeaderKillOneGuard"));
        }
        else sb.Append(GetString("CCInfo1LeaderNotEachOther"));
        sb.Append("</color>").Append(GetString("CCInfo1SameColorCat")).Append("<color=#000080>");

        switch((ColorCatKill)WhenColorCatKilled.GetValue())
        {
            case ColorCatKill.CCatOverride:
                sb.Append(GetString("CCInfo1OverrideKillOtherCat")).Append(GetString("CCInfo1OwnCatDontKill"));
                break;
            case ColorCatKill.CCatAlwaysGuard:
                sb.Append(GetString("CCInfo1CannotKillOtherCat"));
                break;
            case ColorCatKill.CCatJustKill:
                sb.Append(GetString("CCInfo1CanKillColorCat")).Append(GetString("CCInfo1CanKillColorCatAllKill"));
                break;
            case ColorCatKill.COtherCatKill:
                sb.Append(GetString("CCInfo1CanKillColorCat")).Append(GetString("CCInfo1OwnCatDontKill"));
                break;
        }
        sb.Append("</color>").Append(string.Format(GetString("CCInfo1LeaderVision"), ColorString(Color.blue, Main.NormalOptions.ImpostorLightMod.ToString()))).Append("<color=#000080>");
        if (IgnoreVent.GetBool()) sb.Append(GetString("CCInfo1LeaderCannotVent"));
        else sb.Append(GetString("CCInfo1LeaderCanVent"));
        sb.Append("</color>");

        SendMessage(sb.ToString(), PlayerId, "ㅤ");
        sb.Clear();

        sb.Append(GetString("CCInfo2CatHeader")).Append(GetString("CCInfo2BelongsCamp")).Append(GetString("CCInfo2CatTask"));
        sb.Append(string.Format(GetString("CCInfo2CatVision"), ColorString(Color.blue, Main.NormalOptions.CrewLightMod.ToString())));
        //SettingName
        sb.Append($"\n\n{GetString("CCTaskCompleteAbility")}：");
        for (int i = 10; i <= 100; i += 10)
        {
            if (i == T_KnowAllLeader.GetInt())
                sb.Append($"\n ▷{T_KnowAllLeader.GetName(true)}: {T_KnowAllLeader.GetString()}");
            if (i == T_OneGuardOwn.GetInt())
                sb.Append($"\n ▷{T_OneGuardOwn.GetName(true)}: {T_OneGuardOwn.GetString()}");
            if (i == T_CanUseVent.GetInt())
                sb.Append($"\n ▷{T_CanUseVent.GetName(true)}: {T_CanUseVent.GetString()}")
                    .Append($"\n 　┣ {T_VentCooldown.GetName(true)}: {T_VentCooldown.GetString()}")
                    .Append($"\n 　┗ {T_VentMaxTime.GetName(true)}: {T_VentMaxTime.GetString()}");
            if (i == T_OwnLeaderKillcoolDecrease.GetInt())
                sb.Append($"\n ▷{T_OwnLeaderKillcoolDecrease.GetName(true)}: {T_OwnLeaderKillcoolDecrease.GetString()}");
        }
        SendMessage(sb.ToString(), PlayerId, "ㅤ");
        sb.Clear();

        sb.Append(GetString("CCInfo3DecisionHeader")).Append(GetString("CCInfo3WinMostMember"));
        if (LeaderKilled.GetBool()) sb.Append("<color=#000080>").Append(GetString("CCInfo3WinRemainLeader")).Append("</color>");

        sb.Append(GetString("CCInfo3Meeting"));
        if (IgnoreReport.GetBool()) sb.Append("<color=#000080>").Append(GetString("CCInfo3DontReport")).Append("</color>");
        //SettingName
        if (M_LeaderRemain.GetBool())
            sb.Append($"\n ▷{M_LeaderRemain.GetName(true)}");
        if (M_NeutralCatRemain.GetBool())
            sb.Append($"\n ▷{M_NeutralCatRemain.GetName(true)}");
        if (M_RemainCatShowName.GetBool())
            sb.Append($"\n ▷{M_RemainCatShowName.GetName(true)}")
                .Append($"\n 　┗ {M_RemainCatShowNameNum.GetName(true)}: {M_RemainCatShowNameNum.GetString()}");
        if (M_ColorCatCount.GetBool())
            sb.Append($"\n ▷{M_ColorCatCount.GetName(true)}");

        SendMessage(sb.ToString(), PlayerId, "ㅤ");
        sb.Clear();

        sb.Append(GetString("CCInfo4KillCooldown")).Append(GetString("CCInfo4Prohibitions"));
        SendMessage(sb.ToString(), PlayerId, "ㅤ");
    }


    /// <summary>leaderRoleとcatRoleが同じ陣営であるかを返す</summary>
    public static bool IsSameCamp_LederCat(CustomRoles leaderRole, CustomRoles catRole)
    {
        switch (leaderRole)
        {
            case CustomRoles.CCRedLeader:
                if (catRole == CustomRoles.CCRedCat) return true;
                break;
            case CustomRoles.CCBlueLeader:
                if (catRole == CustomRoles.CCBlueCat) return true;
                break;
            case CustomRoles.CCYellowLeader:
                if (catRole == CustomRoles.CCYellowCat) return true;
                break;
        }
        return false;
    }

    /// <summary>各条件に合ったプレイヤーの人数を取得し、配列に同順で格納します。</summary>
    public static int[] CountLivingPlayersByPredicates(params Predicate<PlayerControl>[] predicates)
    {
        int[] counts = new int[predicates.Length];
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            for (int i = 0; i < predicates.Length; i++)
            {
                if (predicates[i](pc)) counts[i]++;
            }
        }
        return counts;
    }
}

using System.Linq;
using System.Text;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Utils;
using static TownOfHostY.Translator;
using static TownOfHostY.CatchCat.Option;
using static TownOfHostY.CatchCat.Common;
using UnityEngine;

namespace TownOfHostY.CatchCat;

static class Infomation
{
    // Meeting
    public static void ShowMeeting()
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
            if (needNewline) sb.Append('\n');

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
                foreach (var pc in Main.AllAlivePlayerControls.Where(p => p.Is(CustomRoles.CCNoCat)))
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
                    sb.Append('\n').Append(string.Format(GetString("Message.CCMidwayResultsYellow"), YellowTeam));
                if (NoCat == 0)
                    sb.Append("\n\n").Append(GetString("Message.CCMidwayResultsSuddenDeath"));
                needNewline = true; isShow[3] = false; continue;
            }
            break;
        }
        SendMessageAutoSplit(sb.ToString(), true, title);
        Logger.Info("リーダー" + Leader + "人生存中。無陣営猫残り" + NoCat + "人", "MidwayResults");
    }

    // /nコマンド
    public static void ShowSetting(StringBuilder sb)
    {
        sb.Append(GetString("Leader")).Append(':');
        sb.AppendFormat("\n{0}×{1}", GetRoleName(CustomRoles.CCRedLeader), CustomRoles.CCRedLeader.GetCount());
        sb.AppendFormat("\n{0}×{1}", GetRoleName(CustomRoles.CCBlueLeader), CustomRoles.CCBlueLeader.GetCount());
        if (CustomRoles.CCYellowLeader.IsEnable()) sb.AppendFormat("\n{0}×{1}", GetRoleName(CustomRoles.CCYellowLeader), CustomRoles.CCYellowLeader.GetCount());
        sb.Append("\n--------------------------------------------------------\n");
        sb.Append("<line-height=1.7pic>").Append(GetString("Settings")).Append(":\n<size=80%>");

        // common
        sb.Append($" {IgnoreReport.GetName(true)}: {IgnoreReport.GetString()}\n");

        // leader
        sb.Append($"\n {LeaderIgnoreVent.GetName(true)}: {LeaderIgnoreVent.GetString()}\n");
        sb.Append($" {LeaderKilled.GetName(true)}: {LeaderKilled.GetString()}\n");
        if (LeaderKilled.GetBool())
        {
            if(LK_CatCount.GetBool()) sb.Append($" ┣ {LK_CatCount.GetName(true)}: {LK_CatCount.GetString()}\n");
            sb.Append($" ┗ {LK_OneGuard.GetName(true)}: {LK_OneGuard.GetString()}\n");
        }

        // cat
        sb.Append($"\n {WhenColorCatKilled.GetName(true)}: {WhenColorCatKilled.GetString()}\n");
        sb.Append($" {GetString("CCTaskCompleteAbility")}: {TaskCompleteAbility.GetString()}\n");
        if (TaskCompleteAbility.GetBool())
            for (int i = 10; i <= 100; i += 10)
            {
                if (i == T_CanUseVent.GetInt())
                    sb.Append($" ・{T_CanUseVent.GetName(true)}: <color=#000080>{T_CanUseVent.GetString()}</color>\n")
                        .Append($" 　┣ {T_VentCooldown.GetName(true)}: {T_VentCooldown.GetString()}\n")
                        .Append($" 　┗ {T_VentMaxTime.GetName(true)}: {T_VentMaxTime.GetString()}\n");
                if (i == T_OneGuardOwn.GetInt())
                    sb.Append($" ・{T_OneGuardOwn.GetName(true)}: <color=#000080>{T_OneGuardOwn.GetString()}</color>\n");
                if (i == T_KnowAllLeader.GetInt())
                    sb.Append($" ・{T_KnowAllLeader.GetName(true)}: <color=#000080>{T_KnowAllLeader.GetString()}</color>\n");
                if (i == T_OwnLeaderKillcoolDecrease.GetInt())
                    sb.Append($" ・{T_OwnLeaderKillcoolDecrease.GetName(true)}: <color=#000080>{T_OwnLeaderKillcoolDecrease.GetString()}</color>\n");
            }

        // meeting
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
    public static void ShowSettingHelp(byte PlayerId)
    {
        SendMessageAutoSplit(GetString("CCInfoStart") + ":", true, sendTo: PlayerId);

        var sb = new StringBuilder();
        sb.Append(GetString("CCInfo1LeaderHeader")).Append(GetString("CCInfo1MakeCat")).Append("<color=#000080>");
        if (LeaderKilled.GetBool())
        {
            sb.Append(GetString("CCInfo1LeaderEachOther"));
            if (LK_CatCount.GetBool())
                sb.Append(GetString("CCInfo1LeaderKillAFewCat")).Append($"\n {LK_CatCount.GetName(true)} : {LK_CatCount.GetString()}");
            if (LK_OneGuard.GetBool())
                sb.Append(GetString("CCInfo1LeaderKillOneGuard"));
        }
        else sb.Append(GetString("CCInfo1LeaderNotEachOther"));
        sb.Append("</color>").Append(GetString("CCInfo1SameColorCat")).Append("<color=#000080>");

        switch ((ColorCatKill)WhenColorCatKilled.GetValue())
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
        if (LeaderIgnoreVent.GetBool()) sb.Append(GetString("CCInfo1LeaderCannotVent"));
        else sb.Append(GetString("CCInfo1LeaderCanVent"));
        sb.Append("</color>");
        SendMessageAutoSplit(sb.ToString(), false, sendTo: PlayerId);
        sb.Clear();

        sb.Append(GetString("CCInfo2CatHeader")).Append(GetString("CCInfo2BelongsCamp"));
        sb.Append(string.Format(GetString("CCInfo2CatVision"), ColorString(Color.blue, Main.NormalOptions.CrewLightMod.ToString())))
            .Append("<color=#000080>");

        if (TaskCompleteAbility.GetBool())
        {
            sb.Append(GetString("CCInfo2TaskCompleteAbility"));
            //SettingName
            for (int i = 10; i <= 100; i += 10)
            {
                if (i == T_CanUseVent.GetInt())
                    sb.Append($"\n ▷{T_CanUseVent.GetName(true)}: {T_CanUseVent.GetString()}")
                        .Append($"\n 　┣ {T_VentCooldown.GetName(true)}: {T_VentCooldown.GetString()}")
                        .Append($"\n 　┗ {T_VentMaxTime.GetName(true)}: {T_VentMaxTime.GetString()}");
                if (i == T_OneGuardOwn.GetInt())
                    sb.Append($"\n ▷{T_OneGuardOwn.GetName(true)}: {T_OneGuardOwn.GetString()}");
                if (i == T_KnowAllLeader.GetInt())
                    sb.Append($"\n ▷{T_KnowAllLeader.GetName(true)}: {T_KnowAllLeader.GetString()}");
                if (i == T_OwnLeaderKillcoolDecrease.GetInt())
                    sb.Append($"\n ▷{T_OwnLeaderKillcoolDecrease.GetName(true)}: {T_OwnLeaderKillcoolDecrease.GetString()}");
            }
        }
        else sb.Append(GetString("CCInfo2CatTask"));
        sb.Append("</color>");
        SendMessageAutoSplit(sb.ToString(), false, sendTo: PlayerId);
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

        SendMessageAutoSplit(sb.ToString(), false, sendTo: PlayerId);
        sb.Clear();

        sb.Append(GetString("CCInfo4KillCooldown")).Append($"\n<color=#000080>{GetString("KillCooldown")}：{Options.DefaultKillCooldown}s</color>")
            .Append(GetString("CCInfo4Prohibitions"));
        SendMessageAutoSplit(sb.ToString(),  false, sendTo: PlayerId);
    }
}
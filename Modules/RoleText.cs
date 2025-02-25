using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.Core;

namespace TownOfHostY;
public static class RoleText
{
    /// <summary>
    /// GetDisplayRoleNameDataからRoleNameを構築
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <returns>構築されたRoleName</returns>
    public static string GetDisplayRoleName(bool isMeeting, PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;
        //デフォルト値
        bool enabled = seer == seen
            || seen.Is(CustomRoles.GM)
            || (Main.VisibleTasksCount && !seer.IsAlive() && !Options.GhostCantSeeOtherRoles.GetBool());
        //オーバーライドによる表示ではサブロールは見えないようにする/上記場合のみ表示
        var roleText = GetRoleNameText(seen.PlayerId, showSubRole: enabled);

        //seen側による変更
        seen.GetRoleClass()?.OverrideDisplayRoleNameAsSeen(seer, isMeeting, ref enabled, ref roleText);
        Revealer.OverrideDisplayRoleNameAsSeen(seen, isMeeting, ref enabled, ref roleText);

        //seer側による変更
        seer.GetRoleClass()?.OverrideDisplayRoleNameAsSeer(seen, isMeeting, ref enabled, ref roleText);

        return enabled ? roleText : "";
    }
    /// <summary>
    /// GetTeamMarkから取得
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <returns>TeamMark</returns>
    public static string GetDisplayTeamMark(PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;
        bool enabled = false;

        // 陣営表示ONのとき
        if (Options.DisplayTeamMark.GetBool())
        {
            enabled = seer == seen // 自分自身はtrue
                || seen.Is(CustomRoles.GM) // GMはtrue
                || (Main.VisibleTasksCount && !seer.IsAlive() && !Options.GhostCantSeeOtherRoles.GetBool()); // 幽霊で役職見れるとき
        }

        // 幽霊が陣営のみ見れる設定ならtrue
        enabled |= Main.VisibleTasksCount && !seer.IsAlive()
            && Options.GhostCantSeeOtherRoles.GetBool() && Options.GhostCanSeeOtherTeams.GetBool();

        return enabled ? Utils.GetTeamMark(seen.GetCustomRole(), 90) : "";
    }
    /// <summary>
    /// 引数の指定通りのRoleNameを表示
    /// </summary>
    /// <returns>RoleNameを構築する色とテキスト(Color, string)</returns>
    public static string GetRoleNameText(byte playerId, bool allWrite = false, bool showSubRole = true)
    {
        StringBuilder roleText = new();

        // 全表示の時、showSubRoleはオフにする
        if (allWrite) showSubRole = false;
        // 属性表示時、本家TOH使用で書くか
        var isTOHDisplay = Options.GetAddonShowModes() == AddonShowMode.TOH;

        // 属性リスト
        var subRolesList = PlayerState.GetByPlayerId(playerId).SubRoles;
        // Addonが先に表示されるので前に持ってくる
        if (subRolesList != null)
        {
            var count = subRolesList.Count;
            // 必ず省略せずに先頭に表示させる属性
            foreach (var subRole in subRolesList.Where(role => role.IsFirstWriteAddOn()))
            {
                var str = subRole.ToString();
                // 先頭に表示する属性の中で専用表示にするもの
                switch (subRole)
                {
                    case CustomRoles.LastImpostor:
                        str = "Last-";
                        break;
                    case CustomRoles.CompleteCrew:
                        str = "Complete-";
                        break;
                }
                roleText.Append(Utils.ColorString(Utils.GetRoleColor(subRole), Translator.GetRoleString(str)));
                count--;
            }
            // 属性表示(Yオリジナル)
            if (showSubRole && !isTOHDisplay)
            {
                // 二つ以上の属性がある場合は省略
                if (count >= 2 && Options.GetAddonShowModes() == AddonShowMode.Default)
                {
                    roleText.Insert(0, Utils.ColorString(Color.gray, "＋"));
                }
                // 属性数が1つ、または省略しない設定
                else
                {
                    int i = 0;
                    foreach (var subRole in subRolesList)
                    {
                        if (subRole.IsFirstWriteAddOn()) continue;

                        roleText.Append(Utils.ColorString(Utils.GetRoleColor(subRole), Utils.GetRoleName(subRole)));
                        i++;
                        if (i % 2 == 0) roleText.Append('\n');
                    }
                }
            }
        }

        // ●役職表示
        roleText.Append(allWrite ? GetAllMainRoleText(playerId) : GetDisplayMainRole(playerId));

        // 属性表示(リザルト等、全テキスト表示)
        if (allWrite)
        {
            string subRoleMarks = GetSubRoleMarks(subRolesList);
            if (subRoleMarks != string.Empty)
            {
                roleText.Append($" / {GetSubRoleMarks(subRolesList)}");
            }
        }
        // 属性表示(本家TOH仕様)
        else if (showSubRole && isTOHDisplay)
        {
            string subRoleMarks = GetSubRoleMarks(subRolesList);
            if (roleText.ToString() != string.Empty && subRoleMarks != string.Empty)
                roleText.Append((subRolesList.Count >= 2) ? "\n" : " ").Append(subRoleMarks); //空じゃなければ空白を追加
        }

        return roleText.ToString();
    }

    public static string GetDisplayMainRole(byte playerId, bool disableColor = false)
    {
        var sb = new StringBuilder();
        var MainRoles = PlayerState.GetByPlayerId(playerId).MainRoleList;

        if (MainRoles == null || MainRoles.Count == 0) return "";

        // 二つ表示
        if (Main.ShowChangeMainRole.Contains(playerId) && MainRoles.Count >= 2)
        {
            var PreviousRole = MainRoles[^2];
            // ひとつ前の役職を表示
            if (disableColor)
            {
                sb.Append(Utils.GetRoleName(PreviousRole));
                sb.Append(" ⇒ ");
            }
            else
            {
                sb.Append(Utils.ColorString(Utils.GetRoleColor(PreviousRole), Utils.GetRoleName(PreviousRole)));
                sb.Append(Utils.ColorString(Color.white, " ⇒ "));
            }
        }
        // 現在の役職
        var NowRole = MainRoles.Last();
        (Color color, string text) = (Utils.GetRoleColor(NowRole), Utils.GetRoleName(NowRole));

        // 役職による役職表示名オーバーライド
        CustomRoleManager.GetByPlayerId(playerId)?.OverrideShowMainRoleText(ref color, ref text);

        // 現在の役職を表示
        sb.Append(disableColor ? text : Utils.ColorString(color, text));

        return sb.ToString();
    }

    public static string GetAllMainRoleText(byte playerId, bool disableColor = false)
    {
        var MainRoles = PlayerState.GetByPlayerId(playerId).MainRoleList;
        var sb = new StringBuilder();

        bool first = true; // 矢印は初め付けない
        foreach (var role in MainRoles)
        {
            // 色付けるか
            if (disableColor)
            {
                if (!first) sb.Append("⇒");
                sb.Append(Utils.GetRoleName(role));
            }
            else
            {
                if (!first) sb.Append(Utils.ColorString(Color.gray, "⇒"));
                sb.Append(Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role)));
            }
            first = false;
        }
        return sb.ToString();
    }

    public static string GetSubRolesText(byte id, bool disableColor = false)
    {
        var SubRoles = PlayerState.GetByPlayerId(id).SubRoles;
        if (SubRoles.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var role in SubRoles)
        {
            if (role == CustomRoles.NotAssigned || role.IsFirstWriteAddOn()) continue;

            var RoleText = disableColor ? Utils.GetRoleName(role) : Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role));
            sb.Append($"{Utils.ColorString(Color.gray, " + ")}{RoleText}");
        }

        return sb.ToString();
    }
    public static string GetSubRoleMarks(List<CustomRoles> subRolesList)
    {
        var sb = new StringBuilder(100);
        if (subRolesList != null)
        {
            foreach (var subRole in subRolesList)
            {
                if (subRole.IsFirstWriteAddOn()) continue;
                switch (subRole)
                {
                    case CustomRoles.AddWatch: sb.Append(AddWatch.SubRoleMark); break;
                    case CustomRoles.AddLight: sb.Append(AddLight.SubRoleMark); break;
                    case CustomRoles.AddSeer: sb.Append(AddSeer.SubRoleMark); break;
                    case CustomRoles.Autopsy: sb.Append(Autopsy.SubRoleMark); break;
                    case CustomRoles.VIP: sb.Append(VIP.SubRoleMark); break;
                    case CustomRoles.Revenger: sb.Append(Revenger.SubRoleMark); break;
                    case CustomRoles.Management: sb.Append(Management.SubRoleMark); break;
                    case CustomRoles.Sending: sb.Append(Sending.SubRoleMark); break;
                    case CustomRoles.TieBreaker: sb.Append(TieBreaker.SubRoleMark); break;
                    case CustomRoles.Loyalty: sb.Append(Loyalty.SubRoleMark); break;
                    case CustomRoles.PlusVote: sb.Append(PlusVote.SubRoleMark); break;
                    case CustomRoles.Guarding: sb.Append(Guarding.SubRoleMark); break;
                    case CustomRoles.AddBait: sb.Append(AddBait.SubRoleMark); break;
                    case CustomRoles.Refusing: sb.Append(Refusing.SubRoleMark); break;
                    case CustomRoles.Revealer: sb.Append(Revealer.SubRoleMark); break;

                    case CustomRoles.Sunglasses: sb.Append(Sunglasses.SubRoleMark); break;
                    case CustomRoles.Clumsy: sb.Append(Clumsy.SubRoleMark); break;
                    case CustomRoles.InfoPoor: sb.Append(InfoPoor.SubRoleMark); break;
                    case CustomRoles.NonReport: sb.Append(NonReport.SubRoleMark); break;
                }
            }
        }
        return sb.ToString();
    }
}
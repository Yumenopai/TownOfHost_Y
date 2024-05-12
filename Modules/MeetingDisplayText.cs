using System.Linq;
using System.Text;
using UnityEngine;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Madmate;
using TownOfHostY.Roles.Impostor;
using static TownOfHostY.Translator;

namespace TownOfHostY.Modules;
public static class MeetingDisplayText
{
    /***************************************
    Meeting中の名前に無理矢理加えてTextを表示させる(Vanilla視点対応)

    【上段の場合】
    (最大全角10文字) 超えると2行になりズレるので文字数ブレるものは注意
    ○○○○    // 何かしら表示させるもの 終わりに必ず改行　ここでの表示段数を{Column}に手動格納
    \n          // ちょうどいい位置までの調整に<line-height={height}em>を使用
    PlayerName  // 元々ある表示名(Vanillaの場合[RoleText,Suffix]がある時ない時で調整必要)
    \n          // 下側には何も表示させないため空白を置きつつ<line-height={height}em>で調整
    　(空白)　  // 上と同じぶんの行がいる為{Column}ぶんの改行が必要
    ***************************************/

    // 左上(1番)
    private static (string, int, float) LeftUp()
    {
        StringBuilder addText = new();
        int column = 0;
        float height = 2f;

        // パン屋が居ればパン屋生存中を表示する
        (string t, int c) = Bakery.AddMeetingDisplay();
        addText.Append(t); column += c;

        // Report
        if (Options.ShowReportReason.GetBool())
        {
            var dead = ReportDeadBodyPatch.ReportTarget;
            // 緊急ボタンでの会議
            if (dead == null)
            {
                addText.Append($"<i>{GetString("MDisplay.Button")}</i>\n".Color(Color.white));
                column += 1;
            }
            // 通報:{0}の死体
            else if (!ReportDeadBodyPatch.SpecialMeeting)
            {
                var colorText = Palette.GetColorName(dead.DefaultOutfit.ColorId);
                addText.Append("<color=#ffffff><i>");
                addText.Append(string.Format(GetString("MDisplay.ReportBody"), colorText.Color(dead.Color)));
                addText.Append("</i></color>\n");
                column += 1;
            }
        }

        return (addText.ToString(), column, height);
    }
    // 真ん中(2番)
    private static (string, int, float) CenterUp()
    {
        StringBuilder addText = new();
        int column = 0;
        float height = 3.2f;

        (string t, int c) = EvilIgnition.AddMeetingDisplay();
        addText.Append(t); column += c;
        (t, c) = Nimrod.AddMeetingDisplay();
        addText.Append(t); column += c;
        (t, c) = MadNimrod.AddMeetingDisplay();
        addText.Append(t); column += c;

        return (addText.ToString(), column, height);
    }
    // 右上(3番)
    private static (string, int, float) RightUp(bool forVanilla)
    {
        StringBuilder addText = new();
        int column = 0;
        float height = 2f;

        // ModName&Version (Vanilla Only)
        if (forVanilla)
        {
            addText.Append($"<color={Main.ModColor}>TOH_Y</color> v{Main.PluginVersion}\n\n".Color(Color.white));
            column += 2;
            height = 3.5f;
        }

        // Revenge
        if (Options.ShowRevengeTarget.GetBool())
        {
            if (MeetingHudPatch.RevengeTargetPlayer.Count() >= 2)
            {
                addText.Append("<line-height=0.12em>\n</line-height>")
                    //.Append($"<line-height=0.87em>道連れ死亡:\n</line-height>　複数発生しています\n".Color(Color.white));
                    .Append($"<line-height=0.87em>{GetString("MDisplay.RevengeHeader")}:\n</line-height>　{GetString("MDisplay.RevengeMultiple")}\n".Color(Color.white));
                column += 2;
                height = 0.8f;
            }

            foreach (var Exiled_Target in MeetingHudPatch.RevengeTargetPlayer)
            {
                var colorT = Palette.GetColorName(Exiled_Target.revengeTarget.DefaultOutfit.ColorId).Color(Exiled_Target.revengeTarget.Color);
                var colorE = Palette.GetColorName(Exiled_Target.exiled.DefaultOutfit.ColorId).Color(Exiled_Target.exiled.Color);

                addText.Append("<line-height=0.12em>\n</line-height>")
                    .Append($"<line-height=0.87em>{GetString("MDisplay.RevengeHeader")}:\n</line-height>")
                    .Append($"{colorT}<={colorE}\n".Color(Color.white));
                column += 2;
                height = 0.8f;
            }
        }

        return (addText.ToString(), column, height);
    }

    // MODクライアント側
    public static string AddTextForClient(string name, int area)
    {
        // 表示するテキスト
        string addText = "";
        // 高さ調節用
        int column = 0;
        float height = 2f;

        // 0 = 左上(1番目)
        if (area == 0)
        {
            (addText, column, height) = LeftUp();
        }
        // 1 = 真ん中上(2番目)
        else if (area == 1)
        {
            (addText, column, height) = CenterUp();
        }
        // 2 = 右上(3番目)
        else if (area == 2)
        {
            (addText, column, height) = RightUp(false);
        }
        else return name;
        // 何も足されていない時は早期リターン
        if (addText == "") return name;

        string plusDisplay = $"<align={"left"}>{addText}</align>" +
            $"<line-height={height}em>\n</line-height>";

        string adjust = $"<line-height={height}em>\n</line-height>";
        for (int i = 0; i < column; i++) adjust += '\n';
        adjust += "ㅤ";

        return plusDisplay + name + adjust;
    }

    // Vanilla側
    public static string AddTextForVanilla
        (PlayerControl pc, string name, string suffix, string roleText, bool isMeeting)
    {
        if (!isMeeting) return name;

        // 表示するテキスト
        string addText = "";
        // 高さ調節用
        int column = 0;
        float height = 2f;

        // 0 = 左上(1番目)
        if (pc == Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).ElementAtOrDefault(0))
        {
            (addText, column, height) = LeftUp();
            if (addText == "") return name;
        }
        // 1 = 真ん中上(2番目)
        else if (pc == Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).ElementAtOrDefault(1))
        {
            (addText, column, height) = CenterUp();
            if (addText == "") return name;
        }
        // 2 = 右上(3番目)
        else if (pc == Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).ElementAtOrDefault(2))
        {
            (addText, column, height) = RightUp(true);
        }
        // それ以外は早期リターン
        else return name;

        // 名前が数行になる時に一段ぶん少なくする
        if (roleText != "") height -= 0.5f;
        if (suffix != "") height -= 0.5f;

        string plusDisplay = $"<align={"left"}>{addText}</align>" +
            $"<line-height={height}em>\n</line-height>";

        string adjust = $"<line-height={height}em>\n</line-height>";
        for (int i = 0; i < column; i++) adjust += '\n';
        adjust += "ㅤ";

        return plusDisplay + name + adjust;
    }
}

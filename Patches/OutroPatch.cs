using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;
using TownOfHostY.Templates;
using static TownOfHostY.Translator;
using TownOfHostY.Roles.Neutral;

namespace TownOfHostY;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
class EndGamePatch
{
    public static Dictionary<byte, string> SummaryText = new();
    public static string KillLog = "";
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        try
        {
            GameStates.InGame = false;

            Logger.Info("-----------ゲーム終了-----------", "Phase");
            if (!GameStates.IsModHost) return;
            SummaryText = new();
            foreach (var id in PlayerState.AllPlayerStates.Keys)
                SummaryText[id] = Utils.SummaryTexts(id, false);

            // 役職変更表示のリセット
            Main.ShowChangeMainRole.Clear();

            var sb = new StringBuilder($"<size=100%><align={"center"}>{GetString("KillLog")}</align></size>");
            sb.Append("<size=70%>");
            foreach (var kvp in PlayerState.AllPlayerStates.OrderBy(x => x.Value.RealKiller.Item1.Ticks))
            {
                var date = kvp.Value.RealKiller.Item1;
                if (date == DateTime.MinValue) continue;
                var killerId = kvp.Value.GetRealKiller();
                var targetId = kvp.Key;
                sb.Append($"\n{date:T} {Main.AllPlayerNames[targetId]}({RoleText.GetRoleNameText(targetId, showSubRole:false)}) [{Utils.GetVitalText(kvp.Key)}]");
                if (killerId != byte.MaxValue && killerId != targetId)
                    sb.Append($"\n\t\t<size=75%>⇐ {Main.AllPlayerNames[killerId]}({RoleText.GetRoleNameText(killerId, showSubRole: false)})</size>");
            }
            KillLog = sb.ToString();

            if (Main.NormalOptions != null)
                Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
            //winnerListリセット
            EndGameResult.CachedWinners = new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();
            var winner = new List<PlayerControl>();
            foreach (var pc in Main.AllPlayerControls)
            {
                if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) winner.Add(pc);
            }
            foreach (var team in CustomWinnerHolder.WinnerRoles)
            {
                winner.AddRange(Main.AllPlayerControls.Where(p => p.Is(team) && !winner.Any(x => x.PlayerId == p.PlayerId)));
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                if (CustomWinnerHolder.LoserIds.Contains(pc.PlayerId)) winner.RemoveAll(x => x.PlayerId == pc.PlayerId);
            }

            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Draw && CustomWinnerHolder.WinnerTeam != CustomWinner.None)
            {
                //HideAndSeek専用
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    winner = new();
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        var role = PlayerState.GetByPlayerId(pc.PlayerId).GetNowMainRole();
                        if (role.GetCustomRoleTypes() == CustomRoleTypes.Impostor)
                        {
                            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                                winner.Add(pc);
                        }
                        else if (role.GetCustomRoleTypes() == CustomRoleTypes.Crewmate)
                        {
                            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
                                winner.Add(pc);
                        }
                        else if (role == CustomRoles.HASTroll && pc.Data.IsDead)
                        {
                            //トロールが殺されていれば単独勝ち
                            winner = new()
                        {
                            pc
                        };
                            break;
                        }
                        else if (role == CustomRoles.HASFox && CustomWinnerHolder.WinnerTeam != CustomWinner.HASTroll && !pc.Data.IsDead)
                        {
                            winner.Add(pc);
                            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.HASFox);
                        }
                    }
                }
            }

            Main.winnerList = new();
            foreach (var pc in winner)
            {
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;

                EndGameResult.CachedWinners.Add(new CachedPlayerData(pc.Data));
                Main.winnerList.Add(pc.PlayerId);
            }

            Main.VisibleTasksCount = false;
            if (AmongUsClient.Instance.AmHost)
            {
                try
                {
                    Main.RealOptionsData.Restore(GameOptionsManager.Instance.CurrentGameOptions);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to restore RealOptionsData: {ex.Message}", "EndGamePatch");
                }

                try
                {
                    GameOptionsSender.AllSenders.Clear();
                    GameOptionsSender.AllSenders.Add(new NormalGameOptionsSender());
                }
                catch { }
            }
            //オブジェクト破棄
            CustomRoleManager.Dispose();

            // ログ内にゲーム結果を出力
            Logger.Info("■■■■ゲーム結果■■■■", "EndGame");
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);
            foreach (var id in Main.winnerList)
            {
                Logger.Info($"★ {SummaryText[id].RemoveColorTags()}", "EndGame");
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                Logger.Info($"　 {SummaryText[id].RemoveColorTags()}", "EndGame");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"EndGamePatch.Postfix で例外: {ex.Message}", "EndGamePatch");
            Logger.Exception(ex, "EndGamePatch");
        }
    }
}
[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
class SetEverythingUpPatch
{
    public static string LastWinsText = "";

    public static void Postfix(EndGameManager __instance)
    {
        try
        {
            if (__instance == null) return;
            if (!Main.playerVersion.ContainsKey(0)) return;
            //#######################################
            //          ==勝利陣営表示==
            //#######################################

            //__instance.WinText.alignment = TMPro.TextAlignmentOptions.Right;
            var WinnerTextObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            WinnerTextObject.transform.position = new(__instance.WinText.transform.position.x/* + 2.4f*/, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
            WinnerTextObject.transform.localScale = new(0.6f, 0.6f, 0.6f);
            var WinnerText = WinnerTextObject.GetComponent<TMPro.TextMeshPro>(); //WinTextと同じ型のコンポーネントを取得
            WinnerText.fontSizeMin = 3f;
            WinnerText.text = "";

            string CustomWinnerText = "";
            string AdditionalWinnerText = "";
            string CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Crewmate);

            var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
            if (winnerRole >= 0)
            {
                CustomWinnerText = Utils.GetRoleName(winnerRole);
                CustomWinnerColor = Utils.GetRoleColorCode(winnerRole);
                if (winnerRole.IsNeutral())
                {
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
                }
            }
            if (AmongUsClient.Instance.AmHost && PlayerState.GetByPlayerId(0).GetNowMainRole() == CustomRoles.GM)
            {
                __instance.WinText.text = "Game Over";
                __instance.WinText.color = Utils.GetRoleColor(CustomRoles.GM);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.GM);
            }
            switch (CustomWinnerHolder.WinnerTeam)
            {
                //通常勝利
                case CustomWinner.Crewmate:
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Engineer);
                    break;
                //特殊勝利
                case CustomWinner.Terrorist:
                    __instance.Foreground.material.color = Color.red;
                    break;
                case CustomWinner.Lovers:
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Lovers);
                    break;
                //引き分け処理
                case CustomWinner.Draw:
                    __instance.WinText.text = GetString("ForceEnd");
                    __instance.WinText.color = Color.white;
                    __instance.BackgroundBar.material.color = Color.gray;
                    WinnerText.text = GetString("ForceEndText");
                    WinnerText.color = Color.gray;
                    break;
                //全滅
                case CustomWinner.None:
                    __instance.WinText.text = "";
                    __instance.WinText.color = Color.black;
                    __instance.BackgroundBar.material.color = Color.gray;
                    WinnerText.text = GetString("EveryoneDied");
                    WinnerText.color = Color.gray;
                    break;
            }

            foreach (var addWinnerRole in CustomWinnerHolder.AdditionalWinnerRoles)
            {
                var addWinnerName = Utils.GetRoleName(addWinnerRole);
                AdditionalWinnerText += "＆" + Utils.ColorString(Utils.GetRoleColor(addWinnerRole), addWinnerName);
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
            {
                if (Options.IsCCMode)
                {
                    if (CustomWinnerHolder.WinnerTeam == CustomWinner.RedL)     WinnerText.text = $"<color={CustomWinnerColor}>{GetString("CCRedWin")}</color>";
                    if (CustomWinnerHolder.WinnerTeam == CustomWinner.BlueL)    WinnerText.text = $"<color={CustomWinnerColor}>{GetString("CCBlueWin")}</color>";
                    if (CustomWinnerHolder.WinnerTeam == CustomWinner.YellowL)  WinnerText.text = $"<color={CustomWinnerColor}>{GetString("CCYellowWin")}</color>";
                }
                else
                    WinnerText.text = $"<color={CustomWinnerColor}>{CustomWinnerText}{AdditionalWinnerText}{GetString("Win")}</color>";
            }
            LastWinsText = WinnerText.text;
            LastWinsText = LastWinsText.RemoveHtmlTags();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //#######################################
            //           ==最終結果表示==
            //#######################################

            var Pos = Camera.main?.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane)) ?? Vector3.zero;

            StringBuilder sb = new($"{GetString("RoleSummaryText")}");
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);
            foreach (var id in Main.winnerList)
            {
                sb.Append($"\n<color={CustomWinnerColor}>★</color> ").Append(EndGamePatch.SummaryText[id]);
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id]);
            }
            var RoleSummary = TMPTemplate.Create(
                "RoleSummaryText",
                sb.ToString(),
                Color.white,
                1.25f,
                TMPro.TextAlignmentOptions.TopLeft,
                setActive: true);
            if (RoleSummary != null && __instance.Navigation != null && __instance.Navigation.ExitButton != null)
                RoleSummary.transform.position = new Vector3(__instance.Navigation.ExitButton.transform.position.x + -0.05f, Pos.y - 0.13f, -15f);
            if (RoleSummary != null) RoleSummary.transform.localScale = new Vector3(1f, 1f, 1f);

            //var RoleSummaryRectTransform = RoleSummary.GetComponent<RectTransform>();
            //RoleSummaryRectTransform.anchoredPosition = new Vector2(Pos.x + 3.5f, Pos.y - 0.1f);

            //Utils.ApplySuffix();
        }
        catch (Exception ex)
        {
            Logger.Error($"SetEverythingUpPatch.Postfix で例外: {ex.Message}", "SetEverythingUpPatch");
            Logger.Exception(ex, "SetEverythingUpPatch");
        }
    }
}
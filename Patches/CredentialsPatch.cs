using System.Globalization;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch]
    public static class CredentialsPatch
    {
        public static SpriteRenderer TohLogo { get; private set; }

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        class PingTrackerUpdatePatch
        {
            static StringBuilder sb = new();
            static void Postfix(PingTracker __instance)
            {
                __instance.text.alignment = TextAlignmentOptions.TopRight;

                sb.Clear();

                sb.Append("\r\n").Append(Main.credentialsText);

                if (Options.NoGameEnd.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("NoGameEnd")));
                if (Options.IsStandardHAS) sb.Append($"\r\n").Append(Utils.ColorString(Color.yellow, GetString("StandardHAS")));
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("HideAndSeek")));
                if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.NoModHost")));
                if (DebugModeManager.IsDebugMode) sb.Append("\r\n").Append(Utils.ColorString(Color.green, "デバッグモード"));

                var offset_x = 1.2f; //右端からのオフセット
                if (HudManager.InstanceExists && HudManager._instance.Chat.ChatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
                if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
                __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

                if (GameStates.IsLobby)
                {
                    if (Options.IsStandardHAS && !CustomRoles.Sheriff.IsEnable() && !CustomRoles.SerialKiller.IsEnable() && CustomRoles.Egoist.IsEnable())
                        sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.EgoistCannotWin")));
                }

                __instance.text.text += sb.ToString();
            }
        }
        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        class VersionShowerStartPatch
        {
            static TextMeshPro SpecialEventText;
            static void Postfix(VersionShower __instance)
            {
                Main.credentialsText = $"<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginVersion}";
#if DEBUG
                Main.credentialsText += $"\r\n<color={Main.ModColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
#endif
                var credentials = Object.Instantiate(__instance.text);
                credentials.text = Main.credentialsText;
                credentials.alignment = TextAlignmentOptions.Right;
                credentials.transform.position = new Vector3(4.6f, 3.2f, 0);

                ErrorText.Create(__instance.text);
                if (Main.hasArgumentException && ErrorText.Instance != null)
                {
                    ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
                }

                VersionChecker.Check();

                if (SpecialEventText == null)
                {
                    SpecialEventText = Object.Instantiate(__instance.text);
                    SpecialEventText.name = "SpecialEventText";
                    SpecialEventText.text = "";
                    SpecialEventText.color = Color.white;
                    SpecialEventText.fontSizeMin = 3f;
                    SpecialEventText.alignment = TextAlignmentOptions.Center;
                    SpecialEventText.transform.localPosition = new Vector3(0f, -1.2f, 0f);
                }
                SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
                if (Main.IsInitialRelease)
                {
                    SpecialEventText.text = $"Happy Birthday to {Main.ModName}!";
                    ColorUtility.TryParseHtmlString(Main.ModColor, out var col);
                    SpecialEventText.color = col;
                }
                if (Main.IsOneNightRelease && CultureInfo.CurrentCulture.Name == "ja-JP")
                {
                    SpecialEventText.text = "TOH_YS(制限版)へようこそ！" +
                        "\n<size=55%>6/22のAmongUs内部的サイレント更新のため、" +
                        "\nホスト系MODの役職に不具合が発生しております。" +
                        "\nしばらくはこのTOH_YSをご利用ください。\n</size><size=40%>\nTOH_YSのＳはSimpleのＳです。</size>";
                    SpecialEventText.color = Color.yellow;
                }
                //if (Main.IsValentine)
                //{
                //    SpecialEventText.text = "♥happy Valentine♥";
                //    if (CultureInfo.CurrentCulture.Name == "ja-JP")
                //        SpecialEventText.text += "<size=60%>\n<color=#b58428>チョコレート屋で遊んでみてね。</size></color>";
                //    SpecialEventText.color = Utils.GetRoleColor(CustomRoles.Lovers);
                //}
                if (Main.IsChristmas && CultureInfo.CurrentCulture.Name == "ja-JP")
                {
                    SpecialEventText.text = "★Merry Christmas★\n<size=15%>\n\nTOH_Yからのプレゼントはありません。</size>";
                    SpecialEventText.color = Utils.GetRoleColor(CustomRoles.Rainbow);
                }
            }
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        class TitleLogoPatch
        {
            public static GameObject amongUsLogo;
            static void Postfix(MainMenuManager __instance)
            {
                if ((amongUsLogo = GameObject.Find("bannerLogo_AmongUs")) != null)
                {
                    amongUsLogo.transform.localScale *= 0.4f;
                    amongUsLogo.transform.position += Vector3.up * 0.25f;
                }

                var tohLogo = new GameObject("titleLogo_TOH");
                tohLogo.transform.position = Vector3.up;
                tohLogo.transform.localScale *= 1.2f;
                var renderer = tohLogo.AddComponent<SpriteRenderer>();
                renderer.sprite = Utils.LoadSprite("TownOfHost.Resources.TownOfHost-Logo.png", 300f);
            }
        }
        [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
        class ModManagerLateUpdatePatch
        {
            public static void Prefix(ModManager __instance)
            {
                __instance.ShowModStamp();

                LateTask.Update(Time.deltaTime);
                CheckMurderPatch.Update();
            }
            public static void Postfix(ModManager __instance)
            {
                var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
                __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
                    __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
                    new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
            }
        }
    }
}

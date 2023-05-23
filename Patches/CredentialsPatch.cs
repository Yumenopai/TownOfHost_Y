using System.Globalization;
using System.Text;
using HarmonyLib;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    class PingTrackerUpdatePatch
    {
        static StringBuilder sb = new();
        static void Postfix(PingTracker __instance)
        {
            __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;

            sb.Clear();
            sb.Append(Main.credentialsText);

            if (Options.NoGameEnd.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("NoGameEnd")));
            if (Options.IsStandardHAS) sb.Append($"\r\n").Append(Utils.ColorString(Color.yellow, GetString("StandardHAS")));
            if (Options.CurrentGameMode.IsCatMode()) sb.Append($"\r\n").Append(Utils.ColorString(Color.gray, GetString("CatchCat")));
            if (Options.CurrentGameMode.IsOneNightMode()) sb.Append($"\r\n").Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ONVillager), GetString("OneNight")));
            if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.NoModHost")));
            if (DebugModeManager.IsDebugMode) sb.Append("\r\n").Append(Utils.ColorString(Color.green, "デバッグモード"));

            var offset_x = 1.2f; //右端からのオフセット
            if (HudManager.InstanceExists && HudManager._instance.Chat.ChatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
            if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
            __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

            if (GameStates.IsLobby)
            {
                if (Options.IsStandardHAS
                && !CustomRoles.Sheriff.IsEnable()
                && !CustomRoles.MadSheriff.IsEnable()
                && !CustomRoles.SillySheriff.IsEnable()
                && !CustomRoles.Hunter.IsEnable()
                && !CustomRoles.SerialKiller.IsEnable()
                && CustomRoles.Egoist.IsEnable())
                    sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.EgoistCannotWin")));
            }
            __instance.text.text += sb.ToString();
        }
    }
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    class VersionShowerStartPatch
    {
        static TMPro.TextMeshPro SpecialEventText;
        static void Postfix(VersionShower __instance)
        {
            Main.credentialsText = $"\r\n<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginVersion}";
#if DEBUG
            Main.credentialsText += $"\r\n<color={Main.ModColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
#endif
            var credentials = Object.Instantiate(__instance.text);
            credentials.text = Main.credentialsText;
            credentials.alignment = TMPro.TextAlignmentOptions.TopRight;
            credentials.transform.position = new Vector3(4.6f, 3.2f, 0);

            ErrorText.Create(__instance.text);
            if (Main.hasArgumentException && ErrorText.Instance != null)
            {
                ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
            }

            if (SpecialEventText == null)
            {
                SpecialEventText = Object.Instantiate(__instance.text);
                SpecialEventText.text = "";
                SpecialEventText.color = Color.white;
                SpecialEventText.fontSize += 2.5f;
                SpecialEventText.alignment = TMPro.TextAlignmentOptions.Top;
                SpecialEventText.transform.position = new Vector3(0, 0.5f, 0);
            }
            SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
            if (Main.IsInitialRelease)
            {
                SpecialEventText.text = $"Happy Birthday to {Main.ModName}!";
                ColorUtility.TryParseHtmlString(Main.ModColor, out var col);
                SpecialEventText.color = col;
            }
            if (Main.IsOneNightRelease)
            {
                SpecialEventText.text = $"<size=60%>New Game Mode</size>\n{GetString("OneNight")} Release!";
                SpecialEventText.color = Utils.GetRoleColor(CustomRoles.ONVillager);
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

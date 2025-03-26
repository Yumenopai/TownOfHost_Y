using System;
using HarmonyLib;
using TownOfHostY.Modules;
using TownOfHostY.Templates;
using UnityEngine;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(MainMenuManager))]
    public class MainMenuManagerPatch
    {
        private static SimpleButton discordButton;
        private static SimpleButton twitterButton;
        private static SimpleButton wikiwikiButton;
        private static SimpleButton gitHubButton;
        public static SimpleButton UpdateButton { get; private set; }

        [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
        public static void StartPostfix(MainMenuManager __instance)
        {
            SimpleButton.SetBase(__instance.quitButton);
            //Discordボタンを生成
            if (SimpleButton.IsNullOrDestroyed(discordButton))
            {
                discordButton = CreateButton(
                    "DiscordButton",
                    new(-2.45f, -2.7f, 1f),
                    new(86, 98, 246, byte.MaxValue),
                    new(173, 179, 244, byte.MaxValue),
                    () => Application.OpenURL(Main.DiscordInviteUrl),
                    "Discord",
                    new(1.85f, 0.5f),
                    isActive: Main.ShowDiscordButton);
            }

            // Twitterボタンを生成
            if (SimpleButton.IsNullOrDestroyed(twitterButton))
            {
                twitterButton = CreateButton(
                    "TwitterButton",
                    new(-0.85f, -2.7f, 1f),
                    new(29, 160, 241, byte.MaxValue),
                    new(169, 215, 242, byte.MaxValue),
                    () => Application.OpenURL("https://twitter.com/yumeno_AmongUs"),
                    "Twitter/X",
                    new(1.85f, 0.5f));
            }
            // WIKIWIKIボタンを生成
            if (SimpleButton.IsNullOrDestroyed(wikiwikiButton))
            {
                wikiwikiButton = CreateButton(
                    "WikiwikiButton",
                    new(0.75f, -2.7f, 1f),
                    new(255, 142, 168, byte.MaxValue),
                    new(255, 226, 153, byte.MaxValue),
                    () => Application.OpenURL("https://wikiwiki.jp/tohy_amongus"),
                    "WIKIWIKI",
                    new(1.85f, 0.5f));
            }
            // GitHubボタンを生成
            if (SimpleButton.IsNullOrDestroyed(gitHubButton))
            {
                gitHubButton = CreateButton(
                    "GitHubButton",
                    new(2.35f, -2.7f, 1f),
                    new(153, 153, 153, byte.MaxValue),
                    new(209, 209, 209, byte.MaxValue),
                    () => Application.OpenURL("https://github.com/Yumenopai/TownOfHost_Y"),
                    "GitHub",
                    new(1.85f, 0.5f));
            }

            //Updateボタンを生成
            if (SimpleButton.IsNullOrDestroyed(UpdateButton))
            {
                UpdateButton = CreateButton(
                    "UpdateButton",
                    new(0f, -0.6f, -10f),
                    new(255, 255, 60, byte.MaxValue),
                    new(255, 255, 224, byte.MaxValue),
                    () =>
                    {
                        UpdateButton.Button.gameObject.SetActive(false);
                        ModUpdater.StartUpdate(ModUpdater.downloadUrl);
                    },
                    "ModUpdaterで上書き",
                    new(5f, 3f),
                    isActive: false);
            }

#if RELEASE
            // フリープレイの無効化
            var howToPlayButton = __instance.howToPlayButton;
            var freeplayButton = howToPlayButton.transform.parent.Find("FreePlayButton");
            if (freeplayButton != null)
            {
                freeplayButton.gameObject.SetActive(false);
            }
            // フリープレイが消えるのでHowToPlayをセンタリング
            howToPlayButton.transform.SetLocalX(0);
#endif
        }
        /// <summary>TOHロゴの子としてボタンを生成</summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="normalColor">普段のボタンの色</param>
        /// <param name="hoverColor">マウスが乗っているときのボタンの色</param>
        /// <param name="action">押したときに発火するアクション</param>
        /// <param name="label">ボタンのテキスト</param>
        /// <param name="scale">ボタンのサイズ 変更しないなら不要</param>
        private static SimpleButton CreateButton(
                    string name,
                    Vector3 localPosition,
                    Color32 normalColor,
                    Color32 hoverColor,
                    Action action,
                    string label,
                    Vector2? scale = null,
                    bool isActive = true)
        {
            var button = new SimpleButton(CredentialsPatch.TohLogo.transform, name, localPosition, normalColor, hoverColor, action, label, isActive);

            if (scale.HasValue) button.Scale = scale.Value;
            return button;
        }

        // プレイメニュー，アカウントメニュー，クレジット画面が開かれたらロゴとボタンを消す
        [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenOnlineMenu))]
        [HarmonyPatch(nameof(MainMenuManager.ClickBackOnline))]
        [HarmonyPatch(nameof(MainMenuManager.OpenAccountMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenCredits))]
        [HarmonyPostfix]
        public static void OpenMenuPostfix()
        {
            if (CredentialsPatch.TohLogo != null)
            {
                CredentialsPatch.TohLogo.gameObject.SetActive(false);
            }
        }
        [HarmonyPatch(nameof(MainMenuManager.ResetScreen)), HarmonyPostfix]
        public static void ResetScreenPostfix()
        {
            if (CredentialsPatch.TohLogo != null)
            {
                CredentialsPatch.TohLogo.gameObject.SetActive(true);
            }
        }

        private static GameObject ModWarningText;

        // オンラインメニューの一部ボタンを消す
        [HarmonyPatch(nameof(MainMenuManager.OpenOnlineMenu)), HarmonyPostfix]
        public static void OpenOnlineMenuPrefix(MainMenuManager __instance)
        {
            if (VersionChecker.IsSupported && Main.AllowPublicRoom && Main.IsPublicAvailableOnThisVersion) return;

            // 部屋作成ボタン
            __instance.createGameButton.gameObject.transform.localPosition = new Vector3(-2.1f, 0.35f, 0f);
            // 部屋探しボタン
            __instance.findGameButton.gameObject.SetActive(false);
            // 部屋コードボタン
            var objECB = GameObject.Find("Enter Code Button");
            if (objECB)
            {
                objECB?.SetActive(false);
            }
            // 仕切りライン
            var line = GameObject.Find("OnlineButtons/AspectSize/Scaler/Line");
            if (line)
            {
                line?.SetActive(false);
            }

            // 公開制限説明
            var objmenu = GameObject.Find("OnlineButtons/AspectSize/Scaler");
            if (objmenu && ModWarningText == null)
            {
                ModWarningText = new GameObject("ModWarning");
                ModWarningText.transform.SetParent(objmenu.transform);
                ModWarningText.transform.localPosition = new Vector3(1.4f, 0.25f, 0f);
                ModWarningText.transform.localScale = new Vector3(1.9f, 1.9f, 2f);
                var renderer = ModWarningText.AddComponent<SpriteRenderer>();
                renderer.sprite = Utils.LoadSprite($"TownOfHost_Y.Resources.warning_online.png", 400f);
            }
        }
    }
}
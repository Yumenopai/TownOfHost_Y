using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using AmongUs.Data;
using Assets.InnerNet;
using AmongUs.Data.Player;
using System.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using BepInEx.Unity.IL2CPP.Utils.Collections;

using Object = UnityEngine.Object;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MainMenuManager))]
    public class MainMenuManagerPatch
    {
        private static PassiveButton template;
        private static PassiveButton discordButton;
        private static PassiveButton twitterButton;
        private static PassiveButton wikiwikiButton;
        private static PassiveButton gitHubButton;

        [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
        public static void StartPostfix(MainMenuManager __instance)
        {
            if (template == null) template = __instance.quitButton;
            if (template == null) return;
            //Discordボタンを生成
            if (discordButton == null)
            {
                discordButton = CreateButton(
                    "DiscordButton",
                    new(-2.45f, -2.7f, 1f),
                    new(86, 98, 246, byte.MaxValue),
                    new(173, 179, 244, byte.MaxValue),
                    () => Application.OpenURL(Main.DiscordInviteUrl),
                    "Discord",
                    new(1.85f, 0.5f));
            }
            discordButton.gameObject.SetActive(Main.ShowDiscordButton);

            // Twitterボタンを生成
            if (twitterButton == null)
            {
                twitterButton = CreateButton(
                    "TwitterButton",
                    new(-0.85f, -2.7f, 1f),
                    new(29, 160, 241, byte.MaxValue),
                    new(169, 215, 242, byte.MaxValue),
                    () => Application.OpenURL("https://twitter.com/yumeno_AmongUs"),
                    "Twitter",
                    new(1.85f, 0.5f));
            }
            // WIKIWIKIボタンを生成
            if (wikiwikiButton == null)
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
            if (gitHubButton == null)
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
        private static PassiveButton CreateButton(string name, Vector3 localPosition, Color32 normalColor, Color32 hoverColor, Action action, string label, Vector2? scale = null)
        {
            var button = Object.Instantiate(template, CredentialsPatch.TohLogo.transform);
            button.name = name;
            Object.Destroy(button.GetComponent<AspectPosition>());
            button.transform.localPosition = localPosition;

            button.OnClick = new();
            button.OnClick.AddListener(action);

            var buttonText = button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMP_Text>();
            buttonText.DestroyTranslator();
            buttonText.fontSize = buttonText.fontSizeMax = buttonText.fontSizeMin = 3.7f;
            buttonText.enableWordWrapping = false;
            buttonText.text = label;
            var normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();
            var hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();
            normalSprite.color = normalColor;
            hoverSprite.color = hoverColor;

            // ラベルをセンタリング
            var container = buttonText.transform.parent;
            Object.Destroy(container.GetComponent<AspectPosition>());
            Object.Destroy(buttonText.GetComponent<AspectPosition>());
            container.SetLocalX(0f);
            buttonText.transform.SetLocalX(0f);
            buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var buttonCollider = button.GetComponent<BoxCollider2D>();
            if (scale.HasValue)
            {
                normalSprite.size = hoverSprite.size = buttonCollider.size = scale.Value;
            }
            // 当たり判定のズレを直す
            buttonCollider.offset = new(0f, 0f);

            return button;
        }

        // プレイメニュー，アカウントメニュー，クレジット画面が開かれたらロゴとボタンを消す
        [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
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
    }

    public static class ObjectHelper
    {
        /// <summary>
        /// オブジェクトの<see cref="TextTranslatorTMP"/>コンポーネントを破棄します
        /// </summary>
        public static void DestroyTranslator(this GameObject obj)
        {
            var translator = obj.GetComponent<TextTranslatorTMP>();
            if (translator != null)
            {
                Object.Destroy(translator);
            }
        }
        /// <summary>
        /// オブジェクトの<see cref="TextTranslatorTMP"/>コンポーネントを破棄します
        /// </summary>
        public static void DestroyTranslator(this MonoBehaviour obj) => obj.gameObject.DestroyTranslator();
    }
}
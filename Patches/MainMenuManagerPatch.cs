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

    /*
    public class ModNews
    {
        public int Number;
        public int BeforeNumber;
        public string Title;
        public string SubTitle;
        public string ShortTitle;
        public string Text;
        public string Date;

        public Announcement ToAnnouncement()
        {
            var result = new Announcement
            {
                Number = Number,
                Title = Title,
                SubTitle = SubTitle,
                ShortTitle = ShortTitle,
                Text = Text,
                Language = (uint)DataManager.Settings.Language.CurrentLanguage,
                Date = Date,
                Id = "ModNews"
            };

            return result;
        }
    }

    [HarmonyPatch]
    public class ModNewsHistory
    {
        public static List<ModNews> AllModNews = new();

        [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.Init)), HarmonyPostfix]
        public static void Initialize(ref Il2CppSystem.Collections.IEnumerator __result)
        {
            static IEnumerator GetEnumerator()
            {
                while (AnnouncementPopUp.UpdateState == AnnouncementPopUp.AnnounceState.Fetching) yield return null;
                if (AnnouncementPopUp.UpdateState > AnnouncementPopUp.AnnounceState.Fetching && DataManager.Player.Announcements.AllAnnouncements.Count > 0) yield break;

                AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.Fetching;
                AllModNews.Clear();

                {
                    var news = new ModNews
                    {
                        Number = 100002,
                        //BeforeNumber = 0,
                        Title = "Town Of Host_Y v3.0.2.3",
                        SubTitle = "★★★★v3.0.2.3アップデート!★★★★",
                        ShortTitle = "★TOH_Y v3.0.2.3",
                        Text = "TownOfHost_Yのご利用ありがとうございます！\n"

                            + "\n 【新役職】呪狼・マッドシェリフ・バカシェリフ・共鳴者・ダークハイド・ラブカッター・オポチュニストキラー(オポチュニストのキル可能設定)"
                            + "\n 【新レイアウトの設定画面】：陣営ごとのタブ\n"
                            + "\n ホストが重くカク付く現象を比較的修正。さらに負荷軽減設定(ワカホリ,にじスタ対象)を追加。全員から役職名見えるのが重いの..etc.\n"

                            + "\n【注意】\nプレイする際は必ずTOH_Yであることを明記・通知してください。本家TOHではないことを十分理解した上で(参加者にも理解させた上で)ご利用ください。"
                            + "\nまた、本家TOHとTOH_Yの同時使用はできません。必ず1つのMODだけを使用するようにしてください。",
                        Date = "2022-11-21T00:00:00Z"

                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100003,
                        //BeforeNumber = 0,
                        Title = "Town Of Host_Y v402.3.9",
                        SubTitle = "★★★★v402.3.9アップデート!★★★★",
                        ShortTitle = "★TOH_Y v402.3.9",
                        Text = "Thank you for playing TownOfHost_Y!\n"

                            + "\n 【対応】AU本体v2022.12.8・本家TOHv4.0.2"
                            + "\n 【新役職】グリーディア・アンビショナー・ブラインダー・純愛者"
                            + "\n 【新属性】ウォッチング・ライティング・サングラス・シーイング"
                            + "\n 【(予告)新要素】新ゲームモード『猫取合戦』"
                            + "\n ここはそれぞれ孤独な猫たちが集う場。いつも自身のタスクをこなしながら穏やかな日々を過ごしていた。或る日、この地の神がここの孤独な猫たちを見て、裏切り者が生まれた時に備えていくつか集団を作るべきと判断し、数名にリーダーを任命した。\n"

                            + "\n【注意】プレイの際は必ずTOH_Yであることを通知して下さい。",
                        Date = "2023-1-12T00:00:00Z"

                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100004,
                        //BeforeNumber = 0,
                        Title = "Town Of Host_Y v402.4",
                        SubTitle = "★★★★★v402.4アップデート!★★★★★",
                        ShortTitle = "★TOH_Y v402.4",
                        Text = "Thank you for playing TownOfHost_Y!\n"
                            + "\n 【新要素】新ゲームモード『猫取合戦』"
                            + "\n ここはそれぞれ孤独な猫たちが集う場。いつも自身のタスクをこなしながら穏やかな日々を過ごしていた。或る日、この地の神がここの孤独な猫たちを見て、裏切り者が生まれた時に備えていくつか集団を作るべきと判断し、数名にリーダーを任命した。\n"

                            + "\n【注意】プレイの際は必ずTOH_Yであることを通知して下さい。本家TOHではないことを十分理解した上で(参加者にも理解させた上で)ご利用ください。"
                            + "特に公開部屋で遊ぶときは注意してください。自分の名前の上にTownOfHost_Yと表記されたり、参加者に自動的にお知らせが流れたりしますが、「TOHだよ！」と偽ったり「TOHの新バージョン」のように本家と区別がつかない言い方をするのはお辞めください。\n"
                            + "\nまた、本家TOHとTOH_Yの同時使用はできません。必ず1つのMODだけを使用するようにしてください。",
                        Date = "2023-1-15T00:00:00Z"

                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100005,
                        //BeforeNumber = 0,
                        Title = "Town Of Host_Y v402.5",
                        SubTitle = "★★★★★v402.5アップデート!★★★★★",
                        ShortTitle = "★TOH_Y v402.5",
                        Text = "Thank you for playing TownOfHost_Y!\n"

                            + "\n 【新役職】弁護士(追跡者)・[属性]オートプシー"
                            + "\n 【新機能】シンクロカラーモード"
                            + "\n 　　　　-----クローン・50-50・三つ巴・ツイン"
                            + "\n スキンは勿論のこと、ペット、色、ネームプレート、プレイヤー、レベルまで何から何まで一緒なため、自分が会議の何番目にいるのかすらわからない時もある圧倒的お遊び系モード。\n"

                            + "\n【注意】プレイの際は必ずTOH_Yであることを通知して下さい。"
                            + "\n・野良でのwelcomeメッセージやYouTube、Twitter等を見ているとTOH_Yを使用しているにも関わらず、TownOfHost(無印)になっているのをよく見かけます。"
                            + "本家TOHと勘違いされるパターンも実際起こりますので充分お気を付けください。"
                            + "\n・特に公開ルームで部屋を立てる際も、何もわからずに参加してくる方にでも本家TOHでないことが伝わるような通知を心がけてください。\n何卒宜しくお願い致します。 ",
                        Date = "2023-1-26T00:00:00Z"

                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100006,
                        //BeforeNumber = 0,
                        Title = "Town Of Host_Y v402.6",
                        SubTitle = "★★★★★v402.6アップデート!★★★★★",
                        ShortTitle = "★TOH_Y v402.6",
                        Text = "Thank you for playing TownOfHost_Y!\n"

                            + "\n 【新役職】クライアント"
                            + "\n 【新属性】VIP・クラムシー・リベンジャー"
                            + "\n 【新機能】既存役職の新オプションを12種追加"
                            + "\n 【新機能】「自身の役職説明自動表示」を含む新機能を5種追加"
                            + "\n 【期間限定役職】チョコレート屋～2/15\n"

                            + "\n【注意】プレイの際は必ずTOH_Yであることを通知して下さい。"
                            + "\n・野良でのwelcomeメッセージやYouTube、Twitter等を見ているとTOH_Yを使用しているにも関わらず、TownOfHost(無印)になっているのをよく見かけます。"
                            + "本家TOHと勘違いされるパターンも実際起こりますので充分お気を付けください。"
                            + "\n・特に公開ルームで部屋を立てる際も、何もわからずに参加してくる方にでも本家TOHでないことが伝わるような通知を心がけてください。\n何卒宜しくお願い致します。 ",
                        Date = "2023-02-09T00:00:00Z"

                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100007,
                        //BeforeNumber = 0,
                        Title = "Town Of Host_Y v411.7",
                        SubTitle = "★★★★★v411.7アップデート!★★★★★",
                        ShortTitle = "★TOH_Y v411.7",
                        Text = "Thank you for playing TownOfHost_Y!\n"
                        + "\n<b>新MODゲームモード【ワンナイト】実装！</b>\n"
                        + "\n　一回の議論だけで人狼を探し出す「ワンナイト人狼」をAmong Usでアレンジして実装！一回の行動ターンと一回の会議で人狼を見つけて追放しましょう。"
                        + "\n　行動ターンの要素と、役職を基とした議論の要素と。どの視点で詰めていくかはお任せします。。。\n"
                        + "\n　詳しい説明は[/h n]コマンド、Discord、GitHub何れかから！\n"
                        + "\n<b>-【ワンナイトモードの役職】</b>"
                        + "\n　・人狼/大狼"
                        + "\n　・狂人/狂信者"
                        + "\n　・村人/占い師/怪盗/村長/狩人/パン屋/罠師"
                        + "\n　・吊人\n"
                        + "\n----------------------------------------------------------------"
                        + "\n　スタンダードモードもいくつかアップデート。\n"
                        + "\n<b>-【新役職】スカベンジャー</b>"
                        + "\n　スカベンジャーにキルされた死体は、通報することが出来なくなる。(通報ボタンを押しても反応しない)\n"
                        + "\n<b>-【新属性】マネジメント</b>"
                        + "\n　タスクマネージャーの属性版。全員の完了タスク合計数がわかる。(デフォルトは会議中のみ)\n"
                        + "\n<b>-【新機能】にじいろスター</b>"
                        + "\n　ネームプレートが虹色になるようになりました。また設定で、タスクターン中、他視点から役職名が表示されない設定を追加しました。\n"
                        + "\n<b>-【期間限定役職復刻】チョコレート屋</b> ～3/15"
                        + "\n　ホワイトデーでお返しを！チョコレート屋が帰ってきます。コメントを忘れずに確認してね！\n"
                        + "\nなにか気になったことやバグ報告はTOH_YのDiscordまでご連絡ください。\n\nTown Of Host_Y：Yumeno",
                        Date = "2023-03-09T00:00:00Z"

                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100008,
                        //BeforeNumber = 0,
                        Title = "Town Of Host_Y v412.8",
                        SubTitle = "★★★★★v412.8アップデート!★★★★★",
                        ShortTitle = "★TOH_Y v412.8",
                        Text = "Thank you for playing TownOfHost_Y!\n"
                        + "\n<b>エイプリルフールだ！Yで遊ばない？</b>\n"
                        + "\n　ポテンシャリストという意味の分からない役職が登場！"
                        + "\n　タスクを完了させるとランダムなクルー役職に変化、能力が開花しちゃいます。残念ながら期間限定なので今後また遊べるかはわかりません。。。"
                        + "\n----------------------------------------------------------------"
                        + "\n　その他アップデート内容↓↓↓\n"
                        + "\n<b>-【新役職】イビルディバイナー</b>"
                        + "\n　いわゆる占い師の能力が使えるインポスター。キルボタンを1回だけ押すと占うことができるけど、キルクールは消費してしまうので占うかキルか、迷いながら行動しよう。\n"
                        + "\n<b>-【新役職】テレパシスターズ</b>"
                        + "\n　イビルトラッカーの複数人版を、Ｙでアレンジしてみました。以心伝心でキルしていこう。新要素として、シスターズ同士のベント回数が共有されていたりします。\n"
                        + "\n<b>-【新役職】メディック</b>"
                        + "\n　ベントでプレイヤーを選択するという新たな機能が登場。このメディックは、選んだプレイヤーを一度キルガードします。\n"
                        + "\nなにか気になったことやバグ報告はTOH_YのDiscordまでご連絡ください。\n\nTown Of Host_Y：Yumeno",
                        Date = "2023-03-31T00:00:00Z"

                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100009,
                        //BeforeNumber = 0,
                        Title = "Town Of Host_Y v412.9",
                        SubTitle = "★★★★★v412.9アップデート!★★★★★",
                        ShortTitle = "★TOH_Y v412.9",
                        Text = "Thank you for playing TownOfHost_Y!\n"
                        + "\n<b>なんと、なんと、新属性大量発生！</b>\n"
                        + "\n　マッド系役職でよく使われている(らしい)、属性が一斉に新登場！"
                        + "\n　ランダムで属性を付与する【ADD-ON Setting】以外にも、<b>ラストインポスター</b>や、今回の新登場属性<b>コンプリートクルー</b>にも属性を直接付与することができるのでぜひお試しあれ。"
                        + "\n----------------------------------------------------------------"
                        + "\n<b>-【新役職】シェイプキラー・グラージシェリフ・キャンドルライター・占い師・霊媒師・トトカルチョ</b>"
                        + "\n　クライアントの制作者、くろにゃんこ氏と再びコラボレーション！シェイプキラーと占い師はくろにゃんこ氏に開発していただきました！\n"
                        + "\n<b>-【新属性】インフォプアー・タイブレーカー・ノンレポート・センディング・ロイヤルティ・プラスボート・ガーディング・ベイティング・リフュージング・コンプリートクルー</b>"
                        + "\n　より能力の幅を広くするため、新属性の開発を行いました。マッドメイトをさらに強くするもよし、ランダムにデバフ属性のみを付与させて遊ぶもよし、色々な遊び方を試してみてください。\n"
                        + "\n<b>-【新機能】役職設定モード</b>（スタンダードモードのみ）"
                        + "\n　0%100%のみの設定方法から、オンオフの設定に切り替えました。本家TOH対応に向けての機能です。また、属性のみしか設定できない属性オンリーモードも作ってみました。議論がなかなか面白くなりそうな予感がします。\n"
                        + "\nなにか気になったことやバグ報告はTOH_YのDiscordまでご連絡ください。\n\nTown Of Host_Y：Yumeno",
                        Date = "2023-05-5T00:00:00Z"

                    };
                    AllModNews.Add(news);
                }
                AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.NotStarted;
            }

            __result = Effects.Sequence(GetEnumerator().WrapToIl2Cpp(), __result);
        }

        [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
        public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] Il2CppReferenceArray<Announcement> aRange)
        {
            List<Announcement> list = new();
            foreach (var a in aRange) list.Add(a);
            foreach (var m in AllModNews) list.Add(m.ToAnnouncement());
            list.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

            __instance.allAnnouncements = new Il2CppSystem.Collections.Generic.List<Announcement>();
            foreach (var a in list) __instance.allAnnouncements.Add(a);


            __instance.HandleChange();
            __instance.OnAddAnnouncement?.Invoke();

            return false;
        }
    }*/
}
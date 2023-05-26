using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.CoreScripts;
using HarmonyLib;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static List<string> ChatHistory = new();
        public static bool Prefix(ChatController __instance)
        {
            if (__instance.TextArea.text == "") return false;
            __instance.TimeSinceLastMessage = 3f;
            var text = __instance.TextArea.text;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");

            var tag = !PlayerControl.LocalPlayer.Data.IsDead ? "SendChatHost" : "SendChatDeadHost";
            if (text.StartsWith("試合結果:") || text.StartsWith("キル履歴:")) tag = "SendSystemChat";
            VoiceReader.ReadHost(text, tag);

            switch (args[0])
            {
                case "/dump":
                    canceled = true;
                    Utils.DumpLog();
                    break;
                case "/v":
                case "/version":
                    canceled = true;
                    string version_text = "";
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key))
                    {
                        version_text += $"{kvp.Key}:{Utils.GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "") HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, version_text);
                    break;
                default:
                    Main.isChatCommand = false;
                    break;
            }
            if (AmongUsClient.Instance.AmHost)
            {
                Main.isChatCommand = true;
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        Utils.SendMessage("Winner: " + string.Join(",", Main.winnerList.Select(b => Main.AllPlayerNames[b])));
                        break;

                    case "/l":
                    case "/lastresult":
                        canceled = true;
                        Utils.ShowLastResult();
                        break;

                    case "/kl":
                    case "/killlog":
                        canceled = true;
                        Utils.ShowKillLog();
                        break;

                    case "/r":
                    case "/rename":
                        canceled = true;
                        Main.nickName = args.Length > 1 ? Main.nickName = args[1] : "";
                        break;

                    case "/hn":
                    case "/hidename":
                        canceled = true;
                        Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
                        GameStartManagerPatch.GameStartManagerStartPatch.HideName.text =
                            ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                                ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                                : $"<color={Main.ModColor}>{Main.HideName.Value}</color>";
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                Utils.ShowActiveRoles();
                                break;
                            default:
                                Utils.ShowActiveSettings();
                                break;
                        }
                        break;

                    case "/w":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;
                                GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, false);
                                break;
                            case "impostor":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                                break;
                            case "none":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                                break;
                            case "jackal":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JSchrodingerCat);
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JClient);
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor | jackal | none");
                                cancelVal = "/w";
                                break;
                        }
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Admin, 0);
                        break;

                    case "/dis":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
                                break;

                            case "impostor":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                                cancelVal = "/dis";
                                break;
                        }
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Admin, 0);
                        break;

                    case "/h":
                    case "/help":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                subArgs = args.Length < 3 ? "" : args[2];
                                GetRolesInfo(subArgs);
                                break;

                            case "a":
                            case "addons":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "lastimpostor":
                                    case "limp":
                                        Utils.SendMessage(Utils.GetRoleName(CustomRoles.LastImpostor) + GetString("LastImpostorInfoLong"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n lastimpostor(limp)");
                                        break;
                                }
                                break;

                            case "m":
                            case "modes":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "catchcat":
                                    case "cc":
                                        Utils.SendMessage(GetString("CatInfo1"));
                                        Utils.SendMessage(GetString("CatInfo2"));
                                        Utils.SendMessage(GetString("CatInfo3"));
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        Utils.SendMessage(GetString("NoGameEndInfo"));
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        Utils.SendMessage(GetString("SyncButtonModeInfo"));
                                        break;

                                    case "randommapsmode":
                                    case "rmm":
                                        Utils.SendMessage(GetString("RandomMapsModeInfo"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)");
                                        break;
                                }
                                break;


                            case "n":
                            case "now":
                                Utils.ShowActiveSettingsHelp();
                                break;

                            default:
                                Utils.ShowHelp();
                                break;
                        }
                        break;

                    case "/m":
                    case "/myrole":
                        canceled = true;
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, Utils.SendRoleInfo(PlayerControl.LocalPlayer));
                        break;

                    case "/t":
                    case "/template":
                        canceled = true;
                        if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                        else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"{GetString("ForExample")}:\n{args[0]} test");
                        break;

                    case "/mw":
                    case "/messagewait":
                        canceled = true;
                        if (args.Length > 1 && float.TryParse(args[1], out float sec))
                        {
                            Main.MessageWait.Value = sec;
                            Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                        }
                        else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                        break;

                    case "/say":
                        canceled = true;
                        if (args.Length > 1)
                            Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")}</color>");
                        break;

                    case "/exile":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                        Utils.GetPlayerById(id)?.RpcExileV2();
                        break;

                    case "/kill":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                        Utils.GetPlayerById(id2)?.RpcMurderPlayer(Utils.GetPlayerById(id2));
                        break;

                    case "/vo":
                    case "/voice":
                        canceled = true;
                        if (args.Length > 1 && args[1] == "reset")
                        {
                            VoiceReader.ResetVoiceNo();
                        }
                        else if (args.Length > 1 && args[1] == "random")
                        {
                            VoiceReader.SetRandomVoiceNo();
                        }
                        else if (args.Length > 1 && int.TryParse(args[1], out int voiceNo))
                        {
                            var name = VoiceReader.SetHostVoiceNo(voiceNo);
                            if (name != null && name != "")
                                Utils.SendMessage($"ホスト の読上げを {name} に変更しました");
                        }
                        else
                            Utils.SendMessage(VoiceReader.GetVoiceIdxMsg());

                        break;

                    default:
                        Main.isChatCommand = false;
                        break;
                }
            }
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.TextArea.Clear();
                __instance.TextArea.SetText(cancelVal);
                __instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }

        public static void GetRolesInfo(string role)
        {
            var roleList = new Dictionary<CustomRoles, string>
            {
                //GM
                { CustomRoles.GM, "ゲームマスター" },
                //Impostor役職
                { (CustomRoles)(-1), $"== {GetString("Impostor")} ==" }, //区切り用
                { CustomRoles.BountyHunter, "バウンティハンター" },
                { CustomRoles.EvilTracker,"イビルトラッカー" },
                { CustomRoles.EvilWatcher,"イビルウォッチャー" },
                { CustomRoles.FireWorks, "花火職人" },
                { CustomRoles.Mare, "メアー" },
                { CustomRoles.Mafia, "マフィア" },
                { CustomRoles.SerialKiller, "シリアルキラー" },
                { CustomRoles.ShapeKiller, "シェイプキラー" },
                { CustomRoles.ShapeMaster, "シェイプマスター" },
                { CustomRoles.TimeThief, "タイムシーフ"},
                { CustomRoles.Sniper, "スナイパー" },
                { CustomRoles.Puppeteer, "パペッティア" },
                { CustomRoles.Vampire, "ヴァンパイア" },
                { CustomRoles.Warlock, "ウォーロック" },
                { CustomRoles.Witch, "ウィッチ" },
                { CustomRoles.AntiAdminer, "アンチアドミナー" },
                { CustomRoles.Evilneko, "イビル猫又" },
                { CustomRoles.CursedWolf, "呪狼" },
                { CustomRoles.Greedier, "グリーディア" },
                { CustomRoles.Ambitioner, "アンビショナー" },
                { CustomRoles.Scavenger, "スカベンジャー" },
                { CustomRoles.EvilDiviner, "イビルディバイナー" },
                { CustomRoles.Telepathisters, "テレパシスターズ" },
                //Madmate役職
                { (CustomRoles)(-2), $"== {GetString("Madmate")} ==" }, //区切り用
                { CustomRoles.MadGuardian, "マッドガーディアン" },
                { CustomRoles.Madmate, "マッドメイト" },
                { CustomRoles.MadSnitch, "マッドスニッチ" },
                { CustomRoles.MadDictator, "マッドディクテーター" },
                { CustomRoles.MadNatureCalls, "マッドネイチャコール" },
                { CustomRoles.MadBrackOuter, "マッドブラックアウター" },
                { CustomRoles.MadSheriff, "マッドシェリフ" },
                { CustomRoles.SKMadmate, "サイドキックマッドメイト" },
                //両陣営役職
                //{ (CustomRoles)(-3), $"== {GetString("Impostor")} or {GetString("Crewmate")} ==" }, //区切り用
                //Crewmate役職
                { (CustomRoles)(-4), $"== {GetString("Crewmate")} ==" }, //区切り用
                { CustomRoles.Bait, "ベイト" },
                { CustomRoles.Dictator, "ディクテーター" },
                { CustomRoles.Doctor, "ドクター" },
                { CustomRoles.Lighter, "ライター" },
                { CustomRoles.Mayor, "メイヤー" },
                { CustomRoles.NiceWatcher, "ナイスウォッチャー" },
                { CustomRoles.SabotageMaster, "サボタージュマスター" },
                { CustomRoles.Seer,"シーア" },
                { CustomRoles.Sheriff, "シェリフ" },
                { CustomRoles.Snitch, "スニッチ" },
                { CustomRoles.SpeedBooster, "スピードブースター" },
                { CustomRoles.Trapper, "トラッパー" },
                { CustomRoles.TimeManager, "タイムマネージャー"},
                { CustomRoles.Hunter, "ハンター" },
                { CustomRoles.TaskManager, "タスマネ" },
                { CustomRoles.Bakery, "パン屋" },
                { CustomRoles.Express, "エクスプレス" },
                { CustomRoles.Chairman, "チェアマン" },
                { CustomRoles.Rainbow, "にじいろスター" },
                { CustomRoles.Nekomata, "猫又" },
                { CustomRoles.SeeingOff, "見送り人" },
                { CustomRoles.SillySheriff, "バカシェリフ" },
                { CustomRoles.Sympathizer, "共鳴者" },
                { CustomRoles.Blinder, "ブラインダー" },
                { CustomRoles.Medic, "メディック" },
                { CustomRoles.GrudgeSheriff, "グラージシェリフ" },
                { CustomRoles.CandleLighter, "キャンドルライター" },
                { CustomRoles.FortuneTeller,"占い師" },
                //Neutral役職
                { (CustomRoles)(-5), $"== {GetString("Neutral")} ==" }, //区切り用
                { CustomRoles.Arsonist, "アーソニスト" },
                { CustomRoles.Egoist, "エゴイスト" },
                { CustomRoles.Executioner, "エクスキューショナー" },
                { CustomRoles.Jester, "ジェスター" },
                { CustomRoles.Opportunist, "オポチュニスト" },
                { CustomRoles.SchrodingerCat, "シュレディンガーの猫" },
                { CustomRoles.Terrorist, "テロリスト" },
                { CustomRoles.Jackal, "ジャッカル" },
                { CustomRoles.JClient, "クライアント" },
                { CustomRoles.AntiComplete, "アンチコンプリート" },
                { CustomRoles.Workaholic, "ワーカホリック" },
                { CustomRoles.DarkHide, "ダークハイド" },
                { CustomRoles.LoveCutter, "ラブカッター" },
                { CustomRoles.PlatonicLover, "純愛者" },
                { CustomRoles.Lawyer, "弁護士" },
                { CustomRoles.Totocalcio, "トトカルチョ" },
                //属性
                { (CustomRoles)(-6), $"== {GetString("Addons")} ==" }, //区切り用
                {CustomRoles.LastImpostor, "ラストインポスター" },
                {CustomRoles.Lovers, "ラバーズ" },
                {CustomRoles.Workhorse, "ワークホース" },
                {CustomRoles.AddWatch, "ウォッチング" },
                {CustomRoles.AddLight, "ライティング" },
                {CustomRoles.Sunglasses, "サングラス" },
                {CustomRoles.AddSeer, "シーイング" },
                {CustomRoles.Autopsy, "オートプシー" },
                {CustomRoles.VIP, "VIP" },
                {CustomRoles.Clumsy, "クラムシー" },
                {CustomRoles.Revenger, "リベンジャー" },
                {CustomRoles.Management, "マネジメント" },
                {CustomRoles.InfoPoor, "インフォプアー" },
                {CustomRoles.Sending, "センディング" },
                {CustomRoles.TieBreaker, "タイブレーカー" },
                {CustomRoles.NonReport, "ノンレポート" },
                {CustomRoles.Loyalty, "ロイヤルティ" },
                {CustomRoles.PlusVote, "プラスボート" },
                {CustomRoles.Guarding, "ガーディング" },
                {CustomRoles.AddBait, "ベイティング" },
                {CustomRoles.Refusing, "リフュージング" },
                {CustomRoles.CompreteCrew, "コンプリートクルー" },

                //HAS
                //{ (CustomRoles)(-7), $"== {GetString("HideAndSeek")} ==" }, //区切り用
                //{ CustomRoles.HASFox, "hfo" },
                //{ CustomRoles.HASTroll, "htr" },

            };
            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            foreach (var r in roleList)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
                {
                    Utils.SendMessage(GetString(roleName) + GetString($"{roleName}InfoLong"));
                    return;
                }

                var roleText = $"{roleName.ToLower()}({roleShort.ToLower()}), ";
                if ((int)r.Key < 0)
                {
                    msg += rolemsg + "\n" + roleShort + "\n";
                    rolemsg = "";
                }
                else if ((rolemsg.Length + roleText.Length) > 40)
                {
                    msg += rolemsg + "\n";
                    rolemsg = roleText;
                }
                else
                {
                    rolemsg += roleText;
                }
            }
            msg += rolemsg;
            Utils.SendMessage(msg);
        }
        public static void OnReceiveChat(PlayerControl player, string text)
        {
            if (player != null)
            {
                var tag = !player.Data.IsDead ? "SendChatAlive" : "SendChatDead";
                VoiceReader.Read(text, Palette.GetColorName(player.Data.DefaultOutfit.ColorId), tag);
            }

            if (!AmongUsClient.Instance.AmHost) return;
            string[] args = text.Split(' ');
            string subArgs = "";
            switch (args[0])
            {
                case "/l":
                case "/lastresult":
                    Utils.ShowLastResult(player.PlayerId);
                    break;

                case "/kl":
                case "/killlog":
                    Utils.ShowKillLog(player.PlayerId);
                    break;

                case "/n":
                case "/now":
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                            Utils.ShowActiveRoles(player.PlayerId);
                            break;

                        default:
                            Utils.ShowActiveSettings(player.PlayerId);
                            break;
                    }
                    break;

                case "/h":
                case "/help":
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "n":
                        case "now":
                            Utils.ShowActiveSettingsHelp(player.PlayerId);
                            break;
                    }
                    break;

                case "/m":
                case "/myrole":
                    Utils.SendMessage(Utils.SendRoleInfo(player), player.PlayerId);
                    break;

                case "/t":
                case "/template":
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                    else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                    break;

                case "/vo":
                case "/voice":
                    var color = Palette.GetColorName(player.Data.DefaultOutfit.ColorId);
                    if (VoiceReader.VoiceReaderMode == null || !VoiceReader.VoiceReaderMode.GetBool())
                        Utils.SendMessage($"現在読上げは停止しています", player.PlayerId);
                    else if (args.Length > 1 && args[1] == "n")
                        Utils.SendMessage($"{color} の現在の読上げは {VoiceReader.GetVoiceName(color)} です", player.PlayerId);
                    else if (args.Length > 1 && int.TryParse(args[1], out int voiceNo))
                    {
                        var name = VoiceReader.SetVoiceNo(color, voiceNo);
                        if (name != null && name != "")
                        {
                            Utils.SendMessage($"{color} の読上げを {name} に変更しました", player.PlayerId);
                            break;
                        }
                        Utils.SendMessage($"{color} の読上げを変更できませんでした", player.PlayerId);
                    }
                    else
                        Utils.SendMessage(VoiceReader.GetVoiceIdxMsg(), player.PlayerId);

                    break;

                default:
                    break;
            }
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static bool DoBlockChat = false;
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.TimeSinceLastMessage)) return;
            if (DoBlockChat) return;
            var player = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
            if (player == null) return;
            (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
            Main.MessagesToSend.RemoveAt(0);
            int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
            var name = player.Data.PlayerName;
            if (clientId == -1)
            {
                player.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                player.SetName(name);
            }
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(clientId);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(title)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.PlayerName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
            __instance.TimeSinceLastMessage = 0f;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(string chatText)
        {
            switch (chatText)
            {
                default:
                    break;
            }
            if (!AmongUsClient.Instance.AmHost) return;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    class RpcSendChatPatch
    {
        public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(chatText))
            {
                __result = false;
                return false;
            }
            int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
            chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
            if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
            if (chatText.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0)
                DestroyableSingleton<Telemetry>.Instance.SendWho();
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
            messageWriter.Write(chatText);
            messageWriter.EndMessage();
            __result = true;
            return false;
        }
    }
}
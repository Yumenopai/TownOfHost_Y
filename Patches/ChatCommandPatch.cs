using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.CoreScripts;
using HarmonyLib;
using Hazel;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Madmate;
using static TownOfHostY.Translator;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static List<string> ChatHistory = new();
        private static Dictionary<CustomRoles, string> roleCommands;

        public static bool Prefix(ChatController __instance)
        {
            if (__instance.freeChatField.textArea.text == "") return __instance.quickChatField.Visible;
            __instance.timeSinceLastMessage = 3f;
            var text = __instance.freeChatField.textArea.text;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");

            if (args[0] == "/cmd" && args.Length >= 2)
            {
                canceled = true;
                string cmdArg = args[1].StartsWith("/") ? args[1] : "/" + args[1];
                string[] newArgs = new string[args.Length - 1];
                newArgs[0] = cmdArg;
                for (int i = 2; i < args.Length; i++) newArgs[i - 1] = args[i];
                args = newArgs;
                text = string.Join(" ", args);
            }

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
                switch (args[0]?.ToLower())
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
                        GameStartManagerPatch.HideName.text = Main.HideName.Value;
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
                            case "v":
                            case "vanilla":
                                Utils.ShowVanillaSetting();
                                break;
                            default:
                                Utils.ShowActiveSettings();
                                break;
                        }
                        break;

                    case "/w":
                        canceled = true;
                        if (!GameStates.IsInGame) break;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;
                                foreach (var player in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Crewmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameEndChecker.StartEndGame(GameOverReason.CrewmatesByTask);
                                break;
                            case "impostor":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;
                                foreach (var player in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorsByKill);
                                break;
                            case "none":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorsByKill);
                                break;
                            case "jackal":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorsByKill);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor | jackal | none");
                                cancelVal = "/w";
                                break;
                        }
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
                        break;

                    case "/dis":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.CrewmateDisconnect, false);
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
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
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
                        if (!AmongUsClient.Instance.IsGameStarted) break;

                        string RoleInfoTitleString = GetString("RoleInfoTitle");
                        string RoleInfoTitle = Utils.ColorString(Utils.GetRoleColor(PlayerControl.LocalPlayer.GetCustomRole()), RoleInfoTitleString);
                        Utils.SendMessage(Utils.GetMyRoleInfo(PlayerControl.LocalPlayer), PlayerControl.LocalPlayer.PlayerId, RoleInfoTitle);
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
                                Utils.SendMessage(string.Format(GetString("Message.VoiceChangeHost"), name), 0);
                        }
                        else
                            Utils.SendMessage(VoiceReader.GetVoiceIdxMsg(), 0);
                        break;

                    case "/killFlash":
                    case "/kf":
                        canceled = true;
                        Utils.SetKillFlashAfterDead(PlayerControl.LocalPlayer, true);
                        break;

                    case "/killFlashAll":
                    case "/kfa":
                        canceled = true;
                        if (GameStates.InGame)
                            Main.AllPlayerControls.Do(pc => pc.KillFlash());
                        break;

                    case "/offhat":
                    case "/offskin":
                    case "/offvisor":
                    case "/offpet":
                    case "/offskinall":
                        canceled = true;
                        if (args.Length > 1)
                        {
                            var colorName = args[1];
                            var skinTarget = Utils.GetPlayerByColorName(colorName);
                            if (skinTarget != null)
                            {
                                var hat = args[0] == "/offhat" || args[0] == "/offskinall";
                                var skin = args[0] == "/offskin" || args[0] == "/offskinall";
                                var visor = args[0] == "/offvisor" || args[0] == "/offskinall";
                                var pet = args[0] == "/offpet" || args[0] == "/offskinall";
                                SkinControle.RpcSetSkin(skinTarget, hat, skin, visor, pet);
                                Utils.SendMessage($"ホストにより {SkinControle.GetSetTypeName(hat, skin, visor, pet)} がリセットにされました", skinTarget.PlayerId);
                            }
                        }

                        break;

                    default:
                        Main.isChatCommand = false;
                        break;
                }
            }
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(cancelVal);
            }
            return !canceled;
        }
        public static void SendCustomChat(string SendName, string command, string name, PlayerControl sender = null)
        {
            Logger.Info($"SendCustomChat SendName: {SendName}, command: {command}, name: {name} sender: {sender?.name}", "SendCustomChat");
            if (sender == null) sender = PlayerControl.LocalPlayer;
            var crs = CustomRpcSender.Create("AllSend");
            crs.AutoStartRpc(sender.NetId, (byte)RpcCalls.SetName)
                .Write(sender.Data.NetId)
                .Write(SendName)
                .EndRpc()
                .AutoStartRpc(sender.NetId, (byte)RpcCalls.SendChat)
                .Write(command)
                .EndRpc()
                .AutoStartRpc(sender.NetId, (byte)RpcCalls.SetName)
                .Write(sender.Data.NetId)
                .Write(name)
                .EndRpc()
                .SendMessage();
            sender.SetName(SendName);
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(sender, command);
            sender.SetName(name);
        }

        public static void GetRolesInfo(string role, byte PlayerId = byte.MaxValue)
        {
            // 初回のみ処理
            if (roleCommands == null)
            {
#pragma warning disable IDE0028  // Dictionary初期化の簡素化をしない
                roleCommands = new Dictionary<CustomRoles, string>();

                // GM
                roleCommands.Add(CustomRoles.GM, "ゲームマスター");

                // Impostor役職
                roleCommands.Add((CustomRoles)(-1), $"== {GetString("Impostor")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Impostor);

                // Madmate役職
                roleCommands.Add((CustomRoles)(-2), $"== {GetString("Madmate")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Madmate);
                roleCommands.Add(CustomRoles.SKMadmate, "サイドキックマッドメイト");

                // Crewmate役職
                roleCommands.Add((CustomRoles)(-3), $"== {GetString("Crewmate")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Crewmate);

                // Neutral役職
                roleCommands.Add((CustomRoles)(-4), $"== {GetString("Neutral")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Neutral);

                // 属性
                roleCommands.Add((CustomRoles)(-5), $"== {GetString("Addons")} ==");  // 区切り用
                roleCommands.Add(CustomRoles.LastImpostor, "ラストインポスター");
                roleCommands.Add(CustomRoles.Lovers, "ラバーズ");
                roleCommands.Add(CustomRoles.Workhorse, "ワークホース");
                roleCommands.Add(CustomRoles.CompleteCrew, "コンプリートクルー");
                roleCommands.Add(CustomRoles.AddWatch, "ウォッチング");
                roleCommands.Add(CustomRoles.Sunglasses, "サングラス");
                roleCommands.Add(CustomRoles.AddLight, "ライティング");
                roleCommands.Add(CustomRoles.AddSeer, "シーイング");
                roleCommands.Add(CustomRoles.Autopsy, "オートプシー");
                roleCommands.Add(CustomRoles.VIP, "VIP");
                roleCommands.Add(CustomRoles.Clumsy, "クラムシー");
                roleCommands.Add(CustomRoles.Revenger, "リベンジャー");
                roleCommands.Add(CustomRoles.Management, "マネジメント");
                roleCommands.Add(CustomRoles.InfoPoor, "インフォプアー");
                roleCommands.Add(CustomRoles.Sending, "センディング");
                roleCommands.Add(CustomRoles.TieBreaker, "タイブレーカー");
                roleCommands.Add(CustomRoles.NonReport, "ノンレポート");
                roleCommands.Add(CustomRoles.Loyalty, "ロイヤルティ");
                roleCommands.Add(CustomRoles.PlusVote, "プラスボート");
                roleCommands.Add(CustomRoles.Guarding, "ガーディング");
                roleCommands.Add(CustomRoles.AddBait, "ベイティング");
                roleCommands.Add(CustomRoles.Refusing, "リフュージング");
                roleCommands.Add(CustomRoles.Revealer, "リヴェラー");

                // HAS
                roleCommands.Add((CustomRoles)(-6), $"== {GetString("HideAndSeek")} ==");  // 区切り用
                roleCommands.Add(CustomRoles.HASFox, "hfo");
                roleCommands.Add(CustomRoles.HASTroll, "htr");
#pragma warning restore IDE0028
            }

            foreach (var r in roleCommands)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
                {
                    Utils.SendMessage(Utils.GetRoleInfoLong(r.Key), PlayerId);
                    return;
                }
            }
            Utils.SendMessage(GetString("Message.HelpRoleNone"), PlayerId);
        }
        private static void ConcatCommands(CustomRoleTypes roleType)
        {
            var roles = CustomRoleManager.AllRolesInfo.Values.Where(role => role.CustomRoleType == roleType);
            foreach (var role in roles)
            {
                if (role.ChatCommand is null) continue;

                roleCommands[role.RoleName] = role.ChatCommand;
            }
        }
        public static void OnReceiveChat(PlayerControl player, string text)
        {
            if (player != null)
            {
                var tag = !player.Data.IsDead ? "SendChatAlive" : "SendChatDead";
                VoiceReader.Read(text, Palette.GetColorName(player.Data.DefaultOutfit.ColorId), tag);
            }

            if (!AmongUsClient.Instance.AmHost) return;

            // ニムロッド会議中
            if(Nimrod.IsExecutionMeeting() || MadNimrod.IsExecutionMeeting())
            {
                if(text.Length > 0)
                {
                    Utils.SendMessage(GetString("Message.NowNimrodMeeting"),
                        title: $"<color={Utils.GetRoleColorCode(CustomRoles.Nimrod)}>{GetString("IsNimrodMeetingTitle")}</color>");
                }
            }

            string[] args = text.Split(' ');
            string subArgs = "";

            if (text.StartsWith("/") && !text.Contains("cmd"))
            {
                Utils.SendMessage(GetString("Error.CommandFailed"), player.PlayerId);
                return;
            }
            if (args[0] != "/cmd" || args.Length <= 1) return;
            args = args.Skip(1).ToArray();
            if (args[0].StartsWith("/") is false) args[0] = $"/{args[0]}";

            switch (args[0]?.ToLower())
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
                        case "v":
                        case "vanilla":
                            Utils.ShowVanillaSetting(player.PlayerId);
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

                        case "r":
                        case "roles":
                            subArgs = args.Length < 3 ? "" : args[2];
                            GetRolesInfo(subArgs, player.PlayerId);
                            break;
                    }
                    break;

                case "/m":
                case "/myrole":
                    if (!AmongUsClient.Instance.IsGameStarted) break;

                    string RoleInfoTitleString = GetString("RoleInfoTitle");
                    string RoleInfoTitle = Utils.ColorString(Utils.GetRoleColor(player.GetCustomRole()), RoleInfoTitleString);
                    Utils.SendMessage(Utils.GetMyRoleInfo(player), player.PlayerId, RoleInfoTitle);
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
                        Utils.SendMessage(string.Format(GetString("Message.VoiceNotAvailable")), player.PlayerId);
                    else if (args.Length > 1 && args[1] == "n")
                        Utils.SendMessage(string.Format(GetString("Message.VoiceNow"), color, VoiceReader.GetVoiceName(color)), player.PlayerId);
                    else if (args.Length > 1 && int.TryParse(args[1], out int voiceNo))
                    {
                        var name = VoiceReader.SetVoiceNo(color, voiceNo);
                        if (name != null && name != "")
                        {
                            Utils.SendMessage(string.Format(GetString("Message.VoiceChange"), color, name), player.PlayerId);
                            break;
                        }
                        Utils.SendMessage(string.Format(GetString("Message.VoiceChangeFailed"), color), player.PlayerId);
                    }
                    else
                        Utils.SendMessage(VoiceReader.GetVoiceIdxMsg(), player.PlayerId);
                    break;

                case "/killFlash":
                case "/kf":
                    Utils.SetKillFlashAfterDead(player);
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
            if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.timeSinceLastMessage)) return;
            if (DoBlockChat) return;
            var player = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
            if (player == null) return;
            (string msg, byte sendTo, string title, bool custom) = Main.MessagesToSend[0];
            Main.MessagesToSend.RemoveAt(0);

            if (custom)
            {
                SendCustomChat(msg, sendTo: sendTo);
                return;
            }

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
                .Write(player.Data.NetId)
                .Write(title)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.NetId)
                .Write(player.Data.PlayerName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
            __instance.timeSinceLastMessage = 0f;
        }
        public static void SendCustomChat(string SendName, PlayerControl sender = null, byte sendTo = byte.MaxValue)
        {
            //Logger.Info($"SendName: {SendName}, sender: {sender?.name}, sendTo: {sendTo}", "SendCustomChat");
            Logger.Info($"sender: {sender?.name}, sendTo: {sendTo}", "SendCustomChat");
            string command = "\n\n";
            if (sender == null) sender = PlayerControl.LocalPlayer;
            if (sender.Data.IsDead)
                sender = PlayerControl.AllPlayerControls.ToArray().OrderBy(x => x.PlayerId).Where(x => !x.Data.IsDead).FirstOrDefault();
            string name = sender.Data?.PlayerName;
            int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
            if (clientId == -1)
            {
                sender.SetName(SendName);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(sender, command);
                sender.SetName(name);
            }
            var writer = CustomRpcSender.Create("CustomSend");
            writer.StartMessage(clientId);
            writer.StartRpc(sender.NetId, (byte)RpcCalls.SetName)
                .Write(sender.Data.NetId)
                .Write(SendName)
                .EndRpc()
                .StartRpc(sender.NetId, (byte)RpcCalls.SendChat)
                .Write(command)
                .EndRpc()
                .StartRpc(sender.NetId, (byte)RpcCalls.SetName)
                .Write(sender.Data.NetId)
                .Write(name)
                .EndRpc()
                .EndMessage()
                .SendMessage();
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
            // Validate input
            if (string.IsNullOrWhiteSpace(chatText))
            {
                __result = false;
                return false;
            }

            // Adjust for local name line feeds (guard for nulls)
            var localName = PlayerControl.LocalPlayer != null ? PlayerControl.LocalPlayer.name : string.Empty;
            int return_count = 0;
            if (!string.IsNullOrEmpty(localName))
            {
                return_count = localName.Count(x => x == '\n');
            }
            chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();

            // Local echo (safe guards)
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
            {
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
            }

            // Optional telemetry (guard instance)
            try
            {
                if (chatText.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var ut = DestroyableSingleton<UnityTelemetry>.Instance;
                    if (ut != null) ut.SendWho();
                }
            }
            catch { /* ignore telemetry issues */ }

            // Send chat RPC using public API
            try
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None, -1);
                writer.Write(chatText);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                __result = true;
            }
            catch (System.Exception ex)
            {
                Logger.Error($"RpcSendChat failed: {ex.Message}", nameof(RpcSendChatPatch));
                __result = false;
            }
            return false;
        }
    }
}
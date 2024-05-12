using System;
using System.IO;
using System.Text.RegularExpressions;
using HarmonyLib;
using TownOfHostY.Attributes;
using static TownOfHostY.Translator;
namespace TownOfHostY
{
    public static class BanManager
    {
        private static readonly string DENY_NAME_LIST_PATH = @"./TOH_DATA/DenyName.txt";
        private static readonly string BAN_LIST_PATH = @"./TOH_DATA/BanList.txt";

        [PluginModuleInitializer]
        public static void Init()
        {
            Directory.CreateDirectory("TOH_DATA");
            if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
        }
        public static void AddBanPlayer(InnerNet.ClientData player, bool cheat = false)
        {
            if (!AmongUsClient.Instance.AmHost || player == null) return;
            if (!CheckBanList(player))
            {
                var addName = cheat ? " (Cheat)" : "";
                if (player.FriendCode != "") File.AppendAllText(BAN_LIST_PATH, $"{player.FriendCode},{player.PlayerName}{addName}\n");
                if (cheat && player.ProductUserId != "") File.AppendAllText(BAN_LIST_PATH, $"{player.ProductUserId},{player.PlayerName}{addName}\n");
                Logger.SendInGame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
            }
        }
        public static void CheckDenyNamePlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string[] kickName =
            {
                "mod",
                "toh",
                "tohy",
                "モッド",
                "もっど",
                "勧誘",
                "招待",
                "宣伝"
            };
            foreach (var line in kickName)
            {
                if (line == "") continue;
                if (Regex.IsMatch(player.PlayerName?.ToLower(), line))
                {
                    AmongUsClient.Instance.KickPlayer(player.Id, false);
                    Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                    return;
                }
            }

            if (!Options.ApplyDenyNameList.GetBool()) return;
            try
            {
                Directory.CreateDirectory("TOH_DATA");
                if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
                using StreamReader sr = new(DENY_NAME_LIST_PATH);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") continue;
                    if (Regex.IsMatch(player.PlayerName, line))
                    {
                        AmongUsClient.Instance.KickPlayer(player.Id, false);
                        Logger.SendInGame(string.Format(GetString("Message.KickedByDenyName"), player.PlayerName, line));
                        Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "CheckDenyNamePlayer");
            }
        }
        public static void CheckBanPlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost || !Options.ApplyBanList.GetBool()) return;
            if (CheckBanList(player))
            {
                AmongUsClient.Instance.KickPlayer(player.Id, true);
                Logger.SendInGame(string.Format(GetString("Message.BanedByBanList"), player.PlayerName));
                Logger.Info($"{player.PlayerName}は過去にBAN済みのためBANされました。", "BAN");
                return;
            }
        }
        public static bool CheckBanList(InnerNet.ClientData player)
        {
            if (player == null) return false;
            try
            {
                Directory.CreateDirectory("TOH_DATA");
                if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
                using StreamReader sr = new(BAN_LIST_PATH);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") continue;
                    if (player.FriendCode == line.Split(",")[0]) return true;
                    if (player.ProductUserId == line.Split(",")[0]) return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "CheckBanList");
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
    class BanMenuSelectPatch
    {
        public static void Postfix(BanMenu __instance, int clientId)
        {
            InnerNet.ClientData recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
            if (recentClient == null) return;
            if (!BanManager.CheckBanList(recentClient)) __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using TownOfHostY.Modules;
using static TownOfHostY.Translator;
using TownOfHostY.Roles;

namespace TownOfHostY
{
    public class GameStartManagerPatch
    {
        private static float timer = 600f;
        private static TextMeshPro warningText;
        public static TextMeshPro HideName;
        private static PassiveButton cancelButton;

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ReallyBegin))]
        public class GameStartManagerReallyBeginPatch
        {
            public static void Postfix(GameStartManager __instance, bool neverShow)
            {
                //MOD同意機能 現在使用していないためfalseで閉じる
#if false

                //if (!Main.CanPublicRoom.Value) return;
                if (GameStartManager.Instance.startState != GameStartManager.StartingStates.Countdown) return;
                //Logger.Info($"CanPublicRoom: {Main.CanPublicRoom.Value}", "GameStartManagerStart");

                foreach (var pc in Main.AllPlayerControls.Where(x => x.PlayerId != PlayerControl.LocalPlayer.PlayerId))
                {
                    var target = pc.GetClient();
                    if (target == null) continue;
                    Logger.Info($"ConsentCheck name: {target.PlayerName}, id; {target.Id}, check: {Main.ConsentModUse.ContainsKey(target.Id)}", "GameStartManagerStart");
                    if (!Main.ConsentModUse.ContainsKey(target.Id))
                    {
                        AmongUsClient.Instance.KickPlayer(target.Id, false);
                        Utils.SendMessage(string.Format(GetString("Message.ModCheckKick"), target.PlayerName));
                    }
                }
#endif
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        public class GameStartManagerStartPatch
        {
            public static void Postfix(GameStartManager __instance)
            {
                __instance.MinPlayers = 1;

                __instance.GameRoomNameCode.text = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
                // Reset lobby countdown timer
                timer = 600f;

                HideName = Object.Instantiate(__instance.GameRoomNameCode, __instance.GameRoomNameCode.transform);
                HideName.gameObject.SetActive(true);
                HideName.name = "HideName";
                HideName.color =
                    ColorUtility.TryParseHtmlString(Main.HideColor.Value, out var color) ? color :
                    ColorUtility.TryParseHtmlString(Main.ModColor, out var modColor) ? modColor : HideName.color;
                HideName.text = Main.HideName.Value;

                warningText = Object.Instantiate(__instance.GameStartText, __instance.transform);
                warningText.name = "WarningText";
                warningText.transform.localPosition = new(0f, 0f - __instance.transform.localPosition.y, -1f);
                warningText.gameObject.SetActive(false);

                cancelButton = Object.Instantiate(__instance.StartButton, __instance.transform);
                cancelButton.name = "CancelButton";
                var cancelLabel = cancelButton.GetComponentInChildren<TextMeshPro>();
                cancelLabel.DestroyTranslator();
                cancelLabel.text = GetString("Cancel");
                cancelButton.transform.localScale = new(0.4f, 0.4f, 1f);
                cancelButton.activeTextColor = Color.red;
                cancelButton.inactiveTextColor = Color.red;
                cancelButton.transform.localPosition = new(0f, -0.2f, 0f);
                var buttonComponent = cancelButton.GetComponent<PassiveButton>();
                buttonComponent.OnClick = new();
                buttonComponent.OnClick.AddListener((Action)(() => __instance.ResetStartState()));
                cancelButton.gameObject.SetActive(false);

                if (!AmongUsClient.Instance.AmHost) return;

                // Make Public Button
                if (!Main.AllowPublicRoom || ModUpdater.hasUpdate || !VersionChecker.IsSupported || !Main.IsPublicAvailableOnThisVersion)
                {
                    __instance.HostPrivateButton.inactiveTextColor = Palette.DisabledClear;
                    __instance.HostPrivateButton.activeTextColor = Palette.DisabledClear;
                }

                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public class GameStartManagerUpdatePatch
        {
            public static void Prefix(GameStartManager __instance)
            {
                // Lobby code
                if (DataManager.Settings.Gameplay.StreamerMode)
                {
                    __instance.GameRoomNameCode.color = new(__instance.GameRoomNameCode.color.r, __instance.GameRoomNameCode.color.g, __instance.GameRoomNameCode.color.b, 0);
                    HideName.enabled = true;
                }
                else
                {
                    __instance.GameRoomNameCode.color = new(__instance.GameRoomNameCode.color.r, __instance.GameRoomNameCode.color.g, __instance.GameRoomNameCode.color.b, 255);
                    HideName.enabled = false;
                }
            }
            public static void Postfix(GameStartManager __instance)
            {
                if (!AmongUsClient.Instance) return;

                string warningMessage = "";
                if (AmongUsClient.Instance.AmHost)
                {
                    bool canStartGame = true;
                    List<string> mismatchedPlayerNameList = new();
                    foreach (var client in AmongUsClient.Instance.allClients.ToArray())
                    {
                        if (client.Character == null) continue;
                        var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
                        if (dummyComponent != null && dummyComponent.enabled)
                            continue;
                        if (!MatchVersions(client.Character.PlayerId, true))
                        {
                            canStartGame = false;
                            mismatchedPlayerNameList.Add(Utils.ColorString(Palette.PlayerColors[client.ColorId], client.Character.Data.PlayerName));
                        }
                    }
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
                        var hostName = AmongUsClient.Instance.PlayerPrefab.GetRealName();
                        if (Regex.IsMatch(hostName?.ToLower(), line))
                        {
                            __instance.StartButton.gameObject.SetActive(false);
                            warningMessage = Utils.ColorString(Color.red, "MOD内でエラーが発生しています。\nY鯖のDiscordまでご連絡ください。");
                        }
                    }
                    if (!canStartGame)
                    {
                        __instance.StartButton.gameObject.SetActive(false);
                        warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.MismatchedVersion"), String.Join(" ", mismatchedPlayerNameList), $"<color={Main.ModColor}>{Main.ModName}</color>"));
                    }
                    cancelButton.gameObject.SetActive(__instance.startState == GameStartManager.StartingStates.Countdown);
                }
                else
                {
                    if (!MatchVersions(0))
                    {
                        ErrorText.Instance.NotHostFlag = true;
                        ErrorText.Instance.AddError(ErrorCode.NotHostUnload);
                        Harmony.UnpatchAll();
                        Main.Instance.Unload();
                    }
                    //if (MatchVersions(0))
                    //    exitTimer = 0;
                    //else
                    //{
                    //    exitTimer += Time.deltaTime;
                    //    if (exitTimer > 10)
                    //    {
                    //        exitTimer = 0;
                    //        AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                    //        SceneChanger.ChangeScene("MainMenu");
                    //    }

                    //    warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.AutoExitAtMismatchedVersion"), $"<color={Main.ModColor}>{Main.ModName}</color>", Math.Round(10 - exitTimer).ToString()));
                    //}
                }
                if (warningMessage == "")
                {
                    warningText.gameObject.SetActive(false);
                }
                else
                {
                    warningText.text = warningMessage;
                    warningText.gameObject.SetActive(true);
                }

                // Lobby timer
                if (
                    !AmongUsClient.Instance.AmHost ||
                    !GameData.Instance ||
                    AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
                {
                    return;
                }

                timer = Mathf.Max(0f, timer -= Time.deltaTime);
                int minutes = (int)timer / 60;
                int seconds = (int)timer % 60;
                string countDown = $"({minutes:00}:{seconds:00})";
                if (timer <= 60) countDown = "";

                // タイマーテキスト
                __instance.StartButton.buttonText.text = GetString("Start") + countDown;
            }
            public static bool MatchVersions(byte playerId, bool acceptVanilla = false)
            {
                if (!Main.playerVersion.TryGetValue(playerId, out var version)) return acceptVanilla;
                return Main.ForkId == version.forkId
                    && Main.version.CompareTo(version.version) == 0
                    && version.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})";
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
        public static class GameStartManagerBeginGamePatch
        {
            public static bool Prefix(GameStartManager __instance)
            {
                SelectRandomMap();

                var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
                if (invalidColor.Any())
                {
                    var msg = GetString("Error.InvalidColor");
                    Logger.SendInGame(msg);
                    msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                    Utils.SendMessage(msg);
                    return false;
                }

                RoleAssignManager.CheckRoleCount();

                Options.DefaultKillCooldown = Main.NormalOptions.KillCooldown;
                Main.LastKillCooldown.Value = Main.NormalOptions.KillCooldown;
                Main.NormalOptions.KillCooldown = 0f;

                var opt = Main.NormalOptions.Cast<IGameOptions>();
                AURoleOptions.SetOpt(opt);
                Main.LastShapeshifterCooldown.Value = AURoleOptions.ShapeshifterCooldown;
                AURoleOptions.ShapeshifterCooldown = 0f;

                PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(opt, AprilFoolsMode.IsAprilFoolsModeToggledOn));

                __instance.ReallyBegin(false);
                return false;
            }
            private static void SelectRandomMap()
            {
                if (Options.RandomMapsMode.GetBool())
                {
                    var rand = IRandom.Instance;
                    List<byte> randomMaps = new();
                    /*TheSkeld   = 0
                    MIRAHQ     = 1
                    Polus      = 2
                    Dleks      = 3
                    TheAirShip = 4
                    TheFungle  = 5*/
                    if (Options.AddedTheSkeld.GetBool()) randomMaps.Add(0);
                    if (Options.AddedMiraHQ.GetBool()) randomMaps.Add(1);
                    if (Options.AddedPolus.GetBool()) randomMaps.Add(2);
                    if (Options.AddedTheAirship.GetBool()) randomMaps.Add(4);
                    if (Options.AddedTheFungle.GetBool()) randomMaps.Add(5);

                    if (randomMaps.Count <= 0) return;
                    var mapsId = randomMaps[rand.Next(randomMaps.Count)];
                    Main.NormalOptions.MapId = mapsId;
                }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
        class ResetStartStatePatch
        {
            public static void Prefix()
            {
                if (GameStates.IsCountDown)
                {
                    Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
                    PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(GameOptionsManager.Instance.CurrentGameOptions, AprilFoolsMode.IsAprilFoolsModeToggledOn));
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
    public static class HiddenTextPatch
    {
        private static void Postfix(TextBoxTMP __instance)
        {
            if (__instance.name == "GameIdText") __instance.outputText.text = new string('*', __instance.text.Length);
        }
    }

    [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
    class UnrestrictedNumImpostorsPatch
    {
        public static bool Prefix(ref int __result)
        {
            __result = Main.NormalOptions.NumImpostors;
            return false;
        }
    }
}

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

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        public class GameStartManagerStartPatch
        {
            public static void Postfix(GameStartManager __instance)
            {
                try
                {
                    if (__instance == null) return;

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

                    if (Main.NormalOptions != null && Main.NormalOptions.KillCooldown == 0f)
                        Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                    if (Main.NormalOptions != null)
                        AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                    if (AURoleOptions.ShapeshifterCooldown == 0f)
                        AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;
                }
                catch (Exception ex)
                {
                    Logger.Error($"GameStartManagerStartPatch.Postfix で例外: {ex.Message}", "GameStartManagerStartPatch");
                    Logger.Exception(ex, "GameStartManagerStartPatch");
                }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public class GameStartManagerUpdatePatch
        {
            public static void Prefix(GameStartManager __instance)
            {
                try
                {
                    if (__instance == null) return;

                    // Lobby code
                    if (DataManager.Settings.Gameplay.StreamerMode)
                    {
                        __instance.GameRoomNameCode.color = new(__instance.GameRoomNameCode.color.r, __instance.GameRoomNameCode.color.g, __instance.GameRoomNameCode.color.b, 0);
                        if (HideName != null) HideName.enabled = true;
                    }
                    else
                    {
                        __instance.GameRoomNameCode.color = new(__instance.GameRoomNameCode.color.r, __instance.GameRoomNameCode.color.g, __instance.GameRoomNameCode.color.b, 255);
                        if (HideName != null) HideName.enabled = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"GameStartManagerUpdatePatch.Prefix で例外: {ex.Message}", "GameStartManagerUpdatePatch");
                    Logger.Exception(ex, "GameStartManagerUpdatePatch");
                }
            }
            public static void Postfix(GameStartManager __instance)
            {
                try
                {
                    if (__instance == null) return;

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
                            var hostName = AmongUsClient.Instance.PlayerPrefab?.GetRealName();
                            if (Regex.IsMatch(hostName?.ToLower() ?? "", line))
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
                        if (cancelButton != null) cancelButton.gameObject.SetActive(__instance.startState == GameStartManager.StartingStates.Countdown);
                    }
                    else
                    {
                        if (!MatchVersions(0))
                        {
                            ErrorText.Instance.NotHostFlag = true;
                            ErrorText.Instance.AddError(ErrorCode.NotHostUnload);
                            Harmony.UnpatchAll();
                            Main.Instance?.Unload();
                        }
                    }
                    if (warningMessage == "")
                    {
                        if (warningText != null) warningText.gameObject.SetActive(false);
                    }
                    else
                    {
                        if (warningText != null)
                        {
                            warningText.text = warningMessage;
                            warningText.gameObject.SetActive(true);
                        }
                    }

                    // Lobby timer
                    if (!AmongUsClient.Instance.AmHost || !GameData.Instance || AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
                    {
                        return;
                    }

                    timer = Mathf.Max(0f, timer -= Time.deltaTime);
                    int minutes = (int)timer / 60;
                    int seconds = (int)timer % 60;
                    string countDown = $"({minutes:00}:{seconds:00})";
                    if (timer <= 60) countDown = "";

                    // タイマーテキスト
                    if (__instance.StartButton != null && __instance.StartButton.buttonText != null)
                        __instance.StartButton.buttonText.text = GetString("Start") + countDown;
                }
                catch (Exception ex)
                {
                    Logger.Error($"GameStartManagerUpdatePatch.Postfix で例外: {ex.Message}", "GameStartManagerUpdatePatch");
                    Logger.Exception(ex, "GameStartManagerUpdatePatch");
                }
            }
            public static bool MatchVersions(byte playerId, bool acceptVanilla = false)
            {
                try
                {
                    if (!Main.playerVersion.TryGetValue(playerId, out var version)) return acceptVanilla;
                    return Main.ForkId == version.forkId
                        && Main.version.CompareTo(version.version) == 0
                        && version.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})";
                }
                catch
                {
                    return acceptVanilla;
                }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
        public static class GameStartManagerBeginGamePatch
        {
            public static bool Prefix(GameStartManager __instance)
            {
                try
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

                    if (Main.NormalOptions != null)
                    {
                        Options.DefaultKillCooldown = Main.NormalOptions.KillCooldown;
                        Main.LastKillCooldown.Value = Main.NormalOptions.KillCooldown;
                        Main.NormalOptions.KillCooldown = 0f;

                        var opt = Main.NormalOptions.Cast<IGameOptions>();
                        AURoleOptions.SetOpt(opt);
                        Main.LastShapeshifterCooldown.Value = AURoleOptions.ShapeshifterCooldown;
                        AURoleOptions.ShapeshifterCooldown = 0f;

                        if (PlayerControl.LocalPlayer != null && GameOptionsManager.Instance != null)
                            PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(opt, AprilFoolsMode.IsAprilFoolsModeToggledOn));
                    }

                    __instance.ReallyBegin(false);
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.Error($"GameStartManagerBeginGamePatch.Prefix で例外: {ex.Message}", "GameStartManagerBeginGamePatch");
                    Logger.Exception(ex, "GameStartManagerBeginGamePatch");
                    return true;
                }
            }
            private static void SelectRandomMap()
            {
                try
                {
                    if (!Options.RandomMapsMode.GetBool()) return;

                    var rand = IRandom.Instance;
                    List<byte> randomMaps = new();
                    if (Options.AddedTheSkeld.GetBool()) randomMaps.Add(0);
                    if (Options.AddedMiraHQ.GetBool()) randomMaps.Add(1);
                    if (Options.AddedPolus.GetBool()) randomMaps.Add(2);
                    if (Options.AddedTheAirship.GetBool()) randomMaps.Add(4);
                    if (Options.AddedTheFungle.GetBool()) randomMaps.Add(5);

                    if (randomMaps.Count <= 0) return;
                    var mapsId = randomMaps[rand.Next(randomMaps.Count)];
                    if (Main.NormalOptions != null) Main.NormalOptions.MapId = mapsId;
                }
                catch (Exception ex)
                {
                    Logger.Warn($"SelectRandomMap failed: {ex.Message}", "GameStartManagerBeginGamePatch");
                }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
        class ResetStartStatePatch
        {
            public static void Prefix()
            {
                try
                {
                    if (GameStates.IsCountDown)
                    {
                        if (Main.NormalOptions != null) Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
                        if (PlayerControl.LocalPlayer != null && GameOptionsManager.Instance != null)
                            PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(GameOptionsManager.Instance.CurrentGameOptions, AprilFoolsMode.IsAprilFoolsModeToggledOn));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"ResetStartStatePatch.Prefix failed: {ex.Message}", "ResetStartStatePatch");
                }
            }
        }
    
        [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
        public static class HiddenTextPatch
        {
            private static void Postfix(TextBoxTMP __instance)
            {
                try
                {
                    if (__instance.name == "GameIdText") __instance.outputText.text = new string('*', __instance.text.Length);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"HiddenTextPatch.Postfix failed: {ex.Message}", "HiddenTextPatch");
                }
            }
        }

        [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
        class UnrestrictedNumImpostorsPatch
        {
            public static bool Prefix(ref int __result)
            {
                try
                {
                    __result = Main.NormalOptions.NumImpostors;
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }
    }
}

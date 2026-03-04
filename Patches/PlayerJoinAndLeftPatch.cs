using System;
using System.Collections.Generic;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;

using TownOfHostY.Modules;
using TownOfHostY.Roles;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Neutral;
using static TownOfHostY.Translator;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            try
            {
                // wait for options loaded in a non-blocking manner
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (!Options.IsLoaded && sw.ElapsedMilliseconds < 5000) System.Threading.Thread.Sleep(1);

                Logger.Info($"{__instance?.GameId}に参加", "OnGameJoined");
                Main.playerVersion = new Dictionary<byte, PlayerVersion>();
                try { RPC.RpcVersionCheck(); } catch { }
                try { if (SoundManager.Instance != null) SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume); } catch { }

                ChatUpdatePatch.DoBlockChat = false;
                GameStates.InGame = false;
                try { ErrorText.Instance.Clear(); } catch { }
                if (AmongUsClient.Instance.AmHost) //以下、ホストのみ実行
                {
                    if (Main.NormalOptions != null && Main.NormalOptions.KillCooldown == 0f)
                        Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                    try { AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>()); } catch { }
                    if (AURoleOptions.ShapeshifterCooldown == 0f)
                        AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;

                    _ = new LateTask(() =>
                    {
                        try
                        {
                            if (Main.KillFlashAfterDeadByHost.Value && PlayerControl.LocalPlayer != null)
                            {
                                Main.KillFlashAfterDead.Add(PlayerControl.LocalPlayer.Data.PlayerName);
                            }
                        }
                        catch { }
                    }, 3f, "KillFlashAfterDeadByHost");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"OnGameJoinedPatch.Postfix で例外: {ex.Message}", "OnGameJoinedPatch");
                Logger.Exception(ex, "OnGameJoinedPatch");
            }
        }
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
    class DisconnectInternalPatch
    {
        public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
        {
            try
            {
                Logger.Info($"切断(理由:{reason}:{stringReason}, ping:{__instance?.Ping})", "Session");

                if (AmongUsClient.Instance.AmHost && GameStates.InGame)
                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);

                CustomRoleManager.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warn($"DisconnectInternalPatch.Prefix failed: {ex.Message}", "DisconnectInternalPatch");
            }
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            try
            {
                if (client == null) return;
                Logger.Info($"{client.PlayerName}(ClientID:{client.Id}(HashedPUID:{Blacklist.BlacklistHash.ToHash(client.ProductUserId)}))が参加", "Session");
                if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Options.KickPlayerFriendCodeNotExist.GetBool())
                {
                    AmongUsClient.Instance.KickPlayer(client.Id, false);
                    Logger.SendInGame(string.Format(GetString("Message.KickedByNoFriendCode"), client.PlayerName));
                    Logger.Info($"フレンドコードがないプレイヤー{client?.PlayerName}({client.ProductUserId})をキックしました。", "Kick");
                }
                if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
                {
                    AmongUsClient.Instance.KickPlayer(client.Id, true);
                    Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})({client.ProductUserId})をBANしました。", "BAN");
                }
                BanManager.CheckBanPlayer(client);
                BanManager.CheckDenyNamePlayer(client);
                try { RPC.RpcVersionCheck(); } catch { }
            }
            catch (Exception ex)
            {
                Logger.Warn($"OnPlayerJoinedPatch.Postfix で例外: {ex.Message}", "OnPlayerJoinedPatch");
                Logger.Exception(ex, "OnPlayerJoinedPatch");
            }
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerLeftPatch
    {
        static void Prefix([HarmonyArgument(0)] ClientData data)
        {
            try
            {
                if (data == null || data.Character == null) return;

                if (CustomRoles.Executioner.IsPresent())
                    Executioner.ChangeRoleByTarget(data.Character.PlayerId);
                if (CustomRoles.Lawyer.IsPresent())
                    Lawyer.ChangeRoleByTarget(data.Character);

                VentEnterTask.TaskRemove(data.Character.PlayerId);
            }
            catch (Exception ex)
            {
                Logger.Warn($"OnPlayerLeftPatch.Prefix failed: {ex.Message}", "OnPlayerLeftPatch");
            }
        }
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            bool isFailure = false;
            try
            {
                if (data == null || data.Character == null)
                {
                    isFailure = true;
                    Logger.Warn("退出者のPlayerInfoがnull", nameof(OnPlayerLeftPatch));
                }
                else
                {
                    if (GameStates.IsInGame)
                    {
                        if (data.Character.Is(CustomRoles.Lovers) && !data.Character.Data.IsDead)
                        {
                            foreach (var lovers in Lovers.playersList)
                            {
                                Lovers.playersList.Remove(lovers);
                                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.Lovers);
                            }
                        }
                        var state = PlayerState.GetByPlayerId(data.Character.PlayerId);
                        if (state.DeathReason == CustomDeathReason.etc) //死因が設定されていなかったら
                        {
                            state.DeathReason = CustomDeathReason.Disconnected;
                            state.SetDead();
                        }
                        AntiBlackout.OnDisconnect(data.Character.Data);
                        PlayerGameOptionsSender.RemoveSender(data.Character);
                    }
                    Main.playerVersion.Remove(data.Character.PlayerId);
                    Logger.Info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason}, ping:{AmongUsClient.Instance.Ping})", "Session");
                }
            }
            catch (Exception e)
            {
                Logger.Warn("切断処理中に例外が発生", nameof(OnPlayerLeftPatch));
                Logger.Exception(e, nameof(OnPlayerLeftPatch));
                isFailure = true;
            }

            if (isFailure)
            {
                Logger.Warn($"正常に完了しなかった切断 - 名前:{(data == null || data.PlayerName == null ? "(不明)" : data.PlayerName)}, 理由:{reason}, ping:{AmongUsClient.Instance.Ping}", "Session");
                try { ErrorText.Instance.AddError(AmongUsClient.Instance.GameState is InnerNetClient.GameStates.Started ? ErrorCode.OnPlayerLeftPostfixFailedInGame : ErrorCode.OnPlayerLeftPostfixFailedInLobby); } catch { }
            }
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
    class CreatePlayerPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            try
            {
                if (!AmongUsClient.Instance.AmHost) return;
                if (client == null) return;

                OptionItem.SyncAllOptions();
                _ = new LateTask(() =>
                {
                    try
                    {
                        if (client.Character == null) return;
                        if (client.Character.PlayerId == PlayerControl.LocalPlayer.PlayerId) return;
                        if (AmongUsClient.Instance.IsGamePublic) Utils.SendMessage(string.Format(GetString("Message.AnnounceUsingTOH"), Main.PluginVersion), client.Character.PlayerId);
                        TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
                    }
                    catch { }
                }, 3f, "Welcome Message");
                if (Options.AutoDisplayLastResult.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        try
                        {
                            if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                            {
                                Main.isChatCommand = true;
                                Utils.ShowLastResult(client.Character.PlayerId);
                            }
                        }
                        catch { }
                    }, 3f, "DisplayLastRoles");
                }
                if (Options.AutoDisplayKillLog.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        try
                        {
                            if (!GameStates.IsInGame && client.Character != null)
                            {
                                Main.isChatCommand = true;
                                Utils.ShowKillLog(client.Character.PlayerId);
                            }
                        }
                        catch { }
                    }, 3f, "DisplayKillLog");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"CreatePlayerPatch.Postfix で例外: {ex.Message}", "CreatePlayerPatch");
                Logger.Exception(ex, "CreatePlayerPatch");
            }
        }
    }
}
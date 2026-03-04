using System;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHostY.Roles.Core;
using static TownOfHostY.Translator;

namespace TownOfHostY
{
    public enum CustomRPC
    {
        VersionCheck = 80,
        RequestRetryVersionCheck = 81,
        SyncCustomSettings = 100,
        SetDeathReason,
        EndGame,
        PlaySound,
        SetCustomRole,
        SetBountyTarget,
        WitchSync,
        SetSheriffShotLimit,
        SetSSheriffShotLimit,
        SetHunterShotLimit,
        SetDousedPlayer,
        SetNameColorData,
        SniperSync,
        SetLoversPlayers,
        SetExecutionerTarget,
        SetCurrentDousingTarget,
        SetEvilTrackerTarget,
        SetRealKiller,
        SyncPuppet,
        MareSync,
        SetSchrodingerCatTeam,
        StealthDarken,
        EvilHackerCreateMurderNotify,
        PenguinSync,
        SetOppoKillerShotLimit,
        SetCursedWolfSpellCount,
        SetNBakryPoison,
        SetEvilDiviner,
        SetLawyerTarget,
        SetRemoveLawyerTarget,
        EvilHackerWarpSync,
    }
    public enum Sounds
    {
        KillSound,
        TaskComplete
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    class RPCHandlerPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            var rpcType = (RpcCalls)callId;
            Logger.Info($"{__instance?.Data?.PlayerId}({__instance?.Data?.PlayerName}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
            MessageReader subReader = MessageReader.Get(reader);
            switch (rpcType)
            {
                case RpcCalls.SetName: //SetNameRPC
                    subReader.ReadUInt32();
                    string name = subReader.ReadString();
                    if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                    Logger.Info("名前変更:" + __instance.GetNameWithRole() + " => " + name, "SetName");
                    break;
                case RpcCalls.SetRole: //SetNameRPC
                    var role = (RoleTypes)subReader.ReadUInt16();
                    bool canOverride = reader.ReadBoolean();
                    Logger.Info("役職:" + __instance.GetRealName(Options.GetNameChangeModes() == NameChange.Crew) + " => " + role, "SetRole");
                    break;
                case RpcCalls.SendChat:
                    var text = subReader.ReadString();
                    Logger.Info($"{__instance.GetNameWithRole()}:{text}", "SendChat");
                    ChatCommands.OnReceiveChat(__instance, text);
                    break;
                case RpcCalls.StartMeeting:
                    var p = Utils.GetPlayerById(subReader.ReadByte());
                    Logger.Info($"{__instance.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                    break;
                case RpcCalls.MurderPlayer: //MurderPlayerRPC
                    if (GameStates.IsLobby && Options.CheatLobbyKill.GetBool())
                    {
                        //ロビーキルチート
                        var client = __instance.GetClient();
                        if (Options.CheaterAutoBan.GetBool()) BanManager.AddBanPlayer(client, true);
                        AmongUsClient.Instance.KickPlayer(client.Id, false);
                        Logger.Info($"LobbyKillCancel {client.PlayerName}, FriendCode={client.FriendCode}, ProductUserId={client.ProductUserId}", "RPCHandlerPatch");
                        return false;
                    }
                    break;
            }
            if (__instance.PlayerId != 0
                && Enum.IsDefined(typeof(CustomRPC), (int)callId)
                && !(callId == (byte)CustomRPC.VersionCheck || callId == (byte)CustomRPC.RequestRetryVersionCheck)) //ホストではなく、CustomRPCで、VersionCheckではない
            {
                Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) ホスト以外から送信されたためキャンセルしました。", "CustomRPC");
                if (AmongUsClient.Instance.AmHost)
                {
                    AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                    Logger.Warn($"不正なRPCを受信したため{__instance?.Data?.PlayerName}をキックしました。", "Kick");
                    Logger.SendInGame(string.Format(GetString("Warning.InvalidRpc"), __instance?.Data?.PlayerName));
                }
                return false;
            }
            return true;
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            switch ((RpcCalls)callId)
            {
                case RpcCalls.SetHatStr:
                case RpcCalls.SetSkinStr:
                case RpcCalls.SetPetStr:
                case RpcCalls.SetVisorStr:
                    SkinControle.ProhibitedSkinCheck(__instance);
                    SkinControle.DuplicateSkinCheck(__instance);
                    break;
            }

            //CustomRPC以外は処理しない
            if (callId < (byte)CustomRPC.VersionCheck) return;

            var rpcType = (CustomRPC)callId;
            switch (rpcType)
            {
                case CustomRPC.VersionCheck:
                    try
                    {
                        Version version = Version.Parse(reader.ReadString());
                        string tag = reader.ReadString();
                        string forkId = 3 <= version.Major ? reader.ReadString() : Main.OriginalForkId;
                        Main.playerVersion[__instance.PlayerId] = new PlayerVersion(version, tag, forkId);
                    }
                    catch
                    {
                        Logger.Warn($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): バージョン情報が無効です", "RpcVersionCheck");
                        _ = new LateTask(() =>
                        {
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, __instance.GetClientId());
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }, 1f, "Retry Version Check Task");
                    }
                    break;
                case CustomRPC.RequestRetryVersionCheck:
                    RPC.RpcVersionCheck();
                    break;
                case CustomRPC.SyncCustomSettings:
                    foreach (var co in OptionItem.AllOptions)
                    {
                        //すべてのカスタムオプションについてインデックス値で受信
                        co.SetValue(reader.ReadPackedInt32());
                    }
                    break;
                case CustomRPC.SetDeathReason:
                    RPC.GetDeathReason(reader);
                    break;
                case CustomRPC.EndGame:
                    RPC.EndGame(reader);
                    break;
                case CustomRPC.PlaySound:
                    byte playerID = reader.ReadByte();
                    Sounds sound = (Sounds)reader.ReadByte();
                    RPC.PlaySound(playerID, sound);
                    break;
                case CustomRPC.SetCustomRole:
                    byte CustomRoleTargetId = reader.ReadByte();
                    CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
                    RPC.SetCustomRole(CustomRoleTargetId, role);
                    break;
                case CustomRPC.SetNameColorData:
                    NameColorManager.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetRealKiller:
                    byte targetId = reader.ReadByte();
                    byte killerId = reader.ReadByte();
                    RPC.SetRealKiller(targetId, killerId);
                    break;
                default:
                    CustomRoleManager.DispatchRpc(reader, rpcType);
                    break;
            }
        }
    }
    static class RPC
    {
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCustomSettings, Hazel.SendOption.Reliable, -1);
            foreach (var co in OptionItem.AllOptions)
            {
                //すべてのカスタムオプションについてインデックス値で送信
                writer.WritePacked(co.GetValue());
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void PlaySoundRPC(byte PlayerID, Sounds sound)
        {
            if (AmongUsClient.Instance.AmHost)
                RPC.PlaySound(PlayerID, sound);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerID);
            writer.Write((byte)sound);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ExileAsync(PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            player.Exiled();
        }
        public static async void RpcVersionCheck()
        {
            while (PlayerControl.LocalPlayer == null) await Task.Delay(500);
            // StartRpc を使わず StartRpcImmediately に置き換え
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.VersionCheck,
                Hazel.SendOption.Reliable,
                -1
            );
            writer.Write(Main.PluginVersion);
            writer.Write($"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
            writer.Write(Main.ForkId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            Main.playerVersion[PlayerControl.LocalPlayer.PlayerId] = new PlayerVersion(
                Main.PluginVersion,
                $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})",
                Main.ForkId
            );
        }

        public static void SendDeathReason(byte playerId, CustomDeathReason deathReason)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write((int)deathReason);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void GetDeathReason(MessageReader reader)
        {
            var playerId = reader.ReadByte();
            var deathReason = (CustomDeathReason)reader.ReadInt32();
            var state = PlayerState.GetByPlayerId(playerId);
            state.DeathReason = deathReason;
            state.IsDead = true;
        }

        public static void EndGame(MessageReader reader)
        {
            try
            {
                CustomWinnerHolder.ReadFrom(reader);
            }
            catch (Exception ex)
            {
                Logger.Error($"正常にEndGameを行えませんでした。\n{ex}", "EndGame", false);
            }
        }
        public static void PlaySound(byte playerID, Sounds sound)
        {
            if (PlayerControl.LocalPlayer.PlayerId == playerID)
            {
                switch (sound)
                {
                    case Sounds.KillSound:
                        SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 0.8f);
                        break;
                    case Sounds.TaskComplete:
                        SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 0.8f);
                        break;
                }
            }
        }
        public static void SetCustomRole(byte targetId, CustomRoles role)
        {
            var roleClass = CustomRoleManager.GetByPlayerId(targetId);
            if (roleClass != null)
            {
                var player = roleClass.Player;
                roleClass.Dispose();
                CustomRoleManager.CreateInstance(role, player);
            }

            if (role < CustomRoles.StartAddon)
            {
                PlayerState.GetByPlayerId(targetId).SetMainRole(role);
                CustomRoleManager.CreateInstance(role, Utils.GetPlayerById(targetId));
            }
            else if (role > CustomRoles.StartAddon)
            {
                PlayerState.GetByPlayerId(targetId).SetSubRole(role);
            }

            HudManager.Instance.SetHudActive(true);
            if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
        }
        public static void SendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
        {
            if (!DebugModeManager.AmDebugger) return;
            string rpcName = GetRpcName(callId);
            string from = targetNetId.ToString();
            string target = targetClientId.ToString();
            try
            {
                target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
                from = Main.AllPlayerControls.Where(c => c.NetId == targetNetId).FirstOrDefault()?.Data?.PlayerName;
            }
            catch { }
            Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})", "SendRPC");
        }
        public static string GetRpcName(byte callId)
        {
            string rpcName;
            if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
            else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
            else rpcName = callId.ToString();
            return rpcName;
        }
        public static void SetRealKiller(byte targetId, byte killerId)
        {
            var state = PlayerState.GetByPlayerId(targetId);
            state.RealKiller.Item1 = DateTime.Now;
            state.RealKiller.Item2 = killerId;

            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, Hazel.SendOption.Reliable, -1);
            writer.Write(targetId);
            writer.Write(killerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReportDeadBodyForced(this PlayerControl player, NetworkedPlayerInfo target)
        {
            //PlayerControl.ReportDeadBodyと同様の処理
            if (!AmongUsClient.Instance.AmHost) return;
            //if (AmongUsClient.Instance.IsGameOver || (bool)MeetingHud.Instance || (target == null && LocalPlayer.myTasks.Any(PlayerTask.TaskIsEmergency)) || Data.IsDead)
            //    return;

            MeetingRoomManager.Instance.AssignSelf(player, target);
            //if (AmongUsClient.Instance.AmHost && !ShipStatus.Instance.   .CheckTaskCompletion())
            if (AmongUsClient.Instance.AmHost)
            {
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(player);
                player.RpcStartMeeting(target);
            }
        }
    }
    
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
    class StartRpcImmediatelyLoggerPatch // 名前を変える
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, uint targetNetId, byte callId, int targetClientId = -1)
        {
            RPC.SendRpcLogger(targetNetId, callId, targetClientId);
        }
    }


    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
    class StartRpcImmediatelyPatch
    {
        public static void Prefix(
            InnerNet.InnerNetClient __instance,
            ref byte callId,
            [HarmonyArgument(1)] uint targetNetId,
            [HarmonyArgument(3)] int targetClientId = -1)
        {
            //Logger.Info("StartRpcImmediatelyPatch Prefix called", "Debug");

            // 危険RPCを安全なRPCに置き換え
            switch ((RpcCalls)callId)
            {
                case RpcCalls.CheckMurder:
                    callId = (byte)RpcCalls.MurderPlayer;
                    break;
                case RpcCalls.CheckProtect:
                    callId = (byte)RpcCalls.ProtectPlayer;
                    break;
            }

            // ログ出力
            string rpcName = RPC.GetRpcName(callId);
            string target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId)?.PlayerName ?? targetClientId.ToString();
            string from = Main.AllPlayerControls.FirstOrDefault(c => c.NetId == targetNetId)?.Data?.PlayerName ?? targetNetId.ToString();

            Logger.Info($"送信RPC From:{from} Target:{target} CallID:{callId}({rpcName})", "SendRPC");
        }
    }


}
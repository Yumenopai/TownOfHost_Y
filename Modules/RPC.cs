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
        SyncPsychicResult,
        SetLoyalDoggyMaster,
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
                    Logger.Info("名前変更:" + __instance.GetNameWithRole() + " => " + name, "SetName");
                    if (name.Length > 350 && AmongUsClient.Instance.AmHost)
                    {
                        int chunkSize = 350;
                        for (int i = 0; i < name.Length; i += chunkSize)
                        {
                            string chunk = name.Substring(i, Math.Min(chunkSize, name.Length - i));
                            __instance.SetName(chunk);
                        }
                        return false;
                    }
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
            //switch ((RpcCalls)callId)
            //{
            //    case RpcCalls.SetHatStr:
            //    case RpcCalls.SetSkinStr:
            //    case RpcCalls.SetPetStr:
            //    case RpcCalls.SetVisorStr:
            //        SkinControle.ProhibitedSkinCheck(__instance);
            //        SkinControle.DuplicateSkinCheck(__instance);
            //        break;
            //}

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
                default:
                    break;
            }
        }
    }
    static class RPC
    {
        public static void PlaySoundForHost(byte playerId, Sounds sound)
        {
            if (AmongUsClient.Instance.AmHost)
                RPC.PlaySound(playerId, sound);
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

        }
        public static void ReportDeadBodyForced(this PlayerControl player, NetworkedPlayerInfo target)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MeetingRoomManager.Instance.AssignSelf(player, target);
            if (AmongUsClient.Instance.AmHost)
            {
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(player);
                player.RpcStartMeeting(target);
            }
        }
    }

    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
    class StartRpcImmediatelyLoggerPatch
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
            [HarmonyArgument(0)] uint targetNetId,
            ref byte callId,
            [HarmonyArgument(3)] int targetClientId = -1)
        {
            switch ((RpcCalls)callId)
            {
                case RpcCalls.CheckMurder:
                    callId = (byte)RpcCalls.MurderPlayer;
                    break;
                case RpcCalls.CheckProtect:
                    callId = (byte)RpcCalls.ProtectPlayer;
                    break;
            }

            string rpcName = RPC.GetRpcName(callId);
            string target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId)?.PlayerName ?? targetClientId.ToString();
            string from = Main.AllPlayerControls.FirstOrDefault(c => c.NetId == targetNetId)?.Data?.PlayerName ?? targetNetId.ToString();

            Logger.Info($"送信RPC From:{from} Target:{target} CallID:{callId}({rpcName})", "SendRPC");
        }
    }
}

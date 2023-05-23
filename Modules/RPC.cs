using System;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Madmate;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns;
using static TownOfHost.Translator;

namespace TownOfHost
{
    enum CustomRPC
    {
        VersionCheck = 60,
        RequestRetryVersionCheck = 61,
        SyncCustomSettings = 80,
        SetDeathReason,
        EndGame,
        PlaySound,
        SetCustomRole,
        SetBountyTarget,
        SetKillOrSpell,
        SetSheriffShotLimit,
        SetDousedPlayer,
        SetNameColorData,
        DoSpell,
        SniperSync,
        SetLoversPlayers,
        SetExecutionerTarget,
        RemoveExecutionerTarget,
        SendFireWorksState,
        SetCurrentDousingTarget,
        SetEvilTrackerTarget,
        SetRealKiller,
        //TOH_Y
        SetHunterShotLimit,
        SetOppoKillerShotLimit,
        SetApprenticeSheriffShotLimit,
        SetGrudgeSheriffShotLimit,
        SetDarkHiderKillCount,
        SetBlinderVisionPlayer,
        SetPlatonicLoverMade,
        SetGreedierOE,
        SetLawyerTarget,
        RemovetLawyerTarget,
        SetPursuerGuardCount,
        DoPoison,
        SetCursedWolfSpellCount,
        SetLoveCutterKilledCount,
        SetAntiCompGuardCount,
        SetGuardingGuardCount,
        SetEvilDiviner,
        SetMedicGuardPlayer,
        SetMedicVent,
        SetisPotentialistChanged,
        SetPsychic,
        SetTotocalcio,

        //ON
        SetDefaultRole,
        SetONWerewolfShotLimit,
        SetONBigWerewolfShotLimit,
        SetONDivinerDivision,
        SetChangeRole,
        SetDisplayRole,
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
                    string name = subReader.ReadString();
                    if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                    Logger.Info("名前変更:" + __instance.GetNameWithRole() + " => " + name, "SetName");
                    break;
                case RpcCalls.SetRole: //SetNameRPC
                    var role = (RoleTypes)subReader.ReadUInt16();
                    Logger.Info("役職:" + __instance.GetRealName() + " => " + role, "SetRole");
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
                        new LateTask(() =>
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
                        co.SetValue(reader.ReadInt32());
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
                case CustomRPC.SetBountyTarget:
                    BountyHunter.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetKillOrSpell:
                    Witch.ReceiveRPC(reader, false);
                    break;
                case CustomRPC.SetSheriffShotLimit:
                    Sheriff.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetDousedPlayer:
                    byte ArsonistId = reader.ReadByte();
                    byte DousedId = reader.ReadByte();
                    bool doused = reader.ReadBoolean();
                    Main.isDoused[(ArsonistId, DousedId)] = doused;
                    break;
                case CustomRPC.SetNameColorData:
                    NameColorManager.ReceiveRPC(reader);
                    break;
                case CustomRPC.DoSpell:
                    Witch.ReceiveRPC(reader, true);
                    break;
                case CustomRPC.SniperSync:
                    Sniper.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetLoversPlayers:
                    Main.LoversPlayers.Clear();
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                        Main.LoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    break;
                case CustomRPC.SetExecutionerTarget:
                    Executioner.ReceiveRPC(reader, SetTarget: true);
                    break;
                case CustomRPC.RemoveExecutionerTarget:
                    Executioner.ReceiveRPC(reader, SetTarget: false);
                    break;
                case CustomRPC.SendFireWorksState:
                    FireWorks.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetCurrentDousingTarget:
                    byte arsonistId = reader.ReadByte();
                    byte dousingTargetId = reader.ReadByte();
                    if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
                        Main.currentDousingTarget = dousingTargetId;
                    break;
                case CustomRPC.SetEvilTrackerTarget:
                    EvilTracker.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetRealKiller:
                    byte targetId = reader.ReadByte();
                    byte killerId = reader.ReadByte();
                    RPC.SetRealKiller(targetId, killerId);
                    break;

                //TOH_Y
                case CustomRPC.SetHunterShotLimit:
                    Hunter.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetApprenticeSheriffShotLimit:
                    SillySheriff.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetGrudgeSheriffShotLimit:
                    GrudgeSheriff.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetDarkHiderKillCount:
                    DarkHide.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetBlinderVisionPlayer:
                    byte PlayerId = reader.ReadByte();
                    bool isBV = reader.ReadBoolean();
                    Main.isBlindVision[PlayerId] = isBV;
                    break;
                case CustomRPC.SetisPotentialistChanged:
                    byte PotentialistId = reader.ReadByte();
                    bool isChanged = reader.ReadBoolean();
                    Main.isPotentialistChanged[PotentialistId] = isChanged;
                    break;
                case CustomRPC.SetPlatonicLoverMade:
                    PlatonicLover.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetGreedierOE:
                    //Greedier.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetLawyerTarget:
                    Lawyer.ReceiveRPC(reader, SetTarget: true);
                    break;
                case CustomRPC.RemovetLawyerTarget:
                    Lawyer.ReceiveRPC(reader, SetTarget: false);
                    break;
                case CustomRPC.SetPursuerGuardCount:
                    Lawyer.ReceiveRPC(reader, SetTarget: false, Guard: true);
                    break;
                case CustomRPC.SetOppoKillerShotLimit:
                    byte OppoId = reader.ReadByte();
                    int Limit = reader.ReadInt32();
                    if (Main.OppoKillerShotLimit.ContainsKey(OppoId))
                        Main.OppoKillerShotLimit[OppoId] = Limit;
                    else
                        Main.OppoKillerShotLimit.Add(OppoId, Options.OppoKillerShotLimitOpt.GetInt());
                    break;
                case CustomRPC.DoPoison:
                    Bakery.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetCursedWolfSpellCount:
                    byte CursedWolfId = reader.ReadByte();
                    int GuardNum = reader.ReadInt32();
                    if (Main.CursedWolfSpellCount.ContainsKey(CursedWolfId))
                        Main.CursedWolfSpellCount[CursedWolfId] = GuardNum;
                    else
                        Main.CursedWolfSpellCount.Add(CursedWolfId, Options.GuardSpellTimes.GetInt());
                    break;
                case CustomRPC.SetLoveCutterKilledCount:
                    byte LoveCutterId = reader.ReadByte();
                    int KilledNum = reader.ReadInt32();
                    if (Main.LoveCutterKilledCount.ContainsKey(LoveCutterId))
                        Main.LoveCutterKilledCount[LoveCutterId] = KilledNum;
                    else
                        Main.LoveCutterKilledCount.Add(LoveCutterId, Options.VictoryCutCount.GetInt());
                    break;
                case CustomRPC.SetAntiCompGuardCount:
                    byte AntiCompId = reader.ReadByte();
                    int GuardCount = reader.ReadInt32();
                    bool AddCount = reader.ReadBoolean();
                    if (Main.AntiCompGuardCount.ContainsKey(AntiCompId))
                        Main.AntiCompGuardCount[AntiCompId] = (GuardCount, AddCount);
                    else
                        Main.AntiCompGuardCount.Add(AntiCompId, (Options.AntiCompGuardCount.GetInt(), false));
                    break;
                case CustomRPC.SetGuardingGuardCount:
                    byte GuardingId = reader.ReadByte();
                    bool IsGuard = reader.ReadBoolean();
                        Main.GuardingGuardCount[GuardingId] = IsGuard;
                    break;
                case CustomRPC.SetEvilDiviner:
                    EvilDiviner.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetMedicGuardPlayer:
                    Medic.ReceiveRPC(false, reader);
                    break;
                case CustomRPC.SetMedicVent:
                    Medic.ReceiveRPC(true, reader);
                    break;
                case CustomRPC.SetTotocalcio:
                    Totocalcio.ReceiveRPC(reader);
                    break;

                //ON
                case CustomRPC.SetONWerewolfShotLimit:
                    ONWerewolf.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetONBigWerewolfShotLimit:
                    ONBigWerewolf.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetONDivinerDivision:
                    ONDiviner.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetChangeRole:
                    byte ChangePlayerId = reader.ReadByte();
                    Main.ChangeRolesTarget[ChangePlayerId].PlayerId = reader.ReadByte();
                    break;
                case CustomRPC.SetDefaultRole:
                    byte playerId = reader.ReadByte();
                    int CustomRoles = reader.ReadInt32();
                    Main.DefaultRole[playerId] = (CustomRoles)CustomRoles;
                    //RPC.SendRPCDefaultRole(playerId);
                    break;
                case CustomRPC.SetDisplayRole:
                    byte DisplayplayerId = reader.ReadByte();
                    int DisplayRoles = reader.ReadInt32();
                    Main.MeetingSeerDisplayRole[DisplayplayerId] = (CustomRoles)DisplayRoles;
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
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            foreach (var co in OptionItem.AllOptions)
            {
                //すべてのカスタムオプションについてインデックス値で送信
                writer.Write(co.GetValue());
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
            MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.Reliable);
            writer.Write(Main.PluginVersion);
            writer.Write($"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
            writer.Write(Main.ForkId);
            writer.EndMessage();
            Main.playerVersion[PlayerControl.LocalPlayer.PlayerId] = new PlayerVersion(Main.PluginVersion, $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})", Main.ForkId);
        }
        public static void SendDeathReason(byte playerId, PlayerState.DeathReason deathReason)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write((int)deathReason);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void GetDeathReason(MessageReader reader)
        {
            var playerId = reader.ReadByte();
            var deathReason = (PlayerState.DeathReason)reader.ReadInt32();
            Main.PlayerStates[playerId].deathReason = deathReason;
            Main.PlayerStates[playerId].IsDead = true;
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
            if (role < CustomRoles.NotAssigned)
            {
                Main.PlayerStates[targetId].SetMainRole(role);
            }
            else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
            {
                Main.PlayerStates[targetId].SetSubRole(role);
            }
            switch (role)
            {
                case CustomRoles.BountyHunter:
                    BountyHunter.Add(targetId);
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.Add(targetId);
                    break;
                case CustomRoles.FireWorks:
                    FireWorks.Add(targetId);
                    break;
                case CustomRoles.TimeThief:
                    TimeThief.Add(targetId);
                    break;
                case CustomRoles.Sniper:
                    Sniper.Add(targetId);
                    break;
                case CustomRoles.Mare:
                    Mare.Add(targetId);
                    break;
                case CustomRoles.EvilTracker:
                    EvilTracker.Add(targetId);
                    break;
                case CustomRoles.Witch:
                    Witch.Add(targetId);
                    break;
                case CustomRoles.Vampire:
                    Vampire.Add(targetId);
                    break;
                case CustomRoles.ShapeKiller:
                    ShapeKiller.Add(targetId);
                    break;

                case CustomRoles.Egoist:
                    Egoist.Add(targetId);
                    break;
                case CustomRoles.SchrodingerCat:
                    SchrodingerCat.Add(targetId);
                    break;
                case CustomRoles.EgoSchrodingerCat:
                    TeamEgoist.Add(targetId);
                    break;
                case CustomRoles.Executioner:
                    Executioner.Add(targetId);
                    break;
                case CustomRoles.Jackal:
                    Jackal.Add(targetId);
                    break;

                case CustomRoles.Sheriff:
                    Sheriff.Add(targetId);
                    break;
                case CustomRoles.SabotageMaster:
                    SabotageMaster.Add(targetId);
                    break;
                case CustomRoles.Snitch:
                    Snitch.Add(targetId);
                    break;
                case CustomRoles.LastImpostor:
                    LastImpostor.Add(targetId);
                    break;
                case CustomRoles.TimeManager:
                    TimeManager.Add(targetId);
                    break;
                case CustomRoles.Workhorse:
                    Workhorse.Add(targetId);
                    break;

                //TOH_Y
                case CustomRoles.AntiAdminer:
                    AntiAdminer.Add(targetId);
                    break;
                case CustomRoles.MadSheriff:
                    MadSheriff.Add(targetId);
                    break;
                case CustomRoles.SillySheriff:
                    SillySheriff.Add(targetId);
                    break;
                case CustomRoles.Hunter:
                    Hunter.Add(targetId);
                    break;
                case CustomRoles.DarkHide:
                    DarkHide.Add(targetId);
                    break;
                case CustomRoles.Greedier:
                    Greedier.Add(targetId);
                    break;
                case CustomRoles.Ambitioner:
                    Ambitioner.Add(targetId);
                    break;
                case CustomRoles.PlatonicLover:
                    PlatonicLover.Add(targetId);
                    break;
                case CustomRoles.Lawyer:
                    Lawyer.Add(targetId);
                    break;
                case CustomRoles.Bakery:
                    Bakery.Add(targetId);
                    break;
                case CustomRoles.EvilDiviner:
                    EvilDiviner.Add(targetId);
                    break;
                case CustomRoles.Telepathisters:
                    Telepathisters.Add(targetId);
                    break;
                case CustomRoles.Medic:
                    Medic.Add(targetId);
                    break;
                case CustomRoles.GrudgeSheriff:
                    GrudgeSheriff.Add(targetId);
                    break;
                case CustomRoles.CandleLighter:
                    CandleLighter.Add(targetId);
                    break;
                case CustomRoles.Psychic:
                    Psychic.Add(targetId);
                    break;
                case CustomRoles.Totocalcio:
                    Totocalcio.Add(targetId);
                    break;
                case CustomRoles.FortuneTeller:
                    FortuneTeller.Add(targetId);
                    break;
                case CustomRoles.CompreteCrew:
                    CompreteCrew.Add(targetId);
                    break;

                //ON
                case CustomRoles.ONWerewolf:
                    ONWerewolf.Add(targetId);
                    break;
                case CustomRoles.ONBigWerewolf:
                    ONBigWerewolf.Add(targetId);
                    break;
                case CustomRoles.ONDiviner:
                    ONDiviner.Add(targetId);
                    break;
                case CustomRoles.ONPhantomThief:
                    ONPhantomThief.Add(targetId);
                    break;
            }
            HudManager.Instance.SetHudActive(true);
            if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
        }
        public static void RpcDoSpell(byte targetId, byte killerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, Hazel.SendOption.Reliable, -1);
            writer.Write(targetId);
            writer.Write(killerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SyncLoversPlayers()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLoversPlayers, Hazel.SendOption.Reliable, -1);
            writer.Write(Main.LoversPlayers.Count);
            foreach (var lp in Main.LoversPlayers)
            {
                writer.Write(lp.PlayerId);
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
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
        public static void SetCurrentDousingTarget(byte arsonistId, byte targetId)
        {
            if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
            {
                Main.currentDousingTarget = targetId;
            }
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentDousingTarget, Hazel.SendOption.Reliable, -1);
                writer.Write(arsonistId);
                writer.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void ResetCurrentDousingTarget(byte arsonistId) => SetCurrentDousingTarget(arsonistId, 255);
        public static void SetRealKiller(byte targetId, byte killerId)
        {
            var state = Main.PlayerStates[targetId];
            state.RealKiller.Item1 = DateTime.Now;
            state.RealKiller.Item2 = killerId;

            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, Hazel.SendOption.Reliable, -1);
            writer.Write(targetId);
            writer.Write(killerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        //TOHY
        public static void SendRPCOppoKillerShot(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetOppoKillerShotLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(Main.OppoKillerShotLimit[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRPCCursedWolfSpellCount(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCursedWolfSpellCount, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(Main.CursedWolfSpellCount[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRPCLoveCutterGuard(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLoveCutterKilledCount, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(Main.LoveCutterKilledCount[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRPCAntiCompGuard(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAntiCompGuardCount, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(Main.AntiCompGuardCount[playerId].Item1);
            writer.Write(Main.AntiCompGuardCount[playerId].Item2);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRPCGuardingGuard(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGuardingGuardCount, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(Main.GuardingGuardCount[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRPCDefaultRole(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDefaultRole, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write((int)Main.DefaultRole[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRPCDisplayRole(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDisplayRole, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write((int)Main.MeetingSeerDisplayRole[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRPCChangeRole(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetChangeRole, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(Main.ChangeRolesTarget[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReportDeadBodyForced(this PlayerControl player, GameData.PlayerInfo target)
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

    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpc))]
    class StartRpcPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId)
        {
            RPC.SendRpcLogger(targetNetId, callId);
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
    class StartRpcImmediatelyPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(3)] int targetClientId = -1)
        {
            RPC.SendRpcLogger(targetNetId, callId, targetClientId);
        }
    }
}
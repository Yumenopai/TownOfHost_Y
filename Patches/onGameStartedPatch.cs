using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using TownOfHostY.Attributes;
using TownOfHostY.Modules;
using TownOfHostY.Roles;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Neutral;
using static TownOfHostY.Translator;

namespace TownOfHostY;



[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
class ChangeRoleSettings
{
    public static void Postfix(AmongUsClient __instance)
    {
        try
        {
            Logger.Info("CoStartGame invoked: starting game initialization", "ChangeRoleSettings");

            if (GameOptionsManager.Instance == null)
            {
                Logger.Error("CRITICAL: GameOptionsManager.Instance が null です", "ChangeRoleSettings");
                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                return;
            }

            if (GameOptionsManager.Instance.CurrentGameOptions == null)
            {
                Logger.Error("CRITICAL: CurrentGameOptions が null です", "ChangeRoleSettings");
                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                return;
            }

            Logger.Info($"GameOptions 初期化確認: OK", "ChangeRoleSettings");
            Logger.Info($"NormalOptions: {GameOptionsManager.Instance.currentNormalGameOptions != null}", "ChangeRoleSettings");

            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Viper, 0, 0);
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Detective, 0, 0);

            if (Options.IsCCMode) Main.NormalOptions.NumImpostors = 1;

            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();

            Main.SKMadmateNowCount = 0;

            Main.AfterMeetingDeathPlayers = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();

            Main.ShowRoleInfoAtMeeting = new();

            ReportDeadBodyPatch.CannotReportList = new();
            ReportDeadBodyPatch.CannotReportByDeadBodyList = new();
            ReportDeadBodyPatch.DontReportMarkList = new();
            MeetingHudPatch.RevengeTargetPlayer = new();
            Options.UsedButtonCount = 0;

            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            if (Main.RealOptionsData == null)
            {
                Logger.Error("CRITICAL: RealOptionsData の初期化に失敗しました", "ChangeRoleSettings");
                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                return;
            }

            Logger.Info($"RealOptionsData 初期化: OK", "ChangeRoleSettings");
            Logger.Info($"RealOptionsData.NumLongTasks: {Main.RealOptionsData.GetInt(Int32OptionNames.NumLongTasks)}", "ChangeRoleSettings");
            Logger.Info($"RealOptionsData.NumShortTasks: {Main.RealOptionsData.GetInt(Int32OptionNames.NumShortTasks)}", "ChangeRoleSettings");

            Main.introDestroyed = false;

            
            HudManagerCoShowIntroPatch.Cancel = true;

           
            SelectRolesPatch.roleAssigned = false;
            RpcSetTasksPatch.taskIds.Clear();

            if (Options.CurrentGameMode == CustomGameMode.Standard)
            {
                _ = new LateTask(() =>
                {
                    GameDataSerializePatch.SerializeMessageCount++;
                    bool IsSend = false;
                    var stream = MessageWriter.Get(SendOption.Reliable);
                    stream.StartMessage(5);
                    stream.Write(AmongUsClient.Instance.GameId);
                    foreach (var data in GameData.Instance.AllPlayers)
                    {
                        if (data.PlayerId == 0) continue; 
                        if (IsSend)
                        {
                            stream = MessageWriter.Get(SendOption.Reliable);
                            stream.StartMessage(5);
                            stream.Write(AmongUsClient.Instance.GameId);
                            IsSend = false;
                        }
                        data.Disconnected = true;
                        stream.StartMessage(1);
                        stream.WritePacked(data.NetId);
                        data.Serialize(stream, false);
                        stream.EndMessage();
                        if (stream.Length > 800)
                        {
                            IsSend = true;
                            stream.EndMessage();
                            AmongUsClient.Instance.SendOrDisconnect(stream);
                            stream.Recycle();
                        }
                    }
                    if (!IsSend)
                    {
                        stream.EndMessage();
                        AmongUsClient.Instance.SendOrDisconnect(stream);
                        stream.Recycle();
                    }
                    GameDataSerializePatch.SerializeMessageCount--;
                    Logger.Info("CoGameIntroWeight: Disconnected=true 送信完了", "StandardIntro");
                   
                    GameDataSerializePatch.DontTouch = true;
                    foreach (var data in GameData.Instance.AllPlayers)
                        data.Disconnected = false;
                }, 0.5f, "CoGameIntroWeight");
            }

            RandomSpawn.CustomNetworkTransformPatch.FirstTP = new();

            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();
            Main.PlayerColors = new();
            Main.AllPlayerNames = new();

            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Any())
            {
                var msg = Translator.GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            GameModuleInitializerAttribute.InitializeAll();

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }

            foreach (var pc in Main.AllPlayerControls)
            {
                var colorId = pc.Data.DefaultOutfit.ColorId;
                if (AmongUsClient.Instance.AmHost)
                {
                    if (Options.GetNameChangeModes() == NameChange.Color)
                    {
                        if (pc.Is(CustomRoles.Rainbow)) pc.RpcSetName(GetString("RainbowColor"));
                        else pc.RpcSetName(Palette.GetColorName(colorId));
                    }
                }
                PlayerState.Create(pc.PlayerId);
                Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;
                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.FirstTP.Add(pc.PlayerId, true);
                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new NetworkedPlayerInfo.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId, outfit.NamePlateId);
                Main.clientIdList.Add(pc.GetClientId());

                if (Options.ShowRoleInfoAtFirstMeeting.GetBool())
                {
                    Main.ShowRoleInfoAtMeeting.Add(pc.PlayerId);
                }
            }

            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    Options.HideAndSeekKillDelayTimer = Options.KillDelay.GetFloat();
                }
                if (Options.IsStandardHAS)
                {
                    Options.HideAndSeekKillDelayTimer = Options.StandardHASWaitingTime.GetFloat();
                }
            }

            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;

            Logger.Info("ChangeRoleSettings 完了", "ChangeRoleSettings");
        }
        catch (System.Exception ex)
        {
            Logger.Error($"ChangeRoleSettings で例外が発生しました: {ex.Message}", "ChangeRoleSettings");
            Logger.Exception(ex, "ChangeRoleSettings");
            AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
        }
    }
}

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
class SelectRolesPatch
{
    
    public static List<byte> Disconnected = new();
   
    public static bool roleAssigned = false;

  
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        if (Options.CurrentGameMode != CustomGameMode.Standard) return true;

        
        Dictionary<byte, CustomRpcSender> senders = new();
        foreach (var pc in Main.AllPlayerControls)
        {
            senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.None, false)
                .StartMessage(pc.GetClientId());
        }
        RpcSetRoleReplacer.StartReplace(senders);

        RoleAssignManager.SelectAssignRoles();

        List<PlayerControl> AllPlayers = new();
        foreach (var pc in Main.AllPlayerControls)
            AllPlayers.Add(pc);

        // GM割り当て
        if (Options.EnableGM.GetBool())
        {
            AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
            PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
            PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SetMainRole(CustomRoles.GM);
            PlayerControl.LocalPlayer.Data.IsDead = true; // バニラアサイン除外
        }

        //  Desync役職割り当て
        Dictionary<(byte, byte), RoleTypes> rolesMap = new();
        foreach (var (role, info) in CustomRoleManager.AllRolesInfo)
        {
            if (info.IsDesyncImpostor)
            {
                AssignDesyncRole(role, AllPlayers, senders, rolesMap, BaseRole: info.BaseRoleType.Invoke());
            }
        }

        
        {
            Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> playerInfos = new();
            foreach (NetworkedPlayerInfo data in GameData.Instance.AllPlayers)
            {
                if (data.Object != null && !data.IsDead && !Disconnected.Contains(data.PlayerId))
                    playerInfos.Add(data);
            }

            IGameOptions currentGameOptions = GameOptionsManager.Instance.CurrentGameOptions;
            int adjustedNumImpostors = GameOptionsManager.Instance.CurrentGameOptions.GetAdjustedNumImpostors(playerInfos.Count);

            
            if (CustomRoles.jO.IsPresent())
                adjustedNumImpostors--;

            Logger.Info($"NomalAssign playerInfos: {playerInfos.Count}, impostor: {adjustedNumImpostors}", "AssignRoles");

            GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
                playerInfos, currentGameOptions, RoleTeamTypes.Impostor,
                adjustedNumImpostors, new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Impostor));
            GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
                playerInfos, currentGameOptions, RoleTeamTypes.Crewmate,
                int.MaxValue, new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Crewmate));
        }

        
        return false;
    }

    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        
        RpcSetRoleReplacer.Release();
        RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

       
        RpcSetRoleReplacer.senders = null;
        RpcSetRoleReplacer.OverriddenSenderList = null;
        RpcSetRoleReplacer.StoragedData = null;
        roleAssigned = false; 
        Disconnected.Clear();

      
        List<PlayerControl> Crewmates = new();
        List<PlayerControl> Impostors = new();
        List<PlayerControl> Scientists = new();
        List<PlayerControl> Engineers = new();
        List<PlayerControl> Trackers = new();
        List<PlayerControl> Noisemakers = new();
        List<PlayerControl> Detectives = new();
        List<PlayerControl> GuardianAngels = new();
        List<PlayerControl> Shapeshifters = new();
        List<PlayerControl> Phantoms = new();
        List<PlayerControl> Vipers = new();

        foreach (var pc in Main.AllPlayerControls)
        {
            pc.Data.IsDead = false; 
            var state = PlayerState.GetByPlayerId(pc.PlayerId);
            if (state.GetNowMainRole() != CustomRoles.NotAssigned) continue; 

            var role = CustomRoles.NotAssigned;
            switch (pc.Data.Role.Role)
            {
                case RoleTypes.Crewmate: Crewmates.Add(pc); role = CustomRoles.Crewmate; break;
                case RoleTypes.Impostor: Impostors.Add(pc); role = CustomRoles.Impostor; break;
                case RoleTypes.Scientist: Scientists.Add(pc); role = CustomRoles.Scientist; break;
                case RoleTypes.Engineer: Engineers.Add(pc); role = CustomRoles.Engineer; break;
                case RoleTypes.Tracker: Trackers.Add(pc); role = CustomRoles.Tracker; break;
                case RoleTypes.Noisemaker: Noisemakers.Add(pc); role = CustomRoles.Noisemaker; break;
                case RoleTypes.Detective: Detectives.Add(pc); role = CustomRoles.Detective; break;
                case RoleTypes.GuardianAngel: GuardianAngels.Add(pc); role = CustomRoles.GuardianAngel; break;
                case RoleTypes.Shapeshifter: Shapeshifters.Add(pc); role = CustomRoles.Shapeshifter; break;
                case RoleTypes.Phantom: Phantoms.Add(pc); role = CustomRoles.Phantom; break;
                case RoleTypes.Viper: Vipers.Add(pc); role = CustomRoles.Viper; break;
                default:
                    Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                    break;
            }
            if (role != CustomRoles.NotAssigned)
                state.SetMainRole(role);
        }

        
        foreach (var role in CustomRolesHelper.AllStandardRoles)
        {
            if (role.IsVanilla()) continue;
            if (role == CustomRoles.Opportunist && Opportunist.OptionCanKill.GetBool()) continue;
            if (role is not CustomRoles.Opportunist &&
                CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;

            var baseRoleList = role.GetRoleTypes() switch
            {
                RoleTypes.Impostor => Impostors,
                RoleTypes.Shapeshifter => Shapeshifters,
                RoleTypes.Phantom => Phantoms,
                RoleTypes.Viper => Vipers,
                RoleTypes.Scientist => Scientists,
                RoleTypes.Engineer => Engineers,
                RoleTypes.Tracker => Trackers,
                RoleTypes.Noisemaker => Noisemakers,
                RoleTypes.Detective => Detectives,
                RoleTypes.GuardianAngel => GuardianAngels,
                _ => Crewmates,
            };
            AssignCustomRolesFromList(role, baseRoleList);
        }

        // Random-Addon
        List<PlayerControl> allPlayersbySub = new();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (!pc.Is(CustomRoles.GM)) allPlayersbySub.Add(pc);
        }
        if (!CustomRoles.PlatonicLover.IsEnable() && CustomRoles.Lovers.IsEnable())
            AssignCustomSubRolesFromList(CustomRoles.Lovers, allPlayersbySub, 2);
        foreach (var role in new CustomRoles[] {
            CustomRoles.AddWatch, CustomRoles.Sunglasses, CustomRoles.AddLight,
            CustomRoles.AddSeer, CustomRoles.Autopsy, CustomRoles.VIP,
            CustomRoles.Clumsy, CustomRoles.Revenger, CustomRoles.Management,
            CustomRoles.InfoPoor, CustomRoles.Sending, CustomRoles.TieBreaker,
            CustomRoles.NonReport, CustomRoles.PlusVote, CustomRoles.Guarding,
            CustomRoles.AddBait, CustomRoles.Refusing, CustomRoles.Revealer })
        {
            AssignCustomSubRolesFromList(role, allPlayersbySub);
        }

        foreach (var pair in PlayerState.AllPlayerStates)
        {
            ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.GetNowMainRole());
            foreach (var subRole in pair.Value.SubRoles)
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
        }

        CustomRoleManager.CreateInstance();
        foreach (var pc in Main.AllPlayerControls)
        {
            HudManager.Instance.SetHudActive(true);
            pc.ResetKillCooldown();

            // DirectAssign-Addon
            if (pc.GetCustomRole().IsAddAddOn()
                && (Options.AddOnBuffAssign[pc.GetCustomRole()].GetBool() || Options.AddOnDebuffAssign[pc.GetCustomRole()].GetBool()))
            {
                foreach (var Addon in CustomRolesHelper.AllAddOnRoles)
                {
                    if (Options.AddOnRoleOptions.TryGetValue((pc.GetCustomRole(), Addon), out var option) && option.GetBool())
                        pc.RpcSetCustomRole(Addon);
                }
            }

            // 通常モードでかくれんぼをする人用
            if (Options.IsStandardHAS)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    if (seer == pc) continue;
                    if (pc.GetCustomRole().IsImpostor() || pc.IsNeutralKiller())
                        NameColorManager.Add(seer.PlayerId, pc.PlayerId);
                }
            }
            foreach (var seer in Main.AllPlayerControls)
            {
                if (seer == pc) continue;
                if (pc.Is(CustomRoles.GM)
                    || (pc.Is(CustomRoles.Workaholic) && Workaholic.Seen)
                    || pc.Is(CustomRoles.Rainbow))
                    NameColorManager.Add(seer.PlayerId, pc.PlayerId, pc.GetRoleColorCode());
            }
        }

        GameEndChecker.SetPredicateToNormal();
        SkinChangeMode.ChangeSkin();

        GameOptionsSender.AllSenders.Clear();
        foreach (var pc in Main.AllPlayerControls)
        {
            GameOptionsSender.AllSenders.Add(
                new PlayerGameOptionsSender(pc)
            );
        }

        Utils.CountAlivePlayers(true);
        Utils.SyncAllSettings();
        SetColorPatch.IsAntiGlitchDisabled = false;

        
        if (Options.CurrentGameMode == CustomGameMode.Standard)
            StandardIntroHelper.ShowIntroForVanilla();
    }

    
    private static void AssignDesyncRole(
        CustomRoles role,
        List<PlayerControl> AllPlayers,
        Dictionary<byte, CustomRpcSender> senders,
        Dictionary<(byte, byte), RoleTypes> rolesMap,
        RoleTypes BaseRole,
        RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        if (!role.IsPresent()) return;

        var hostId = PlayerControl.LocalPlayer.PlayerId;
        var rand = IRandom.Instance;

        for (var i = 0; i < role.GetRealCount(); i++)
        {
            if (AllPlayers.Count <= 0) break;
            var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
            AllPlayers.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
            Logger.Info("役職設定(desync):" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");

            var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
            var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

            foreach (var target in Main.AllPlayerControls)
                rolesMap[(player.PlayerId, target.PlayerId)] =
                    (player.PlayerId == target.PlayerId) ? selfRole : othersRole;

            foreach (var seer in Main.AllPlayerControls)
                if (player.PlayerId != seer.PlayerId)
                    rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;

            RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
            player.StartCoroutine(player.CoSetRole(othersRole, false));
            player.Data.IsDead = true;
        }
    }

    
    public static bool AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, ref int assignedNum, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        assignedNum = 0;
        if (!role.IsPresent()) return false;

        var hostId = PlayerControl.LocalPlayer.PlayerId;
        var rand = IRandom.Instance;

        for (var i = 0; i < role.GetRealCount(); i++)
        {
            if (AllPlayers.Count <= 0) break;
            var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
            AllPlayers.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
            Logger.Info("役職設定(desync):" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");

            var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
            var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

            if (player.PlayerId == hostId)
                RpcSetRoleReplacer.RpcSetRoleDirect(player, selfRole, player);
            else
                player.StartCoroutine(player.CoSetRole(othersRole, false));

            player.Data.IsDead = true;
            assignedNum++;
        }
        return assignedNum > 0;
    }

   
    public static void AssignRolesNormal(Dictionary<RoleTypes, int> roleTypesList, int assignedNumImpostors)
    {
        Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> playerInfos = new();
        foreach (NetworkedPlayerInfo data in GameData.Instance.AllPlayers)
        {
            if (data.Object != null && !data.IsDead && !Disconnected.Contains(data.PlayerId))
                playerInfos.Add(data);
        }

        IGameOptions currentGameOptions = GameOptionsManager.Instance.CurrentGameOptions;
        int adjustedNumImpostors = GameOptionsManager.Instance.CurrentGameOptions.GetAdjustedNumImpostors(playerInfos.Count);
        adjustedNumImpostors = Math.Max(0, adjustedNumImpostors - assignedNumImpostors);

        Logger.Info($"AssignRolesNormal playerInfos: {playerInfos.Count}, impostor: {adjustedNumImpostors}", "AssignRoles");

        GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
            playerInfos, currentGameOptions, RoleTeamTypes.Impostor,
            adjustedNumImpostors, new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Impostor));
        GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
            playerInfos, currentGameOptions, RoleTeamTypes.Crewmate,
            int.MaxValue, new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Crewmate));
    }

    
    public static Dictionary<RoleTypes, List<PlayerControl>> GetRoleTypePlayers()
    {
        Dictionary<RoleTypes, List<PlayerControl>> roleTypePlayers = new();
        foreach (var roleType in new RoleTypes[] {
            RoleTypes.Crewmate, RoleTypes.Scientist, RoleTypes.Engineer,
            RoleTypes.Tracker, RoleTypes.Noisemaker, RoleTypes.GuardianAngel,
            RoleTypes.Impostor, RoleTypes.Shapeshifter, RoleTypes.Phantom,
            RoleTypes.Viper, RoleTypes.Detective })
        {
            roleTypePlayers.Add(roleType, new());
        }

        foreach (var pc in Main.AllPlayerControls)
        {
            pc.Data.IsDead = false; 
            var state = PlayerState.GetByPlayerId(pc.PlayerId);
            if (state.GetNowMainRole() != CustomRoles.NotAssigned) continue;

            var roleType = pc.Data.Role.Role;
            if (!roleTypePlayers.TryGetValue(roleType, out var list))
            {
                Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                continue;
            }
            list.Add(pc);

            var defaultRole = roleType switch
            {
                RoleTypes.Crewmate => CustomRoles.Crewmate,
                RoleTypes.Scientist => CustomRoles.Scientist,
                RoleTypes.Engineer => CustomRoles.Engineer,
                RoleTypes.Tracker => CustomRoles.Tracker,
                RoleTypes.Noisemaker => CustomRoles.Noisemaker,
                RoleTypes.GuardianAngel => CustomRoles.GuardianAngel,
                RoleTypes.Impostor => CustomRoles.Impostor,
                RoleTypes.Shapeshifter => CustomRoles.Shapeshifter,
                RoleTypes.Phantom => CustomRoles.Phantom,
                RoleTypes.Viper => CustomRoles.Viper,
                RoleTypes.Detective => CustomRoles.Detective,
                _ => CustomRoles.NotAssigned,
            };
            if (defaultRole != CustomRoles.NotAssigned)
                state.SetMainRole(defaultRole);
        }

        return roleTypePlayers;
    }

    public static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1)
    {
        if (players == null || players.Count <= 0) return null;
        var rand = IRandom.Instance;
        var count = Math.Clamp(RawCount, 0, players.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetRealCount(), 0, players.Count);
        if (count <= 0) return null;
        List<PlayerControl> AssignedPlayers = new();
        SetColorPatch.IsAntiGlitchDisabled = true;
        for (var i = 0; i < count; i++)
        {
            var player = players[rand.Next(0, players.Count)];
            AssignedPlayers.Add(player);
            players.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role, true);
            Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                if (player.Is(CustomRoles.HASTroll))
                    player.RpcSetColor(2);
                else if (player.Is(CustomRoles.HASFox))
                    player.RpcSetColor(3);
            }
        }
        SetColorPatch.IsAntiGlitchDisabled = false;
        return AssignedPlayers;
    }

    private static List<PlayerControl> AssignCustomSubRolesFromList(CustomRoles role, List<PlayerControl> allPlayersbySub, int RawCount = -1)
    {
        if (allPlayersbySub == null || allPlayersbySub.Count <= 0) return null;
        var rand = IRandom.Instance;
        var count = Math.Clamp(RawCount, 0, allPlayersbySub.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayersbySub.Count);
        if (count <= 0) return null;
        List<PlayerControl> AssignedPlayers = new();

        for (var i = 0; i < count; i++)
        {
            var player = allPlayersbySub[rand.Next(0, allPlayersbySub.Count)];
            AssignedPlayers.Add(player);
            allPlayersbySub.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(role);
            Logger.Info("属性設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "AssignSubRoles");
        }
        return AssignedPlayers;
    }
}


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole)), HarmonyPriority(Priority.High)]
public class RpcSetRoleReplacer
{
    public static bool doReplace = false;
    public static Dictionary<byte, CustomRpcSender> senders;
    public static List<(PlayerControl, RoleTypes)> StoragedData = new();
    
    public static List<CustomRpcSender> OverriddenSenderList;

   
    public static bool DoReplace() => doReplace;

    
    public static void RpcSetRoleDirect(PlayerControl target, RoleTypes role, PlayerControl sendTo)
    {
        if (target == null || sendTo == null) return;
        var writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.SetRole, SendOption.Reliable, sendTo.GetClientId());
        writer.Write((ushort)role);
        writer.Write(true);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
    {
        if (doReplace && senders != null)
        {
            StoragedData.Add((__instance, roleType));
            return false;
        }
        else return true;
    }

   
    public static void Release()
    {
      
        if (Options.CurrentGameMode == CustomGameMode.Standard)
        {
            foreach (var pair in StoragedData)
                pair.Item1.StartCoroutine(pair.Item1.CoSetRole(pair.Item2, false));
            doReplace = false;
            return;
        }

        
        if (senders == null)
        {
            foreach (var pair in StoragedData)
                pair.Item1.StartCoroutine(pair.Item1.CoSetRole(pair.Item2, false));
            doReplace = false;
            return;
        }

        
        foreach (var pc in Main.AllPlayerControls)
        {
            var playerInfo = GameData.Instance.GetPlayerById(pc.PlayerId);
            if (playerInfo != null && playerInfo.Disconnected)
                SelectRolesPatch.Disconnected.Add(pc.PlayerId);
        }

        foreach (var kvp in senders)
        {
            var sender = kvp.Value;

            
            if (OverriddenSenderList.Contains(sender)) continue;
            if (sender.CurrentState != CustomRpcSender.State.InRootMessage)
                throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

            foreach (var pair in StoragedData)
            {
                
                pair.Item1.StartCoroutine(pair.Item1.CoSetRole(pair.Item2, false));

                var targetPc = Main.AllPlayerControls.FirstOrDefault(x => x.PlayerId == kvp.Key);
                if (targetPc == null) continue;

                sender.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, targetPc.GetClientId())
                    .Write((ushort)pair.Item2)
                    .Write(false)
                    .EndRpc();
            }
            sender.EndMessage();
        }

        doReplace = false;
    }

    public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
    {
        RpcSetRoleReplacer.senders = senders;
        StoragedData = new();
        OverriddenSenderList = new();
        doReplace = true;
    }

    
    public static void StartReplace()
    {
        RpcSetRoleReplacer.senders = null;
        StoragedData = new();
        OverriddenSenderList = new();
        doReplace = true;
    }
}


public static class StandardIntroHelper
{
    public static void ShowIntroForVanilla()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        

        
        GameDataSerializePatch.DontTouch = false;
        GameDataSerializePatch.SerializeMessageCount++;

        
        var stream = MessageWriter.Get(SendOption.Reliable);
        stream.StartMessage(5);
        stream.Write(AmongUsClient.Instance.GameId);
        {
            var data = PlayerControl.LocalPlayer.Data;
            data.Disconnected = true;
            stream.StartMessage(1);
            stream.WritePacked(data.NetId);
            data.Serialize(stream, false);
            stream.EndMessage();
        }
        stream.StartMessage(2);
        stream.WritePacked(PlayerControl.LocalPlayer.NetId);
        stream.Write((byte)RpcCalls.SetRole);
        stream.Write((ushort)RoleTypes.Crewmate);
        stream.Write(true);
        stream.EndMessage();

        int i = 0;
        foreach (var data in GameData.Instance.AllPlayers)
        {
            i++;
            data.Disconnected = false;
            if (i > 4) continue; 
            stream.StartMessage(1);
            stream.WritePacked(data.NetId);
            data.Serialize(stream, false);
            stream.EndMessage();
        }
        stream.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(stream);
        stream.Recycle();
       

        
        _ = new LateTask(() =>
        {
            if (!SelectRolesPatch.roleAssigned)
            {
                var sender = MessageWriter.Get(SendOption.Reliable);
                sender.StartMessage(5);
                sender.Write(AmongUsClient.Instance.GameId);
                int idx = 0;
                bool issend = false;
                foreach (var data in GameData.Instance.AllPlayers)
                {
                    idx++;
                    if (idx > 4)
                    {
                        if (issend)
                        {
                            sender = MessageWriter.Get(SendOption.Reliable);
                            sender.StartMessage(5);
                            sender.Write(AmongUsClient.Instance.GameId);
                            issend = false;
                        }
                        data.Disconnected = false;
                        sender.StartMessage(1);
                        sender.WritePacked(data.NetId);
                        data.Serialize(sender, false);
                        sender.EndMessage();
                        if (sender.Length > 800)
                        {
                            issend = true;
                            sender.EndMessage();
                            AmongUsClient.Instance.SendOrDisconnect(sender);
                            sender.Recycle();
                        }
                    }
                }
                if (!issend)
                {
                    sender.EndMessage();
                    AmongUsClient.Instance.SendOrDisconnect(sender);
                    sender.Recycle();
                }

                
                _ = new LateTask(() =>
                {
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (RpcSetTasksPatch.taskIds.TryGetValue(pc.PlayerId, out var taskids))
                            pc.Data.RpcSetTasks(taskids);
                        else
                        {
                            Logger.Error($"{pc?.Data?.PlayerName} => taskIds is null", "AssignTask");
                            pc.Data.RpcSetTasks(new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>(0));
                        }
                    }
                    foreach (var pc in Main.AllPlayerControls)
                        PlayerState.GetByPlayerId(pc.PlayerId).InitTask(pc);
                    GameData.Instance.RecomputeTaskCounts();
                    TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
                }, 3f, "StandardIntro_SetTask");

                SelectRolesPatch.roleAssigned = true;
                GameDataSerializePatch.SerializeMessageCount--;
            }
        }, 0.75f, "StandardIntro_RestoreDisconnected");

        Intoro();
    }

    private static void Intoro()
    {
       
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
            if (pc.GetClientId() == -1) continue;

            var role = pc.GetCustomRole();
            var roleType = role.GetRoleTypes();

            
            if (role.GetRoleInfo()?.IsDesyncImpostor == true || role.IsMadmate()
                || (role.IsNeutral() && !role.IsImpostor()))
            {
              
                if (role.IsCrewmate()) roleType = RoleTypes.Crewmate;
                else if (role.IsMadmate()) roleType = RoleTypes.Crewmate;
                else if (role.IsNeutral()) roleType = RoleTypes.Impostor;
            }

            pc.RpcSetRoleDesync(roleType, pc.GetClientId());

           
            foreach (var seen in Main.AllPlayerControls)
            {
                if (role.GetCustomRoleTypes() != CustomRoleTypes.Impostor) continue;
                if (seen.GetCustomRole().GetCustomRoleTypes() != CustomRoleTypes.Impostor) continue;
                if (role.GetRoleInfo()?.IsDesyncImpostor == true) continue;
                if (seen.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true) continue;
                var capPc = pc;
                var capSeen = seen;
                _ = new LateTask(() =>
                    capSeen.RpcSetRoleDesync(RoleTypes.Impostor, capPc.GetClientId()),
                    0.1f, "StandardIntro_ImpSync");
            }
        }

       
        new LateTask(() =>
        {
            foreach (var pc in Main.AllPlayerControls)
                pc.cosmetics.nameText.text = pc.name;
            PlayerControl.LocalPlayer.StopAllCoroutines();
            HudManagerCoShowIntroPatch.Cancel = false;
            DestroyableSingleton<HudManager>.Instance.StartCoroutine(
                DestroyableSingleton<HudManager>.Instance.CoShowIntro());
            DestroyableSingleton<HudManager>.Instance.HideGameLoader();
            Utils.NotifyRoles();
            Logger.Info("ShowIntroForVanilla: CoShowIntro 呼び出し完了", "StandardIntro");
        }, 0.2f, "StandardIntro_ShowIntro");

       
        new LateTask(() =>
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (pc.GetClientId() == -1) continue;

                var role = pc.GetCustomRole();
                var roleInfo = role.GetRoleInfo();
                var roleType = role.GetRoleTypes();

                if (role.GetRoleInfo()?.IsDesyncImpostor == true || role.IsMadmate()
                    || role.IsNeutral())
                {
                    if (role.IsCrewmate()) roleType = RoleTypes.Crewmate;
                    else if (role.IsMadmate()) roleType = RoleTypes.Phantom;
                    else if (role.IsNeutral()) roleType = RoleTypes.Crewmate;
                }

                
                var baseRole = roleInfo?.BaseRoleType?.Invoke() ?? roleType;
                pc.RpcSetRoleDesync(baseRole, pc.GetClientId());
            }
        }, 2.2f, "StandardIntro_PostIntroRoleSync");

         _ = new LateTask(() => SetRole(), 5.5f, "StandardIntro_SetRole");
    }

    
    public static void SetRole()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        

        _ = new LateTask(() =>
        {
           

            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;

                var role = pc.GetCustomRole();
                var roleInfo = role.GetRoleInfo();

                
                if (pc.PlayerId != PlayerControl.LocalPlayer.PlayerId
                    && (roleInfo?.IsDesyncImpostor ?? false)) continue;

                var baseRole = roleInfo?.BaseRoleType?.Invoke() ?? RoleTypes.Crewmate;
                pc.RpcSetRoleDesync(baseRole, pc.GetClientId());
            }
        }, 2.0f, "StandardIntro_SetRoleDelay");
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

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
        //注:この時点では役職は設定されていません。
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);

        if (Options.IsCCMode) Main.NormalOptions.NumImpostors = 1;
        //if (Options.IsONMode) Main.NormalOptions.NumEmergencyMeetings = 0;

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

        Main.introDestroyed = false;

        RandomSpawn.CustomNetworkTransformPatch.FirstTP = new();

        Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
        Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

        Main.LastNotifyNames = new();

        Main.PlayerColors = new();
        //名前の記録
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
            Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
            ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
            pc.cosmetics.nameText.text = pc.name;

            RandomSpawn.CustomNetworkTransformPatch.FirstTP.Add(pc.PlayerId, true);
            var outfit = pc.Data.DefaultOutfit;
            Camouflage.PlayerSkins[pc.PlayerId] = new NetworkedPlayerInfo.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId, outfit.NamePlateId);
            Main.clientIdList.Add(pc.GetClientId());

            // 初手会議での役職説明表示
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
    }
}
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
class SelectRolesPatch
{
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        //StandardMode用
        if (Options.CurrentGameMode != CustomGameMode.Standard) return false;

        //CustomRpcSenderとRpcSetRoleReplacerの初期化
        RpcSetRoleReplacer.StartReplace();

        RoleAssignManager.SelectAssignRoles();

        var assignedNum = 0;
        var assignedNumImpostors = 0;

        Dictionary<RoleTypes, int> roleTypesList = new();
        foreach (var roleTypes in new RoleTypes[] { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Tracker, RoleTypes.Noisemaker, RoleTypes.Shapeshifter, RoleTypes.Phantom })
        {
            roleTypesList.Add(roleTypes, GetRoleTypesCount(roleTypes));
        }

        List<PlayerControl> AllPlayers = new();
        foreach (var pc in Main.AllPlayerControls)
        {
            AllPlayers.Add(pc);
        }

        //GM割り当て
        if (Options.EnableGM.GetBool())
        {
            AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
            PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
            PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SetMainRole(CustomRoles.GM);
        }
        foreach (var (role, info) in CustomRoleManager.AllRolesInfo)
        {
            //Desync系の役職割り当て
            if (info.IsDesyncImpostor)
            {
                switch (role)
                {
                    case CustomRoles.StrayWolf:
                        AssignDesyncRole(CustomRoles.StrayWolf, AllPlayers, ref assignedNum, BaseRole: RoleTypes.Impostor);
                        assignedNumImpostors += assignedNum;
                        continue;
                    case CustomRoles.Opportunist:
                        if (!Opportunist.OptionCanKill.GetBool()) continue;
                        break;
                }

                AssignDesyncRole(role, AllPlayers, ref assignedNum, BaseRole: info.BaseRoleType.Invoke());
            }

            if (role == CustomRoles.jO && CustomRoles.jO.IsPresent())
            {
                // インポスター枠を使用するがクルーカウントの為-1に
                assignedNumImpostors--;
            }
        }

        //バニラの役職割り当て
        AssignRolesNormal(roleTypesList, assignedNumImpostors);

        //MODの役職割り当て
        RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く

        //Utils.ApplySuffix();

        var roleTypePlayers = GetRoleTypePlayers();
        foreach (var role in CustomRolesHelper.AllStandardRoles)
        {
            if (role.IsVanilla()) continue;

            if (role == CustomRoles.Opportunist && Opportunist.OptionCanKill.GetBool()) continue;
            if (role is not CustomRoles.Opportunist &&
                CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;

            if (!roleTypePlayers.TryGetValue(role.GetRoleTypes(), out var list)) continue;

            AssignCustomRolesFromList(role, list);
        }

        // Random-Addon
        List<PlayerControl> allPlayersbySub = new();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (!pc.Is(CustomRoles.GM)) allPlayersbySub.Add(pc);
        }
        if (!CustomRoles.PlatonicLover.IsEnable() && CustomRoles.Lovers.IsEnable())
            AssignCustomSubRolesFromList(CustomRoles.Lovers, allPlayersbySub, 2);
        foreach (var role in new CustomRoles[] { CustomRoles.AddWatch, CustomRoles.Sunglasses, CustomRoles.AddLight,
                                                   CustomRoles.AddSeer, CustomRoles.Autopsy, CustomRoles.VIP,
                                                   CustomRoles.Clumsy, CustomRoles.Revenger, CustomRoles.Management,
                                                   CustomRoles.InfoPoor, CustomRoles.Sending, CustomRoles.TieBreaker,
                                                   CustomRoles.NonReport, CustomRoles.PlusVote, CustomRoles.Guarding,
                                                   CustomRoles.AddBait, CustomRoles.Refusing })
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
                    {
                        pc.RpcSetCustomRole(Addon);
                    }
                }
            }

            //通常モードでかくれんぼをする人用
            if (Options.IsStandardHAS)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    if (seer == pc) continue;
                    if (pc.GetCustomRole().IsImpostor() || pc.IsNeutralKiller()) //変更対象がインポスター陣営orキル可能な第三陣営
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

        return false;
    }
    public static void AssignRolesNormal(Dictionary<RoleTypes, int> roleTypesList, int assignedNumImpostors)
    {
        var list = AmongUsClient.Instance.allClients.ToArray()
        .Where(c => c.Character != null && c.Character.Data != null &&
                    !c.Character.Data.Disconnected && !c.Character.Data.IsDead &&
                    PlayerState.GetByPlayerId(c.Character.PlayerId).GetNowMainRole() == CustomRoles.NotAssigned)
        .OrderBy(c => c.Id).Select(c => c.Character.Data).ToList();
        int adjustedNumImpostors = Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors) - assignedNumImpostors;
        Logger.Info($"NomalAssign list: {list.Count}, impostor: {adjustedNumImpostors}(desync: {assignedNumImpostors})", "AssignRoles");
        AssignRolesForTeam(list, roleTypesList, RoleTeamTypes.Impostor, adjustedNumImpostors, RoleTypes.Impostor);
        AssignRolesForTeam(list, roleTypesList, RoleTeamTypes.Crewmate, int.MaxValue, RoleTypes.Crewmate);
    }
    private static void AssignRolesForTeam(List<NetworkedPlayerInfo> players, Dictionary<RoleTypes, int> roleTypesList, RoleTeamTypes team, int teamMax, RoleTypes defaultRole)
    {
        int num = 0;
        List<RoleTypes> list = new();

        if (roleTypesList != null)
        {
            IEnumerable<RoleBehaviour> source = from role in DestroyableSingleton<RoleManager>.Instance.AllRoles
                                                where role.TeamType == team && !RoleManager.IsGhostRole(role.Role)
                                                select role;
            foreach (var roleBehaviour in source)
            {
                if (!roleTypesList.TryGetValue(roleBehaviour.Role, out int count)) continue;
                Logger.Info($"NomalAssign team: {team}, role: {roleBehaviour.Role}, count: {count}", "AssignRolesForTeam");
                for (int i = 0; i < count; i++)
                {
                    list.Add(roleBehaviour.Role);
                }
            }
            AssignRolesFromList(players, teamMax, list, ref num);
        }

        while (list.Count < players.Count && list.Count + num < teamMax)
        {
            list.Add(defaultRole);
        }
        Logger.Info($"DefaultAssign team: {team}, role: {defaultRole}, count: {list.Count}", "AssignRolesForTeam");
        AssignRolesFromList(players, teamMax, list, ref num);
    }
    private static void AssignRolesFromList(List<NetworkedPlayerInfo> players, int teamMax, List<RoleTypes> roleList, ref int rolesAssigned)
    {
        while (roleList.Count > 0 && players.Count > 0 && rolesAssigned < teamMax)
        {
            int index = HashRandom.FastNext(roleList.Count);
            RoleTypes roleType = roleList[index];
            roleList.RemoveAt(index);
            int index2 = HashRandom.FastNext(players.Count);
            players[index2].Object.RpcSetRole(roleType, false);
            players.RemoveAt(index2);
            rolesAssigned++;
        }
    }

    public static bool AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, ref int assignedNum, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        assignedNum = 0;

        if (!role.IsPresent()) return false;

        var hostId = PlayerControl.LocalPlayer.PlayerId;
        var rand = IRandom.Instance;
        var rolesMap = RpcSetRoleReplacer.RolesMap;

        for (var i = 0; i < role.GetRealCount(); i++)
        {
            if (AllPlayers.Count <= 0) break;
            var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
            AllPlayers.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);

            var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
            var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

            //Desync役職視点
            foreach (var target in Main.AllPlayerControls)
            {
                if (player.PlayerId != target.PlayerId)
                {
                    rolesMap[(player.PlayerId, target.PlayerId)] = othersRole;
                }
                else
                {
                    rolesMap[(player.PlayerId, target.PlayerId)] = selfRole;
                }
            }

            //他者視点
            foreach (var seer in Main.AllPlayerControls)
            {
                if (player.PlayerId != seer.PlayerId)
                {
                    rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;
                }
            }
            RpcSetRoleReplacer.OverriddenSenderList.Add(player.PlayerId);
            //ホスト視点はロール決定
            player.StartCoroutine(player.CoSetRole(othersRole, false));
            assignedNum++;

            Logger.Info("役職設定(desync):" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");
        }

        return assignedNum > 0;
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
    private static int GetRoleTypesCount(RoleTypes roleTypes)
    {
        int count = 0;
        foreach (var role in CustomRolesHelper.AllStandardRoles)
        {
            if (role == CustomRoles.Opportunist && Opportunist.OptionCanKill.GetBool()) continue;
            if (role is not CustomRoles.Opportunist &&
                CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;
            if (role == CustomRoles.Egoist && Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;
            if (role.GetRoleTypes() == roleTypes)
                count += role.GetRealCount();
        }
        return count;
    }
    public static Dictionary<RoleTypes, List<PlayerControl>> GetRoleTypePlayers()
    {
        Dictionary<RoleTypes, List<PlayerControl>> roleTypePlayers = new();
        foreach (var roleType in new RoleTypes[] { RoleTypes.Crewmate, RoleTypes.Scientist, RoleTypes.Engineer,
                                                   RoleTypes.Tracker, RoleTypes.Noisemaker, RoleTypes.GuardianAngel,
                                                   RoleTypes.Impostor, RoleTypes.Shapeshifter, RoleTypes.Phantom })
        {
            roleTypePlayers.Add(roleType, new());
        }

        foreach (var pc in Main.AllPlayerControls)
        {
            var state = PlayerState.GetByPlayerId(pc.PlayerId);
            if (state.GetNowMainRole() != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ

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
                _ => CustomRoles.NotAssigned,
            };
            state.SetMainRole(defaultRole);
        }

        return roleTypePlayers;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole)), HarmonyPriority(Priority.High)]
public class RpcSetRoleReplacer
{
    private static bool doReplace = false;
    private static Dictionary<byte, CustomRpcSender> senders;
    public static List<(PlayerControl, RoleTypes)> StoragedData = new();
    // 役職DesyncなどRolesMapでSetRoleRpcを書き込みするリスト
    public static List<byte> OverriddenSenderList;
    public static Dictionary<(byte, byte), RoleTypes> RolesMap;
    public static bool DoReplace() => doReplace;

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
        ReleaseNormalSetRole(true);
        ReleaseDesyncSetRole(true);
        senders.Do(kvp => kvp.Value.SendMessage());

        DummySetRole();

        // 不要なオブジェクトの削除
        EndReplace();
    }
    public static void DummySetRole()
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
            DummySetRole(pc);
        }
    }
    public static void DummySetRole(PlayerControl target)
    {
        int targetClientId = target.GetClientId();
        if (!RolesMap.TryGetValue((target.PlayerId, target.PlayerId), out var roleType))
        {
            roleType = StoragedData.FirstOrDefault(x => x.Item1.PlayerId == target.PlayerId).Item2;
        }
        Logger.Info($"sent {target.name} {roleType}", "DummySetRole");

        var stream = MessageWriter.Get(SendOption.Reliable);
        stream.StartMessage(6);
        stream.Write(AmongUsClient.Instance.GameId);
        stream.WritePacked(targetClientId);
        {
            SetDisconnectedMessage(stream, true);

            stream.StartMessage(2);
            stream.WritePacked(target.NetId);
            stream.Write((byte)RpcCalls.SetRole);
            stream.Write((ushort)roleType);
            stream.Write(true);     //canOverrideRole
            stream.EndMessage();
            Logger.Info($"DummySetRole to:{target?.name}({targetClientId}) player:{target?.name}({roleType})", "★RpcSetRole");

            SetDisconnectedMessage(stream, false);
        }
        stream.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(stream);
        stream.Recycle();
    }
    private static void SetDisconnectedMessage(MessageWriter stream, bool disconnected)
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            //if (pc.PlayerId != target.PlayerId) continue;
            pc.Data.Disconnected = disconnected;

            stream.StartMessage(1);
            stream.WritePacked(pc.Data.NetId);
            pc.Data.Serialize(stream, false);
            stream.EndMessage();
        }
    }
    private static void ReleaseDesyncSetRole(bool skipSelf)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            foreach (var target in Main.AllPlayerControls)
            {
                if (skipSelf && seer.PlayerId == target.PlayerId &&
                    seer.PlayerId != PlayerControl.LocalPlayer.PlayerId) continue;
                if (RolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var roleType))
                {
                    if (roleType == RoleTypes.Scientist &&
                        StoragedData.Any(x => x.Item1.PlayerId == seer.PlayerId && x.Item2 == RoleTypes.Noisemaker))
                    {
                        Logger.Info($"ChangeNoisemaker seer: {seer.PlayerId}, target: {target.PlayerId},{roleType}=>{RoleTypes.Noisemaker}", "MakeDesyncSender");
                        roleType = RoleTypes.Noisemaker;
                    }

                    var sender = senders[seer.PlayerId];
                    sender.AutoStartRpc(seer.NetId, (byte)RpcCalls.SetRole, target.GetClientId())
                        .Write((ushort)roleType)
                        .Write(true) //canOverrideRole
                        .EndRpc();
                    Logger.Info($"ReleaseDesyncSetRole to:{target?.name}({target.GetClientId()}) player:{seer?.name}({roleType})", "RpcSetRole");
                }
            }
        }
    }
    private static void ReleaseNormalSetRole(bool skipSelf)
    {
        foreach (var senderPair in senders)
        {
            var sender = senderPair.Value;
            var targetId = senderPair.Key;
            if (OverriddenSenderList.Contains(targetId)) continue;
            if (sender.CurrentState != CustomRpcSender.State.InRootMessage)
                throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

            foreach (var pair in StoragedData)
            {
                if (skipSelf && targetId == pair.Item1.PlayerId &&
                    targetId != PlayerControl.LocalPlayer.PlayerId) continue;

                var player = pair.Item1;
                var roleType = pair.Item2;
                var clientId = Utils.GetPlayerById(targetId).GetClientId();

                player.StartCoroutine(player.CoSetRole(roleType, false));
                sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetRole, clientId)
                    .Write((ushort)roleType)
                    .Write(true)           //canOverrideRole = false
                    .EndRpc();
                Logger.Info($"ReleaseNormalSetRole toClientId:{clientId} player:{player?.name}({roleType})", "RpcSetRole");
            }
            sender.EndMessage();
        }
        doReplace = false;
    }
    public static void StartReplace()
    {
        senders = new();
        foreach (var pc in Main.AllPlayerControls)
        {
            senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                    .StartMessage(pc.GetClientId());
        }
        StoragedData = new();
        OverriddenSenderList = new();
        RolesMap = new();
        doReplace = true;
    }
    public static void EndReplace()
    {
        senders = null;
        OverriddenSenderList = null;
        RolesMap = null;
        StoragedData = null;
    }
}

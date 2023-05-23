using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHost.Modules;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Madmate;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);

            if (Options.CurrentGameMode.IsCatMode())
            {
                Main.NormalOptions.NumImpostors = 1;
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
            }
            if (Options.CurrentGameMode.IsOneNightMode())
            {
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);

                Main.NormalOptions.NumEmergencyMeetings = 0;
            }

            Main.PlayerStates = new();

            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();

            Main.WarlockTimer = new Dictionary<byte, float>();
            Main.isDoused = new Dictionary<(byte, byte), bool>();
            Main.ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            Main.CursedPlayers = new Dictionary<byte, PlayerControl>();
            Main.isCurseAndKill = new Dictionary<byte, bool>();
            Main.SKMadmateNowCount = 0;
            Main.isCursed = false;
            Main.PuppeteerList = new Dictionary<byte, byte>();

            Main.AfterMeetingDeathPlayers = new();
            Main.ResetCamPlayerList = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();
            Main.SpeedBoostTarget = new Dictionary<byte, byte>();
            Main.MayorUsedButtonCount = new Dictionary<byte, int>();

            ReportDeadBodyPatch.CanReport = new();
            ReportDeadBodyPatch.CanReportByDeadBody = new();

            Options.UsedButtonCount = 0;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            MeetingTimeManager.Init();
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.currentDousingTarget = 255;
            Main.PlayerColors = new();

            //TOH_Y
            Main.ChairmanUsedButtonCount = new Dictionary<byte, int>();
            Main.CursedWolfSpellCount = new Dictionary<byte, int>();
            Main.LoveCutterKilledCount = new Dictionary<byte, int>();
            Main.colorchange = new Dictionary<byte, float>();
            Main.isBlindVision = new Dictionary<byte, bool>();
            Main.OppoKillerShotLimit = new();
            Main.RevengeTargetPlayer = new();
            Main.AntiCompGuardCount = new Dictionary<byte, (int, bool)>();
            Main.GuardingGuardCount = new Dictionary<byte, bool>();
            Main.ExiledPlayer = 253;
            Main.ONMeetingExiledPlayers = new();
            Main.ONKillCount = 0;
            Main.isPotentialistChanged = new Dictionary<byte, bool>();
            Main.IsAdd1NextExiled = new Dictionary<byte, bool>();

            //ON
            Main.DefaultRole = new();
            Main.MeetingSeerDisplayRole = new();
            Main.ChangeRolesTarget = new();

            //名前の記録
            Main.AllPlayerNames = new();

            Camouflage.Init();
            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Count() != 0)
            {
                var msg = Translator.GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

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
                if (AmongUsClient.Instance.AmHost && Options.ColorNameMode.GetBool() && !pc.Is(CustomRoles.Rainbow))
                    pc.RpcSetName(Palette.GetColorName(colorId));
                else if (AmongUsClient.Instance.AmHost && Options.ColorNameMode.GetBool() && pc.Is(CustomRoles.Rainbow))
                    pc.RpcSetName(GetString("RainbowColor"));

                Main.PlayerStates[pc.PlayerId] = new(pc.PlayerId);
                Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;

                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.CanReportByDeadBody[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP.Add(pc.PlayerId, 0);
                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                SkinChangeMode.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId, outfit.NamePlateId);

                Main.clientIdList.Add(pc.GetClientId());
            }
            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                Main.RefixCooldownDelay = 0;
                if (Options.IsStandardHAS)
                {
                    Options.HideAndSeekKillDelayTimer = Options.StandardHASWaitingTime.GetFloat();
                }
            }
            FallFromLadder.Reset();
            BountyHunter.Init();
            SerialKiller.Init();
            FireWorks.Init();
            Sniper.Init();
            TimeThief.Init();
            Mare.Init();
            Witch.Init();
            SabotageMaster.Init();
            Egoist.Init();
            Executioner.Init();
            Jackal.Init();
            Sheriff.Init();
            EvilTracker.Init();
            Snitch.Init();
            SchrodingerCat.Init();
            Vampire.Init();
            TimeManager.Init();
            FortuneTeller.Init();
            ShapeKiller.Init();
            LastImpostor.Init();
            //TOH_Y
            Hunter.Init();
            MadSheriff.Init();
            SillySheriff.Init();
            DarkHide.Init();
            AntiAdminer.Init();
            Greedier.Init();
            Ambitioner.Init();
            PlatonicLover.Init();
            Lawyer.Init();
            Bakery.Init();
            EvilDiviner.Init();
            Telepathisters.Init();
            Medic.Init();
            GrudgeSheriff.Init();
            CandleLighter.Init();
            Psychic.Init();
            Totocalcio.Init();

            //ON
            ONWerewolf.Init();
            ONBigWerewolf.Init();
            ONDiviner.Init();
            ONPhantomThief.Init();
            ONDeadTargetArrow.Init();

            TargetArrow.Init();
            DoubleTrigger.Init();
            VentSelect.Init();
            Workhorse.Init();
            CompreteCrew.Init();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            //ウォッチャーの陣営抽選
            Options.SetWatcherTeam(Options.EvilWatcherChance.GetFloat());

            if (Options.CurrentGameMode.IsCatMode())
            {
                List<PlayerControl> AllPlayers = new();
                foreach (var pc in Main.AllPlayerControls)
                {
                    AllPlayers.Add(pc);
                }

                if (Options.EnableGM.GetBool())
                {
                    AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                    PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                    PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                    PlayerControl.LocalPlayer.Data.IsDead = true;
                }

                Dictionary<(byte, byte), RoleTypes> rolesMap = new();

                AssignDesyncRole(CustomRoles.CatYellowLeader, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.CatBlueLeader, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);

                MakeDesyncSender(senders, rolesMap);
            }
            else if (Options.CurrentGameMode.IsOneNightMode())
            {
                List<PlayerControl> AllPlayers = new();
                foreach (var pc in Main.AllPlayerControls)
                {
                    AllPlayers.Add(pc);
                    //ついでに初期化
                    ONDeadTargetArrow.Add(pc.PlayerId);
                }

                if (Options.EnableGM.GetBool())
                {
                    AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                    PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                    PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                    PlayerControl.LocalPlayer.Data.IsDead = true;
                }

                Dictionary<(byte, byte), RoleTypes> rolesMap = new();

                AssignDesyncRole(CustomRoles.ONDiviner, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.ONPhantomThief, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);

                MakeDesyncSender(senders, rolesMap);
            }
            else
            {
                RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
                foreach (var roleTypes in RoleTypesList)
                {
                    var roleOpt = Main.NormalOptions.roleOptions;
                    int additionalNum = GetAdditionalRoleTypesCount(roleTypes);
                    roleOpt.SetRoleRate(roleTypes, roleOpt.GetNumPerGame(roleTypes) + additionalNum, additionalNum > 0 ? 100 : roleOpt.GetChancePerGame(roleTypes));
                }

                List<PlayerControl> AllPlayers = new();
                foreach (var pc in Main.AllPlayerControls)
                {
                    AllPlayers.Add(pc);
                }

                if (Options.EnableGM.GetBool())
                {
                    AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                    PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                    PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                    PlayerControl.LocalPlayer.Data.IsDead = true;
                }

                Dictionary<(byte, byte), RoleTypes> rolesMap = new();

                AssignDesyncRole(CustomRoles.Sheriff, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.Arsonist, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.Jackal, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                //TOH_Y
                AssignDesyncRole(CustomRoles.Hunter, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.SillySheriff, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.MadSheriff, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.DarkHide, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.PlatonicLover, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.Totocalcio, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                if (Options.OpportunistCanKill.GetBool())
                    AssignDesyncRole(CustomRoles.Opportunist, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);

                MakeDesyncSender(senders, rolesMap);
            }
            //以下、バニラ側の役職割り当てが入る
        }
        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            // 不要なオブジェクトの削除
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Utils.ApplySuffix();

            var rand = IRandom.Instance;

            List<PlayerControl> Crewmates = new();
            List<PlayerControl> Impostors = new();
            List<PlayerControl> Scientists = new();
            List<PlayerControl> Engineers = new();
            List<PlayerControl> GuardianAngels = new();
            List<PlayerControl> Shapeshifters = new();

            List<PlayerControl> allPlayersbySub = new();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する

                if (!pc.Is(CustomRoles.GM))
                    allPlayersbySub.Add(pc);

                if (Main.PlayerStates[pc.PlayerId].MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        Crewmates.Add(pc);
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        Impostors.Add(pc);
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        Scientists.Add(pc);
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        Engineers.Add(pc);
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.GuardianAngel:
                        GuardianAngels.Add(pc);
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        Shapeshifters.Add(pc);
                        role = CustomRoles.Shapeshifter;
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                Main.PlayerStates[pc.PlayerId].SetMainRole(role);
            }

            if (Options.CurrentGameMode.IsCatMode())
            {
                //SetColorPatch.IsAntiGlitchDisabled = true;
                //foreach (var pc in PlayerControl.AllPlayerControls)
                //{
                //    if (pc.Is(RoleType.Impostor))
                //        pc.RpcSetColor(0);
                //    else if (pc.Is(RoleType.Crewmate))
                //        pc.RpcSetColor(1);
                //}

                //役職設定処理
                AssignCustomRolesFromList(CustomRoles.CatRedLeader, Impostors);

                foreach (var pair in Main.PlayerStates)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }

                foreach (var pc in Main.AllPlayerControls)
                {
                    HudManager.Instance.SetHudActive(true);
                    Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更
                }
                GameEndChecker.SetPredicateToHideAndSeek();

                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in Main.AllPlayerControls)
                {
                    GameOptionsSender.AllSenders.Add(
                        new PlayerGameOptionsSender(pc)
                    );
                }

                // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
                Main.ResetCamPlayerList.AddRange(PlayerControl.AllPlayerControls.ToArray().Where(p =>
                p.GetCustomRole().IsCatLeaderRoles()).Select(p => p.PlayerId));
            }
            else if (Options.CurrentGameMode.IsOneNightMode())
            {
                //役職設定処理
                AssignCustomRolesFromList(CustomRoles.ONBigWerewolf, Impostors);
                AssignCustomRolesFromList(CustomRoles.ONWerewolf, Impostors);
                AssignCustomRolesFromList(CustomRoles.ONMadman, Crewmates);
                AssignCustomRolesFromList(CustomRoles.ONMadFanatic, Crewmates);
                AssignCustomRolesFromList(CustomRoles.ONMayor, Crewmates);
                AssignCustomRolesFromList(CustomRoles.ONHunter, Crewmates);
                AssignCustomRolesFromList(CustomRoles.ONBakery, Crewmates);
                AssignCustomRolesFromList(CustomRoles.ONTrapper, Crewmates);
                AssignCustomRolesFromList(CustomRoles.ONHangedMan, Crewmates);
                AssignCustomRolesFromList(CustomRoles.ONVillager, Crewmates);

                //残りを割り当て
                {
                    SetColorPatch.IsAntiGlitchDisabled = true;
                    foreach (var imp in Impostors)
                    {
                        Main.PlayerStates[imp.PlayerId].SetMainRole(CustomRoles.ONWerewolf);
                        Logger.Info("役職設定:" + imp?.Data?.PlayerName + " = " + CustomRoles.ONWerewolf.ToString(), "AssignRoles");
                    }
                    foreach (var crew in Crewmates)
                    {
                        Main.PlayerStates[crew.PlayerId].SetMainRole(CustomRoles.ONVillager);
                        Logger.Info("役職設定:" + crew?.Data?.PlayerName + " = " + CustomRoles.ONVillager.ToString(), "AssignRoles");
                    }
                    SetColorPatch.IsAntiGlitchDisabled = false;
                }

                foreach (var pair in Main.PlayerStates)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }

                foreach (var pc in Main.AllPlayerControls)
                {
                    switch (pc.GetCustomRole())
                    {
                        case CustomRoles.ONWerewolf:
                            ONWerewolf.Add(pc.PlayerId);
                            break;
                        case CustomRoles.ONBigWerewolf:
                            ONBigWerewolf.Add(pc.PlayerId);
                            break;
                        case CustomRoles.ONDiviner:
                            ONDiviner.Add(pc.PlayerId);
                            break;
                        case CustomRoles.ONPhantomThief:
                            ONPhantomThief.Add(pc.PlayerId);
                            break;
                    }
                    Main.DefaultRole[pc.PlayerId] = pc.GetCustomRole();
                    Main.MeetingSeerDisplayRole[pc.PlayerId] = pc.GetCustomRole();
                    Main.ChangeRolesTarget.Add(pc.PlayerId, null);
                    RPC.SendRPCDefaultRole(pc.PlayerId);

                    HudManager.Instance.SetHudActive(true);
                    pc.ResetKillCooldown();
                }
                GameEndChecker.SetPredicateToOneNight();

                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in Main.AllPlayerControls)
                {
                    GameOptionsSender.AllSenders.Add(
                        new PlayerGameOptionsSender(pc)
                    );
                }

                // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
                Main.ResetCamPlayerList.AddRange(PlayerControl.AllPlayerControls.ToArray().Where(p =>
                p.Is(CustomRoles.ONDiviner) || p.Is(CustomRoles.ONPhantomThief)).Select(p => p.PlayerId));
            }
            else
            {
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x < CustomRoles.NotAssigned))
                {
                    if (role.IsVanilla()) continue;
                    if (!role.IsStanderdRole()) continue;

                    if (Options.RoleSettingMode == RoleSettingMode.OnOffSet && role.IsAddOnOnlyRole()) continue;
                    if (Options.RoleSettingMode == RoleSettingMode.AddOnOnly && !role.IsAddOnOnlyRole()) continue;

                    if (role is CustomRoles.Sheriff or CustomRoles.Arsonist
                        or CustomRoles.Hunter or CustomRoles.SillySheriff or CustomRoles.MadSheriff
                        or CustomRoles.DarkHide or CustomRoles.PlatonicLover or CustomRoles.Totocalcio or CustomRoles.Jackal) continue;
                    if (role == CustomRoles.Egoist && Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;
                    if (role == CustomRoles.Opportunist && Options.OpportunistCanKill.GetBool()) continue;
                    var baseRoleTypes = role.GetRoleTypes() switch
                    {
                        RoleTypes.Impostor => Impostors,
                        RoleTypes.Shapeshifter => Shapeshifters,
                        RoleTypes.Scientist => Scientists,
                        RoleTypes.Engineer => Engineers,
                        RoleTypes.GuardianAngel => GuardianAngels,
                        _ => Crewmates,
                    };
                    AssignCustomRolesFromList(role, baseRoleTypes);
                }

                //SubRoles
                if (!CustomRoles.PlatonicLover.IsEnable()) AssignLoversRolesFromList(allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.AddWatch, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Sunglasses, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.AddLight, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.AddSeer, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Autopsy, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.VIP, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Clumsy, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Revenger, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Management, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.InfoPoor, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Sending, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.TieBreaker, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.NonReport, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.PlusVote, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Guarding, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.AddBait, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Refusing, allPlayersbySub);

                //RPCによる同期
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Watcher))
                    {
                        Main.PlayerStates[pc.PlayerId].SetMainRole(Options.IsEvilWatcher ? CustomRoles.EvilWatcher : CustomRoles.NiceWatcher);
                    }

                    var role = pc.GetCustomRole();
                }
                foreach (var pair in Main.PlayerStates)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                    foreach (var subRole in pair.Value.SubRoles)
                        ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
                }

                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Data.Role.Role == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(pc.PlayerId, false);

                    switch (pc.GetCustomRole())
                    {
                        case CustomRoles.BountyHunter:
                            BountyHunter.Add(pc.PlayerId);
                            break;
                        case CustomRoles.SerialKiller:
                            SerialKiller.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Witch:
                            Witch.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Warlock:
                            Main.CursedPlayers.Add(pc.PlayerId, null);
                            Main.isCurseAndKill.Add(pc.PlayerId, false);
                            break;
                        case CustomRoles.FireWorks:
                            FireWorks.Add(pc.PlayerId);
                            break;
                        case CustomRoles.TimeThief:
                            TimeThief.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Sniper:
                            Sniper.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Mare:
                            Mare.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Vampire:
                            Vampire.Add(pc.PlayerId);
                            break;

                        case CustomRoles.Arsonist:
                            foreach (var ar in Main.AllPlayerControls)
                                Main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                            break;
                        case CustomRoles.Executioner:
                            Executioner.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Egoist:
                            Egoist.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Jackal:
                            Jackal.Add(pc.PlayerId);
                            break;

                        case CustomRoles.Sheriff:
                            Sheriff.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Mayor:
                            Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                            break;
                        case CustomRoles.SabotageMaster:
                            SabotageMaster.Add(pc.PlayerId);
                            break;
                        case CustomRoles.EvilTracker:
                            EvilTracker.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Snitch:
                            Snitch.Add(pc.PlayerId);
                            break;
                        case CustomRoles.SchrodingerCat:
                            SchrodingerCat.Add(pc.PlayerId);
                            break;
                        case CustomRoles.TimeManager:
                            TimeManager.Add(pc.PlayerId);
                            break;
                        /*********************TOH_Y**********************/
                        case CustomRoles.AntiAdminer:
                            AntiAdminer.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Hunter:
                            Hunter.Add(pc.PlayerId);
                            break;
                        case CustomRoles.MadSheriff:
                            MadSheriff.Add(pc.PlayerId);
                            break;
                        case CustomRoles.SillySheriff:
                            SillySheriff.Add(pc.PlayerId);
                            break;
                        case CustomRoles.DarkHide:
                            DarkHide.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Greedier:
                            Greedier.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Ambitioner:
                            Ambitioner.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Rainbow:
                            int chance = UnityEngine.Random.Range(0, 17);
                            Main.colorchange[pc.PlayerId] = chance;
                            pc.SetNamePlate("nameplate_flagRainbow");
                            CustomRpcSender.Create(name: $"RpcSetNamePlate({pc.Data.PlayerName})").AutoStartRpc(pc.NetId, (byte)RpcCalls.SetNamePlateStr)
                                .Write("nameplate_flagRainbow")
                                .EndRpc();
                            break;
                        case CustomRoles.Chairman:
                            Main.ChairmanUsedButtonCount[pc.PlayerId] = 0;
                            break;
                        case CustomRoles.CursedWolf:
                            Main.CursedWolfSpellCount[pc.PlayerId] = Options.GuardSpellTimes.GetInt();
                            break;
                        case CustomRoles.LoveCutter:
                            Main.LoveCutterKilledCount[pc.PlayerId] = 0;
                            break;
                        case CustomRoles.Blinder:
                            foreach (var p in Main.AllPlayerControls)
                                Main.isBlindVision.Add(p.PlayerId, false);
                            break;
                        case CustomRoles.Opportunist:
                            if (Options.OpportunistCanKill.GetBool())
                                Main.OppoKillerShotLimit.TryAdd(pc.PlayerId, Options.OppoKillerShotLimitOpt.GetInt());
                            break;
                        case CustomRoles.AntiComplete:
                            Main.AntiCompGuardCount[pc.PlayerId] = (Options.AntiCompGuardCount.GetInt(), false);
                            break;
                        case CustomRoles.Potentialist:
                            Main.isPotentialistChanged[pc.PlayerId] = false;
                            break;
                        case CustomRoles.PlatonicLover:
                            PlatonicLover.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Lawyer:
                            Lawyer.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Bakery:
                            Bakery.Add(pc.PlayerId);
                            break;
                        case CustomRoles.EvilDiviner:
                            EvilDiviner.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Telepathisters:
                            Telepathisters.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Medic:
                            Medic.Add(pc.PlayerId);
                            break;
                        case CustomRoles.GrudgeSheriff:
                            GrudgeSheriff.Add(pc.PlayerId);
                            break;
                        case CustomRoles.CandleLighter:
                            CandleLighter.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Psychic:
                            Psychic.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Totocalcio:
                            Totocalcio.Add(pc.PlayerId);
                            break;
                        case CustomRoles.FortuneTeller:
                            FortuneTeller.Add(pc.PlayerId);
                            break;
                        case CustomRoles.ShapeKiller:
                            ShapeKiller.Add(pc.PlayerId);
                            break;
                    }
                    HudManager.Instance.SetHudActive(true);
                    pc.ResetKillCooldown();

                    if (pc.GetCustomRole().IsAddAddOn()
                        && (Options.AddOnBuffAssign[pc.GetCustomRole()].GetBool() || Options.AddOnDebuffAssign[pc.GetCustomRole()].GetBool()))
                    {
                        foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsAddOn()))
                        {
                            if (Options.AddOnRoleOptions.TryGetValue((pc.GetCustomRole(), Addon), out var option) && option.GetBool())
                            {
                                pc.RpcSetCustomRole(Addon);
                            }
                        }
                    }
                    foreach (var subRole in pc.GetCustomSubRoles())
                    {
                        switch (subRole)
                        {
                            // ここに属性のAddを追加
                            case CustomRoles.Guarding:
                                Main.GuardingGuardCount[pc.PlayerId] = false;
                                break;

                            case CustomRoles.Loyalty:
                                foreach (var target in Main.AllPlayerControls.Where(x => x.GetCustomRole().IsImpostor()))
                                {
                                    NameColorManager.Add(pc.PlayerId, target.PlayerId);
                                }
                                break;

                            case CustomRoles.Refusing:
                                Main.IsAdd1NextExiled[pc.PlayerId] = false;
                                break;

                            default:
                                break;
                        }
                    }

                    //通常モードでかくれんぼをする人用 色変更
                    if (Options.IsStandardHAS)
                    {
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (pc.GetCustomRole().IsImpostor() || (pc.IsNeutralKiller() && !pc.Is(CustomRoles.Arsonist) && !pc.Is(CustomRoles.PlatonicLover) && !pc.Is(CustomRoles.Totocalcio))) //変更対象がインポスター陣営orキル可能な第三陣営
                                NameColorManager.Add(seer.PlayerId, pc.PlayerId);
                        }
                    }
                    foreach (var seer in Main.AllPlayerControls)
                    {
                        if (seer == pc) continue;
                        if (pc.Is(CustomRoles.GM) || pc.Is(CustomRoles.Rainbow) || (pc.Is(CustomRoles.Workaholic) && Options.WorkaholicSeen.GetBool()))
                            NameColorManager.Add(seer.PlayerId, pc.PlayerId, pc.GetRoleColorCode());
                    }
                }

                RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
                foreach (var roleTypes in RoleTypesList)
                {
                    var roleOpt = Main.NormalOptions.roleOptions;
                    roleOpt.SetRoleRate(roleTypes, roleOpt.GetNumPerGame(roleTypes) - GetAdditionalRoleTypesCount(roleTypes), roleOpt.GetChancePerGame(roleTypes));
                }

                GameEndChecker.SetPredicateToNormal();
                SkinChangeMode.ChangeSkin();

                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in Main.AllPlayerControls)
                {
                    GameOptionsSender.AllSenders.Add(new PlayerGameOptionsSender(pc));
                }

                // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
                Main.ResetCamPlayerList.AddRange(Main.AllPlayerControls.ToArray().Where(p =>
                (p.GetCustomRole() is CustomRoles.Arsonist)
                || ((p.GetCustomRole() == CustomRoles.Opportunist) && Options.OpportunistCanKill.GetBool())).Select(p => p.PlayerId));//TOH_Y
            }

            Utils.CountAlivePlayers(true);
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        private static void AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
        {
            if (!role.IsEnable()) return;

            var hostId = PlayerControl.LocalPlayer.PlayerId;
            var rand = IRandom.Instance;

            for (var i = 0; i < role.GetCount(); i++)
            {
                if (AllPlayers.Count <= 0) break;
                var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
                AllPlayers.Remove(player);
                Main.PlayerStates[player.PlayerId].SetMainRole(role);

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
                RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
                //ホスト視点はロール決定
                player.SetRole(othersRole);
                player.Data.IsDead = true;
            }
        }
        public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
        {
            var hostId = PlayerControl.LocalPlayer.PlayerId;
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                var sender = senders[seer.PlayerId];
                foreach (var target in Main.AllPlayerControls)
                {
                    if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var role))
                    {
                        sender.RpcSetRole(seer, role, target.GetClientId());
                    }
                }
            }
        }

        private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1)
        {
            if (players == null || players.Count <= 0) return null;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, players.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, players.Count);
            if (count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new();
            SetColorPatch.IsAntiGlitchDisabled = true;
            for (var i = 0; i < count; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                AssignedPlayers.Add(player);
                players.Remove(player);
                Main.PlayerStates[player.PlayerId].SetMainRole(role);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");

                //if (Options.CurrentGameMode == CustomGameMode.CatchCat)
                //{
                //    if (player.Is(CustomRoles.HASTroll))
                //        player.RpcSetColor(2);
                //    else if (player.Is(CustomRoles.HASFox))
                //        player.RpcSetColor(3);
                //}
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
                if (role == CustomRoles.Lovers)
                    Main.LoversPlayers.Add(player);
                allPlayersbySub.Remove(player);
                Main.PlayerStates[player.PlayerId].SetSubRole(role);
                Logger.Info("属性設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "AssignSubRoles");
            }
            if (role == CustomRoles.Lovers)
                RPC.SyncLoversPlayers();

            return AssignedPlayers;
        }

        private static List<PlayerControl> AssignLoversRolesFromList(List<PlayerControl> allPlayersbySub)
        {
            if (CustomRoles.Lovers.IsEnable())
            {
                //Loversを初期化
                Main.LoversPlayers.Clear();
                Main.isLoversDead = false;
                //ランダムに2人選出
                //AssignLoversRoles(2);
                return AssignCustomSubRolesFromList(CustomRoles.Lovers, allPlayersbySub, 2);
            }
            return null;
        }
        //private static void AssignLoversRoles(int RawCount = -1)
        //{
        //    var allPlayers = new List<PlayerControl>();
        //    foreach (var player in Main.AllPlayerControls)
        //    {
        //        if (player.Is(CustomRoles.GM)) continue;
        //        allPlayers.Add(player);
        //    }
        //    var loversRole = CustomRoles.Lovers;
        //    var rand = IRandom.Instance;
        //    var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        //    if (RawCount == -1) count = Math.Clamp(loversRole.GetCount(), 0, allPlayers.Count);
        //    if (count <= 0) return;

        //    for (var i = 0; i < count; i++)
        //    {
        //        var player = allPlayers[rand.Next(0, allPlayers.Count)];
        //        Main.LoversPlayers.Add(player);
        //        allPlayers.Remove(player);
        //        Main.PlayerStates[player.PlayerId].SetSubRole(loversRole);
        //        Logger.Info("属性設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
        //    }
        //    RPC.SyncLoversPlayers();
        //}
        public static int GetAdditionalRoleTypesCount(RoleTypes roleTypes)
        {
            int count = 0;
            foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x < CustomRoles.NotAssigned))
            {
                if (role.IsVanilla()) continue;
                if (Options.RoleSettingMode == RoleSettingMode.OnOffSet && role.IsAddOnOnlyRole()) continue;
                if (Options.RoleSettingMode == RoleSettingMode.AddOnOnly && !role.IsAddOnOnlyRole()) continue;

                if (role is CustomRoles.Sheriff or CustomRoles.Arsonist
                        or CustomRoles.Hunter or CustomRoles.SillySheriff or CustomRoles.MadSheriff
                        or CustomRoles.DarkHide or CustomRoles.PlatonicLover or CustomRoles.Totocalcio or CustomRoles.Jackal) continue;
                if (role == CustomRoles.Egoist && Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;
                if (role == CustomRoles.Opportunist && Options.OpportunistCanKill.GetBool()) continue;
                if (role.GetRoleTypes() == roleTypes)
                    count += role.GetCount();
            }
            return count;
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
        class RpcSetRoleReplacer
        {
            public static bool doReplace = false;
            public static Dictionary<byte, CustomRpcSender> senders;
            public static List<(PlayerControl, RoleTypes)> StoragedData = new();
            // 役職Desyncなど別の処理でSetRoleRpcを書き込み済みなため、追加の書き込みが不要なSenderのリスト
            public static List<CustomRpcSender> OverriddenSenderList;
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
                foreach (var sender in senders)
                {
                    if (OverriddenSenderList.Contains(sender.Value)) continue;
                    if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                        throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                    foreach (var pair in StoragedData)
                    {
                        pair.Item1.SetRole(pair.Item2);
                        sender.Value.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                            .Write((ushort)pair.Item2)
                            .EndRpc();
                    }
                    sender.Value.EndMessage();
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
        }
    }
}
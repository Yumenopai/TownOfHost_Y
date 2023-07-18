using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

using TownOfHostY.Roles;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.AddOns.Impostor;
using TownOfHostY.Roles.AddOns.Crewmate;

namespace TownOfHostY
{
    [Flags]
    public enum CustomGameMode
    {
        Standard = 0x01,
        HideAndSeek = 0x02,
        All = int.MaxValue
    }

    [HarmonyPatch]
    public static class Options
    {
        static Task taskOptionsLoad;
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
        public static void OptionsLoadStart()
        {
            Logger.Info("Options.Load Start", "Options");
            taskOptionsLoad = Task.Run(Load);

            int chance = IRandom.Instance.Next(0, (int)CustomRoles._Max - 1);
            SendDiscord.HostRandomName = Translator.GetString(((CustomRoles)chance).ToString());
        }
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
        public static void WaitOptionsLoad()
        {
            taskOptionsLoad.Wait();
            Logger.Info("Options.Load End", "Options");
        }
        // オプションId
        public const int PresetId = 0;

        // プリセット
        private static readonly string[] presets =
        {
            Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
            Main.Preset4.Value, Main.Preset5.Value
        };

        // ゲームモード
        public static OptionItem GameMode;
        public static CustomGameMode CurrentGameMode
            => GameMode.CurrentValue == 0 ? CustomGameMode.Standard : CustomGameMode.HideAndSeek;

        public static readonly string[] gameModes =
        {
            "Standard", "HideAndSeek",
        };

        // MapActive
        public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || Main.NormalOptions.MapId == 0;
        public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || Main.NormalOptions.MapId == 1;
        public static bool IsActivePolus => AddedPolus.GetBool() || Main.NormalOptions.MapId == 2;
        public static bool IsActiveAirship => AddedTheAirShip.GetBool() || Main.NormalOptions.MapId == 4;

        // 役職数・確率
        public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
        public static Dictionary<CustomRoles, IntegerOptionItem> CustomRoleSpawnChances;
        public static readonly string[] rates =
        {
            "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
            "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };

        //役職直接属性付与
        public static Dictionary<(CustomRoles, CustomRoles), OptionItem> AddOnRoleOptions = new();
        public static Dictionary<CustomRoles, OptionItem> AddOnBuffAssign = new();
        public static Dictionary<CustomRoles, OptionItem> AddOnDebuffAssign = new();

        // 各役職の詳細設定
        public static OptionItem EnableGM;
        public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
        public static OptionItem DefaultShapeshiftCooldown;
        public static OptionItem ImpostorOperateVisibility;
        public static OptionItem CanMakeMadmateCount;
        public static OptionItem MadmateCanFixLightsOut; // TODO:mii-47 マッド役職統一
        public static OptionItem MadmateCanFixComms;
        public static OptionItem MadmateHasImpostorVision;
        public static OptionItem MadmateCanSeeKillFlash;
        public static OptionItem MadmateCanSeeOtherVotes;
        public static OptionItem MadmateCanSeeDeathReason;
        public static OptionItem MadmateRevengeCrewmate;
        public static OptionItem MadmateVentCooldown;
        public static OptionItem MadmateVentMaxTime;

        public static OptionItem LoversAddWin;

        public static OptionItem KillFlashDuration;

        // HideAndSeek
        public static OptionItem AllowCloseDoors;
        public static OptionItem KillDelay;
        // public static OptionItem IgnoreCosmetics;
        public static OptionItem IgnoreVent;
        public static float HideAndSeekKillDelayTimer = 0f;

        // タスク無効化
        public static OptionItem DisableTasks;
        public static OptionItem DisableSwipeCard;
        public static OptionItem DisableSubmitScan;
        public static OptionItem DisableUnlockSafe;
        public static OptionItem DisableUploadData;
        public static OptionItem DisableStartReactor;
        public static OptionItem DisableResetBreaker;

        //デバイスブロック
        public static OptionItem DisableDevices;
        public static OptionItem DisableSkeldDevices;
        public static OptionItem DisableSkeldAdmin;
        public static OptionItem DisableSkeldCamera;
        public static OptionItem DisableMiraHQDevices;
        public static OptionItem DisableMiraHQAdmin;
        public static OptionItem DisableMiraHQDoorLog;
        public static OptionItem DisablePolusDevices;
        public static OptionItem DisablePolusAdmin;
        public static OptionItem DisablePolusCamera;
        public static OptionItem DisablePolusVital;
        public static OptionItem DisableAirshipDevices;
        public static OptionItem DisableAirshipCockpitAdmin;
        public static OptionItem DisableAirshipRecordsAdmin;
        public static OptionItem DisableAirshipCamera;
        public static OptionItem DisableAirshipVital;
        public static OptionItem DisableDevicesIgnoreConditions;
        public static OptionItem DisableDevicesIgnoreImpostors;
        public static OptionItem DisableDevicesIgnoreMadmates;
        public static OptionItem DisableDevicesIgnoreNeutrals;
        public static OptionItem DisableDevicesIgnoreCrewmates;
        public static OptionItem DisableDevicesIgnoreAfterAnyoneDied;

        // ランダムマップ
        public static OptionItem RandomMapsMode;
        public static OptionItem AddedTheSkeld;
        public static OptionItem AddedMiraHQ;
        public static OptionItem AddedPolus;
        public static OptionItem AddedTheAirShip;
        // public static OptionItem AddedDleks;

        // ランダムスポーン
        public static OptionItem RandomSpawn;
        public static OptionItem AirshipAdditionalSpawn;

        // 投票モード
        public static OptionItem VoteMode;
        public static OptionItem WhenSkipVote;
        public static OptionItem WhenSkipVoteIgnoreFirstMeeting;
        public static OptionItem WhenSkipVoteIgnoreNoDeadBody;
        public static OptionItem WhenSkipVoteIgnoreEmergency;
        public static OptionItem WhenNonVote;
        public static OptionItem WhenTie;
        public static readonly string[] voteModes =
        {
            "Default", "Suicide", "SelfVote", "Skip"
        };
        public static readonly string[] tieModes =
        {
            "TieMode.Default", "TieMode.All", "TieMode.Random"
        };
        public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetValue();
        public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetValue();

        // ボタン回数
        public static OptionItem SyncButtonMode;
        public static OptionItem SyncedButtonCount;
        public static int UsedButtonCount = 0;

        // 全員生存時の会議時間
        public static OptionItem AllAliveMeeting;
        public static OptionItem AllAliveMeetingTime;

        // 追加の緊急ボタンクールダウン
        public static OptionItem AdditionalEmergencyCooldown;
        public static OptionItem AdditionalEmergencyCooldownThreshold;
        public static OptionItem AdditionalEmergencyCooldownTime;

        //転落死
        public static OptionItem LadderDeath;
        public static OptionItem LadderDeathChance;

        // 通常モードでかくれんぼ
        public static bool IsStandardHAS => StandardHAS.GetBool() && CurrentGameMode == CustomGameMode.Standard;
        public static OptionItem StandardHAS;
        public static OptionItem StandardHASWaitingTime;

        // リアクターの時間制御
        public static OptionItem SabotageTimeControl;
        public static OptionItem PolusReactorTimeLimit;
        public static OptionItem AirshipReactorTimeLimit;

        // 停電の特殊設定
        public static OptionItem LightsOutSpecialSettings;
        public static OptionItem DisableAirshipViewingDeckLightsPanel;
        public static OptionItem DisableAirshipGapRoomLightsPanel;
        public static OptionItem DisableAirshipCargoLightsPanel;

        // マップ改造
        public static OptionItem AirShipVariableElectrical;
        public static OptionItem DisableAirshipMovingPlatform;

        // その他
        public static OptionItem FixFirstKillCooldown;
        public static OptionItem DisableTaskWin;
        public static OptionItem GhostCanSeeOtherRoles;
        public static OptionItem GhostCanSeeOtherTasks;
        public static OptionItem GhostCanSeeOtherVotes;
        public static OptionItem GhostCanSeeDeathReason;
        public static OptionItem GhostIgnoreTasks;
        public static OptionItem CommsCamouflage;

        // プリセット対象外
        public static OptionItem NoGameEnd;
        public static OptionItem AutoDisplayLastResult;
        public static OptionItem AutoDisplayKillLog;
        public static OptionItem SuffixMode;
        public static OptionItem HideGameSettings;
        public static OptionItem NameChangeMode;
        public static OptionItem ChangeNameToRoleInfo;
        public static OptionItem SkinControle;
        public static OptionItem NoHat;
        public static OptionItem NoFullFaceHat;
        public static OptionItem NoSkin;
        public static OptionItem NoVisor;
        public static OptionItem NoPet;
        public static OptionItem NoDuplicateHat;
        public static OptionItem NoDuplicateSkin;
        public static OptionItem RoleAssigningAlgorithm;

        public static OptionItem ApplyDenyNameList;
        public static OptionItem KickPlayerFriendCodeNotExist;
        public static OptionItem ApplyBanList;

        // TOH_Y機能
        // 会議収集理由表示
        public static OptionItem ShowReportReason;
        // 道連れ対象表示
        public static OptionItem ShowRevengeTarget;
        // 初手会議に役職説明表示
        public static OptionItem ShowRoleInfoAtFirstMeeting;
        // 道連れ設定
        public static OptionItem RevengeNeutral;
        public static OptionItem RevengeMadByImpostor;

        public static OptionItem ChangeIntro;
        public static OptionItem AddonShow;
        public static readonly string[] addonShowModes =
{
            "addonShowModes.Default", "addonShowModes.All", "addonShowModes.TOH"
        };
        public static AddonShowMode GetAddonShowModes() => (AddonShowMode)AddonShow.GetValue();
        public static readonly string[] nameChangeModes =
{
            "nameChangeMode.None", /*"nameChangeMode.Crew", */"nameChangeMode.Color"
        };
        public static NameChange GetNameChangeModes() => (NameChange)NameChangeMode.GetValue();

        public static readonly string[] suffixModes =
        {
            "SuffixMode.None",
            "SuffixMode.Version",
            "SuffixMode.Streaming",
            "SuffixMode.Recording",
            "SuffixMode.RoomHost",
            "SuffixMode.OriginalName"
        };
        public static readonly string[] RoleAssigningAlgorithms =
        {
            "RoleAssigningAlgorithm.Default",
            "RoleAssigningAlgorithm.NetRandom",
            "RoleAssigningAlgorithm.HashRandom",
            "RoleAssigningAlgorithm.Xorshift",
            "RoleAssigningAlgorithm.MersenneTwister",
        };
        public static SuffixModes GetSuffixMode()
        {
            return (SuffixModes)SuffixMode.GetValue();
        }

        public static bool IsLoaded = false;
        public static int GetRoleCount(CustomRoles role)
        {
            return GetRoleChance(role) == 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : 0;
        }

        public static int GetRoleChance(CustomRoles role)
        {
            return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetInt() : 0;
        }
        public static void Load()
        {
            if (IsLoaded) return;
            // プリセット
            _ = PresetOptionItem.Create(0, TabGroup.MainSettings)
                .SetColor(new Color32(204, 204, 0, 255))
                .SetHeader(true);

            // ゲームモード
            GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.MainSettings, false)
                .SetColor(new Color32(204, 204, 0, 255));

            #region 役職・詳細設定
            CustomRoleCounts = new();
            CustomRoleSpawnChances = new();

            var sortedRoleInfo = CustomRoleManager.AllRolesInfo.Values.OrderBy(role => role.ConfigId);
            // GM
            EnableGM = BooleanOptionItem.Create(100, "GM", false, TabGroup.MainSettings, false)
                .SetColor(new Color32(255, 91, 112, 255))
                .SetHeader(true);

            RoleAssignManager.SetupOptionItem();
            // Impostor
            sortedRoleInfo.Where(role => role.CustomRoleType == CustomRoleTypes.Impostor).Do(info =>
            {
                if (info.RoleName != CustomRoles.StrayWolf)
                    {
                        switch (info.RoleName)
                        {
                            case CustomRoles.Telepathisters:
                                SetupTelepathistersOptions(info.ConfigId, info.Tab, info.RoleName);
                                break;
                            default:
                                SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName);
                                break;
                        }
                    info.OptionCreator?.Invoke();
                }
            });

            DefaultShapeshiftCooldown = FloatOptionItem.Create(90100, "DefaultShapeshiftCooldown", new(5f, 999f, 5f), 15f, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);
            ImpostorOperateVisibility = BooleanOptionItem.Create(90110, "ImpostorOperateVisibility", false, TabGroup.ImpostorRoles, false)
                .SetGameMode(CustomGameMode.Standard);

            // Madmate
            RevengeMadByImpostor = BooleanOptionItem.Create(91500, "RevengeMadByImpostor", false, TabGroup.MadmateRoles, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetHeader(true);

            sortedRoleInfo.Where(role => role.CustomRoleType == CustomRoleTypes.Madmate).Do(info =>
                {
                    SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName);
                info.OptionCreator?.Invoke();
            });

            CanMakeMadmateCount = IntegerOptionItem.Create(91510, "CanMakeMadmateCount", new(0, 15, 1), 0, TabGroup.MadmateRoles, false)
                .SetColor(Palette.ImpostorRed)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard)
                .SetValueFormat(OptionFormat.Players);
            MadmateCanFixLightsOut = BooleanOptionItem.Create(91520, "MadmateCanFixLightsOut", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateCanFixComms = BooleanOptionItem.Create(91521, "MadmateCanFixComms", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateHasImpostorVision = BooleanOptionItem.Create(91522, "MadmateHasImpostorVision", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateCanSeeKillFlash = BooleanOptionItem.Create(91523, "MadmateCanSeeKillFlash", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateCanSeeOtherVotes = BooleanOptionItem.Create(91524, "MadmateCanSeeOtherVotes", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateCanSeeDeathReason = BooleanOptionItem.Create(91525, "MadmateCanSeeDeathReason", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateRevengeCrewmate = BooleanOptionItem.Create(91526, "MadmateExileCrewmate", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);

            // Madmate Common Options
            MadmateVentCooldown = FloatOptionItem.Create(91528, "MadmateVentCooldown", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false)
                .SetValueFormat(OptionFormat.Seconds);
            MadmateVentMaxTime = FloatOptionItem.Create(91529, "MadmateVentMaxTime", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false)
                .SetValueFormat(OptionFormat.Seconds);

            // Crewmate
            sortedRoleInfo.Where(role => role.CustomRoleType == CustomRoleTypes.Crewmate).Do(info =>
                {
                    switch (info.RoleName)
                    {
                        case CustomRoles.Sympathizer: //共鳴者は2人固定
                            SetupSingleRoleOptions(info.ConfigId, info.Tab, info.RoleName, 2);
                            break;
                        default:
                            SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName);
                            break;
                    }
                info.OptionCreator?.Invoke();
            });

            // Neutral
            RevengeNeutral = BooleanOptionItem.Create(95000, "RevengeNeutral", true, TabGroup.NeutralRoles, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetHeader(true);

            sortedRoleInfo.Where(role => role.CustomRoleType == CustomRoleTypes.Neutral).Do(info =>
            {
                    switch (info.RoleName)
                    {
                        case CustomRoles.Jackal: //ジャッカルは1人固定
                            SetupSingleRoleOptions(info.ConfigId, info.Tab, info.RoleName, 1);
                            break;
                        case CustomRoles.DarkHide: //ジャッカルは1人固定
                            SetupSingleRoleOptions(info.ConfigId, info.Tab, info.RoleName, 1);
                            break;
                        default:
                            SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName);
                            break;
                    }
                info.OptionCreator?.Invoke();
            });

            // Add-Ons
            SetupSingleRoleOptions(73000, TabGroup.Addons, CustomRoles.Lovers, 2);
            LoversAddWin = BooleanOptionItem.Create(73010, "LoversAddWin", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lovers]);
            LastImpostor.SetupCustomOption();
            CompreteCrew.SetupCustomOption();
            Workhorse.SetupCustomOption();

            AddWatch.SetupCustomOption();
            AddLight.SetupCustomOption();
            AddSeer.SetupCustomOption();
            Autopsy.SetupCustomOption();
            VIP.SetupCustomOption();
            Revenger.SetupCustomOption();
            Management.SetupCustomOption();
            Sending.SetupCustomOption();
            TieBreaker.SetupCustomOption();
            PlusVote.SetupCustomOption();
            Guarding.SetupCustomOption();
            AddBait.SetupCustomOption();
            Refusing.SetupCustomOption();

            Sunglasses.SetupCustomOption();
            Clumsy.SetupCustomOption();
            InfoPoor.SetupCustomOption();
            NonReport.SetupCustomOption();
            #endregion

            HideGameSettings = BooleanOptionItem.Create(1_000_000, "HideGameSettings", false, TabGroup.MainSettings, false)
                .SetColor(Color.gray)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);

            KillFlashDuration = FloatOptionItem.Create(100000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.MainSettings, false)
                .SetColor(Palette.ImpostorRed)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // HideAndSeek
            /********************************************************************************/
            SetupRoleOptions(200000, TabGroup.MainSettings, CustomRoles.HASFox, CustomGameMode.HideAndSeek);
            SetupRoleOptions(200100, TabGroup.MainSettings, CustomRoles.HASTroll, CustomGameMode.HideAndSeek);
            AllowCloseDoors = BooleanOptionItem.Create(201000, "AllowCloseDoors", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            KillDelay = FloatOptionItem.Create(201001, "HideAndSeekWaitingTime", new(0f, 180f, 5f), 10f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.HideAndSeek);
            //IgnoreCosmetics = CustomOption.Create(201002, Color.white, "IgnoreCosmetics", false)
            //    .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = BooleanOptionItem.Create(201003, "IgnoreVent", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.HideAndSeek);
            /********************************************************************************/

            // ランダムスポーン
            RandomSpawn = BooleanOptionItem.Create(100010, "RandomSpawn", false, TabGroup.MainSettings, false)
                .SetColor(Color.yellow)
                .SetHeader(true);
            AirshipAdditionalSpawn = BooleanOptionItem.Create(100011, "AirshipAdditionalSpawn", false, TabGroup.MainSettings, false).SetParent(RandomSpawn);

            //デバイス無効化
            DisableDevices = BooleanOptionItem.Create(100100, "DisableDevices", false, TabGroup.MainSettings, false)
                .SetColor(Color.yellow)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldDevices = BooleanOptionItem.Create(100110, "DisableSkeldDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray);
            DisableSkeldAdmin = BooleanOptionItem.Create(100111, "DisableSkeldAdmin", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices);
            DisableSkeldCamera = BooleanOptionItem.Create(100112, "DisableSkeldCamera", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices);
            DisableMiraHQDevices = BooleanOptionItem.Create(100120, "DisableMiraHQDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray);
            DisableMiraHQAdmin = BooleanOptionItem.Create(100121, "DisableMiraHQAdmin", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices);
            DisableMiraHQDoorLog = BooleanOptionItem.Create(100122, "DisableMiraHQDoorLog", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices);
            DisablePolusDevices = BooleanOptionItem.Create(100130, "DisablePolusDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray);
            DisablePolusAdmin = BooleanOptionItem.Create(100131, "DisablePolusAdmin", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices);
            DisablePolusCamera = BooleanOptionItem.Create(100132, "DisablePolusCamera", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices);
            DisablePolusVital = BooleanOptionItem.Create(100133, "DisablePolusVital", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices);
            DisableAirshipDevices = BooleanOptionItem.Create(100140, "DisableAirshipDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray);
            DisableAirshipCockpitAdmin = BooleanOptionItem.Create(100141, "DisableAirshipCockpitAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices);
            DisableAirshipRecordsAdmin = BooleanOptionItem.Create(100142, "DisableAirshipRecordsAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices);
            DisableAirshipCamera = BooleanOptionItem.Create(100143, "DisableAirshipCamera", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices);
            DisableAirshipVital = BooleanOptionItem.Create(100144, "DisableAirshipVital", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices);
            DisableDevicesIgnoreConditions = BooleanOptionItem.Create(100190, "IgnoreConditions", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray);
            DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(100191, "IgnoreImpostors", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);
            DisableDevicesIgnoreMadmates = BooleanOptionItem.Create(100192, "IgnoreMadmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);
            DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(100193, "IgnoreNeutrals", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);
            DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(100194, "IgnoreCrewmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);
            DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(100195, "IgnoreAfterAnyoneDied", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);

            // リアクターの時間制御
            SabotageTimeControl = BooleanOptionItem.Create(100200, "SabotageTimeControl", false, TabGroup.MainSettings, false)
                .SetColor(Color.magenta)
                .SetHeader(true);
            PolusReactorTimeLimit = FloatOptionItem.Create(100201, "PolusReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds);
            AirshipReactorTimeLimit = FloatOptionItem.Create(100202, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds);

            // 停電の特殊設定
            LightsOutSpecialSettings = BooleanOptionItem.Create(100210, "LightsOutSpecialSettings", false, TabGroup.MainSettings, false)
                .SetColor(Color.magenta);
            DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(100211, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings);
            DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(100212, "DisableAirshipGapRoomLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings);
            DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(100213, "DisableAirshipCargoLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings);

            //コミュサボカモフラージュ
            CommsCamouflage = BooleanOptionItem.Create(100220, "CommsCamouflage", false, TabGroup.MainSettings, false)
                .SetColor(Color.magenta);

            // 会議収集理由表示
            ShowReportReason = BooleanOptionItem.Create(100300, "ShowReportReason", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColor(Color.cyan);

            //道連れ人表記
            ShowRevengeTarget = BooleanOptionItem.Create(100310, "ShowRevengeTarget", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.Standard);

            //初手会議に役職名表示
            ShowRoleInfoAtFirstMeeting = BooleanOptionItem.Create(100320, "ShowRoleInfoAtFirstMeeting", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.Standard);

            // ボタン回数同期
            SyncButtonMode = BooleanOptionItem.Create(100330, "SyncButtonMode", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan);
            SyncedButtonCount = IntegerOptionItem.Create(100331, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.MainSettings, false).SetParent(SyncButtonMode)
                .SetValueFormat(OptionFormat.Times);

            // 投票モード
            VoteMode = BooleanOptionItem.Create(100340, "VoteMode", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan);
            WhenSkipVote = StringOptionItem.Create(100341, "WhenSkipVote", voteModes[0..3], 0, TabGroup.MainSettings, false).SetParent(VoteMode);
            WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(100342, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(100343, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(100344, "WhenSkipVoteIgnoreEmergency", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = StringOptionItem.Create(100345, "WhenNonVote", voteModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode);
            WhenTie = StringOptionItem.Create(100346, "WhenTie", tieModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);

            // 全員生存時の会議時間
            AllAliveMeeting = BooleanOptionItem.Create(100350, "AllAliveMeeting", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.Standard);
            AllAliveMeetingTime = FloatOptionItem.Create(100351, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.MainSettings, false).SetParent(AllAliveMeeting)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // 生存人数ごとの緊急会議
            AdditionalEmergencyCooldown = BooleanOptionItem.Create(100360, "AdditionalEmergencyCooldown", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.Standard);
            AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(100361, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(CustomGameMode.Standard);
            AdditionalEmergencyCooldownTime = FloatOptionItem.Create(100362, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // タスク無効化
            DisableTasks = BooleanOptionItem.Create(100400, "DisableTasks", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColor(Color.green);
            DisableSwipeCard = BooleanOptionItem.Create(100401, "DisableSwipeCardTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks);
            DisableSubmitScan = BooleanOptionItem.Create(100402, "DisableSubmitScanTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks);
            DisableUnlockSafe = BooleanOptionItem.Create(100403, "DisableUnlockSafeTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks);
            DisableUploadData = BooleanOptionItem.Create(100404, "DisableUploadDataTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks);
            DisableStartReactor = BooleanOptionItem.Create(100405, "DisableStartReactorTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks);
            DisableResetBreaker = BooleanOptionItem.Create(100406, "DisableResetBreakerTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks);

            // タスク勝利無効化
            DisableTaskWin = BooleanOptionItem.Create(1_001_000, "DisableTaskWin", false, TabGroup.MainSettings, false)
                .SetColor(Color.green)
                .SetGameMode(CustomGameMode.Standard);

            // 幽霊
            GhostIgnoreTasks = BooleanOptionItem.Create(1_001_010, "GhostIgnoreTasks", false, TabGroup.MainSettings, false)
                .SetColor(Palette.LightBlue)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            GhostCanSeeOtherRoles = BooleanOptionItem.Create(1_001_011, "GhostCanSeeOtherRoles", false, TabGroup.MainSettings, false)
                .SetColor(Palette.LightBlue);
            GhostCanSeeOtherTasks = BooleanOptionItem.Create(1_001_012, "GhostCanSeeOtherTasks", false, TabGroup.MainSettings, false)
                .SetColor(Palette.LightBlue);
            GhostCanSeeOtherVotes = BooleanOptionItem.Create(1_001_013, "GhostCanSeeOtherVotes", false, TabGroup.MainSettings, false)
                .SetColor(Palette.LightBlue);
            GhostCanSeeDeathReason = BooleanOptionItem.Create(1_001_014, "GhostCanSeeDeathReason", false, TabGroup.MainSettings, false)
                .SetColor(Palette.LightBlue)
                .SetGameMode(CustomGameMode.Standard);

            // ランダムマップ
            RandomMapsMode = BooleanOptionItem.Create(100500, "RandomMapsMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColor(Palette.Orange);
            AddedTheSkeld = BooleanOptionItem.Create(100501, "AddedTheSkeld", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode);
            AddedMiraHQ = BooleanOptionItem.Create(100502, "AddedMIRAHQ", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode);
            AddedPolus = BooleanOptionItem.Create(100503, "AddedPolus", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode);
            AddedTheAirShip = BooleanOptionItem.Create(100504, "AddedTheAirShip", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode);
            // MapDleks = CustomOption.Create(100505, Color.white, "AddedDleks", false, RandomMapMode);

            // 転落死
            LadderDeath = BooleanOptionItem.Create(100510, "LadderDeath", false, TabGroup.MainSettings, false)
                .SetColor(Palette.Orange);
            LadderDeathChance = StringOptionItem.Create(100511, "LadderDeathChance", rates[1..], 0, TabGroup.MainSettings, false).SetParent(LadderDeath);

            // マップ改造
            AirShipVariableElectrical = BooleanOptionItem.Create(100520, "AirShipVariableElectrical", false, TabGroup.MainSettings, false)
                .SetColor(Palette.Orange);
            DisableAirshipMovingPlatform = BooleanOptionItem.Create(100530, "DisableAirshipMovingPlatform", false, TabGroup.MainSettings, false)
                .SetColor(Palette.Orange);

            // 初手キルクール調整
            FixFirstKillCooldown = BooleanOptionItem.Create(1_001_020, "FixFirstKillCooldown", false, TabGroup.MainSettings, false)
                .SetColor(Palette.Orange);

            //シンクロカラーモード

            // 通常モードでかくれんぼ用
            StandardHAS = BooleanOptionItem.Create(100600, "StandardHAS", false, TabGroup.MainSettings, false)
                //上記載時にheader消去
                .SetHeader(true)
                .SetColor(Color.yellow)
                .SetGameMode(CustomGameMode.Standard);
            StandardHASWaitingTime = FloatOptionItem.Create(100601, "StandardHASWaitingTime", new(0f, 180f, 2.5f), 10f, TabGroup.MainSettings, false).SetParent(StandardHAS)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // その他
            NoGameEnd = BooleanOptionItem.Create(1_002_000, "NoGameEnd", false, TabGroup.MainSettings, false)
                .SetHeader(true);
            AutoDisplayLastResult = BooleanOptionItem.Create(1_002_001, "AutoDisplayLastResult", true, TabGroup.MainSettings, false);
            AutoDisplayKillLog = BooleanOptionItem.Create(1_002_002, "AutoDisplayKillLog", true, TabGroup.MainSettings, false);
            SuffixMode = StringOptionItem.Create(1_002_003, "SuffixMode", suffixModes, 0, TabGroup.MainSettings, true);
            NameChangeMode = StringOptionItem.Create(1_002_004, "NameChangeMode", nameChangeModes, 0, TabGroup.MainSettings, true);
            ChangeNameToRoleInfo = BooleanOptionItem.Create(1_002_005, "ChangeNameToRoleInfo", true, TabGroup.MainSettings, false);
            AddonShow = StringOptionItem.Create(1_002_006, "AddonShowMode", addonShowModes, 0, TabGroup.MainSettings, true);
            ChangeIntro = BooleanOptionItem.Create(1_002_007, "ChangeIntro", false, TabGroup.MainSettings, false);
            SkinControle = BooleanOptionItem.Create(1_002_010, "SkinControle", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            NoHat = BooleanOptionItem.Create(1_002_011, "NoHat", false, TabGroup.MainSettings, false).SetParent(SkinControle)
                .SetGameMode(CustomGameMode.All);
            NoFullFaceHat = BooleanOptionItem.Create(1_002_012, "NoFullFaceHat", false, TabGroup.MainSettings, false).SetParent(SkinControle)
                .SetGameMode(CustomGameMode.All);
            NoSkin = BooleanOptionItem.Create(1_002_013, "NoSkin", false, TabGroup.MainSettings, false).SetParent(SkinControle)
                .SetGameMode(CustomGameMode.All);
            NoVisor = BooleanOptionItem.Create(1_002_014, "NoVisor", false, TabGroup.MainSettings, false).SetParent(SkinControle)
                .SetGameMode(CustomGameMode.All);
            NoPet = BooleanOptionItem.Create(1_002_015, "NoPet", false, TabGroup.MainSettings, false).SetParent(SkinControle)
                .SetGameMode(CustomGameMode.All);
            NoDuplicateHat = BooleanOptionItem.Create(1_002_016, "NoDuplicateHat", false, TabGroup.MainSettings, false).SetParent(SkinControle)
                .SetGameMode(CustomGameMode.All);
            NoDuplicateSkin = BooleanOptionItem.Create(1_002_017, "NoDuplicateSkin", false, TabGroup.MainSettings, false).SetParent(SkinControle)
                .SetGameMode(CustomGameMode.All);
            RoleAssigningAlgorithm = StringOptionItem.Create(1_002_008, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 0, TabGroup.MainSettings, true)
                .RegisterUpdateValueEvent((object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue));
            VoiceReader.SetupCustomOption();

            ApplyDenyNameList = BooleanOptionItem.Create(1_003_000, "ApplyDenyNameList", true, TabGroup.MainSettings, true)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(1_003_001, "KickPlayerFriendCodeNotExist", false, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);
            ApplyBanList = BooleanOptionItem.Create(1_003_002, "ApplyBanList", true, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);

            DebugModeManager.SetupCustomOption();

            IsLoaded = true;
        }

        public static bool NotShowOption(string optionName)
        {
            return optionName is "KillFlashDuration"
                            or "SuffixMode"
                            or "HideGameSettings"
                            or "AutoDisplayLastResult"
                            or "AutoDisplayKillLog"
                            or "RoleAssigningAlgorithm"
                            or "IsReportShow"
                            or "ShowRoleInfoAtFirstMeeting"
                            or "ChangeNameToRoleInfo"
                            or "AddonShowDontOmit"
                            or "ApplyDenyNameList"
                            or "KickPlayerFriendCodeNotExist"
                            or "ApplyBanList"
                            or "ChangeIntro";
        }
        public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            if (role.IsVanilla()) return;
            int MaxCount = 15;
            if (role.IsImpostor()) MaxCount = 3;

            var spawnOption = IntegerOptionItem.Create(id, role.ToString(), new(0, 100, 10), 0, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetValueFormat(OptionFormat.Percent)
                .SetHeader(true)
                .SetGameMode(customGameMode) as IntegerOptionItem;
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, MaxCount, 1), 1, tab, false).SetParent(spawnOption)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public static void SetupSingleRoleOptions(int id, TabGroup tab, CustomRoles role, int count, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = IntegerOptionItem.Create(id, role.ToString(), new(0, 100, 10), 0, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetValueFormat(OptionFormat.Percent)
                .SetHeader(true)
                .SetGameMode(customGameMode) as IntegerOptionItem;
            // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(count, count, count), count, tab, false).SetParent(spawnOption)
                //.SetHidden(true)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        private static void SetupTelepathistersOptions(int id, TabGroup tab, CustomRoles role)
        {
            var spawnOption = IntegerOptionItem.Create(id, role.ToString(), new(0, 100, 10), 0, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetValueFormat(OptionFormat.Percent)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard) as IntegerOptionItem;
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(2, 3, 1), 2, tab, false).SetParent(spawnOption)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(CustomGameMode.Standard);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        //AddOn
        public static void SetUpAddOnOptions(int Id, CustomRoles PlayerRole, TabGroup tab)
        {
            AddOnBuffAssign[PlayerRole] = BooleanOptionItem.Create(Id, "AddOnBuffAssign", false, tab, false).SetParent(CustomRoleSpawnChances[PlayerRole]);
            Id += 10;
            foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsBuffAddOn()))
            {
                if (Addon == CustomRoles.Loyalty && PlayerRole is
                    CustomRoles.MadSnitch or CustomRoles.Jackal or CustomRoles.JClient or CustomRoles.LastImpostor or CustomRoles.CompreteCrew) continue;
                if (Addon == CustomRoles.Revenger && PlayerRole is CustomRoles.MadNimrod) continue;

                SetUpAddOnRoleOption(PlayerRole, tab, Addon, Id, false, AddOnBuffAssign[PlayerRole]);
                Id++;
            }
            AddOnDebuffAssign[PlayerRole] = BooleanOptionItem.Create(Id, "AddOnDebuffAssign", false, tab, false).SetParent(CustomRoleSpawnChances[PlayerRole]);
            Id += 10;
            foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsDebuffAddOn()))
            {
                SetUpAddOnRoleOption(PlayerRole, tab, Addon, Id, false, AddOnDebuffAssign[PlayerRole]);
                Id++;
            }
        }
        public static void SetUpAddOnRoleOption(CustomRoles PlayerRole, TabGroup tab, CustomRoles role, int Id, bool defaultValue = false, OptionItem parent = null)
        {
            if (parent == null) parent = CustomRoleSpawnChances[PlayerRole];
            var roleName = Utils.GetRoleName(role) + Utils.GetAddonAbilityInfo(role);
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
            AddOnRoleOptions[(PlayerRole, role)] = BooleanOptionItem.Create(Id, "AddOnAssign%role%", defaultValue, tab, false).SetParent(parent);
            AddOnRoleOptions[(PlayerRole, role)].ReplacementDictionary = replacementDic;
        }

        public class OverrideTasksData
        {
            public static Dictionary<CustomRoles, OverrideTasksData> AllData = new();
            public CustomRoles Role { get; private set; }
            public int IdStart { get; private set; }
            public OptionItem doOverride;
            public OptionItem assignCommonTasks;
            public OptionItem numLongTasks;
            public OptionItem numShortTasks;

            public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role, OptionItem option = null)
            {
                this.IdStart = idStart;
                this.Role = role;

                if(option == null) option = CustomRoleSpawnChances[role];
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.GetRoleName(role) } };
                doOverride = BooleanOptionItem.Create(idStart++, "doOverride", false, tab, false).SetParent(option)
                    .SetValueFormat(OptionFormat.None);
                doOverride.ReplacementDictionary = replacementDic;
                assignCommonTasks = BooleanOptionItem.Create(idStart++, "assignCommonTasks", true, tab, false).SetParent(doOverride)
                    .SetValueFormat(OptionFormat.None);
                assignCommonTasks.ReplacementDictionary = replacementDic;
                numLongTasks = IntegerOptionItem.Create(idStart++, "roleLongTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                    .SetValueFormat(OptionFormat.Pieces);
                numLongTasks.ReplacementDictionary = replacementDic;
                numShortTasks = IntegerOptionItem.Create(idStart++, "roleShortTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                    .SetValueFormat(OptionFormat.Pieces);
                numShortTasks.ReplacementDictionary = replacementDic;

                if (!AllData.ContainsKey(role)) AllData.Add(role, this);
                else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
            }
            public static OverrideTasksData Create(int idStart, TabGroup tab, CustomRoles role)
            {
                return new OverrideTasksData(idStart, tab, role);
            }
            public static OverrideTasksData Create(SimpleRoleInfo roleInfo, int idOffset, OptionItem option = null)
            {
                return new OverrideTasksData(roleInfo.ConfigId + idOffset, roleInfo.Tab, roleInfo.RoleName, option);
            }
        }
    }
}
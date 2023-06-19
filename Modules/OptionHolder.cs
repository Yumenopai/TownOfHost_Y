using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Madmate;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns;

namespace TownOfHost
{
    [Flags]
    public enum CustomGameMode
    {
        Standard,
        CatchCat,
        OneNight,
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
        public static CustomGameMode CurrentGameMode => (CustomGameMode)GameMode.GetValue();

        public static readonly string[] gameModes =
        {
            "Standard", "CatchCat", "OneNight",
        };

        // MapActive
        public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || Main.NormalOptions.MapId == 0;
        public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || Main.NormalOptions.MapId == 1;
        public static bool IsActivePolus => AddedPolus.GetBool() || Main.NormalOptions.MapId == 2;
        public static bool IsActiveAirship => AddedTheAirShip.GetBool() || Main.NormalOptions.MapId == 4;

        // 姿変更モードアクティブ
        public static bool IsSyncColorMode => GetSyncColorMode() != 0;

        // 役職数・確率
        public static Dictionary<CustomRoles, int> roleCounts;
        public static Dictionary<CustomRoles, float> roleSpawnChances;
        public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
        public static Dictionary<CustomRoles, BooleanOptionItem> CustomRoleSpawnOnOff;
        //public static Dictionary<CustomRoles, StringOptionItem> CustomRoleSpawnChances;
        public static readonly string[] rates =
        {
            "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
            "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };
        public static readonly string[] ratesZeroOne =
        {
            "Rate0", /*"Rate10", "Rate20", "Rate30", "Rate40", "Rate50",
            "Rate60", "Rate70", "Rate80", "Rate90", */"Rate100",
        };
        public static readonly string[] ratesOne =
        {
            /*"Rate0", "Rate10", "Rate20", "Rate30", "Rate40", "Rate50",
            "Rate60", "Rate70", "Rate80", "Rate90", */"Rate100",
        };

        // 各役職の詳細設定
        public static OptionItem EnableGM;
        public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
        public static OptionItem VampireKillDelay;
        public static OptionItem ShapeMasterShapeshiftDuration;
        public static OptionItem DefaultShapeshiftCooldown;
        public static OptionItem CanMakeMadmateCount;
        public static OptionItem MadGuardianCanSeeWhoTriedToKill;
        public static OptionItem MadSnitchCanVent;
        public static OptionItem MadSnitchCanAlsoBeExposedToImpostor;
        public static OptionItem MadmateCanFixLightsOut;
        public static OptionItem MadmateCanFixComms;
        public static OptionItem MadmateHasImpostorVision;
        public static OptionItem MadmateCanSeeKillFlash;
        public static OptionItem MadmateCanSeeOtherVotes;
        public static OptionItem MadmateCanSeeDeathReason;
        public static OptionItem MadmateRevengeCrewmate;
        public static OptionItem MadmateVentCooldown;
        public static OptionItem MadmateVentMaxTime;

        public static OptionItem EvilWatcherChance;
        public static OptionItem LighterTaskCompletedVision;
        public static OptionItem LighterTaskCompletedDisableLightOut;
        public static OptionItem MayorAdditionalVote;
        public static OptionItem MayorHasPortableButton;
        public static OptionItem MayorNumOfUseButton;
        public static OptionItem DoctorTaskCompletedBatteryCharge;
        public static OptionItem SpeedBoosterUpSpeed; //加速値
        public static OptionItem SpeedBoosterTaskTrigger; //効果を発動するタスク完了数
        public static OptionItem TrapperBlockMoveTime;
        public static OptionItem CanTerroristSuicideWin;
        public static OptionItem ArsonistDouseTime;
        public static OptionItem ArsonistCooldown;
        public static OptionItem KillFlashDuration;

        //役職別設定
        public static Dictionary<CustomRoles, OptionItem> IsCustomKillCool;
        public static Dictionary<CustomRoles, OptionItem> CustomKillCool;
        public static Dictionary<CustomRoles, OptionItem> IsCustomVentCool;
        public static Dictionary<CustomRoles, OptionItem> CustomVentCool;


        //TOH_Y
        //機能
        public static OptionItem OperateVisibilityImpostor;
        public static OptionItem TakeCompanionNeutral;
        //public static OptionItem LoadReduction;//105000
        public static OptionItem SyncColorMode;//105100
        public static readonly string[] SelectSyncColorMode =
        {
            "None", "Clone", "fif_fif", "ThreeCornered", "Twin",
        };
        public static SyncColorMode GetSyncColorMode() => (SyncColorMode)SyncColorMode.GetValue();
        public static OptionItem IsReportShow;//105200
        public static OptionItem ShowRevengeTarget;//105300
        public static OptionItem AddonShowDontOmit;//105400
        public static OptionItem ShowRoleInfoAtFirstMeeting;//105500
        public static OptionItem ChangeIntro;//105600

        //役職TOHY
        public static OptionItem MafiaCanKill;
        public static OptionItem BaitWaitTime;
        public static OptionItem DoctorHasVital;
        public static OptionItem LighterTaskTrigger; //効果を発動するタスク完了数
        public static OptionItem TakeCompanionMad;
        public static OptionItem MadDictatorCanVent;
        public static OptionItem OpportunistCanKill;
        public static OptionItem KOpportunistKillCooldown;
        public static OptionItem KOpportunistHasImpostorVision;
        public static OptionItem TaskManagerSeeNowtask;
        public static OptionItem WorkaholicVentCooldown;
        public static OptionItem WorkaholicSeen;
        public static OptionItem WorkaholicTaskSeen;
        public static OptionItem WorkaholicCannotWinAtDeath;
        public static OptionItem ChairmanNumOfUseButton;
        public static OptionItem ChairmanIgnoreSkip;
        public static OptionItem SympaCheckedTasks;
        public static OptionItem GuardSpellTimes;
        public static OptionItem VictoryCutCount;
        public static OptionItem LoveCutterKnow;
        public static OptionItem LoversAddWin;
        public static OptionItem BlinderVision;
        public static OptionItem ExpressSpeed; //加速値
        public static OptionItem JClientCanVent;
        public static OptionItem JClientVentCooldown;
        public static OptionItem JClientVentMaxTime;
        public static OptionItem JClientCanAlsoBeExposedToJackal;
        public static OptionItem OppoKillerShotLimitOpt;
        public static OptionItem AntiCompGuardCount;
        public static OptionItem AntiCompKnowOption;
        public static OptionItem AntiCompKnowNotask;
        public static OptionItem AntiCompKnowCompTask;
        public static OptionItem AntiCompAddGuardCount;
        public static OptionItem MafiaKillCooldown;
        public static OptionItem RainbowDontSeeTaskTurn;
        public static OptionItem PotentialistTaskTrigger;

        public static OptionItem JClientAfterJackalDead;
        public enum AfterJackalDeadMode
        {
            None,
            Following
        };

        //属性
        public static OptionItem AddLighterDisableLightOut;
        public static OptionItem AddLightAddCrewmateVision;
        public static OptionItem AddLightAddImpostorVision;
        public static OptionItem SunglassesSubCrewmateVision;
        public static OptionItem SunglassesSubImpostorVision;
        public static OptionItem ManagementSeeNowtask;

        //役職直接属性付与
        public static Dictionary<(CustomRoles, CustomRoles), OptionItem> AddOnRoleOptions = new();
        public static Dictionary<CustomRoles, OverrideTasksData> AddOnLoyaltyTask = new();
        public static Dictionary<CustomRoles, OptionItem> AddOnBuffAssign = new();
        public static Dictionary<CustomRoles, OptionItem> AddOnDebuffAssign = new();

        //CatchCat
        public static OptionItem IgnoreVent;
        public static OptionItem LeaderNotKilled;
        public static OptionItem CatNotKilled;

        //OneNight
        public static OptionItem HangedManHasntTask;
        public static OptionItem ONTrapperBlockMoveTime;

        // HideAndSeek
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
        public static OptionItem AddedDleks;

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
        //エレキ構造変化
        public static OptionItem AirShipVariableElectrical;

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

        // タスク上書き
        public static OverrideTasksData MadGuardianTasks;
        public static OverrideTasksData TerroristTasks;
        public static OverrideTasksData SnitchTasks;
        public static OverrideTasksData MadSnitchTasks;
        public static OverrideTasksData WorkaholicTasks;//TOH_Y
        public static OverrideTasksData LoveCutterTasks;
        public static OverrideTasksData JClientTasks;
        public static OverrideTasksData AntiCompleteTasks;

        // その他
        public static OptionItem FixFirstKillCooldown;
        public static OptionItem DisableTaskWin;
        public static OptionItem GhostCanSeeOtherRoles;
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
        public static OptionItem ColorNameMode;
        public static OptionItem ChangeNameToRoleInfo;
        public static OptionItem RoleAssigningAlgorithm;

        public static OptionItem ApplyDenyNameList;
        public static OptionItem KickPlayerFriendCodeNotExist;
        public static OptionItem ApplyBanList;

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



        //public static int SnitchExposeTaskLeft = 1;


        public static bool IsEvilWatcher = false;
        public static void SetWatcherTeam(float EvilWatcherRate)
        {
            EvilWatcherRate = Options.EvilWatcherChance.GetFloat();
            IsEvilWatcher = UnityEngine.Random.Range(1, 100) < EvilWatcherRate;
        }
        public static bool IsLoaded = false;

        static Options()
        {
            ResetRoleCounts();
        }
        public static void ResetRoleCounts()
        {
            roleCounts = new Dictionary<CustomRoles, int>();
            roleSpawnChances = new Dictionary<CustomRoles, float>();

            foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
            {
                roleCounts.Add(role, 0);
                roleSpawnChances.Add(role, 0);
            }
        }

        public static void SetRoleCount(CustomRoles role, int count)
        {
            roleCounts[role] = count;

            if (CustomRoleCounts.TryGetValue(role, out var option))
            {
                option.SetValue(count - 1);
            }
        }

        public static int GetRoleCount(CustomRoles role)
        {
            return (CustomRoleSpawnOnOff.TryGetValue(role, out var Isin) && Isin.GetBool())
                ? CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : roleCounts[role] : 0;
            //var chance = CustomRoleSpawnOnOff.TryGetValue(role, out var sc) ? sc.GetChance() : 0;
            //return chance == 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : roleCounts[role];
        }

        public static float GetRoleChance(CustomRoles role)
        {
            return CustomRoleSpawnOnOff.TryGetValue(role, out var option) ? option.GetValue()/* / 10f */ : roleSpawnChances[role];
        }
        public static void Load()
        {
            if (IsLoaded) return;
            /**************************************** main setting ****************************************/
            // プリセット
            _ = PresetOptionItem.Create(0, TabGroup.MainSettings)
                .SetColor(new Color32(204, 204, 0, 255))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            // ゲームモード
            GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.MainSettings, false)
                .SetColor(new Color32(204, 204, 0, 255))
                .SetGameMode(CustomGameMode.All);

            #region 役職・詳細設定

            HideGameSettings = BooleanOptionItem.Create(1_000_002, "HideGameSettings", false, TabGroup.MainSettings, false)
                .SetColor(Color.gray)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);

            CustomRoleCounts = new();
            CustomRoleSpawnOnOff = new();

            // GM
            EnableGM = BooleanOptionItem.Create(100, "GM", false, TabGroup.MainSettings, false)
                .SetColor(new Color32(255, 91, 112, 255))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            //LoadReduction = BooleanOptionItem.Create(105000, "LoadReduction", false, TabGroup.MainSettings, false)
            //    .SetColor(Color.gray)
            //    .SetGameMode(CustomGameMode.Standard);

            KillFlashDuration = FloatOptionItem.Create(90000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.MainSettings, false)
                .SetColor(Palette.ImpostorRed)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            /**************************************** CatMode ****************************************/
            SetupLeaderRoleOptions(69700, CustomRoles.CatRedLeader);
            SetupLeaderRoleOptions(69800, CustomRoles.CatBlueLeader);
            SetupAddLeaderRoleOptions(69900, CustomRoles.CatYellowLeader);

            IgnoreVent = BooleanOptionItem.Create(69911, "IgnoreVent", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.CatchCat);
            LeaderNotKilled = BooleanOptionItem.Create(69912, "LeaderNotKilled", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.CatchCat);
            CatNotKilled = BooleanOptionItem.Create(69913, "CatNotKilled", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.CatchCat);

            /********************************************************************************/

            // ランダムスポーン
            RandomSpawn = BooleanOptionItem.Create(101300, "RandomSpawn", false, TabGroup.MainSettings, false)
                .SetColor(Color.yellow)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            AirshipAdditionalSpawn = BooleanOptionItem.Create(101301, "AirshipAdditionalSpawn", false, TabGroup.MainSettings, false).SetParent(RandomSpawn)
                .SetGameMode(CustomGameMode.All);

            //デバイス無効化
            DisableDevices = BooleanOptionItem.Create(101200, "DisableDevices", false, TabGroup.MainSettings, false)
                .SetColor(Color.yellow)
                .SetGameMode(CustomGameMode.All);
            DisableSkeldDevices = BooleanOptionItem.Create(101210, "DisableSkeldDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray)
                .SetGameMode(CustomGameMode.All);
            DisableSkeldAdmin = BooleanOptionItem.Create(101211, "DisableSkeldAdmin", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.All);
            DisableSkeldCamera = BooleanOptionItem.Create(101212, "DisableSkeldCamera", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.All);
            DisableMiraHQDevices = BooleanOptionItem.Create(101220, "DisableMiraHQDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray)
                .SetGameMode(CustomGameMode.All);
            DisableMiraHQAdmin = BooleanOptionItem.Create(101221, "DisableMiraHQAdmin", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.All);
            DisableMiraHQDoorLog = BooleanOptionItem.Create(101222, "DisableMiraHQDoorLog", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.All);
            DisablePolusDevices = BooleanOptionItem.Create(101230, "DisablePolusDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray)
                .SetGameMode(CustomGameMode.All);
            DisablePolusAdmin = BooleanOptionItem.Create(101231, "DisablePolusAdmin", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.All);
            DisablePolusCamera = BooleanOptionItem.Create(101232, "DisablePolusCamera", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.All);
            DisablePolusVital = BooleanOptionItem.Create(101233, "DisablePolusVital", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.All);
            DisableAirshipDevices = BooleanOptionItem.Create(101240, "DisableAirshipDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray)
                .SetGameMode(CustomGameMode.All);
            DisableAirshipCockpitAdmin = BooleanOptionItem.Create(101241, "DisableAirshipCockpitAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.All);
            DisableAirshipRecordsAdmin = BooleanOptionItem.Create(101242, "DisableAirshipRecordsAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.All);
            DisableAirshipCamera = BooleanOptionItem.Create(101243, "DisableAirshipCamera", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.All);
            DisableAirshipVital = BooleanOptionItem.Create(101244, "DisableAirshipVital", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.All);
            DisableDevicesIgnoreConditions = BooleanOptionItem.Create(101290, "IgnoreConditions", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetColor(Color.gray)
                .SetGameMode(CustomGameMode.All);
            DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(101291, "IgnoreImpostors", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.All);
            DisableDevicesIgnoreMadmates = BooleanOptionItem.Create(101292, "IgnoreMadmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.All);
            DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(101293, "IgnoreNeutrals", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.All);
            DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(101294, "IgnoreCrewmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.All);
            DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(101295, "IgnoreAfterAnyoneDied", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.All);

            // リアクターの時間制御
            SabotageTimeControl = BooleanOptionItem.Create(100800, "SabotageTimeControl", false, TabGroup.MainSettings, false)
                .SetColor(Color.magenta)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            PolusReactorTimeLimit = FloatOptionItem.Create(100801, "PolusReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.All);
            AirshipReactorTimeLimit = FloatOptionItem.Create(100802, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.All);

            // 停電の特殊設定
            LightsOutSpecialSettings = BooleanOptionItem.Create(101500, "LightsOutSpecialSettings", false, TabGroup.MainSettings, false)
                .SetColor(Color.magenta)
                .SetGameMode(CustomGameMode.All);
            DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(101511, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.All);
            DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(101512, "DisableAirshipGapRoomLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.All);
            DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(101513, "DisableAirshipCargoLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.All);

            //コミュサボのカモフラージュ
            CommsCamouflage = BooleanOptionItem.Create(900_013, "CommsCamouflage", false, TabGroup.MainSettings, false)
                .SetColor(Color.magenta)
                .SetGameMode(CustomGameMode.All);

            // 収集表示
            IsReportShow = BooleanOptionItem.Create(105200, "IsReportShow", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            // 道連れ人表記
            ShowRevengeTarget = BooleanOptionItem.Create(105300, "ShowRevengeTarget", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.Standard);

            ShowRoleInfoAtFirstMeeting = BooleanOptionItem.Create(105500, "ShowRoleInfoAtFirstMeeting", true, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.Standard);


            // ボタン回数同期
            SyncButtonMode = BooleanOptionItem.Create(100200, "SyncButtonMode", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.All);
            SyncedButtonCount = IntegerOptionItem.Create(100201, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.MainSettings, false).SetParent(SyncButtonMode)
                .SetValueFormat(OptionFormat.Times)
                .SetGameMode(CustomGameMode.All);
            
            // 投票モード
            VoteMode = BooleanOptionItem.Create(100500, "VoteMode", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.All);
            WhenSkipVote = StringOptionItem.Create(100510, "WhenSkipVote", voteModes[0..3], 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.All);
            WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(100511, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(100512, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(100513, "WhenSkipVoteIgnoreEmergency", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = StringOptionItem.Create(100520, "WhenNonVote", voteModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.All);
            WhenTie = StringOptionItem.Create(100530, "WhenTie", tieModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);

            // 全員生存時の会議時間
            AllAliveMeeting = BooleanOptionItem.Create(100900, "AllAliveMeeting", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.Standard);
            AllAliveMeetingTime = FloatOptionItem.Create(100901, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.MainSettings, false).SetParent(AllAliveMeeting)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // 生存人数ごとの緊急会議
            AdditionalEmergencyCooldown = BooleanOptionItem.Create(101400, "AdditionalEmergencyCooldown", false, TabGroup.MainSettings, false)
                .SetColor(Color.cyan)
                .SetGameMode(CustomGameMode.Standard);
            AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(101401, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(CustomGameMode.Standard);
            AdditionalEmergencyCooldownTime = FloatOptionItem.Create(101402, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // 各タスク無効化
            DisableTasks = BooleanOptionItem.Create(100300, "DisableTasks", false, TabGroup.MainSettings, false)
                .SetColor(Color.green)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            DisableSwipeCard = BooleanOptionItem.Create(100301, "DisableSwipeCardTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = BooleanOptionItem.Create(100302, "DisableSubmitScanTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = BooleanOptionItem.Create(100303, "DisableUnlockSafeTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = BooleanOptionItem.Create(100304, "DisableUploadDataTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = BooleanOptionItem.Create(100305, "DisableStartReactorTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = BooleanOptionItem.Create(100306, "DisableResetBreakerTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);

            //タスク勝利無効化
            DisableTaskWin = BooleanOptionItem.Create(900_001, "DisableTaskWin", false, TabGroup.MainSettings, false)
                .SetColor(Color.green)
                .SetGameMode(CustomGameMode.Standard);

            //幽霊のタスクを無効に
            GhostIgnoreTasks = BooleanOptionItem.Create(900_012, "GhostIgnoreTasks", false, TabGroup.MainSettings, false)
                .SetColor(Palette.LightBlue)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            GhostCanSeeOtherRoles = BooleanOptionItem.Create(900_010, "GhostCanSeeOtherRoles", false, TabGroup.MainSettings, false)
                .SetColor(Palette.LightBlue)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherVotes = BooleanOptionItem.Create(900_011, "GhostCanSeeOtherVotes", false, TabGroup.MainSettings, false)
                .SetColor(Palette.LightBlue)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeDeathReason = BooleanOptionItem.Create(900_014, "GhostCanSeeDeathReason", false, TabGroup.MainSettings, false)
                .SetColor(Palette.LightBlue)
                .SetGameMode(CustomGameMode.Standard);

            // ランダムマップ
            RandomMapsMode = BooleanOptionItem.Create(100400, "RandomMapsMode", false, TabGroup.MainSettings, false)
                .SetColor(Palette.Orange)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = BooleanOptionItem.Create(100401, "AddedTheSkeld", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedMiraHQ = BooleanOptionItem.Create(100402, "AddedMIRAHQ", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedPolus = BooleanOptionItem.Create(100403, "AddedPolus", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedTheAirShip = BooleanOptionItem.Create(100404, "AddedTheAirShip", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            // MapDleks = CustomOption.Create(100405, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            // 転落死
            LadderDeath = BooleanOptionItem.Create(101100, "LadderDeath", false, TabGroup.MainSettings, false)
                .SetColor(Palette.Orange)
                .SetGameMode(CustomGameMode.All);
            LadderDeathChance = StringOptionItem.Create(101110, "LadderDeathChance", rates[1..], 0, TabGroup.MainSettings, false).SetParent(LadderDeath)
                .SetGameMode(CustomGameMode.All);

            //初手キルクール調整
            FixFirstKillCooldown = BooleanOptionItem.Create(900_000, "FixFirstKillCooldown", false, TabGroup.MainSettings, false)
                .SetColor(Palette.Orange)
                .SetGameMode(CustomGameMode.All);

            //エレキ構造変化
            AirShipVariableElectrical = BooleanOptionItem.Create(101600, "AirShipVariableElectrical", false, TabGroup.MainSettings, false)
                .SetColor(Palette.Orange)
                .SetGameMode(CustomGameMode.All);

            SyncColorMode = StringOptionItem.Create(105100, "SyncColorMode", SelectSyncColorMode, 0, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColor(Color.yellow)
                .SetGameMode(CustomGameMode.Standard);

            // 通常モードでかくれんぼ用
            StandardHAS = BooleanOptionItem.Create(100700, "StandardHAS", false, TabGroup.MainSettings, false)
                .SetColor(Color.yellow)
                .SetGameMode(CustomGameMode.Standard);
            StandardHASWaitingTime = FloatOptionItem.Create(100701, "StandardHASWaitingTime", new(0f, 180f, 2.5f), 10f, TabGroup.MainSettings, false).SetParent(StandardHAS)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // その他
            NoGameEnd = BooleanOptionItem.Create(900_002, "NoGameEnd", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            AutoDisplayLastResult = BooleanOptionItem.Create(1_000_000, "AutoDisplayLastResult", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            AutoDisplayKillLog = BooleanOptionItem.Create(1_000_006, "AutoDisplayKillLog", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            SuffixMode = StringOptionItem.Create(1_000_001, "SuffixMode", suffixModes, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);
            ColorNameMode = BooleanOptionItem.Create(1_000_003, "ColorNameMode", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            ChangeNameToRoleInfo = BooleanOptionItem.Create(1_000_004, "ChangeNameToRoleInfo", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            AddonShowDontOmit = BooleanOptionItem.Create(105400, "AddonShowDontOmit", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard);
            ChangeIntro = BooleanOptionItem.Create(105600, "ChangeIntro", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard);
            RoleAssigningAlgorithm = StringOptionItem.Create(1_000_005, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .RegisterUpdateValueEvent(
                    (object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue)
                );
            VoiceReader.SetupCustomOption();

            ApplyDenyNameList = BooleanOptionItem.Create(1_000_100, "ApplyDenyNameList", true, TabGroup.MainSettings, true)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(1_000_101, "KickPlayerFriendCodeNotExist", false, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);
            ApplyBanList = BooleanOptionItem.Create(1_000_110, "ApplyBanList", true, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);

            DebugModeManager.SetupCustomOption();

            /**************************************** Impostor ****************************************/
            SetupRoleOptions(4900, TabGroup.ImpostorRoles, CustomRoles.NormalImpostor);
            SetupRoleOptions(9000, TabGroup.ImpostorRoles, CustomRoles.EvilWatcher);
            BountyHunter.SetupCustomOption();
            SerialKiller.SetupCustomOption();
            SetupRoleOptions(1200, TabGroup.ImpostorRoles, CustomRoles.ShapeMaster);
            ShapeMasterShapeshiftDuration = FloatOptionItem.Create(1210, "ShapeMasterShapeshiftDuration", new(1, 1000, 1), 10, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.ShapeMaster]);
            Vampire.SetupCustomOption();
            SetupRoleOptions(1400, TabGroup.ImpostorRoles, CustomRoles.Warlock);
            Witch.SetupCustomOption();
            SetupRoleOptions(1600, TabGroup.ImpostorRoles, CustomRoles.Mafia);
            MafiaCanKill = IntegerOptionItem.Create(1610, "MafiaCanKill", new(1, 2, 1), 1, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Mafia])
                .SetValueFormat(OptionFormat.Players);
            MafiaKillCooldown = FloatOptionItem.Create(1611, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Mafia])
                .SetValueFormat(OptionFormat.Seconds);

            FireWorks.SetupCustomOption();
            Sniper.SetupCustomOption();
            SetupRoleOptions(2000, TabGroup.ImpostorRoles, CustomRoles.Puppeteer);
            Mare.SetupCustomOption();
            TimeThief.SetupCustomOption();
            EvilTracker.SetupCustomOption();

            //TOH_Y
            SetupRoleOptions(3000, TabGroup.ImpostorRoles, CustomRoles.Evilneko);//TOH_Y01_11
            TakeCompanionMad = BooleanOptionItem.Create(3010, "TakeCompanionMad", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Evilneko]);
            AntiAdminer.SetupCustomOption();//3100
            SetupRoleOptions(3200, TabGroup.ImpostorRoles, CustomRoles.CursedWolf);//TOH_Y
            GuardSpellTimes = IntegerOptionItem.Create(3210, "GuardSpellTimes", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.CursedWolf])
                .SetValueFormat(OptionFormat.Times);
            Greedier.SetupCustomOption();//3300
            Ambitioner.SetupCustomOption();//3400
            SetupRoleOptions(3500, TabGroup.ImpostorRoles, CustomRoles.Scavenger);//TOH_Y
            EvilDiviner.SetupCustomOption();//3600
            Telepathisters.SetupCustomOption();//3700
            ShapeKiller.SetupCustomOption();//3800

            DefaultShapeshiftCooldown = FloatOptionItem.Create(5011, "DefaultShapeshiftCooldown", new(5f, 999f, 5f), 15f, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard)
                .SetValueFormat(OptionFormat.Seconds);
            OperateVisibilityImpostor = BooleanOptionItem.Create(5100, "OperateVisibilityImpostor", false, TabGroup.ImpostorRoles, false)
                .SetGameMode(CustomGameMode.Standard);

            /********ON********/
            ONWerewolf.SetupCustomOption();//65000
            ONBigWerewolf.SetupCustomOption();//65100

            /**************************************** Madmate ****************************************/
            SetupRoleOptions(10000, TabGroup.MadmateRoles, CustomRoles.Madmate);
            SetUpAddOnOptions(10010, CustomRoles.Madmate, TabGroup.MadmateRoles);

            SetupRoleOptions(10100, TabGroup.MadmateRoles, CustomRoles.MadGuardian);
            MadGuardianCanSeeWhoTriedToKill = BooleanOptionItem.Create(10110, "MadGuardianCanSeeWhoTriedToKill", false, TabGroup.MadmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.MadGuardian]);
            //ID10120~10123を使用
            MadGuardianTasks = OverrideTasksData.Create(10120, TabGroup.MadmateRoles, CustomRoles.MadGuardian);
            SetUpAddOnOptions(10130, CustomRoles.MadGuardian, TabGroup.MadmateRoles);

            SetupRoleOptions(10200, TabGroup.MadmateRoles, CustomRoles.MadSnitch);
            MadSnitchCanVent = BooleanOptionItem.Create(10210, "CanVent", false, TabGroup.MadmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.MadSnitch]);
            MadSnitchCanAlsoBeExposedToImpostor = BooleanOptionItem.Create(10211, "MadSnitchCanAlsoBeExposedToImpostor", false, TabGroup.MadmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.MadSnitch]);
            //ID10220~10223を使用
            MadSnitchTasks = OverrideTasksData.Create(10220, TabGroup.MadmateRoles, CustomRoles.MadSnitch);
            SetUpAddOnOptions(10230, CustomRoles.MadSnitch, TabGroup.MadmateRoles);

            //TOH_Y
            SetupRoleOptions(10300, TabGroup.MadmateRoles, CustomRoles.MadDictator);//TOH_Y01_9
            MadDictatorCanVent = BooleanOptionItem.Create(10310, "CanVent", true, TabGroup.MadmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.MadDictator]);
            SetUpAddOnOptions(10320, CustomRoles.MadDictator, TabGroup.MadmateRoles);
            SetupRoleOptions(10400, TabGroup.MadmateRoles, CustomRoles.MadNatureCalls);//TOH_Y01_10
            SetUpAddOnOptions(10410, CustomRoles.MadNatureCalls, TabGroup.MadmateRoles);
            SetupRoleOptions(10500, TabGroup.MadmateRoles, CustomRoles.MadBrackOuter);//TOH_Y
            SetUpAddOnOptions(10510, CustomRoles.MadBrackOuter, TabGroup.MadmateRoles);
            MadSheriff.SetupCustomOption();

            CanMakeMadmateCount = IntegerOptionItem.Create(5012, "CanMakeMadmateCount", new(0, 15, 1), 0, TabGroup.MadmateRoles, false)
                .SetColor(Utils.GetRoleColor(CustomRoles.Impostor))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard)
                .SetValueFormat(OptionFormat.Players);
            MadmateCanFixLightsOut = BooleanOptionItem.Create(15010, "MadmateCanFixLightsOut", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateCanFixComms = BooleanOptionItem.Create(15011, "MadmateCanFixComms", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateHasImpostorVision = BooleanOptionItem.Create(15012, "MadmateHasImpostorVision", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateCanSeeKillFlash = BooleanOptionItem.Create(15015, "MadmateCanSeeKillFlash", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateCanSeeOtherVotes = BooleanOptionItem.Create(15016, "MadmateCanSeeOtherVotes", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateCanSeeDeathReason = BooleanOptionItem.Create(15017, "MadmateCanSeeDeathReason", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
            MadmateRevengeCrewmate = BooleanOptionItem.Create(15018, "MadmateExileCrewmate", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);

            // Madmate Common Options
            MadmateVentCooldown = FloatOptionItem.Create(15213, "MadmateVentCooldown", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false)
                .SetGameMode(CustomGameMode.Standard).SetHeader(true)
                .SetGameMode(CustomGameMode.Standard).SetValueFormat(OptionFormat.Seconds);
            MadmateVentMaxTime = FloatOptionItem.Create(15214, "MadmateVentMaxTime", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false)
                .SetGameMode(CustomGameMode.Standard).SetValueFormat(OptionFormat.Seconds);

            /********ON********/
            SetupRoleOptions(66000, TabGroup.MadmateRoles, CustomRoles.ONMadman, CustomGameMode.OneNight);
            SetupRoleOptions(66100, TabGroup.MadmateRoles, CustomRoles.ONMadFanatic, CustomGameMode.OneNight);

            /**************************************** Crewmate ****************************************/
            //if(Main.IsAprilFool && CultureInfo.CurrentCulture.Name == "ja-JP")
            //{
            //    var spawnOption = BooleanOptionItem.Create(19900, "PotentialistName", false, TabGroup.CrewmateRoles, false).SetColor(Utils.GetRoleColor(CustomRoles.Rainbow))
            //        .SetHeader(true)
            //        .SetGameMode(CustomGameMode.Standard) as BooleanOptionItem;
            //    var countOption = IntegerOptionItem.Create(19900 + 1, "Maximum", (1, 15, 1), 1, TabGroup.CrewmateRoles, false).SetParent(spawnOption)
            //        .SetValueFormat(OptionFormat.Players)
            //        .SetGameMode(CustomGameMode.Standard);

            //    CustomRoleSpawnOnOff.Add(CustomRoles.Potentialist, spawnOption);
            //    CustomRoleCounts.Add(CustomRoles.Potentialist, countOption);

            //    PotentialistTaskTrigger = IntegerOptionItem.Create(19910, "SpeedBoosterTaskTrigger", new(1, 30, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Potentialist])
            //        .SetValueFormat(OptionFormat.Pieces);
            //}

            SetupRoleOptions(49900, TabGroup.CrewmateRoles, CustomRoles.NiceWatcher);
            //Sheriff
            Sheriff.SetupCustomOption();
            SillySheriff.SetupCustomOption();//35800
            GrudgeSheriff.SetupCustomOption();//36100
            Hunter.SetupCustomOption();//35100d
            SetupRoleOptions(20000, TabGroup.CrewmateRoles, CustomRoles.Bait);
            BaitWaitTime = FloatOptionItem.Create(20010, "BaitWaitTime", new(0f, 15f, 1f), 0f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Bait])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(20100, TabGroup.CrewmateRoles, CustomRoles.Lighter);
            LighterTaskTrigger = IntegerOptionItem.Create(20112, "SpeedBoosterTaskTrigger", new(1, 20, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Pieces);
            LighterTaskCompletedVision = FloatOptionItem.Create(20110, "LighterTaskCompletedVision", new(0f, 5f, 0.25f), 2f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Multiplier);
            LighterTaskCompletedDisableLightOut = BooleanOptionItem.Create(20111, "LighterTaskCompletedDisableLightOut", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Lighter]);
            SetupRoleOptions(20200, TabGroup.CrewmateRoles, CustomRoles.Mayor);
            MayorAdditionalVote = IntegerOptionItem.Create(20210, "MayorAdditionalVote", new(1, 99, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Mayor])
                .SetValueFormat(OptionFormat.Votes);
            MayorHasPortableButton = BooleanOptionItem.Create(20211, "MayorHasPortableButton", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Mayor]);
            MayorNumOfUseButton = IntegerOptionItem.Create(20212, "MayorNumOfUseButton", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(MayorHasPortableButton)
                .SetValueFormat(OptionFormat.Times);
            SabotageMaster.SetupCustomOption();
            Snitch.SetupCustomOption();
            SetupRoleOptions(20600, TabGroup.CrewmateRoles, CustomRoles.SpeedBooster);
            SpeedBoosterUpSpeed = FloatOptionItem.Create(20610, "SpeedBoosterUpSpeed", new(0.1f, 0.5f, 0.1f), 0.3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.SpeedBooster])
                .SetValueFormat(OptionFormat.Multiplier);
            SpeedBoosterTaskTrigger = IntegerOptionItem.Create(20611, "SpeedBoosterTaskTrigger", new(1, 20, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.SpeedBooster])
                .SetValueFormat(OptionFormat.Pieces);
            SetupRoleOptions(20700, TabGroup.CrewmateRoles, CustomRoles.Doctor);
            DoctorHasVital = BooleanOptionItem.Create(20711, "DoctorHasVital", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Doctor]);
            DoctorTaskCompletedBatteryCharge = FloatOptionItem.Create(20710, "DoctorTaskCompletedBatteryCharge", new(0f, 10f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(DoctorHasVital)
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(20800, TabGroup.CrewmateRoles, CustomRoles.Trapper);
            TrapperBlockMoveTime = FloatOptionItem.Create(20810, "TrapperBlockMoveTime", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Trapper])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(20900, TabGroup.CrewmateRoles, CustomRoles.Dictator);
            SetupRoleOptions(21000, TabGroup.CrewmateRoles, CustomRoles.Seer);
            TimeManager.SetupCustomOption();

            //TOH_Y
            Bakery.SetupCustomOption();
            SetupRoleOptions(35200, TabGroup.CrewmateRoles, CustomRoles.TaskManager);
            TaskManagerSeeNowtask = BooleanOptionItem.Create(35210, "TaskmanagerSeeNowtask",false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.TaskManager]);
            SetupRoleOptions(35300, TabGroup.CrewmateRoles, CustomRoles.Nekomata);
            SetupRoleOptions(35400, TabGroup.CrewmateRoles, CustomRoles.Chairman);
            ChairmanNumOfUseButton = IntegerOptionItem.Create(35410, "NumOfUseButton", new(1, 20, 1), 2, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Chairman])
                .SetValueFormat(OptionFormat.Times);
            ChairmanIgnoreSkip = BooleanOptionItem.Create(35411, "ChairmanIgnoreSkip", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Chairman]);
            SetupRoleOptions(35500, TabGroup.CrewmateRoles, CustomRoles.Express);
            ExpressSpeed = FloatOptionItem.Create(35510, "ExpressSpeed", new(1.5f, 3f, 0.25f), 2.0f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Express])
                .SetValueFormat(OptionFormat.Multiplier);
            SetupRoleOptions(35600, TabGroup.CrewmateRoles, CustomRoles.SeeingOff);
            SetupRoleOptions(35700, TabGroup.CrewmateRoles, CustomRoles.Rainbow);//TOH_Y01_8
            RainbowDontSeeTaskTurn = BooleanOptionItem.Create(35710, "RainbowDontSeeTaskTurn", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Rainbow]);
            SetupSingleRoleOptions(35900, TabGroup.CrewmateRoles, CustomRoles.Sympathizer, 2);
            SympaCheckedTasks = IntegerOptionItem.Create(35910, "SympaCheckedTasks", new(1, 20, 1), 5, TabGroup.CrewmateRoles,false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Sympathizer])
                .SetValueFormat(OptionFormat.Pieces);
            SetupRoleOptions(36000, TabGroup.CrewmateRoles, CustomRoles.Blinder);
            BlinderVision = FloatOptionItem.Create(36010, "BlinderVision", new(0f, 5f, 0.05f), 0.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Blinder])
                .SetValueFormat(OptionFormat.Multiplier);
            Medic.SetupCustomOption();//37000
            CandleLighter.SetupCustomOption();//36200
            FortuneTeller.SetupCustomOption();//36400
            Psychic.SetupCustomOption();//36300

            /********ON********/
            SetupRoleOptions(67000, TabGroup.CrewmateRoles, CustomRoles.ONVillager, CustomGameMode.OneNight);
            ONDiviner.SetupCustomOption();//67100
            ONPhantomThief.SetupCustomOption();//67200
            SetupRoleOptions(67300, TabGroup.CrewmateRoles, CustomRoles.ONMayor, CustomGameMode.OneNight);
            SetupSingleRoleOptions(67400, TabGroup.CrewmateRoles, CustomRoles.ONHunter, 1,CustomGameMode.OneNight);
            SetupRoleOptions(67500, TabGroup.CrewmateRoles, CustomRoles.ONBakery, CustomGameMode.OneNight);
            SetupRoleOptions(67600, TabGroup.CrewmateRoles, CustomRoles.ONTrapper, CustomGameMode.OneNight);
            ONTrapperBlockMoveTime = FloatOptionItem.Create(67610, "TrapperBlockMoveTime", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.ONTrapper])
                .SetValueFormat(OptionFormat.Seconds);

            /**************************************** Neutral ****************************************/
            TakeCompanionNeutral = BooleanOptionItem.Create(75000, "TakeCompanionNeutral", true, TabGroup.NeutralRoles, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetHeader(true);

            SetupRoleOptions(50500, TabGroup.NeutralRoles, CustomRoles.Arsonist);
            ArsonistDouseTime = FloatOptionItem.Create(50510, "ArsonistDouseTime", new(1f, 10f, 1f), 3f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Arsonist])
                .SetValueFormat(OptionFormat.Seconds);
            ArsonistCooldown = FloatOptionItem.Create(50511, "Cooldown", new(5f, 100f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Arsonist])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(50000, TabGroup.NeutralRoles, CustomRoles.Jester);
            SetupRoleOptions(50100, TabGroup.NeutralRoles, CustomRoles.Opportunist);
            OpportunistCanKill = BooleanOptionItem.Create(50110, "CanKill", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Opportunist]);
            KOpportunistKillCooldown = FloatOptionItem.Create(50111, "KillCooldown", new(0f, 180f, 2.5f), 30f,TabGroup.NeutralRoles, false).SetParent(OpportunistCanKill)
                .SetValueFormat(OptionFormat.Seconds);
            OppoKillerShotLimitOpt = IntegerOptionItem.Create(50113, "SheriffShotLimit", new(1, 15, 1), 5, TabGroup.NeutralRoles, false).SetParent(OpportunistCanKill)
                .SetValueFormat(OptionFormat.Times);
            KOpportunistHasImpostorVision = BooleanOptionItem.Create(50112, "ImpostorVision", false, TabGroup.NeutralRoles, false).SetParent(OpportunistCanKill);
            SetupRoleOptions(50200, TabGroup.NeutralRoles, CustomRoles.Terrorist);
            CanTerroristSuicideWin = BooleanOptionItem.Create(50210, "CanTerroristSuicideWin", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Terrorist])
                .SetGameMode(CustomGameMode.Standard);
            //50220~50223を使用
            TerroristTasks = OverrideTasksData.Create(50220, TabGroup.NeutralRoles, CustomRoles.Terrorist);
            SchrodingerCat.SetupCustomOption();
            Egoist.SetupCustomOption();
            Executioner.SetupCustomOption();
            Jackal.SetupCustomOption();
            SetupRoleOptions(60600, TabGroup.NeutralRoles, CustomRoles.JClient);
            JClientCanVent = BooleanOptionItem.Create(60611, "CanVent", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.JClient]);
            JClientVentCooldown = FloatOptionItem.Create(60612, "VentCooldown", new(0f, 180f, 5f), 0f, TabGroup.NeutralRoles, false).SetParent(JClientCanVent)
                .SetValueFormat(OptionFormat.Seconds);
            JClientVentMaxTime = FloatOptionItem.Create(60613, "VentMaxTime", new(0f, 180f, 5f), 0f, TabGroup.NeutralRoles, false).SetParent(JClientCanVent)
                .SetValueFormat(OptionFormat.Seconds);
            JClientCanAlsoBeExposedToJackal = BooleanOptionItem.Create(60614, "JClientCanAlsoBeExposedToJackal", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.JClient]);
            JClientAfterJackalDead = StringOptionItem.Create(60615, "JClientAfterJackalDead", Enum.GetNames(typeof(AfterJackalDeadMode)), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.JClient]);
            //ID60620~60623を使用
            JClientTasks = OverrideTasksData.Create(60620, TabGroup.NeutralRoles, CustomRoles.JClient);
            SetUpAddOnOptions(60630, CustomRoles.JClient, TabGroup.NeutralRoles);

            PlatonicLover.SetupCustomOption();//60400

            //TOH_Y
            SetupRoleOptions(60000, TabGroup.NeutralRoles, CustomRoles.AntiComplete);//TOH_Y01_13
            AntiCompGuardCount = IntegerOptionItem.Create(60010, "AntiCompGuardCount", new(0, 20, 1), 2, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.AntiComplete])
                .SetValueFormat(OptionFormat.Times);
            AntiCompKnowOption = BooleanOptionItem.Create(60011, "AntiCompKnowOption", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.AntiComplete]);
            AntiCompKnowNotask = BooleanOptionItem.Create(60012, "AntiCompKnowNotask", true, TabGroup.NeutralRoles, false).SetParent(AntiCompKnowOption);
            AntiCompKnowCompTask = BooleanOptionItem.Create(60013, "AntiCompKnowCompTask", false, TabGroup.NeutralRoles, false).SetParent(AntiCompKnowOption);
            AntiCompAddGuardCount = IntegerOptionItem.Create(60014, "AntiCompAddGuardCount", new(0, 10, 1), 0, TabGroup.NeutralRoles, false).SetParent(AntiCompKnowOption)
                .SetValueFormat(OptionFormat.Times);
            AntiCompleteTasks = OverrideTasksData.Create(60020, TabGroup.NeutralRoles, CustomRoles.AntiComplete, AntiCompKnowOption);
            SetupRoleOptions(60100, TabGroup.NeutralRoles, CustomRoles.Workaholic);//TOH_Y01_14
            WorkaholicSeen = BooleanOptionItem.Create(60110, "WorkaholicSeen", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Workaholic]);
            WorkaholicTaskSeen = BooleanOptionItem.Create(60111, "WorkaholicTaskSeen", true, TabGroup.NeutralRoles, false).SetParent(WorkaholicSeen);
            WorkaholicCannotWinAtDeath = BooleanOptionItem.Create(60113, "WorkaholicCannotWinAtDeath", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Workaholic]);
            WorkaholicVentCooldown = FloatOptionItem.Create(60112, "VentCooldown", new(0f, 180f, 2.5f), 0f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Workaholic])
                .SetValueFormat(OptionFormat.Seconds);
            //60120~60123を使用
            WorkaholicTasks = OverrideTasksData.Create(60120, TabGroup.NeutralRoles, CustomRoles.Workaholic);
            DarkHide.SetupCustomOption();// 60200
            SetupRoleOptions(60300, TabGroup.NeutralRoles, CustomRoles.LoveCutter);
            VictoryCutCount = IntegerOptionItem.Create(60310, "VictoryCutCount", new(1, 20, 1), 2, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.LoveCutter])
                .SetValueFormat(OptionFormat.Times);
            LoveCutterKnow = BooleanOptionItem.Create(60311, "LoveCutterKnow", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.LoveCutter]);
            //60320~60323を使用
            LoveCutterTasks = OverrideTasksData.Create(60320, TabGroup.NeutralRoles, CustomRoles.LoveCutter, LoveCutterKnow);
            Lawyer.SetupCustomOption();// 60500
            Totocalcio.SetupCustomOption();// 60700

            /********ON********/
            SetupRoleOptions(66500, TabGroup.NeutralRoles, CustomRoles.ONHangedMan, CustomGameMode.OneNight);
            HangedManHasntTask = BooleanOptionItem.Create(66510, "HangedManHasntTask", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.ONHangedMan]);

            /**************************************** Add-Ons ****************************************/
            LastImpostor.SetupCustomOption();   //79000
            SetupLoversRoleOptionsToggle(50300);
            LoversAddWin = BooleanOptionItem.Create(50310, "LoversAddWin", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Lovers]);
            Workhorse.SetupCustomOption();//78900
            CompreteCrew.SetupCustomOption();//77700

            SetupRoleOptions(79100, TabGroup.Addons, CustomRoles.AddWatch);
            SetupRoleOptions(79200, TabGroup.Addons, CustomRoles.AddLight);
            AddLightAddCrewmateVision = FloatOptionItem.Create(79210, "AddLightAddCrewmateVision", new(0f, 5f, 0.1f), 0.3f, TabGroup.Addons, false)
            .SetValueFormat(OptionFormat.Multiplier).SetGameMode(CustomGameMode.Standard);
            AddLightAddImpostorVision = FloatOptionItem.Create(79211, "AddLightAddImpostorVision", new(0f, 5f, 0.1f), 0.5f, TabGroup.Addons, false)
                .SetValueFormat(OptionFormat.Multiplier).SetGameMode(CustomGameMode.Standard);
            AddLighterDisableLightOut = BooleanOptionItem.Create(79212, "AddLighterDisableLightOut", true, TabGroup.Addons, false)
                .SetGameMode(CustomGameMode.Standard);
            SetupRoleOptions(79400, TabGroup.Addons, CustomRoles.AddSeer);
            SetupRoleOptions(79500, TabGroup.Addons, CustomRoles.Autopsy);
            SetupRoleOptions(79600, TabGroup.Addons, CustomRoles.VIP);
            SetupRoleOptions(79800, TabGroup.Addons, CustomRoles.Revenger);
            SetupRoleOptions(79900, TabGroup.Addons, CustomRoles.Management);
            ManagementSeeNowtask = BooleanOptionItem.Create(79910, "ManagementSeeNowtask", true, TabGroup.Addons, false)
                .SetGameMode(CustomGameMode.Standard);
            SetupRoleOptions(77600, TabGroup.Addons, CustomRoles.Sending);
            SetupRoleOptions(77100, TabGroup.Addons, CustomRoles.TieBreaker);
            SetupRoleOptions(77300, TabGroup.Addons, CustomRoles.PlusVote);
            SetupRoleOptions(77400, TabGroup.Addons, CustomRoles.Guarding);
            SetupRoleOptions(77500, TabGroup.Addons, CustomRoles.AddBait);
            SetupRoleOptions(77800, TabGroup.Addons, CustomRoles.Refusing);

            SetupRoleOptions(79300, TabGroup.Addons, CustomRoles.Sunglasses);
            SunglassesSubCrewmateVision = FloatOptionItem.Create(79310, "SunglassesSubCrewmateVision", new(0f, 5f, 0.05f), 0.2f, TabGroup.Addons, false)
                .SetValueFormat(OptionFormat.Multiplier).SetGameMode(CustomGameMode.Standard);
            SunglassesSubImpostorVision = FloatOptionItem.Create(79311, "SunglassesSubImpostorVision", new(0f, 5f, 0.1f), 0.5f, TabGroup.Addons, false)
                .SetValueFormat(OptionFormat.Multiplier).SetGameMode(CustomGameMode.Standard);
            SetupRoleOptions(79700, TabGroup.Addons, CustomRoles.Clumsy);
            SetupRoleOptions(77000, TabGroup.Addons, CustomRoles.InfoPoor);
            SetupRoleOptions(77200, TabGroup.Addons, CustomRoles.NonReport);

            #endregion
            IsLoaded = true;
        }

        public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            int MaxCount = 15;
            if (role.IsImpostor()) MaxCount = 3;

             var spawnOption = BooleanOptionItem.Create(id, role.ToString(), false, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as BooleanOptionItem;
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, MaxCount, 1), 1, tab, false).SetParent(spawnOption)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(customGameMode);

            CustomRoleSpawnOnOff.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        private static void SetupLoversRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var role = CustomRoles.Lovers;
            var spawnOption = BooleanOptionItem.Create(id, role.ToString(), false, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as BooleanOptionItem;

            var countOption = IntegerOptionItem.Create(id + 1, "NumberOfLovers", new(2, 2, 1), 2, TabGroup.Addons, false).SetParent(spawnOption)
                .SetHidden(true)
                .SetGameMode(customGameMode);

            CustomRoleSpawnOnOff.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public static void SetupSingleRoleOptions(int id, TabGroup tab, CustomRoles role, int count, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = BooleanOptionItem.Create(id, role.ToString(),false, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as BooleanOptionItem;
            // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(count, count, count), count, tab, false).SetParent(spawnOption)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(customGameMode);

            CustomRoleSpawnOnOff.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }

        //TOH_Y CATCHCAT
        public static void SetupLeaderRoleOptions(int id, CustomRoles role)
        {
            var spawnOption = BooleanOptionItem.Create(id, role.ToString() + "Fixed", false, TabGroup.MainSettings, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                //.SetHidden(true)
                .SetGameMode(CustomGameMode.CatchCat) as BooleanOptionItem;
            // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, TabGroup.MainSettings, false).SetParent(spawnOption)
                //.SetHidden(true)
                .SetGameMode(CustomGameMode.CatchCat);

            CustomRoleSpawnOnOff.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public static void SetupAddLeaderRoleOptions(int id, CustomRoles role)
        {
            var spawnOption = BooleanOptionItem.Create(id, role.ToString(), false, TabGroup.MainSettings, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                //.SetHidden(true)
                .SetGameMode(CustomGameMode.CatchCat) as BooleanOptionItem;
            // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, TabGroup.MainSettings, false).SetParent(spawnOption)
                .SetHidden(true)
                .SetGameMode(CustomGameMode.CatchCat);

            CustomRoleSpawnOnOff.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }

        //AddOn
        public static void SetUpAddOnOptions(int Id, CustomRoles PlayerRole, TabGroup tab)
        {
            AddOnBuffAssign[PlayerRole] = BooleanOptionItem.Create(Id, "AddOnBuffAssign", false, tab, false).SetParent(CustomRoleSpawnOnOff[PlayerRole]);
            Id += 10;
            foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsBuffAddOn()))
            {
                if (Addon == CustomRoles.Loyalty && PlayerRole is
                    CustomRoles.MadSnitch or CustomRoles.JClient or CustomRoles.LastImpostor or CustomRoles.CompreteCrew) continue;

                SetUpAddOnRoleOption(PlayerRole, tab, Addon, Id, false, AddOnBuffAssign[PlayerRole]);
                Id++;
            }
            AddOnDebuffAssign[PlayerRole] = BooleanOptionItem.Create(Id, "AddOnDebuffAssign", false, tab, false).SetParent(CustomRoleSpawnOnOff[PlayerRole]);
            Id += 10;
            foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsDebuffAddOn()))
            {
                SetUpAddOnRoleOption(PlayerRole, tab, Addon, Id, false, AddOnDebuffAssign[PlayerRole]);
                Id++;
            }
        }
        public static void SetUpAddOnRoleOption(CustomRoles PlayerRole, TabGroup tab, CustomRoles role, int Id, bool defaultValue = false, OptionItem parent = null)
        {
            if (parent == null) parent = CustomRoleSpawnOnOff[PlayerRole];
            var roleName = Utils.GetRoleName(role) + Utils.GetAddRoleInfo(role);
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
            AddOnRoleOptions[(PlayerRole,role)] = BooleanOptionItem.Create(Id, "AddOnAssign%role%", defaultValue, tab, false).SetParent(parent);
            AddOnRoleOptions[(PlayerRole,role)].ReplacementDictionary = replacementDic;
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

            public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role)
            {
                this.IdStart = idStart;
                this.Role = role;
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.GetRoleName(role) } };
                doOverride = BooleanOptionItem.Create(idStart++, "doOverride", false, tab, false).SetParent(CustomRoleSpawnOnOff[role])
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
            public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role, OptionItem option)
            {
                this.IdStart = idStart;
                this.Role = role;
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
            public static OverrideTasksData Create(int idStart, TabGroup tab, CustomRoles role, OptionItem option)
            {
                return new OverrideTasksData(idStart, tab, role, option);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

using TownOfHostY.Modules;
using TownOfHostY.Roles;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Unit;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.AddOns.Impostor;
using TownOfHostY.Roles.AddOns.Crewmate;

namespace TownOfHostY;

[Flags]
public enum CustomGameMode
{
    Standard,
    HideAndSeek,
    CatchCat,
    //OneNight,
    HideMenu,
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
        "Standard", "HideAndSeek", /*"CatchCat", "OneNight",*/
    };

    // MapActive
    public static bool IsActiveSkeld => AddedTheSkeld.GetValue() == 1 || Main.NormalOptions.MapId == 0;
    public static bool IsActiveMiraHQ => AddedMiraHQ.GetValue() == 1 || Main.NormalOptions.MapId == 1;
    public static bool IsActivePolus => AddedPolus.GetValue() == 1 || Main.NormalOptions.MapId == 2;
    public static bool IsActiveAirship => AddedTheAirship.GetValue() == 1 || Main.NormalOptions.MapId == 4;
    public static bool IsActiveFungle => AddedTheFungle.GetValue() == 1 || Main.NormalOptions.MapId == 5;

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
    public static OptionItem MadmateCanFixLightsOut;
    public static OptionItem MadmateCanFixComms;
    public static OptionItem MadmateHasImpostorVision;
    public static OptionItem MadmateCanSeeKillFlash;
    public static OptionItem MadmateCanSeeOtherVotes;
    public static OptionItem MadmateCanSeeDeathReason;
    public static OptionItem MadmateRevengeCrewmate;
    public static OptionItem MadmateVentCooldown;
    public static OptionItem MadmateVentMaxTime;

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
    public static OptionItem DisableRewindTapes;
    public static OptionItem DisableVentCleaning;
    public static OptionItem DisableBuildSandcastle;
    public static OptionItem DisableTestFrisbee;
    public static OptionItem DisableWaterPlants;
    public static OptionItem DisableCatchFish;
    public static OptionItem DisableHelpCritter;
    public static OptionItem DisableTuneRadio;
    public static OptionItem DisableAssembleArtifact;

    // マップ設定
    public static OptionItem MapOption_Skeld;
    public static OptionItem MapOption_MiraHQ;
    public static OptionItem MapOption_Polus;
    public static OptionItem MapOption_Airship;
    public static OptionItem MapOption_Fungle;

    //デバイスブロック
    public static OptionItem DisableDevices_Skeld;
    public static OptionItem DisableAdmin_Skeld;
    public static OptionItem DisableCamera_Skeld;
    public static OptionItem DisableDevices_MiraHQ;
    public static OptionItem DisableAdmin_MiraHQ;
    public static OptionItem DisableDoorLog_MiraHQ;
    public static OptionItem DisableDevices_Polus;
    public static OptionItem DisableAdmin_Polus;
    public static OptionItem DisableCamera_Polus;
    public static OptionItem DisableVital_Polus;
    public static OptionItem DisableDevices_Airship;
    public static OptionItem DisableCockpitAdmin_Airship;
    public static OptionItem DisableRecordsAdmin_Airship;
    public static OptionItem DisableCamera_Airship;
    public static OptionItem DisableVital_Airship;
    public static OptionItem DisableDevices_Fungle;
    public static OptionItem DisableVital_Fungle;
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
    public static OptionItem AddedTheAirship;
    public static OptionItem AddedTheFungle;

    // ランダムスポーン
    public static OptionItem RandomSpawn_Skeld;
    public static OptionItem RandomSpawn_MiraHQ;
    public static OptionItem RandomSpawn_Polus;
    public static OptionItem RandomSpawn_Airship;
    public static OptionItem RandomSpawn_Fungle;
    public static OptionItem AdditionalSpawn_Skeld;
    public static OptionItem AdditionalSpawn_MiraHQ;
    public static OptionItem AdditionalSpawn_Polus;
    public static OptionItem AdditionalSpawn_Airship;
    public static OptionItem AdditionalSpawn_AirshipTAKADA;
    public static OptionItem AdditionalSpawn_Fungle;
    public static OptionItem DisableNearButton_Skeld;
    public static OptionItem DisableNearButton_MiraHQ;
    public static OptionItem DisableNearButton_Polus;
    public static OptionItem DisableNearButton_Airship;
    public static OptionItem DisableNearButton_Fungle;
    public static OptionItem FirstFixedSpawn_Skeld;
    public static OptionItem FirstFixedSpawn_MiraHQ;
    public static OptionItem FirstFixedSpawn_Polus;
    public static OptionItem FirstFixedSpawn_Fungle;

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
    public static bool IsStandardHAS => StandardHAS.GetValue() == 1 && CurrentGameMode == CustomGameMode.Standard;
    public static OptionItem StandardHAS;
    public static OptionItem StandardHASWaitingTime;

    // リアクターの時間制御
    public static OptionItem SabotageTimeControl_Polus;
    public static OptionItem SabotageTimeControl_Airship;
    public static OptionItem SabotageTimeControl_Fungle;
    public static OptionItem PolusReactorTimeLimit;
    public static OptionItem AirshipReactorTimeLimit;
    public static OptionItem FungleReactorTimeLimit;
    public static OptionItem FungleMushroomMixupDuration;

    // サボタージュのクールダウン変更
    public static OptionItem ModifySabotageCooldown;
    public static OptionItem SabotageCooldown;

    // 停電の特殊設定
    public static OptionItem LightsOutSpecialSettings_Airship;
    public static OptionItem DisableAirshipViewingDeckLightsPanel;
    public static OptionItem DisableAirshipGapRoomLightsPanel;
    public static OptionItem DisableAirshipCargoLightsPanel;
    public static OptionItem BlockDisturbancesToSwitches;
    // キノコカオスサボ時のボタン無効
    public static OptionItem DisableButtonInMushroomMixup;

    // マップ改造
    public static OptionItem ResetDoorsEveryTurns_Polus;
    public static OptionItem ResetDoorsEveryTurns_Airship;
    public static OptionItem ResetDoorsEveryTurns_Fungle;
    public static OptionItem DoorsResetMode_Polus;
    public static OptionItem DoorsResetMode_Airship;
    public static OptionItem DoorsResetMode_Fungle;

    public static OptionItem AirShipVariableElectrical;
    public static OptionItem DisableAirshipMovingPlatform;
    public static OptionItem FungleCanUseZipline;
    public static OptionItem FungleCanUseZiplineFromTop;
    public static OptionItem FungleCanUseZiplineFromUnder;
    public static OptionItem FungleCanSporeTrigger;

    // その他
    public static OptionItem FixFirstKillCooldown;
    public static OptionItem DisableTaskWin;
    public static OptionItem GhostCantSeeOtherRoles;
    public static OptionItem GhostCantSeeOtherTasks;
    public static OptionItem GhostCantSeeOtherVotes;
    public static OptionItem GhostCanSeeOtherTeams;
    public static OptionItem GhostCanSeeDeathReason;
    public static OptionItem GhostIgnoreTasks;
    public static OptionItem CommsCamouflage;

    public static OptionItem SkinControle;
    public static OptionItem NoHat;
    public static OptionItem NoFullFaceHat;
    public static OptionItem NoSkin;
    public static OptionItem NoVisor;
    public static OptionItem NoPet;
    public static OptionItem NoDuplicateHat;
    public static OptionItem NoDuplicateSkin;

    // プリセット対象外
    public static OptionItem NoGameEnd;
    public static OptionItem AutoDisplayLastResult;
    public static OptionItem AutoDisplayKillLog;
    public static OptionItem SuffixMode;
    public static OptionItem HideGameSettings;
    public static OptionItem NameChangeMode;
    public static OptionItem ChangeNameToRoleInfo;
    public static OptionItem RoleAssigningAlgorithm;

    public static OptionItem ApplyDenyNameList;
    public static OptionItem KickPlayerFriendCodeNotExist;
    public static OptionItem ApplyBanList;
    public static OptionItem AntiCheat;
    public static OptionItem CheaterAutoBan;
    public static OptionItem CheatLobbyKill;

    // 8人以上のパケット問題
    public static OptionItem FixSpawnPacketSize;

    // ModGameMode
    public static bool IsHASMode => CurrentGameMode == CustomGameMode.HideAndSeek;
    public static bool IsCCMode => CurrentGameMode == CustomGameMode.CatchCat;
    //public static bool IsONMode => CurrentGameMode == CustomGameMode.OneNight;

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
    public static OptionItem RevengeImpostorByImpostor;

    public static OptionItem HostGhostIgnoreTasks;
    public static OptionItem ChangeIntro;
    public static OptionItem DisplayTeamMark;
    public static OptionItem AddonShow;
    public static readonly string[] addonShowModes =
    {
        "addonShowModes.Default", "addonShowModes.All", "addonShowModes.TOH"
    };
    public static AddonShowMode GetAddonShowModes() => (AddonShowMode)AddonShow.GetValue();
    public static readonly string[] nameChangeModes =
    {
        "nameChangeMode.None", "nameChangeMode.Crew", "nameChangeMode.Color"
    };
    public static NameChange GetNameChangeModes() => (NameChange)NameChangeMode.GetValue();

    public static readonly string[] suffixModes =
    {
        "SuffixMode.None",
        //"SuffixMode.Version",
        //"SuffixMode.Streaming",
        //"SuffixMode.Recording",
        //"SuffixMode.RoomHost",
        //"SuffixMode.OriginalName"
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

    // シンクロカラーモード
    public static OptionItem SyncColorModeSelect;
    public static readonly string[] SelectSyncColorMode =
    {
        "None", "Clone", "fif_fif", "ThreeCornered", "Twin"
    };
    public static OptionItem SCM_NothingMeetingNameColor;
    public static OptionItem SCM_RestoredDeadPlayer;

    // モード読み取り
    public static bool IsSyncColorMode => GetSyncColorMode() != SyncColorMode.None;
    public static SyncColorMode GetSyncColorMode() => (SyncColorMode)SyncColorModeSelect.GetValue();

    // ======================
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

        //9人以上部屋で落ちる現象の対策
        FixSpawnPacketSize = StringOptionItem.Create(3, "FixSpawnPacketSize", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(new Color32(255, 91, 112, 255))
            .SetGameMode(CustomGameMode.All);

        // プリセット
        _ = PresetOptionItem.Create(0, TabGroup.ModMainSettings)
            .SetColor(new Color32(204, 204, 0, 255))
            .SetHeader(true)
            .SetGameMode(CustomGameMode.All);

        // ゲームモード
        GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.ModMainSettings, false)
            .SetColor(new Color32(204, 204, 0, 255))
            .SetGameMode(CustomGameMode.All);

        HideGameSettings = StringOptionItem.Create((int)offsetId.FeatSpecial + 300, "HideGameSettings", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Color.gray);

        #region 役職・詳細設定
        CustomRoleCounts = new();
        CustomRoleSpawnChances = new();

        var sortedRoleInfo = CustomRoleManager.AllRolesInfo.Values.OrderBy(role => role.ConfigId);
        // GM
        EnableGM = StringOptionItem.Create((int)offsetId.GM, "EnableGM", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(new Color32(255, 91, 112, 255))
            .SetHeader(true, Translator.GetString("GM"))
            .SetGameMode(CustomGameMode.All);

        // SpecialEvent
        if (Main.IsChristmas)
        {
            Potentialist.SetupRoleOptions();
            Potentialist.RoleInfo.OptionCreator?.Invoke();
        }
        if (Main.IsAprilFool)
        {
            JackOLantern.SetupRoleOptions();
            JackOLantern.RoleInfo.OptionCreator?.Invoke();
        }

        // 常設 新役職
        sortedRoleInfo.Where(role => role.RoleName.IsNewRole()).Do(info =>
        {
            SetupRoleOptions(info);
            info.OptionCreator?.Invoke();
        });
        // 常設
        sortedRoleInfo.Where(role => !role.RoleName.IsDontShowOptionRole() && !role.RoleName.IsNewRole()).Do(info =>
        {
            SetupRoleOptions(info);
            info.OptionCreator?.Invoke();
        });

        TextOptionItem.Create((int)offsetId.Text + 0, "Head.CommonImpostor", TabGroup.ImpostorRoles);
        DefaultShapeshiftCooldown = FloatOptionItem.Create((int)offsetId.FeatNonDisplay + 1000, "DefaultShapeshiftCooldown", new(5f, 999f, 5f), 15f, TabGroup.ImpostorRoles, false)
            .SetValueFormat(OptionFormat.Seconds);
        ImpostorOperateVisibility = StringOptionItem.Create((int)offsetId.FeatNonDisplay + 1010, "ImpostorOperateVisibility", new string[] { "Off", "On" }, 0, TabGroup.ImpostorRoles, false);

        // Madmate
        CanMakeMadmateCount = IntegerOptionItem.Create((int)offsetId.MadTOH + 400, "CanMakeMadmateCount", new(0, 15, 1), 0, TabGroup.MadmateRoles, false)
            .SetColor(Palette.ImpostorRed)
            .SetHeader(true, Translator.GetString("SKMadmateInfo"))
            .SetValueFormat(OptionFormat.Players);
        MadmateCanFixLightsOut = StringOptionItem.Create((int)offsetId.MadTOH + 410, "MadmateCanFixLightsOut", new string[] { "Off", "On" }, 0, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateCanFixComms = StringOptionItem.Create((int)offsetId.MadTOH + 411, "MadmateCanFixComms", new string[] { "Off", "On" }, 0, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateHasImpostorVision = StringOptionItem.Create((int)offsetId.MadTOH + 412, "MadmateHasImpostorVision", new string[] { "Off", "On" }, 0, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateCanSeeKillFlash = StringOptionItem.Create((int)offsetId.MadTOH + 413, "MadmateCanSeeKillFlash", new string[] { "Off", "On" }, 0, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateCanSeeOtherVotes = StringOptionItem.Create((int)offsetId.MadTOH + 414, "MadmateCanSeeOtherVotes", new string[] { "Off", "On" }, 0, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateCanSeeDeathReason = StringOptionItem.Create((int)offsetId.MadTOH + 415, "MadmateCanSeeDeathReason", new string[] { "Off", "On" }, 0, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateRevengeCrewmate = StringOptionItem.Create((int)offsetId.MadTOH + 416, "MadmateExileCrewmate", new string[] { "Off", "On" }, 0, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);

        TextOptionItem.Create((int)offsetId.Text + 1, "Head.CommonMadmate", TabGroup.MadmateRoles);
        MadmateVentCooldown = FloatOptionItem.Create((int)offsetId.FeatNonDisplay + 2000, "MadmateVentCooldown", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false)
            .SetValueFormat(OptionFormat.Seconds);
        MadmateVentMaxTime = FloatOptionItem.Create((int)offsetId.FeatNonDisplay + 2010, "MadmateVentMaxTime", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false)
            .SetValueFormat(OptionFormat.Seconds);

        // Add-Ons
        //TextOptionItem.Create((int)offsetId.Text + 15, "Head.NewAddOn", TabGroup.Addons).SetColor(Color.green);

        TextOptionItem.Create((int)offsetId.Text + 10, "Head.ImpostorAddOn", TabGroup.Addons).SetColor(Palette.ImpostorRed);
        LastImpostor.SetupCustomOption();

        TextOptionItem.Create((int)offsetId.Text + 11, "Head.CrewmateAddOn", TabGroup.Addons).SetColor(Palette.CrewmateBlue);
        CompreteCrew.SetupCustomOption();
        Workhorse.SetupCustomOption();

        TextOptionItem.Create((int)offsetId.Text + 12, "Head.NeutralAddOn", TabGroup.Addons).SetColor(Palette.Orange);
        Lovers.SetupCustomOption();

        TextOptionItem.Create((int)offsetId.Text + 13, "Head.BuffAddOn", TabGroup.Addons).SetColor(Color.yellow);
        AddLight.SetupCustomOption();
        Management.SetupCustomOption();
        AddWatch.SetupCustomOption();
        AddSeer.SetupCustomOption();
        Autopsy.SetupCustomOption();
        VIP.SetupCustomOption();
        Revenger.SetupCustomOption();
        Sending.SetupCustomOption();
        TieBreaker.SetupCustomOption();
        PlusVote.SetupCustomOption();
        Guarding.SetupCustomOption();
        AddBait.SetupCustomOption();
        Refusing.SetupCustomOption();
        Revealer.SetupCustomOption();

        TextOptionItem.Create((int)offsetId.Text + 14, "Head.DebuffAddOn", TabGroup.Addons).SetColor(Palette.Purple);
        Sunglasses.SetupCustomOption();
        Clumsy.SetupCustomOption();
        InfoPoor.SetupCustomOption();
        NonReport.SetupCustomOption();
        #endregion

        TextOptionItem.Create((int)offsetId.Text + 20, "Head.RoleAssign", TabGroup.ModMainSettings).SetColor(Palette.LightBlue);
        RoleAssigningAlgorithm = StringOptionItem.Create((int)offsetId.FeatSpecial + 100, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 0, TabGroup.ModMainSettings, true)
            .RegisterUpdateValueEvent((object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue))
            .SetGameMode(CustomGameMode.All)
            .SetColor(Palette.LightBlue);
        RoleAssignManager.SetupOptionItem();

        // HideAndSeek
        /********************************************************************************/
        SetupRoleOptions((int)offsetId.GModeHaS + 1000, TabGroup.ModMainSettings, CustomRoles.HASFox, customGameMode: CustomGameMode.HideAndSeek);
        SetupRoleOptions((int)offsetId.GModeHaS + 1100, TabGroup.ModMainSettings, CustomRoles.HASTroll, customGameMode: CustomGameMode.HideAndSeek);

        AllowCloseDoors = StringOptionItem.Create((int)offsetId.GModeHaS + 5000, "AllowCloseDoors", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.HideAndSeek);
        KillDelay = FloatOptionItem.Create((int)offsetId.GModeHaS + 5001, "HideAndSeekWaitingTime", new(0f, 180f, 5f), 10f, TabGroup.ModMainSettings, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.HideAndSeek);
        IgnoreVent = StringOptionItem.Create((int)offsetId.GModeHaS + 5002, "IgnoreVent", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetGameMode(CustomGameMode.HideAndSeek);
        /********************************************************************************/

        // CC
        CatchCat.Option.SetupCustomOption();

        TextOptionItem.Create((int)offsetId.FeatMap, "Head.Map", TabGroup.ModMainSettings).SetColor(Palette.Orange).SetGameMode(CustomGameMode.All);
        /**************** SKELD ****************/
        MapOption_Skeld = StringOptionItem.Create((int)offsetId.FeatMap + 10, "MapOption_Skeld", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Palette.Orange)
            .SetGameMode(CustomGameMode.All);
        // ランダムスポーン
        RandomSpawn_Skeld = StringOptionItem.Create((int)offsetId.FeatMap + 100, "RandomSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Skeld)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        AdditionalSpawn_Skeld = StringOptionItem.Create((int)offsetId.FeatMap + 101, "AdditionalSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Skeld)
            .SetGameMode(CustomGameMode.All);
        DisableNearButton_Skeld = StringOptionItem.Create((int)offsetId.FeatMap + 102, "DisableNearButton", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Skeld)
            .SetGameMode(CustomGameMode.All);
        FirstFixedSpawn_Skeld = StringOptionItem.Create((int)offsetId.FeatMap + 103, "FirstFixedSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Skeld)
            .SetGameMode(CustomGameMode.All);
        // デバイス無効化
        DisableDevices_Skeld = StringOptionItem.Create((int)offsetId.FeatMap + 120, "DisableDevices", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Skeld)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        DisableAdmin_Skeld = StringOptionItem.Create((int)offsetId.FeatMap + 121, "DisableAdmin", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Skeld).SetGameMode(CustomGameMode.All);
        DisableCamera_Skeld = StringOptionItem.Create((int)offsetId.FeatMap + 122, "DisableCamera", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Skeld).SetGameMode(CustomGameMode.All);

        /**************** MIRA HQ ****************/
        MapOption_MiraHQ = StringOptionItem.Create((int)offsetId.FeatMap + 20, "MapOption_MiraHQ", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Palette.Orange)
            .SetGameMode(CustomGameMode.All);
        // ランダムスポーン
        RandomSpawn_MiraHQ = StringOptionItem.Create((int)offsetId.FeatMap + 200, "RandomSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_MiraHQ)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        AdditionalSpawn_MiraHQ = StringOptionItem.Create((int)offsetId.FeatMap + 201, "AdditionalSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_MiraHQ)
            .SetGameMode(CustomGameMode.All);
        DisableNearButton_MiraHQ = StringOptionItem.Create((int)offsetId.FeatMap + 202, "DisableNearButton", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_MiraHQ)
            .SetGameMode(CustomGameMode.All);
        FirstFixedSpawn_MiraHQ = StringOptionItem.Create((int)offsetId.FeatMap + 203, "FirstFixedSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_MiraHQ)
            .SetGameMode(CustomGameMode.All);
        // デバイス無効化
        DisableDevices_MiraHQ = StringOptionItem.Create((int)offsetId.FeatMap + 210, "DisableDevices", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_MiraHQ)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        DisableAdmin_MiraHQ = StringOptionItem.Create((int)offsetId.FeatMap + 211, "DisableAdmin", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_MiraHQ).SetGameMode(CustomGameMode.All);
        DisableDoorLog_MiraHQ = StringOptionItem.Create((int)offsetId.FeatMap + 212, "DisableMiraHQDoorLog", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_MiraHQ).SetGameMode(CustomGameMode.All);

        /**************** POLUS ****************/
        MapOption_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 30, "MapOption_Polus", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Palette.Orange)
            .SetGameMode(CustomGameMode.All);
        // ランダムスポーン
        RandomSpawn_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 300, "RandomSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Polus)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        AdditionalSpawn_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 301, "AdditionalSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Polus)
            .SetGameMode(CustomGameMode.All);
        DisableNearButton_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 302, "DisableNearButton", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Polus)
            .SetGameMode(CustomGameMode.All);
        FirstFixedSpawn_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 303, "FirstFixedSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Polus)
            .SetGameMode(CustomGameMode.All);
        // デバイス無効化
        DisableDevices_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 310, "DisableDevices", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Polus)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        DisableAdmin_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 311, "DisableAdmin", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Polus).SetGameMode(CustomGameMode.All);
        DisableCamera_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 312, "DisableCamera", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Polus).SetGameMode(CustomGameMode.All);
        DisableVital_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 313, "DisableVital", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Polus).SetGameMode(CustomGameMode.All);
        // ドアリセット
        ResetDoorsEveryTurns_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 320, "ResetDoorsEveryTurns", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Polus)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        DoorsResetMode_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 321, "DoorsResetMode", EnumHelper.GetAllNames<DoorsReset.ResetMode>(), 0, TabGroup.ModMainSettings, false).SetParent(ResetDoorsEveryTurns_Polus).SetGameMode(CustomGameMode.All);
        // リアクターの時間制御
        SabotageTimeControl_Polus = StringOptionItem.Create((int)offsetId.FeatMap + 350, "SabotageTimeControl", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Polus)
            .SetColor(Color.magenta)
            .SetGameMode(CustomGameMode.All);
        PolusReactorTimeLimit = FloatOptionItem.Create((int)offsetId.FeatMap + 351, "ReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.ModMainSettings, false).SetParent(SabotageTimeControl_Polus)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.All);

        /**************** AIRSHIP ****************/
        MapOption_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 40, "MapOption_Airship", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Palette.Orange)
            .SetGameMode(CustomGameMode.All);
        // ランダムスポーン
        RandomSpawn_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 400, "RandomSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Airship)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        AdditionalSpawn_AirshipTAKADA = StringOptionItem.Create((int)offsetId.FeatMap + 401, "AdditionalSpawn_AirshipTAKADA", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Airship)
            .SetGameMode(CustomGameMode.All);
        AdditionalSpawn_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 402, "AdditionalSpawn_Airship", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Airship)
            .SetGameMode(CustomGameMode.All);
        DisableNearButton_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 403, "DisableNearButton", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Airship)
            .SetGameMode(CustomGameMode.All);
        // デバイス無効化
        DisableDevices_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 410, "DisableDevices", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Airship)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        DisableCockpitAdmin_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 411, "DisableAirshipCockpitAdmin", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Airship).SetGameMode(CustomGameMode.All);
        DisableRecordsAdmin_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 412, "DisableAirshipRecordsAdmin", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Airship).SetGameMode(CustomGameMode.All);
        DisableCamera_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 413, "DisableCamera", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Airship).SetGameMode(CustomGameMode.All);
        DisableVital_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 414, "DisableVital", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Airship).SetGameMode(CustomGameMode.All);
        // ドアリセット
        ResetDoorsEveryTurns_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 420, "ResetDoorsEveryTurns", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Airship)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        DoorsResetMode_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 421, "DoorsResetMode", EnumHelper.GetAllNames<DoorsReset.ResetMode>(), 0, TabGroup.ModMainSettings, false).SetParent(ResetDoorsEveryTurns_Airship).SetGameMode(CustomGameMode.All);
        // エレキ構造の変化
        AirShipVariableElectrical = StringOptionItem.Create((int)offsetId.FeatMap + 430, "AirShipVariableElectrical", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Airship)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        // 昇降機使用制限
        DisableAirshipMovingPlatform = StringOptionItem.Create((int)offsetId.FeatMap + 431, "DisableAirshipMovingPlatform", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Airship)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        // リアクターの時間制御
        SabotageTimeControl_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 450, "SabotageTimeControl", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Airship)
            .SetColor(Color.magenta)
            .SetGameMode(CustomGameMode.All);
        AirshipReactorTimeLimit = FloatOptionItem.Create((int)offsetId.FeatMap + 451, "ReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.ModMainSettings, false).SetParent(SabotageTimeControl_Airship)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.All);
        // 停電解除場所の制限
        LightsOutSpecialSettings_Airship = StringOptionItem.Create((int)offsetId.FeatMap + 460, "LightsOutSpecialSettings_Airship", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Airship)
            .SetColor(Color.magenta).SetGameMode(CustomGameMode.All);
        DisableAirshipViewingDeckLightsPanel = StringOptionItem.Create((int)offsetId.FeatMap + 461, "DisableAirshipViewingDeckLightsPanel", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(LightsOutSpecialSettings_Airship).SetGameMode(CustomGameMode.All);
        DisableAirshipGapRoomLightsPanel = StringOptionItem.Create((int)offsetId.FeatMap + 462, "DisableAirshipGapRoomLightsPanel", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(LightsOutSpecialSettings_Airship).SetGameMode(CustomGameMode.All);
        DisableAirshipCargoLightsPanel = StringOptionItem.Create((int)offsetId.FeatMap + 463, "DisableAirshipCargoLightsPanel", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(LightsOutSpecialSettings_Airship).SetGameMode(CustomGameMode.All);

        /**************** FUNGLE ****************/
        MapOption_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 50, "MapOption_Fungle", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Palette.Orange)
            .SetGameMode(CustomGameMode.All);
        // ランダムスポーン
        RandomSpawn_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 500, "RandomSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Fungle)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        AdditionalSpawn_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 501, "AdditionalSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Fungle)
            .SetGameMode(CustomGameMode.All);
        DisableNearButton_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 502, "DisableNearButton", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Fungle)
            .SetGameMode(CustomGameMode.All);
        FirstFixedSpawn_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 503, "FirstFixedSpawn", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomSpawn_Fungle)
            .SetGameMode(CustomGameMode.All);
        // デバイス無効化
        DisableDevices_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 510, "DisableDevices", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Fungle)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        DisableVital_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 511, "DisableVital", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevices_Fungle).SetGameMode(CustomGameMode.All);
        // ドアリセット
        ResetDoorsEveryTurns_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 520, "ResetDoorsEveryTurns", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Fungle)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        DoorsResetMode_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 521, "DoorsResetMode", EnumHelper.GetAllNames<DoorsReset.ResetMode>(), 0, TabGroup.ModMainSettings, false).SetParent(ResetDoorsEveryTurns_Fungle).SetGameMode(CustomGameMode.All);
        // ジップラインの方向制限
        FungleCanUseZipline = StringOptionItem.Create((int)offsetId.FeatMap + 530, "FungleCanUseZipline", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Fungle)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        FungleCanUseZiplineFromTop = StringOptionItem.Create((int)offsetId.FeatMap + 531, "FungleCanUseZiplineFromTop", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(FungleCanUseZipline).SetGameMode(CustomGameMode.All);
        FungleCanUseZiplineFromUnder = StringOptionItem.Create((int)offsetId.FeatMap + 532, "FungleCanUseZiplineFromUnder", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(FungleCanUseZipline).SetGameMode(CustomGameMode.All);
        // キノコ胞子モヤの無効
        FungleCanSporeTrigger = StringOptionItem.Create((int)offsetId.FeatMap + 540, "FungleCanSporeTrigger", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Fungle)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        // リアクターの時間制御
        SabotageTimeControl_Fungle = StringOptionItem.Create((int)offsetId.FeatMap + 550, "SabotageTimeControl", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Fungle)
            .SetColor(Color.magenta)
            .SetGameMode(CustomGameMode.All);
        FungleReactorTimeLimit = FloatOptionItem.Create((int)offsetId.FeatMap + 551, "ReactorTimeLimit", new(1f, 60f, 1f), 50f, TabGroup.ModMainSettings, false).SetParent(SabotageTimeControl_Fungle)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.All);
        FungleMushroomMixupDuration = FloatOptionItem.Create((int)offsetId.FeatMap + 552, "FungleMushroomMixupDuration", new(1f, 20f, 1f), 10f, TabGroup.ModMainSettings, false).SetParent(SabotageTimeControl_Fungle)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.All);
        // キノコカオスサボ時のボタン無効
        DisableButtonInMushroomMixup = StringOptionItem.Create((int)offsetId.FeatMap + 560, "DisableButtonInMushroomMixup", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(MapOption_Fungle)
            .SetColor(Color.magenta).SetGameMode(CustomGameMode.All);

        // マップ設定：マップ共通
        TextOptionItem.Create((int)offsetId.FeatMap + 1, "Head.MapCommon", TabGroup.ModMainSettings).SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        // デバイス無効 マップ共通除外条件
        DisableDevicesIgnoreConditions = StringOptionItem.Create((int)offsetId.FeatMap + 1000, "IgnoreConditions", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.yellow);
        DisableDevicesIgnoreImpostors = StringOptionItem.Create((int)offsetId.FeatMap + 1001, "IgnoreImpostors", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevicesIgnoreConditions);
        DisableDevicesIgnoreMadmates = StringOptionItem.Create((int)offsetId.FeatMap + 1002, "IgnoreMadmates", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevicesIgnoreConditions);
        DisableDevicesIgnoreNeutrals = StringOptionItem.Create((int)offsetId.FeatMap + 1003, "IgnoreNeutrals", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevicesIgnoreConditions);
        DisableDevicesIgnoreCrewmates = StringOptionItem.Create((int)offsetId.FeatMap + 1004, "IgnoreCrewmates", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevicesIgnoreConditions);
        DisableDevicesIgnoreAfterAnyoneDied = StringOptionItem.Create((int)offsetId.FeatMap + 1005, "IgnoreAfterAnyoneDied", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableDevicesIgnoreConditions);
        // ランダムマップ
        RandomMapsMode = StringOptionItem.Create((int)offsetId.FeatMap + 1010, "RandomMapsMode", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.yellow)
            .SetGameMode(CustomGameMode.All);
        AddedTheSkeld = StringOptionItem.Create((int)offsetId.FeatMap + 1011, "AddedTheSkeld", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);
        AddedMiraHQ = StringOptionItem.Create((int)offsetId.FeatMap + 1012, "AddedMIRAHQ", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);
        AddedPolus = StringOptionItem.Create((int)offsetId.FeatMap + 1013, "AddedPolus", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);
        AddedTheAirship = StringOptionItem.Create((int)offsetId.FeatMap + 1014, "AddedTheAirship", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);
        AddedTheFungle = StringOptionItem.Create((int)offsetId.FeatMap + 1015, "AddedTheFungle", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);

        // サボタージュ：マップ共通
        TextOptionItem.Create((int)offsetId.FeatSabotage, "Head.Sabotage", TabGroup.ModMainSettings).SetColor(Color.magenta).SetGameMode(CustomGameMode.All);
        // サボタージュのクールダウン変更
        ModifySabotageCooldown = StringOptionItem.Create((int)offsetId.FeatSabotage + 100, "ModifySabotageCooldown", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.magenta).SetGameMode(CustomGameMode.All);
        SabotageCooldown = FloatOptionItem.Create((int)offsetId.FeatSabotage + 101, "SabotageCooldown", new(1f, 60f, 1f), 30f, TabGroup.ModMainSettings, false).SetParent(ModifySabotageCooldown)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.All);
        // 停電の配電盤妨害を無効化
        BlockDisturbancesToSwitches = StringOptionItem.Create((int)offsetId.FeatSabotage + 200, "BlockDisturbancesToSwitches", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.magenta).SetGameMode(CustomGameMode.All);
        // コミュサボカモフラージュ
        CommsCamouflage = StringOptionItem.Create((int)offsetId.FeatSabotage + 300, "CommsCamouflage", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.magenta)
            .SetGameMode(CustomGameMode.All);

        TextOptionItem.Create((int)offsetId.FeatMeeting, "Head.Meeting", TabGroup.ModMainSettings).SetColor(Color.cyan).SetGameMode(CustomGameMode.All);
        // 会議収集理由表示
        ShowReportReason = StringOptionItem.Create((int)offsetId.FeatMeeting + 100, "ShowReportReason", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.All);

        // 初手会議に役職名表示
        ShowRoleInfoAtFirstMeeting = StringOptionItem.Create((int)offsetId.FeatMeeting + 200, "ShowRoleInfoAtFirstMeeting", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Color.cyan);

        // ボタン回数同期
        SyncButtonMode = StringOptionItem.Create((int)offsetId.FeatMeeting + 300, "SyncButtonMode", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.cyan);
        SyncedButtonCount = IntegerOptionItem.Create((int)offsetId.FeatMeeting + 301, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.ModMainSettings, false).SetParent(SyncButtonMode)
            .SetValueFormat(OptionFormat.Times);

        // 投票モード
        VoteMode = StringOptionItem.Create((int)offsetId.FeatMeeting + 400, "VoteMode", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.All);
        WhenSkipVote = StringOptionItem.Create((int)offsetId.FeatMeeting + 410, "WhenSkipVote", voteModes[0..3], 0, TabGroup.ModMainSettings, false).SetParent(VoteMode);
        WhenSkipVoteIgnoreFirstMeeting = StringOptionItem.Create((int)offsetId.FeatMeeting + 411, "WhenSkipVoteIgnoreFirstMeeting", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(WhenSkipVote);
        WhenSkipVoteIgnoreNoDeadBody = StringOptionItem.Create((int)offsetId.FeatMeeting + 412, "WhenSkipVoteIgnoreNoDeadBody", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(WhenSkipVote);
        WhenSkipVoteIgnoreEmergency = StringOptionItem.Create((int)offsetId.FeatMeeting + 413, "WhenSkipVoteIgnoreEmergency", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(WhenSkipVote);
        WhenNonVote = StringOptionItem.Create((int)offsetId.FeatMeeting + 420, "WhenNonVote", voteModes, 0, TabGroup.ModMainSettings, false).SetParent(VoteMode)
            .SetGameMode(CustomGameMode.All);
        WhenTie = StringOptionItem.Create((int)offsetId.FeatMeeting + 430, "WhenTie", tieModes, 0, TabGroup.ModMainSettings, false).SetParent(VoteMode);

        // 全員生存時の会議時間
        AllAliveMeeting = StringOptionItem.Create((int)offsetId.FeatMeeting + 500, "AllAliveMeeting", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.cyan);
        AllAliveMeetingTime = FloatOptionItem.Create((int)offsetId.FeatMeeting + 501, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.ModMainSettings, false).SetParent(AllAliveMeeting)
            .SetValueFormat(OptionFormat.Seconds);

        // 生存人数ごとの緊急会議
        AdditionalEmergencyCooldown = StringOptionItem.Create((int)offsetId.FeatMeeting + 600, "AdditionalEmergencyCooldown", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.cyan);
        AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create((int)offsetId.FeatMeeting + 601, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.ModMainSettings, false).SetParent(AdditionalEmergencyCooldown)
            .SetValueFormat(OptionFormat.Players);
        AdditionalEmergencyCooldownTime = FloatOptionItem.Create((int)offsetId.FeatMeeting + 602, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.ModMainSettings, false).SetParent(AdditionalEmergencyCooldown)
            .SetValueFormat(OptionFormat.Seconds);

        TextOptionItem.Create((int)offsetId.FeatRevenge, "Head.Revenge", TabGroup.ModMainSettings).SetColor(Palette.Orange).SetGameMode(CustomGameMode.Standard);
        // 道連れ人表記
        ShowRevengeTarget = StringOptionItem.Create((int)offsetId.FeatRevenge + 100, "ShowRevengeTarget", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Color.cyan);
        RevengeImpostorByImpostor = StringOptionItem.Create((int)offsetId.FeatRevenge + 200, "RevengeImpostorByImpostor", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.ImpostorRed);
        RevengeMadByImpostor = StringOptionItem.Create((int)offsetId.FeatRevenge + 250, "RevengeMadByImpostor", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.ImpostorRed);
        RevengeNeutral = StringOptionItem.Create((int)offsetId.FeatRevenge + 300, "RevengeNeutral", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.Orange);

        TextOptionItem.Create((int)offsetId.FeatTask, "Head.Task", TabGroup.ModMainSettings).SetColor(Color.green).SetGameMode(CustomGameMode.All);
        // タスク無効化
        DisableTasks = StringOptionItem.Create((int)offsetId.FeatTask + 100, "DisableTasks", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.green)
            .SetGameMode(CustomGameMode.All);
        DisableSwipeCard = StringOptionItem.Create((int)offsetId.FeatTask + 101, "DisableSwipeCardTask", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableSubmitScan = StringOptionItem.Create((int)offsetId.FeatTask + 102, "DisableSubmitScanTask", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableUnlockSafe = StringOptionItem.Create((int)offsetId.FeatTask + 103, "DisableUnlockSafeTask", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableUploadData = StringOptionItem.Create((int)offsetId.FeatTask + 104, "DisableUploadDataTask", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableStartReactor = StringOptionItem.Create((int)offsetId.FeatTask + 105, "DisableStartReactorTask", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableResetBreaker = StringOptionItem.Create((int)offsetId.FeatTask + 106, "DisableResetBreakerTask", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableRewindTapes = StringOptionItem.Create((int)offsetId.FeatTask + 107, "DisableRewindTapes", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableVentCleaning = StringOptionItem.Create((int)offsetId.FeatTask + 108, "DisableVentCleaning", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableBuildSandcastle = StringOptionItem.Create((int)offsetId.FeatTask + 109, "DisableBuildSandcastle", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableTestFrisbee = StringOptionItem.Create((int)offsetId.FeatTask + 110, "DisableTestFrisbee", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableWaterPlants = StringOptionItem.Create((int)offsetId.FeatTask + 111, "DisableWaterPlants", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableCatchFish = StringOptionItem.Create((int)offsetId.FeatTask + 112, "DisableCatchFish", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableHelpCritter = StringOptionItem.Create((int)offsetId.FeatTask + 113, "DisableHelpCritter", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableTuneRadio = StringOptionItem.Create((int)offsetId.FeatTask + 114, "DisableTuneRadio", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableAssembleArtifact = StringOptionItem.Create((int)offsetId.FeatTask + 115, "DisableAssembleArtifact", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);

        // タスク勝利無効化
        DisableTaskWin = StringOptionItem.Create((int)offsetId.FeatTask + 200, "DisableTaskWin", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.green);
        //ホストの死後タスク免除
        HostGhostIgnoreTasks = StringOptionItem.Create((int)offsetId.FeatTask + 300, "HostGhostIgnoreTasks", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Color.green);
        //タスク免除
        GhostIgnoreTasks = StringOptionItem.Create((int)offsetId.FeatGhost + 100, "GhostIgnoreTasks", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Color.green);

        TextOptionItem.Create((int)offsetId.FeatGhost, "Head.Ghost", TabGroup.ModMainSettings).SetColor(Palette.LightBlue).SetGameMode(CustomGameMode.All);
        // 幽霊
        GhostCantSeeOtherRoles = StringOptionItem.Create((int)offsetId.FeatGhost + 200, "GhostCantSeeOtherRoles", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);
        GhostCantSeeOtherTasks = StringOptionItem.Create((int)offsetId.FeatGhost + 300, "GhostCantSeeOtherTasks", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);
        GhostCantSeeOtherVotes = StringOptionItem.Create((int)offsetId.FeatGhost + 400, "GhostCantSeeOtherVotes", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);
        GhostCanSeeOtherTeams = StringOptionItem.Create((int)offsetId.FeatGhost + 600, "GhostCanSeeOtherTeams", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);
        GhostCanSeeDeathReason = StringOptionItem.Create((int)offsetId.FeatGhost + 500, "GhostCanSeeDeathReason", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);

        TextOptionItem.Create((int)offsetId.FeatOther, "Head.Other", TabGroup.ModMainSettings).SetColor(Palette.CrewmateBlue).SetGameMode(CustomGameMode.All);
        KillFlashDuration = FloatOptionItem.Create((int)offsetId.FeatOther + 100, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.ModMainSettings, true)
            .SetColor(Palette.ImpostorRed)
            .SetValueFormat(OptionFormat.Seconds);
        // CO可否表示(id+499まで使用)
        DisplayComingOut.SetupCustomOption((int)offsetId.FeatOther + 700);
        // 陣営マーク表示
        DisplayTeamMark = StringOptionItem.Create((int)offsetId.FeatOther + 1200, "DisplayTeamMark", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.CrewmateBlue);
        // 初手キルクール調整
        FixFirstKillCooldown = StringOptionItem.Create((int)offsetId.FeatOther + 200, "FixFirstKillCooldown", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Palette.CrewmateBlue);

        // 転落死
        LadderDeath = StringOptionItem.Create((int)offsetId.FeatOther + 300, "LadderDeath", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Palette.CrewmateBlue)
            .SetGameMode(CustomGameMode.All);
        LadderDeathChance = StringOptionItem.Create((int)offsetId.FeatOther + 301, "LadderDeathChance", rates[1..], 0, TabGroup.ModMainSettings, false).SetParent(LadderDeath).SetGameMode(CustomGameMode.All);

        // スキン設定
        SkinControle = StringOptionItem.Create((int)offsetId.FeatOther + 400, "SkinControle", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetColor(Palette.CrewmateBlue)
            .SetGameMode(CustomGameMode.All);
        NoHat = StringOptionItem.Create((int)offsetId.FeatOther + 401, "NoHat", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoFullFaceHat = StringOptionItem.Create((int)offsetId.FeatOther + 402, "NoFullFaceHat", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoSkin = StringOptionItem.Create((int)offsetId.FeatOther + 403, "NoSkin", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoVisor = StringOptionItem.Create((int)offsetId.FeatOther + 404, "NoVisor", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoPet = StringOptionItem.Create((int)offsetId.FeatOther + 405, "NoPet", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoDuplicateHat = StringOptionItem.Create((int)offsetId.FeatOther + 410, "NoDuplicateHat", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoDuplicateSkin = StringOptionItem.Create((int)offsetId.FeatOther + 411, "NoDuplicateSkin", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        VoiceReader.SetupCustomOption((int)Options.offsetId.FeatOther + 500);

        TextOptionItem.Create((int)offsetId.GModeAdd, "Head.GameMode", TabGroup.ModMainSettings).SetColor(Color.yellow).SetGameMode(CustomGameMode.Standard);
        // シンクロカラーモード
        SyncColorModeSelect = StringOptionItem.Create((int)offsetId.GModeAdd + 100, "SyncColorMode", SelectSyncColorMode, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.yellow)
            .SetGameMode(CustomGameMode.Standard);
        SCM_NothingMeetingNameColor = StringOptionItem.Create((int)offsetId.GModeAdd + 110, "SCM_NothingMeetingNameColor", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(SyncColorModeSelect)
            .SetGameMode(CustomGameMode.Standard);
        SCM_RestoredDeadPlayer = StringOptionItem.Create((int)offsetId.GModeAdd + 111, "SCM_RestoredDeadPlayer", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false).SetParent(SyncColorModeSelect)
            .SetGameMode(CustomGameMode.Standard);

        // 通常モードでかくれんぼ用
        StandardHAS = StringOptionItem.Create((int)offsetId.GModeAdd + 200, "StandardHAS", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetColor(Color.yellow)
            .SetGameMode(CustomGameMode.Standard);
        StandardHASWaitingTime = FloatOptionItem.Create((int)offsetId.GModeAdd + 201, "StandardHASWaitingTime", new(0f, 180f, 2.5f), 10f, TabGroup.ModMainSettings, false).SetParent(StandardHAS)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.Standard);

        // その他
        TextOptionItem.Create((int)offsetId.System, "Head.System", TabGroup.ModMainSettings).SetColor(Color.blue).SetGameMode(CustomGameMode.All);
        NoGameEnd = StringOptionItem.Create((int)offsetId.System + 100, "NoGameEnd", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, false)
            .SetGameMode(CustomGameMode.All);
        AutoDisplayLastResult = StringOptionItem.Create((int)offsetId.System + 200, "AutoDisplayLastResult", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetGameMode(CustomGameMode.All);
        AutoDisplayKillLog = StringOptionItem.Create((int)offsetId.System + 300, "AutoDisplayKillLog", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetGameMode(CustomGameMode.All);
        SuffixMode = StringOptionItem.Create((int)offsetId.System + 400, "SuffixMode", suffixModes, 0, TabGroup.ModMainSettings, true)
            .SetGameMode(CustomGameMode.All);
        NameChangeMode = StringOptionItem.Create((int)offsetId.System + 500, "NameChangeMode", nameChangeModes, 0, TabGroup.ModMainSettings, true)
            .SetGameMode(CustomGameMode.All);
        ChangeNameToRoleInfo = StringOptionItem.Create((int)offsetId.System + 600, "ChangeNameToRoleInfo", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetGameMode(CustomGameMode.All);
        AddonShow = StringOptionItem.Create((int)offsetId.System + 700, "AddonShowMode", addonShowModes, 0, TabGroup.ModMainSettings, true);
        ChangeIntro = StringOptionItem.Create((int)offsetId.System + 800, "ChangeIntro", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true);

        TextOptionItem.Create((int)offsetId.Participation, "Head.Participation", TabGroup.ModMainSettings).SetColor(Palette.Purple).SetGameMode(CustomGameMode.All);
        ApplyDenyNameList = StringOptionItem.Create((int)offsetId.Participation + 100, "ApplyDenyNameList", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetGameMode(CustomGameMode.All);
        KickPlayerFriendCodeNotExist = StringOptionItem.Create((int)offsetId.Participation + 200, "KickPlayerFriendCodeNotExist", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetGameMode(CustomGameMode.All);
        ApplyBanList = StringOptionItem.Create((int)offsetId.Participation + 300, "ApplyBanList", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetGameMode(CustomGameMode.All);
        AntiCheat = StringOptionItem.Create((int)offsetId.Participation + 400, "AntiCheat", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true)
            .SetGameMode(CustomGameMode.All);
        CheaterAutoBan = StringOptionItem.Create((int)offsetId.Participation + 410, "CheaterAutoBan", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true).SetParent(AntiCheat)
            .SetGameMode(CustomGameMode.All);
        CheatLobbyKill = StringOptionItem.Create((int)offsetId.Participation + 420, "CheatLobbyKill", new string[] { "Off", "On" }, 0, TabGroup.ModMainSettings, true).SetParent(AntiCheat)
            .SetGameMode(CustomGameMode.All);

        DebugModeManager.SetupCustomOption();

        OptionSaver.Load();

        IsLoaded = true;
    }

    public static bool NotShowOption(string optionName)
    {
        return optionName is "KillFlashDuration"
                        or "AssignMode"
                        or "SuffixMode"
                        or "HideGameSettings"
                        or "AutoDisplayLastResult"
                        or "AutoDisplayKillLog"
                        or "RoleAssigningAlgorithm"
                        or "ShowReportReason"
                        or "ShowRoleInfoAtFirstMeeting"
                        or "HostGhostIgnoreTasks"
                        or "RevengeNeutral"
                        or "ChangeNameToRoleInfo"
                        or "ApplyDenyNameList"
                        or "KickPlayerFriendCodeNotExist"
                        or "ApplyBanList"
                        or "AntiCheat"
                        or "CheaterAutoBan"
                        or "CheatLobbyKill"
                        or "ChangeIntro"
                        or "AddonShowMode";
    }
    public static void SetupRoleOptions(SimpleRoleInfo info) =>
    SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName, info.AssignInfo.AssignCountRule);
    public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, IntegerValueRule assignCountRule = null, CustomGameMode customGameMode = CustomGameMode.Standard)
    {
        if (role.IsVanilla()) return;
        assignCountRule ??= new(1, 15, 1);

        string roleInfo = "";
        if (role.IsNormalVanillaRole())
        {
            roleInfo = Translator.GetString(Enum.GetName(typeof(CustomRoles), role.VanillaRoleConversion()) + "Blurb");
        }
        else
        {
            roleInfo = Translator.GetString(role.ToString() + "Info");
        }

        var spawnOption = IntegerOptionItem.Create(id, role.ToString(), new(0, 100, 10), 0, tab, false)
            .SetColor(Utils.GetRoleColor(role))
            .SetValueFormat(OptionFormat.Percent)
            .SetHeader(true, roleInfo)
            .SetGameMode(customGameMode) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", assignCountRule, assignCountRule.Step, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(role.IsPairRole() ? OptionFormat.Pair : OptionFormat.Players)
            .SetFixValue(role.IsFixedCountRole())
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }

    //AddOn
    public static void SetUpAddOnOptions(int Id, CustomRoles PlayerRole, TabGroup tab, CustomRoles parentRole = CustomRoles.NotAssigned, bool addRoleName = false)
    {
        if (parentRole == CustomRoles.NotAssigned) parentRole = PlayerRole;

        if (!addRoleName)
        {
            AddOnBuffAssign[PlayerRole] = StringOptionItem.Create(Id, "AddOnBuffAssign", new string[] { "Off", "On" }, 0, tab, false).SetParent(CustomRoleSpawnChances[parentRole]);
        }
        else
        {
            var roleName = Utils.GetRoleName(PlayerRole);
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(PlayerRole), roleName) } };
            AddOnBuffAssign[PlayerRole] = StringOptionItem.Create(Id, "AddOnBuffAssign%role%", new string[] { "Off", "On" }, 0, tab, false).SetParent(CustomRoleSpawnChances[parentRole]);
            AddOnBuffAssign[PlayerRole].ReplacementDictionary = replacementDic;
        }
        Id += 10;
        foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsBuffAddOn()))
        {
            if (Addon == CustomRoles.Loyalty && PlayerRole.IsDontSelectLoyaltyRole()) continue;
            if (Addon == CustomRoles.Revenger && PlayerRole is CustomRoles.MadNimrod) continue;

            SetUpAddOnRoleOption(PlayerRole, tab, Addon, Id, false, AddOnBuffAssign[PlayerRole]);
            Id++;
        }

        if (!addRoleName)
        {
            AddOnDebuffAssign[PlayerRole] = StringOptionItem.Create(Id, "AddOnDebuffAssign", new string[] { "Off", "On" }, 0, tab, false).SetParent(CustomRoleSpawnChances[parentRole]);
        }
        else
        {
            var roleName = Utils.GetRoleName(PlayerRole);
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(PlayerRole), roleName) } };
            AddOnDebuffAssign[PlayerRole] = StringOptionItem.Create(Id, "AddOnDebuffAssign%role%", new string[] { "Off", "On" }, 0, tab, false).SetParent(CustomRoleSpawnChances[parentRole]);
            AddOnDebuffAssign[PlayerRole].ReplacementDictionary = replacementDic;
        }
        Id += 10;
        foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsDebuffAddOn()))
        {
            SetUpAddOnRoleOption(PlayerRole, tab, Addon, Id, false, AddOnDebuffAssign[PlayerRole]);
            Id++;
        }
    }
    public static void SetUpAddOnRoleOption(CustomRoles PlayerRole, TabGroup tab, CustomRoles role, int Id, bool defaultValue = false, OptionItem parent = null)
    {
        // 親オプションが指定されていなければデフォルトの親を使用
        if (parent == null && CustomRoleSpawnChances.ContainsKey(PlayerRole))
            parent = CustomRoleSpawnChances[PlayerRole];

        // 置換用の文字列辞書を作成
        Dictionary<string, string> replacementDic = new()
    {
        { "%role%", Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role)) + "ㅤ" + Utils.GetAddonAbilityInfo(role) }
    };

        var key = (PlayerRole, role);

        if (!AddOnRoleOptions.ContainsKey(key))
        {
            OptionItem newOption;

            if (parent != null)
            {
                // 親がある場合 → StringOptionItem を作成して親に紐づけ
                newOption = StringOptionItem.Create(
                    Id,
                    role.ToString(),
                    new string[] { "Off", "On" },
                    defaultValue ? 1 : 0,
                    tab,
                    true
                );
                newOption.SetParent(parent);
            }
            else
            {
                // 親がない場合 → 単独で作成
                newOption = StringOptionItem.Create(
                    Id,
                    role.ToString(),
                    new string[] { "Off", "On" },
                    defaultValue ? 1 : 0,
                    tab,
                    true
                );
            }

            AddOnRoleOptions[key] = newOption;
        }



        // ReplacementDictionary を安全に設定
        AddOnRoleOptions[key].ReplacementDictionary = replacementDic;
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

        public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role, OptionItem option = null, bool addRoleName = false)
        {
            this.IdStart = idStart;
            this.Role = role;

            if (option == null) option = CustomRoleSpawnChances[role];
            if (!addRoleName)
            {
                doOverride = StringOptionItem.Create(idStart++, "doOverride", new string[] { "Off", "On" }, 0, tab, false).SetParent(option)
                    .SetValueFormat(OptionFormat.None);
            }
            else
            {
                var roleName = Utils.GetRoleName(role);
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
                doOverride = StringOptionItem.Create(idStart++, "doOverride%role%", new string[] { "Off", "On" }, 0, tab, false).SetParent(option);
                doOverride.ReplacementDictionary = replacementDic;
            }

            assignCommonTasks = StringOptionItem.Create(idStart++, "assignCommonTasks", new string[] { "Off", "On" }, 0, tab, false).SetParent(doOverride)
                .SetValueFormat(OptionFormat.None);
            numLongTasks = IntegerOptionItem.Create(idStart++, "roleLongTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                .SetValueFormat(OptionFormat.Pieces);
            numShortTasks = IntegerOptionItem.Create(idStart++, "roleShortTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                .SetValueFormat(OptionFormat.Pieces);

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
        }
        public static OverrideTasksData Create(int idStart, TabGroup tab, CustomRoles role)
        {
            return new OverrideTasksData(idStart, tab, role);
        }
        public static OverrideTasksData Create(SimpleRoleInfo roleInfo, int idOffset, OptionItem option = null, CustomRoles setRole = CustomRoles.NotAssigned)
        {
            bool addRoleName = false;
            if (setRole == CustomRoles.NotAssigned) setRole = roleInfo.RoleName;
            else addRoleName = true;

            return new OverrideTasksData(roleInfo.ConfigId + idOffset, roleInfo.Tab, setRole, option, addRoleName);
        }
    }

    public enum offsetId
    {
        Main = 0,
        Text = 100,
        GM = 500,

        //Unit
        UnitSpecial = 1000,
        UnitImp = 2000,
        UnitMad = 3000,
        UnitCrew = 4000,
        UnitNeu = 5000,
        UnitMix = 6000,

        // Impostor
        ImpSpecial = 10000,
        ImpDefault = 11000,
        ImpTOH = 12000,
        ImpY = 20000,

        // Madmate
        MadSpecial = 28000,
        MadTOH = 28500,
        MadY = 29000,

        // Crewmate
        CrewSpecial = 30000,
        CrewDefault = 31000,
        CrewSheriff = 32000,
        CrewTOH = 33000,
        CrewY = 40000,

        // Neutral
        NeuSpecial = 50000,
        NeuJackal = 51000,
        NeuFox = 51800,
        NeuTOH = 52000,
        NeuY = 60000,

        // Addon
        AddonSpecial = 70000,
        AddonImp = 72000,
        AddonMad = 74000,
        AddonCrew = 76000,
        AddonNeu = 78000,
        AddonBuff = 80000,
        AddonDebuff = 85000,

        // Feature
        FeatNonDisplay = 90000,

        FeatSpecial = 100000,
        FeatMap = 105000,
        FeatSabotage = 110000,
        FeatMeeting = 115000,
        FeatRevenge = 120000,
        FeatTask = 125000,
        FeatGhost = 130000,
        FeatOther = 135000,
        GModeAdd = 140000,
        System = 145000,
        Participation = 150000,

        // GameMode
        GModeHaS = 200000,
        GModeCC = 210000,
        GModeON = 220000,
    }
}
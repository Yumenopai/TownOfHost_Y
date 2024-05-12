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
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.AddOns.Impostor;
using TownOfHostY.Roles.AddOns.Crewmate;
using TownOfHostY.CatchCat;

namespace TownOfHostY;

[Flags]
public enum CustomGameMode
{
    Standard,
    HideAndSeek,
    CatchCat,
    //OneNight,
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
        "Standard", "HideAndSeek", "CatchCat",/* "OneNight",*/
    };

    // MapActive
    public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || Main.NormalOptions.MapId == 0;
    public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || Main.NormalOptions.MapId == 1;
    public static bool IsActivePolus => AddedPolus.GetBool() || Main.NormalOptions.MapId == 2;
    public static bool IsActiveAirship => AddedTheAirShip.GetBool() || Main.NormalOptions.MapId == 4;
    public static bool IsActiveFungle => AddedTheFungle.GetBool() || Main.NormalOptions.MapId == 5;

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
    public static OptionItem DisableFungleDevices;
    public static OptionItem DisableFungleVital;
    //public static OptionItem DisableFungleTelescope;
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
    public static OptionItem AddedTheFungle;

    // ランダムスポーン
    public static OptionItem RandomSpawn;
    public static OptionItem AdditionalSpawn;
    public static OptionItem DisableNearButton;
    public static OptionItem FirstFixedSpawn;

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
    public static OptionItem FungleReactorTimeLimit;
    public static OptionItem FungleMushroomMixupDuration;

    // サボタージュのクールダウン変更
    public static OptionItem ModifySabotageCooldown;
    public static OptionItem SabotageCooldown;

    // 停電の特殊設定
    public static OptionItem LightsOutSpecialSettings;
    public static OptionItem DisableAirshipViewingDeckLightsPanel;
    public static OptionItem DisableAirshipGapRoomLightsPanel;
    public static OptionItem DisableAirshipCargoLightsPanel;
    public static OptionItem BlockDisturbancesToSwitches;
    // キノコカオスサボ時のボタン無効
    public static OptionItem DisableButtonInMushroomMixup;

    // マップ改造
    private static OptionItem MapModificationAirship;
    private static OptionItem MapModificationFungle;

    public static OptionItem AirShipVariableElectrical;
    public static OptionItem DisableAirshipMovingPlatform;
    public static OptionItem ResetDoorsEveryTurns;
    public static OptionItem DoorsResetMode;
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
    public static OptionItem ForceProtect;
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
        OptionSaver.Initialize();

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
        CustomRoleCounts = new();
        CustomRoleSpawnChances = new();

        var sortedRoleInfo = CustomRoleManager.AllRolesInfo.Values.OrderBy(role => role.ConfigId);
        // GM
        EnableGM = BooleanOptionItem.Create((int)offsetId.GM, "GM", false, TabGroup.MainSettings, false)
            .SetColor(new Color32(255, 91, 112, 255))
            .SetHeader(true)
            .SetGameMode(CustomGameMode.All);

        // SpecialEvent
        if (Main.IsAprilFool)
        {
            Potentialist.SetupRoleOptions();
            Potentialist.RoleInfo.OptionCreator?.Invoke();
        }

        sortedRoleInfo.Where(role => !role.RoleName.IsDontShowOptionRole()).Do(info =>
        {
            SetupRoleOptions(info);
            info.OptionCreator?.Invoke();
        });

        TextOptionItem.Create((int)offsetId.Text + 0, "Head.CommonImpostor", TabGroup.ImpostorRoles);
        DefaultShapeshiftCooldown = FloatOptionItem.Create((int)offsetId.FeatNonDisplay + 1000, "DefaultShapeshiftCooldown", new(5f, 999f, 5f), 15f, TabGroup.ImpostorRoles, false)
            .SetValueFormat(OptionFormat.Seconds);
        ImpostorOperateVisibility = BooleanOptionItem.Create((int)offsetId.FeatNonDisplay + 1010, "ImpostorOperateVisibility", false, TabGroup.ImpostorRoles, false);

        // Madmate
        CanMakeMadmateCount = IntegerOptionItem.Create((int)offsetId.MadTOH + 400, "CanMakeMadmateCount", new(0, 15, 1), 0, TabGroup.MadmateRoles, false)
            .SetColor(Palette.ImpostorRed)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Players);
        MadmateCanFixLightsOut = BooleanOptionItem.Create((int)offsetId.MadTOH + 410, "MadmateCanFixLightsOut", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateCanFixComms = BooleanOptionItem.Create((int)offsetId.MadTOH + 411, "MadmateCanFixComms", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateHasImpostorVision = BooleanOptionItem.Create((int)offsetId.MadTOH + 412, "MadmateHasImpostorVision", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateCanSeeKillFlash = BooleanOptionItem.Create((int)offsetId.MadTOH + 413, "MadmateCanSeeKillFlash", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateCanSeeOtherVotes = BooleanOptionItem.Create((int)offsetId.MadTOH + 414, "MadmateCanSeeOtherVotes", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateCanSeeDeathReason = BooleanOptionItem.Create((int)offsetId.MadTOH + 415, "MadmateCanSeeDeathReason", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);
        MadmateRevengeCrewmate = BooleanOptionItem.Create((int)offsetId.MadTOH + 416, "MadmateExileCrewmate", false, TabGroup.MadmateRoles, false).SetParent(CanMakeMadmateCount).SetGameMode(CustomGameMode.Standard);

        TextOptionItem.Create((int)offsetId.Text + 1, "Head.CommonMadmate", TabGroup.MadmateRoles);
        MadmateVentCooldown = FloatOptionItem.Create((int)offsetId.FeatNonDisplay + 2000, "MadmateVentCooldown", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false)
            .SetValueFormat(OptionFormat.Seconds);
        MadmateVentMaxTime = FloatOptionItem.Create((int)offsetId.FeatNonDisplay + 2010, "MadmateVentMaxTime", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false)
            .SetValueFormat(OptionFormat.Seconds);

        // Add-Ons
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

        TextOptionItem.Create((int)offsetId.Text + 14, "Head.DebuffAddOn", TabGroup.Addons).SetColor(Palette.Purple);
        Sunglasses.SetupCustomOption();
        Clumsy.SetupCustomOption();
        InfoPoor.SetupCustomOption();
        NonReport.SetupCustomOption();
        #endregion

        RoleAssigningAlgorithm = StringOptionItem.Create((int)offsetId.FeatSpecial + 100, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 0, TabGroup.MainSettings, true)
            .RegisterUpdateValueEvent((object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue))
            .SetGameMode(CustomGameMode.All)
            .SetHeader(true);
        RoleAssignManager.SetupOptionItem();
        HideGameSettings = BooleanOptionItem.Create((int)offsetId.FeatSpecial + 300, "HideGameSettings", false, TabGroup.MainSettings, true)
            .SetColor(Color.gray);

        // HideAndSeek
        /********************************************************************************/
        SetupRoleOptions((int)offsetId.GModeHaS + 1000, TabGroup.MainSettings, CustomRoles.HASFox, customGameMode: CustomGameMode.HideAndSeek);
        SetupRoleOptions((int)offsetId.GModeHaS + 1100, TabGroup.MainSettings, CustomRoles.HASTroll, customGameMode: CustomGameMode.HideAndSeek);

        AllowCloseDoors = BooleanOptionItem.Create((int)offsetId.GModeHaS + 5000, "AllowCloseDoors", false, TabGroup.MainSettings, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.HideAndSeek);
        KillDelay = FloatOptionItem.Create((int)offsetId.GModeHaS + 5001, "HideAndSeekWaitingTime", new(0f, 180f, 5f), 10f, TabGroup.MainSettings, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.HideAndSeek);
        IgnoreVent = BooleanOptionItem.Create((int)offsetId.GModeHaS + 5002, "IgnoreVent", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.HideAndSeek);
        /********************************************************************************/

        // CC
        CatchCat.Option.SetupCustomOption();

        TextOptionItem.Create((int)offsetId.FeatMap, "Head.Map", TabGroup.MainSettings).SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        // ランダムスポーン
        RandomSpawn = BooleanOptionItem.Create((int)offsetId.FeatMap + 100, "RandomSpawn", false, TabGroup.MainSettings, false)
            .SetColor(Color.yellow)
            .SetGameMode(CustomGameMode.All);
        AdditionalSpawn = BooleanOptionItem.Create((int)offsetId.FeatMap + 110, "AdditionalSpawn", false, TabGroup.MainSettings, false).SetParent(RandomSpawn)
            .SetGameMode(CustomGameMode.All);
        DisableNearButton = BooleanOptionItem.Create((int)offsetId.FeatMap + 120, "DisableNearButton", false, TabGroup.MainSettings, false).SetParent(RandomSpawn)
            .SetGameMode(CustomGameMode.All);
        FirstFixedSpawn = BooleanOptionItem.Create((int)offsetId.FeatMap + 130, "FirstFixedSpawn", true, TabGroup.MainSettings, false).SetParent(RandomSpawn)
            .SetGameMode(CustomGameMode.All);

        // デバイス無効化
        DisableDevices = BooleanOptionItem.Create((int)offsetId.FeatMap + 200, "DisableDevices", false, TabGroup.MainSettings, false)
            .SetColor(Color.yellow)
            .SetGameMode(CustomGameMode.All);
        DisableSkeldDevices = BooleanOptionItem.Create((int)offsetId.FeatMap + 210, "DisableSkeldDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
            .SetColor(Color.gray).SetGameMode(CustomGameMode.All);
        DisableSkeldAdmin = BooleanOptionItem.Create((int)offsetId.FeatMap + 211, "DisableSkeldAdmin", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices).SetGameMode(CustomGameMode.All);
        DisableSkeldCamera = BooleanOptionItem.Create((int)offsetId.FeatMap + 212, "DisableSkeldCamera", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices).SetGameMode(CustomGameMode.All);
        DisableMiraHQDevices = BooleanOptionItem.Create((int)offsetId.FeatMap + 220, "DisableMiraHQDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
            .SetColor(Color.gray).SetGameMode(CustomGameMode.All);
        DisableMiraHQAdmin = BooleanOptionItem.Create((int)offsetId.FeatMap + 221, "DisableMiraHQAdmin", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices).SetGameMode(CustomGameMode.All);
        DisableMiraHQDoorLog = BooleanOptionItem.Create((int)offsetId.FeatMap + 222, "DisableMiraHQDoorLog", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices).SetGameMode(CustomGameMode.All);
        DisablePolusDevices = BooleanOptionItem.Create((int)offsetId.FeatMap + 230, "DisablePolusDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
            .SetColor(Color.gray).SetGameMode(CustomGameMode.All);
        DisablePolusAdmin = BooleanOptionItem.Create((int)offsetId.FeatMap + 231, "DisablePolusAdmin", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices).SetGameMode(CustomGameMode.All);
        DisablePolusCamera = BooleanOptionItem.Create((int)offsetId.FeatMap + 232, "DisablePolusCamera", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices).SetGameMode(CustomGameMode.All);
        DisablePolusVital = BooleanOptionItem.Create((int)offsetId.FeatMap + 233, "DisablePolusVital", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices).SetGameMode(CustomGameMode.All);
        DisableAirshipDevices = BooleanOptionItem.Create((int)offsetId.FeatMap + 240, "DisableAirshipDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
            .SetColor(Color.gray).SetGameMode(CustomGameMode.All);
        DisableAirshipCockpitAdmin = BooleanOptionItem.Create((int)offsetId.FeatMap + 241, "DisableAirshipCockpitAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices).SetGameMode(CustomGameMode.All);
        DisableAirshipRecordsAdmin = BooleanOptionItem.Create((int)offsetId.FeatMap + 242, "DisableAirshipRecordsAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices).SetGameMode(CustomGameMode.All);
        DisableAirshipCamera = BooleanOptionItem.Create((int)offsetId.FeatMap + 243, "DisableAirshipCamera", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices).SetGameMode(CustomGameMode.All);
        DisableAirshipVital = BooleanOptionItem.Create((int)offsetId.FeatMap + 244, "DisableAirshipVital", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices).SetGameMode(CustomGameMode.All);
        DisableFungleDevices = BooleanOptionItem.Create((int)offsetId.FeatMap + 250, "DisableFungleDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
            .SetColor(Color.gray).SetGameMode(CustomGameMode.All);
        DisableFungleVital = BooleanOptionItem.Create((int)offsetId.FeatMap + 251, "DisableFungleVital", false, TabGroup.MainSettings, false).SetParent(DisableFungleDevices).SetGameMode(CustomGameMode.All);
        //DisableFungleTelescope = BooleanOptionItem.Create((int)offsetId.FeatMap + 252, "DisableFungleTelescope", false, TabGroup.MainSettings, false).SetParent(DisableFungleDevices).SetGameMode(CustomGameMode.All);
        DisableDevicesIgnoreConditions = BooleanOptionItem.Create((int)offsetId.FeatMap + 290, "IgnoreConditions", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
            .SetColor(Color.gray);
        DisableDevicesIgnoreImpostors = BooleanOptionItem.Create((int)offsetId.FeatMap + 291, "IgnoreImpostors", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);
        DisableDevicesIgnoreMadmates = BooleanOptionItem.Create((int)offsetId.FeatMap + 292, "IgnoreMadmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);
        DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create((int)offsetId.FeatMap + 293, "IgnoreNeutrals", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);
        DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create((int)offsetId.FeatMap + 294, "IgnoreCrewmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);
        DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create((int)offsetId.FeatMap + 295, "IgnoreAfterAnyoneDied", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions);

        // ランダムマップ
        RandomMapsMode = BooleanOptionItem.Create((int)offsetId.FeatMap + 300, "RandomMapsMode", false, TabGroup.MainSettings, false)
            .SetColor(Color.yellow)
            .SetGameMode(CustomGameMode.All);
        AddedTheSkeld = BooleanOptionItem.Create((int)offsetId.FeatMap + 310, "AddedTheSkeld", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);
        AddedMiraHQ = BooleanOptionItem.Create((int)offsetId.FeatMap + 320, "AddedMIRAHQ", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);
        AddedPolus = BooleanOptionItem.Create((int)offsetId.FeatMap + 330, "AddedPolus", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);
        AddedTheAirShip = BooleanOptionItem.Create((int)offsetId.FeatMap + 340, "AddedTheAirShip", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);
        AddedTheFungle = BooleanOptionItem.Create((int)offsetId.FeatMap + 350, "AddedTheFungle", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode).SetGameMode(CustomGameMode.All);

        // マップ改造
        ResetDoorsEveryTurns = BooleanOptionItem.Create((int)offsetId.FeatMap + 400, "ResetDoorsEveryTurns", false, TabGroup.MainSettings, false)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        DoorsResetMode = StringOptionItem.Create((int)offsetId.FeatMap + 401, "DoorsResetMode", EnumHelper.GetAllNames<DoorsReset.ResetMode>(), 0, TabGroup.MainSettings, false).SetParent(ResetDoorsEveryTurns).SetGameMode(CustomGameMode.All);

        MapModificationAirship = BooleanOptionItem.Create((int)offsetId.FeatMap + 500, "MapModificationAirship", false, TabGroup.MainSettings, false)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        AirShipVariableElectrical = BooleanOptionItem.Create((int)offsetId.FeatMap + 501, "AirShipVariableElectrical", false, TabGroup.MainSettings, false).SetParent(MapModificationAirship)
            .SetGameMode(CustomGameMode.All);
        DisableAirshipMovingPlatform = BooleanOptionItem.Create((int)offsetId.FeatMap + 502, "DisableAirshipMovingPlatform", false, TabGroup.MainSettings, false).SetParent(MapModificationAirship)
            .SetGameMode(CustomGameMode.All);

        MapModificationFungle = BooleanOptionItem.Create((int)offsetId.FeatMap + 600, "MapModificationFungle", false, TabGroup.MainSettings, false)
            .SetColor(Color.yellow).SetGameMode(CustomGameMode.All);
        FungleCanSporeTrigger = BooleanOptionItem.Create((int)offsetId.FeatMap + 610, "FungleCanSporeTrigger", false, TabGroup.MainSettings, false).SetParent(MapModificationFungle)
            .SetGameMode(CustomGameMode.All);
        FungleCanUseZipline = BooleanOptionItem.Create((int)offsetId.FeatMap + 620, "FungleCanUseZipline", false, TabGroup.MainSettings, false).SetParent(MapModificationFungle)
            .SetColor(Color.gray).SetGameMode(CustomGameMode.All);
        FungleCanUseZiplineFromTop = BooleanOptionItem.Create((int)offsetId.FeatMap + 621, "FungleCanUseZiplineFromTop", false, TabGroup.MainSettings, false).SetParent(FungleCanUseZipline).SetGameMode(CustomGameMode.All);
        FungleCanUseZiplineFromUnder = BooleanOptionItem.Create((int)offsetId.FeatMap + 622, "FungleCanUseZiplineFromUnder", true, TabGroup.MainSettings, false).SetParent(FungleCanUseZipline).SetGameMode(CustomGameMode.All);

        TextOptionItem.Create((int)offsetId.FeatSabotage, "Head.Sabotage", TabGroup.MainSettings).SetColor(Color.magenta).SetGameMode(CustomGameMode.All);
        // リアクターの時間制御
        SabotageTimeControl = BooleanOptionItem.Create((int)offsetId.FeatSabotage + 100, "SabotageTimeControl", false, TabGroup.MainSettings, false)
            .SetColor(Color.magenta)
            .SetGameMode(CustomGameMode.All);
        PolusReactorTimeLimit = FloatOptionItem.Create((int)offsetId.FeatSabotage + 101, "PolusReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.All);
        AirshipReactorTimeLimit = FloatOptionItem.Create((int)offsetId.FeatSabotage + 102, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.All);
        FungleReactorTimeLimit = FloatOptionItem.Create((int)offsetId.FeatSabotage + 103, "FungleReactorTimeLimit", new(1f, 60f, 1f), 50f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.All);
        FungleMushroomMixupDuration = FloatOptionItem.Create((int)offsetId.FeatSabotage + 104, "FungleMushroomMixupDuration", new(1f, 20f, 1f), 10f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard).SetGameMode(CustomGameMode.All);

        // サボタージュのクールダウン変更
        ModifySabotageCooldown = BooleanOptionItem.Create((int)offsetId.FeatSabotage + 200, "ModifySabotageCooldown", false, TabGroup.MainSettings, false)
            .SetColor(Color.magenta).SetGameMode(CustomGameMode.All);
        SabotageCooldown = FloatOptionItem.Create((int)offsetId.FeatSabotage + 201, "SabotageCooldown", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(ModifySabotageCooldown)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.All);

        // 停電の特殊設定
        LightsOutSpecialSettings = BooleanOptionItem.Create((int)offsetId.FeatSabotage + 300, "LightsOutSpecialSettings", false, TabGroup.MainSettings, false)
            .SetColor(Color.magenta).SetGameMode(CustomGameMode.All);
        DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create((int)offsetId.FeatSabotage + 301, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings).SetGameMode(CustomGameMode.All);
        DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create((int)offsetId.FeatSabotage + 302, "DisableAirshipGapRoomLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings).SetGameMode(CustomGameMode.All);
        DisableAirshipCargoLightsPanel = BooleanOptionItem.Create((int)offsetId.FeatSabotage + 303, "DisableAirshipCargoLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings).SetGameMode(CustomGameMode.All);
        BlockDisturbancesToSwitches = BooleanOptionItem.Create((int)offsetId.FeatSabotage + 304, "BlockDisturbancesToSwitches", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings).SetGameMode(CustomGameMode.All);

        // コミュサボカモフラージュ
        CommsCamouflage = BooleanOptionItem.Create((int)offsetId.FeatSabotage + 400, "CommsCamouflage", false, TabGroup.MainSettings, false)
            .SetColor(Color.magenta)
            .SetGameMode(CustomGameMode.All);

        // キノコカオスサボ時のボタン無効
        DisableButtonInMushroomMixup = BooleanOptionItem.Create((int)offsetId.FeatSabotage + 500, "DisableButtonInMushroomMixup", false, TabGroup.MainSettings, false)
            .SetColor(Color.magenta).SetGameMode(CustomGameMode.All);

        TextOptionItem.Create((int)offsetId.FeatMeeting, "Head.Meeting", TabGroup.MainSettings).SetColor(Color.cyan).SetGameMode(CustomGameMode.All);
        // 会議収集理由表示
        ShowReportReason = BooleanOptionItem.Create((int)offsetId.FeatMeeting + 100, "ShowReportReason", true, TabGroup.MainSettings, true)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.All);

        // 初手会議に役職名表示
        ShowRoleInfoAtFirstMeeting = BooleanOptionItem.Create((int)offsetId.FeatMeeting + 200, "ShowRoleInfoAtFirstMeeting", true, TabGroup.MainSettings, true)
            .SetColor(Color.cyan);

        // ボタン回数同期
        SyncButtonMode = BooleanOptionItem.Create((int)offsetId.FeatMeeting + 300, "SyncButtonMode", false, TabGroup.MainSettings, false)
            .SetColor(Color.cyan);
        SyncedButtonCount = IntegerOptionItem.Create((int)offsetId.FeatMeeting + 301, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.MainSettings, false).SetParent(SyncButtonMode)
            .SetValueFormat(OptionFormat.Times);

        // 投票モード
        VoteMode = BooleanOptionItem.Create((int)offsetId.FeatMeeting + 400, "VoteMode", false, TabGroup.MainSettings, false)
            .SetColor(Color.cyan)
            .SetGameMode(CustomGameMode.All);
        WhenSkipVote = StringOptionItem.Create((int)offsetId.FeatMeeting + 410, "WhenSkipVote", voteModes[0..3], 0, TabGroup.MainSettings, false).SetParent(VoteMode);
        WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create((int)offsetId.FeatMeeting + 411, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote);
        WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create((int)offsetId.FeatMeeting + 412, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote);
        WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create((int)offsetId.FeatMeeting + 413, "WhenSkipVoteIgnoreEmergency", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote);
        WhenNonVote = StringOptionItem.Create((int)offsetId.FeatMeeting + 420, "WhenNonVote", voteModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
            .SetGameMode(CustomGameMode.All);
        WhenTie = StringOptionItem.Create((int)offsetId.FeatMeeting + 430, "WhenTie", tieModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode);

        // 全員生存時の会議時間
        AllAliveMeeting = BooleanOptionItem.Create((int)offsetId.FeatMeeting + 500, "AllAliveMeeting", false, TabGroup.MainSettings, false)
            .SetColor(Color.cyan);
        AllAliveMeetingTime = FloatOptionItem.Create((int)offsetId.FeatMeeting + 501, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.MainSettings, false).SetParent(AllAliveMeeting)
            .SetValueFormat(OptionFormat.Seconds);

        // 生存人数ごとの緊急会議
        AdditionalEmergencyCooldown = BooleanOptionItem.Create((int)offsetId.FeatMeeting + 600, "AdditionalEmergencyCooldown", false, TabGroup.MainSettings, false)
            .SetColor(Color.cyan);
        AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create((int)offsetId.FeatMeeting + 601, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
            .SetValueFormat(OptionFormat.Players);
        AdditionalEmergencyCooldownTime = FloatOptionItem.Create((int)offsetId.FeatMeeting + 602, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
            .SetValueFormat(OptionFormat.Seconds);

        TextOptionItem.Create((int)offsetId.FeatRevenge, "Head.Revenge", TabGroup.MainSettings).SetColor(Palette.Orange).SetGameMode(CustomGameMode.Standard);
        // 道連れ人表記
        ShowRevengeTarget = BooleanOptionItem.Create((int)offsetId.FeatRevenge + 100, "ShowRevengeTarget", true, TabGroup.MainSettings, true)
            .SetColor(Color.cyan);
        RevengeImpostorByImpostor = BooleanOptionItem.Create((int)offsetId.FeatRevenge + 200, "RevengeImpostorByImpostor", false, TabGroup.MainSettings, true)
            .SetColor(Palette.ImpostorRed);
        RevengeMadByImpostor = BooleanOptionItem.Create((int)offsetId.FeatRevenge + 250, "RevengeMadByImpostor", false, TabGroup.MainSettings, true)
            .SetColor(Palette.ImpostorRed);
        RevengeNeutral = BooleanOptionItem.Create((int)offsetId.FeatRevenge + 300, "RevengeNeutral", true, TabGroup.MainSettings, true)
            .SetColor(Palette.Orange);

        TextOptionItem.Create((int)offsetId.FeatTask, "Head.Task", TabGroup.MainSettings).SetColor(Color.green).SetGameMode(CustomGameMode.All);
        // タスク無効化
        DisableTasks = BooleanOptionItem.Create((int)offsetId.FeatTask + 100, "DisableTasks", false, TabGroup.MainSettings, false)
            .SetColor(Color.green)
            .SetGameMode(CustomGameMode.All);
        DisableSwipeCard = BooleanOptionItem.Create((int)offsetId.FeatTask + 101, "DisableSwipeCardTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableSubmitScan = BooleanOptionItem.Create((int)offsetId.FeatTask + 102, "DisableSubmitScanTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableUnlockSafe = BooleanOptionItem.Create((int)offsetId.FeatTask + 103, "DisableUnlockSafeTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableUploadData = BooleanOptionItem.Create((int)offsetId.FeatTask + 104, "DisableUploadDataTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableStartReactor = BooleanOptionItem.Create((int)offsetId.FeatTask + 105, "DisableStartReactorTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableResetBreaker = BooleanOptionItem.Create((int)offsetId.FeatTask + 106, "DisableResetBreakerTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableRewindTapes = BooleanOptionItem.Create((int)offsetId.FeatTask + 107, "DisableRewindTapes", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableVentCleaning = BooleanOptionItem.Create((int)offsetId.FeatTask + 108, "DisableVentCleaning", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableBuildSandcastle = BooleanOptionItem.Create((int)offsetId.FeatTask + 109, "DisableBuildSandcastle", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableTestFrisbee = BooleanOptionItem.Create((int)offsetId.FeatTask + 110, "DisableTestFrisbee", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableWaterPlants = BooleanOptionItem.Create((int)offsetId.FeatTask + 111, "DisableWaterPlants", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableCatchFish = BooleanOptionItem.Create((int)offsetId.FeatTask + 112, "DisableCatchFish", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableHelpCritter = BooleanOptionItem.Create((int)offsetId.FeatTask + 113, "DisableHelpCritter", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableTuneRadio = BooleanOptionItem.Create((int)offsetId.FeatTask + 114, "DisableTuneRadio", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);
        DisableAssembleArtifact = BooleanOptionItem.Create((int)offsetId.FeatTask + 115, "DisableAssembleArtifact", false, TabGroup.MainSettings, false).SetParent(DisableTasks).SetGameMode(CustomGameMode.All);

        // タスク勝利無効化
        DisableTaskWin = BooleanOptionItem.Create((int)offsetId.FeatTask + 200, "DisableTaskWin", false, TabGroup.MainSettings, false)
            .SetColor(Color.green);
        //ホストの死後タスク免除
        HostGhostIgnoreTasks = BooleanOptionItem.Create((int)offsetId.FeatTask + 300, "HostGhostIgnoreTasks", false, TabGroup.MainSettings, true)
            .SetColor(Color.green);
        //タスク免除
        GhostIgnoreTasks = BooleanOptionItem.Create((int)offsetId.FeatGhost + 100, "GhostIgnoreTasks", false, TabGroup.MainSettings, true)
            .SetColor(Color.green);

        TextOptionItem.Create((int)offsetId.FeatGhost, "Head.Ghost", TabGroup.MainSettings).SetColor(Palette.LightBlue).SetGameMode(CustomGameMode.All);
        // 幽霊
        GhostCantSeeOtherRoles = BooleanOptionItem.Create((int)offsetId.FeatGhost + 200, "GhostCantSeeOtherRoles", false, TabGroup.MainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);
        GhostCantSeeOtherTasks = BooleanOptionItem.Create((int)offsetId.FeatGhost + 300, "GhostCantSeeOtherTasks", false, TabGroup.MainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);
        GhostCantSeeOtherVotes = BooleanOptionItem.Create((int)offsetId.FeatGhost + 400, "GhostCantSeeOtherVotes", false, TabGroup.MainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);
        GhostCanSeeOtherTeams = BooleanOptionItem.Create((int)offsetId.FeatGhost + 600, "GhostCanSeeOtherTeams", false, TabGroup.MainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);
        GhostCanSeeDeathReason = BooleanOptionItem.Create((int)offsetId.FeatGhost + 500, "GhostCanSeeDeathReason", false, TabGroup.MainSettings, true)
            .SetColor(Palette.LightBlue)
            .SetGameMode(CustomGameMode.All);

        TextOptionItem.Create((int)offsetId.FeatOther, "Head.Other", TabGroup.MainSettings).SetColor(Palette.CrewmateBlue).SetGameMode(CustomGameMode.All);
        KillFlashDuration = FloatOptionItem.Create((int)offsetId.FeatOther + 100, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.MainSettings, true)
            .SetColor(Palette.ImpostorRed)
            .SetValueFormat(OptionFormat.Seconds);
        // 強制守護天使表示
        ForceProtect = BooleanOptionItem.Create((int)offsetId.FeatOther + 600, "ForceProtect", false, TabGroup.MainSettings, true)
            .SetColor(Palette.CrewmateBlue);
        // CO可否表示(id+499まで使用)
        DisplayComingOut.SetupCustomOption((int)offsetId.FeatOther + 700);
        // 陣営マーク表示
        DisplayTeamMark = BooleanOptionItem.Create((int)offsetId.FeatOther + 1200, "DisplayTeamMark", false, TabGroup.MainSettings, true)
            .SetColor(Palette.CrewmateBlue);
        // 初手キルクール調整
        FixFirstKillCooldown = BooleanOptionItem.Create((int)offsetId.FeatOther + 200, "FixFirstKillCooldown", false, TabGroup.MainSettings, false)
            .SetColor(Palette.CrewmateBlue);

        // 転落死
        LadderDeath = BooleanOptionItem.Create((int)offsetId.FeatOther + 300, "LadderDeath", false, TabGroup.MainSettings, false)
            .SetColor(Palette.CrewmateBlue)
            .SetGameMode(CustomGameMode.All);
        LadderDeathChance = StringOptionItem.Create((int)offsetId.FeatOther + 301, "LadderDeathChance", rates[1..], 0, TabGroup.MainSettings, false).SetParent(LadderDeath).SetGameMode(CustomGameMode.All);

        // スキン設定
        SkinControle = BooleanOptionItem.Create((int)offsetId.FeatOther + 400, "SkinControle", false, TabGroup.MainSettings, true)
            .SetColor(Palette.CrewmateBlue)
            .SetGameMode(CustomGameMode.All);
        NoHat = BooleanOptionItem.Create((int)offsetId.FeatOther + 401, "NoHat", false, TabGroup.MainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoFullFaceHat = BooleanOptionItem.Create((int)offsetId.FeatOther + 402, "NoFullFaceHat", false, TabGroup.MainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoSkin = BooleanOptionItem.Create((int)offsetId.FeatOther + 403, "NoSkin", false, TabGroup.MainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoVisor = BooleanOptionItem.Create((int)offsetId.FeatOther + 404, "NoVisor", false, TabGroup.MainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoPet = BooleanOptionItem.Create((int)offsetId.FeatOther + 405, "NoPet", false, TabGroup.MainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoDuplicateHat = BooleanOptionItem.Create((int)offsetId.FeatOther + 410, "NoDuplicateHat", false, TabGroup.MainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        NoDuplicateSkin = BooleanOptionItem.Create((int)offsetId.FeatOther + 411, "NoDuplicateSkin", false, TabGroup.MainSettings, true).SetParent(SkinControle).SetGameMode(CustomGameMode.All);
        VoiceReader.SetupCustomOption((int)Options.offsetId.FeatOther + 500);

        TextOptionItem.Create((int)offsetId.GModeAdd, "Head.GameMode", TabGroup.MainSettings).SetColor(Color.yellow).SetGameMode(CustomGameMode.Standard);
        // シンクロカラーモード100

        // 通常モードでかくれんぼ用
        StandardHAS = BooleanOptionItem.Create((int)offsetId.GModeAdd + 200, "StandardHAS", false, TabGroup.MainSettings, false)
            //上記載時にheader消去
            .SetColor(Color.yellow)
            .SetGameMode(CustomGameMode.Standard);
        StandardHASWaitingTime = FloatOptionItem.Create((int)offsetId.GModeAdd + 201, "StandardHASWaitingTime", new(0f, 180f, 2.5f), 10f, TabGroup.MainSettings, false).SetParent(StandardHAS)
            .SetValueFormat(OptionFormat.Seconds).SetGameMode(CustomGameMode.Standard);

        // その他
        TextOptionItem.Create((int)offsetId.System, "Head.System", TabGroup.MainSettings).SetColor(Color.blue).SetGameMode(CustomGameMode.All);
        NoGameEnd = BooleanOptionItem.Create((int)offsetId.System + 100, "NoGameEnd", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.All);
        AutoDisplayLastResult = BooleanOptionItem.Create((int)offsetId.System + 200, "AutoDisplayLastResult", true, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
        AutoDisplayKillLog = BooleanOptionItem.Create((int)offsetId.System + 300, "AutoDisplayKillLog", true, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
        SuffixMode = StringOptionItem.Create((int)offsetId.System + 400, "SuffixMode", suffixModes, 0, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
        NameChangeMode = StringOptionItem.Create((int)offsetId.System + 500, "NameChangeMode", nameChangeModes, 0, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
        ChangeNameToRoleInfo = BooleanOptionItem.Create((int)offsetId.System + 600, "ChangeNameToRoleInfo", true, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
        AddonShow = StringOptionItem.Create((int)offsetId.System + 700, "AddonShowMode", addonShowModes, 0, TabGroup.MainSettings, true);
        ChangeIntro = BooleanOptionItem.Create((int)offsetId.System + 800, "ChangeIntro", false, TabGroup.MainSettings, true);

        TextOptionItem.Create((int)offsetId.Participation, "Head.Participation", TabGroup.MainSettings).SetColor(Palette.Purple).SetGameMode(CustomGameMode.All);
        ApplyDenyNameList = BooleanOptionItem.Create((int)offsetId.Participation + 100, "ApplyDenyNameList", true, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
        KickPlayerFriendCodeNotExist = BooleanOptionItem.Create((int)offsetId.Participation + 200, "KickPlayerFriendCodeNotExist", false, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
        ApplyBanList = BooleanOptionItem.Create((int)offsetId.Participation + 300, "ApplyBanList", true, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
        AntiCheat = BooleanOptionItem.Create((int)offsetId.Participation + 400, "AntiCheat", false, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
        CheaterAutoBan = BooleanOptionItem.Create((int)offsetId.Participation + 410, "CheaterAutoBan", false, TabGroup.MainSettings, true).SetParent(AntiCheat)
            .SetGameMode(CustomGameMode.All);
        CheatLobbyKill = BooleanOptionItem.Create((int)offsetId.Participation + 420, "CheatLobbyKill", false, TabGroup.MainSettings, true).SetParent(AntiCheat)
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

        var spawnOption = IntegerOptionItem.Create(id, role.ToString(), new(0, 100, 10), 0, tab, false)
            .SetColor(Utils.GetRoleColor(role))
            .SetValueFormat(OptionFormat.Percent)
            .SetHeader(true)
            .SetGameMode(customGameMode) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", assignCountRule, assignCountRule.Step, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(role.IsPairRole() ? OptionFormat.Pair: OptionFormat.Players)
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
            AddOnBuffAssign[PlayerRole] = BooleanOptionItem.Create(Id, "AddOnBuffAssign", false, tab, false).SetParent(CustomRoleSpawnChances[parentRole]);
        }
        else
        {
            var roleName = Utils.GetRoleName(PlayerRole);
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(PlayerRole), roleName) } };
            AddOnBuffAssign[PlayerRole] = BooleanOptionItem.Create(Id, "AddOnBuffAssign%role%", false, tab, false).SetParent(CustomRoleSpawnChances[parentRole]);
            AddOnBuffAssign[PlayerRole].ReplacementDictionary = replacementDic;
        }
        Id += 10;
        foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsBuffAddOn()))
        {
            if (Addon == CustomRoles.Loyalty && PlayerRole is
                CustomRoles.CustomImpostor or CustomRoles.CustomCrewmate or
                CustomRoles.MadSnitch or CustomRoles.MadDilemma or CustomRoles.Jackal or CustomRoles.JClient or
                CustomRoles.LastImpostor or CustomRoles.CompleteCrew) continue;
            if (Addon == CustomRoles.Revenger && PlayerRole is CustomRoles.MadNimrod) continue;

            SetUpAddOnRoleOption(PlayerRole, tab, Addon, Id, false, AddOnBuffAssign[PlayerRole]);
            Id++;
        }

        if (!addRoleName)
        {
            AddOnDebuffAssign[PlayerRole] = BooleanOptionItem.Create(Id, "AddOnDebuffAssign", false, tab, false).SetParent(CustomRoleSpawnChances[parentRole]);
        }
        else
        {
            var roleName = Utils.GetRoleName(PlayerRole);
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(PlayerRole), roleName) } };
            AddOnDebuffAssign[PlayerRole] = BooleanOptionItem.Create(Id, "AddOnDebuffAssign%role%", false, tab, false).SetParent(CustomRoleSpawnChances[parentRole]);
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
        if (parent == null) parent = CustomRoleSpawnChances[PlayerRole];
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role)) + "  " + Utils.GetAddonAbilityInfo(role) } };
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

        public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role, OptionItem option = null, bool addRoleName = false)
        {
            this.IdStart = idStart;
            this.Role = role;

            if(option == null) option = CustomRoleSpawnChances[role];
            if (!addRoleName)
            {
                doOverride = BooleanOptionItem.Create(idStart++, "doOverride", false, tab, false).SetParent(option)
                    .SetValueFormat(OptionFormat.None);
            }
            else
            {
                var roleName = Utils.GetRoleName(role);
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
                doOverride = BooleanOptionItem.Create(idStart++, "doOverride%role%", false, tab, false).SetParent(option);
                doOverride.ReplacementDictionary = replacementDic;
            }

            assignCommonTasks = BooleanOptionItem.Create(idStart++, "assignCommonTasks", true, tab, false).SetParent(doOverride)
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
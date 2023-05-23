using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnhollowerRuntimeLib;
using UnityEngine;

[assembly: AssemblyFileVersionAttribute(TownOfHost.Main.PluginVersion)]
[assembly: AssemblyInformationalVersionAttribute(TownOfHost.Main.PluginVersion)]
namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
    [BepInIncompatibility("jp.ykundesu.supernewroles")]
    [BepInProcess("Among Us.exe")]
    public class Main : BasePlugin
    {
        // == プログラム設定 / Program Config ==
        // modの名前 / Mod Name (Default: Town Of Host)
        public static readonly string ModName = "Town Of Host_Y";
        // modの色 / Mod Color (Default: #00bfff)
        public static readonly string ModColor = "#ffff00";
        // 公開ルームを許可する / Allow Public Room (Default: true)
        public static readonly bool AllowPublicRoom = true;
        // フォークID / ForkId (Default: OriginalTOH)
        public static readonly string ForkId = "TOH_Y";
        // Discordボタンを表示するか / Show Discord Button (Default: true)
        public static readonly bool ShowDiscordButton = true;
        // Discordサーバーの招待リンク / Discord Server Invite URL (Default: https://discord.gg/W5ug6hXB9V)
        public static readonly string DiscordInviteUrl = "https://discord.gg/YCUY8b3jew";
        // ==========
        public const string OriginalForkId = "OriginalTOH"; // Don't Change The Value. / この値を変更しないでください。
        // == 認証設定 / Authentication Config ==
        // デバッグキーの認証インスタンス
        public static HashAuth DebugKeyAuth { get; private set; }
        // デバッグキーのハッシュ値
        public const string DebugKeyHash = "c0fd562955ba56af3ae20d7ec9e64c664f0facecef4b3e366e109306adeae29d";
        // デバッグキーのソルト
        public const string DebugKeySalt = "59687b";
        // デバッグキーのコンフィグ入力
        public static ConfigEntry<string> DebugKeyInput { get; private set; }

        // ==========
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.emptybottle.townofhost";
        public const string PluginVersion = "412.9.2";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static Version version = Version.Parse(PluginVersion);
        public static BepInEx.Logging.ManualLogSource Logger;
        public static bool hasArgumentException = false;
        public static string ExceptionMessage;
        public static bool ExceptionMessageIsShown = false;
        public static string credentialsText;
        public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
        public static HideNSeekGameOptionsV07 HideNSeekSOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
        //Client Options
        public static ConfigEntry<string> HideName { get; private set; }
        public static ConfigEntry<string> HideColor { get; private set; }
        public static ConfigEntry<bool> ForceJapanese { get; private set; }
        public static ConfigEntry<bool> JapaneseRoleName { get; private set; }
        public static ConfigEntry<float> MessageWait { get; private set; }

        public static Dictionary<byte, PlayerVersion> playerVersion = new();
        //Preset Name Options
        public static ConfigEntry<string> Preset1 { get; private set; }
        public static ConfigEntry<string> Preset2 { get; private set; }
        public static ConfigEntry<string> Preset3 { get; private set; }
        public static ConfigEntry<string> Preset4 { get; private set; }
        public static ConfigEntry<string> Preset5 { get; private set; }
        //Other Configs
        public static ConfigEntry<string> WebhookURL { get; private set; }
        public static ConfigEntry<string> BetaBuildURL { get; private set; }
        public static ConfigEntry<float> LastKillCooldown { get; private set; }
        public static ConfigEntry<float> LastShapeshifterCooldown { get; private set; }
        public static OptionBackupData RealOptionsData;
        public static Dictionary<byte, PlayerState> PlayerStates = new();
        public static Dictionary<byte, string> AllPlayerNames;
        public static Dictionary<(byte, byte), string> LastNotifyNames;
        public static Dictionary<byte, Color32> PlayerColors = new();
        public static Dictionary<byte, PlayerState.DeathReason> AfterMeetingDeathPlayers = new();
        public static Dictionary<CustomRoles, String> roleColors;
        public static bool IsFixedCooldown => CustomRoles.Vampire.IsEnable();
        public static float RefixCooldownDelay = 0f;
        public static List<byte> ResetCamPlayerList;
        public static List<byte> winnerList;
        public static List<int> clientIdList;
        public static List<(string, byte, string)> MessagesToSend;
        public static bool isChatCommand = false;
        public static List<PlayerControl> LoversPlayers = new();
        public static bool isLoversDead = true;
        public static Dictionary<byte, float> AllPlayerKillCooldown = new();

        /// <summary>
        /// 基本的に速度の代入は禁止.スピードは増減で対応してください.
        /// </summary>
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public const float MinSpeed = 0.0001f;
        public static Dictionary<byte, float> WarlockTimer = new();
        public static Dictionary<byte, PlayerControl> CursedPlayers = new();
        public static Dictionary<byte, bool> isCurseAndKill = new();
        public static Dictionary<(byte, byte), bool> isDoused = new();
        public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = new();
        /// <summary>
        /// Key: ターゲットのPlayerId, Value: パペッティアのPlayerId
        /// </summary>
        public static Dictionary<byte, byte> PuppeteerList = new();
        public static Dictionary<byte, byte> SpeedBoostTarget = new();
        public static Dictionary<byte, int> MayorUsedButtonCount = new();
        public static int AliveImpostorCount;
        public static int SKMadmateNowCount;
        public static bool isCursed;
        public static Dictionary<byte, bool> CheckShapeshift = new();
        public static Dictionary<byte, byte> ShapeshiftTarget = new();
        public static bool VisibleTasksCount;
        public static string nickName = "";
        public static bool introDestroyed = false;
        public static byte currentDousingTarget;
        public static float DefaultCrewmateVision;
        public static float DefaultImpostorVision;
        public static bool IsValentine = DateTime.Now.Month == 3 && DateTime.Now.Day is 9 or 10 or 11 or 12 or 13 or 14 or 15;
        public static bool IsChristmas = DateTime.Now.Month == 12 && DateTime.Now.Day is 23 or 24 or 25 or 26;
        public static bool IsAprilFool = DateTime.Now.Month == 4 && DateTime.Now.Day is 1 or 2 or 3;
        public static bool IsInitialRelease = DateTime.Now.Month == 11 && DateTime.Now.Day is 2;
        public static bool IsOneNightRelease = DateTime.Now.Month == 3;

        //TOH_Y
        public static Dictionary<byte, int> ChairmanUsedButtonCount = new();
        public static Dictionary<byte, int> CursedWolfSpellCount = new();
        public static Dictionary<byte, int> LoveCutterKilledCount = new();
        public static Dictionary<byte, float> colorchange = new();
        public static Dictionary<byte, bool> isBlindVision = new();
        public static Dictionary<byte, int> OppoKillerShotLimit = new();
        public static byte ExiledPlayer = 253;
        public static byte BaitKillPlayer = 253;
        public static List<(PlayerControl, PlayerControl)> RevengeTargetPlayer;
        public static Dictionary<byte, (int, bool)> AntiCompGuardCount = new();
        public static Dictionary<byte,　bool> GuardingGuardCount = new();
        public static List<byte> ONMeetingExiledPlayers = new();
        public static int ONKillCount = 0;
        public static Dictionary<byte, bool> isPotentialistChanged = new();
        public static Dictionary<byte, bool> IsAdd1NextExiled = new();

        //怪盗などの交換役職系で使うため持ってきている、初期化は現状ONモードのみ
        public static Dictionary<byte, CustomRoles> DefaultRole = new();
        public static Dictionary<byte, CustomRoles> MeetingSeerDisplayRole = new();
        public static Dictionary<byte, PlayerControl> ChangeRolesTarget = new();

        public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
        public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive());
        public static IEnumerable<PlayerControl> AllDeadPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && !p.IsAlive());

        public static Main Instance;

        public override void Load()
        {
            Instance = this;

            //Client Options
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "Town Of Host_Y");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
            ForceJapanese = Config.Bind("Client Options", "Force Japanese", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", true);
            DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");

            Logger = BepInEx.Logging.Logger.CreateLogSource("TOH_Y");
            TownOfHost.Logger.Enable();
            TownOfHost.Logger.Disable("NotifyRoles");
            TownOfHost.Logger.Disable("SendRPC");
            TownOfHost.Logger.Disable("ReceiveRPC");
            TownOfHost.Logger.Disable("SwitchSystem");
            TownOfHost.Logger.Disable("CustomRpcSender");
            //TownOfHost.Logger.isDetail = true;

            // 認証関連-初期化
            DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

            // 認証関連-認証
            DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

            WarlockTimer = new Dictionary<byte, float>();
            CursedPlayers = new Dictionary<byte, PlayerControl>();
            isDoused = new Dictionary<(byte, byte), bool>();
            ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            MayorUsedButtonCount = new Dictionary<byte, int>();
            winnerList = new();
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte, string)>();
            currentDousingTarget = 255;

            //TOH_Y
            ChairmanUsedButtonCount = new Dictionary<byte, int>();
            LoveCutterKilledCount = new Dictionary<byte, int>();
            CursedWolfSpellCount = new Dictionary<byte, int>();
            colorchange = new Dictionary<byte, float>();
            isBlindVision = new Dictionary<byte, bool>();
            OppoKillerShotLimit = new Dictionary<byte, int>();
            AntiCompGuardCount = new Dictionary<byte, (int, bool)>();
            GuardingGuardCount = new Dictionary<byte, bool>();
            isPotentialistChanged = new Dictionary<byte, bool>();
            IsAdd1NextExiled = new Dictionary<byte, bool>();

            //ON
            DefaultRole = new Dictionary<byte, CustomRoles>();
            MeetingSeerDisplayRole = new Dictionary<byte, CustomRoles>();
            ChangeRolesTarget = new Dictionary<byte, PlayerControl>();


            Preset1 = Config.Bind("Preset Name Options", "Preset1", "Preset_1");
            Preset2 = Config.Bind("Preset Name Options", "Preset2", "Preset_2");
            Preset3 = Config.Bind("Preset Name Options", "Preset3", "Preset_3");
            Preset4 = Config.Bind("Preset Name Options", "Preset4", "Preset_4");
            Preset5 = Config.Bind("Preset Name Options", "Preset5", "Preset_5");
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            BetaBuildURL = Config.Bind("Other", "BetaBuildURL", "");
            MessageWait = Config.Bind("Other", "MessageWait", 0.7f);
            LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);
            LastShapeshifterCooldown = Config.Bind("Other", "LastShapeshifterCooldown", (float)30);

            CustomWinnerHolder.Reset();
            Translator.Init();
            BanManager.Init();
            TemplateManager.Init();
            VoiceReader.Init();

            IRandom.SetInstance(new NetRandomWrapper());

            hasArgumentException = false;
            ExceptionMessage = "";
            try
            {

                roleColors = new Dictionary<CustomRoles, string>()
                {
                    //バニラ役職
                    {CustomRoles.Crewmate, "#ffffff"},
                    {CustomRoles.Engineer, "#8cffff"},
                    {CustomRoles.Scientist, "#8cffff"},
                    {CustomRoles.GuardianAngel, "#ffffff"},
                    //インポスター、シェイプシフター
                    //特殊インポスター役職
                    //マッドメイト系役職
                        //後ろで追加
                    //両陣営可能役職
                    {CustomRoles.Watcher, "#800080"},
                    //特殊クルー役職
                    {CustomRoles.NiceWatcher, "#800080"}, //ウォッチャーの派生
                    {CustomRoles.Bait, "#00f7ff"},
                    {CustomRoles.SabotageMaster, "#0000ff"},
                    {CustomRoles.Snitch, "#b8fb4f"},
                    {CustomRoles.Mayor, "#204d42"},
                    {CustomRoles.Sheriff, "#f8cd46"},
                    {CustomRoles.Lighter, "#eee5be"},
                    {CustomRoles.SpeedBooster, "#00ffff"},
                    {CustomRoles.Doctor, "#80ffdd"},
                    {CustomRoles.Trapper, "#5a8fd0"},
                    {CustomRoles.Dictator, "#df9b00"},
                    {CustomRoles.CSchrodingerCat, "#ffffff"}, //シュレディンガーの猫の派生
                    {CustomRoles.Seer, "#61b26c"},
                    {CustomRoles.TimeManager, "#6495ed"},
                    {CustomRoles.FortuneTeller, "#9370db"},
                    //ニュートラル役職
                    {CustomRoles.Arsonist, "#ff6633"},
                    {CustomRoles.Jester, "#ec62a5"},
                    {CustomRoles.Terrorist, "#00ff00"},
                    {CustomRoles.Executioner, "#611c3a"},
                    {CustomRoles.Opportunist, "#00ff00"},
                    {CustomRoles.SchrodingerCat, "#696969"},
                    {CustomRoles.Egoist, "#5600ff"},
                    {CustomRoles.EgoSchrodingerCat, "#5600ff"},
                    {CustomRoles.Jackal, "#00b4eb"},
                    {CustomRoles.JSchrodingerCat, "#00b4eb"},
                    //HideAndSeek
                    {CustomRoles.HASFox, "#e478ff"},
                    {CustomRoles.HASTroll, "#00ff00"},
                    // GM
                    {CustomRoles.GM, "#ff5b70"},
                    //サブ役職
                    {CustomRoles.LastImpostor, "#ff1493"},
                    {CustomRoles.Lovers, "#ff6be4"},
                    {CustomRoles.Workhorse, "#00ffff"},
                    {CustomRoles.AddWatch, "#800080"},
                    {CustomRoles.AddLight, "#eee5be"},
                    {CustomRoles.Sunglasses, "#883fd1"},
                    {CustomRoles.AddSeer, "#61b26c"},
                    {CustomRoles.Autopsy, "#80ffdd"},
                    {CustomRoles.VIP, "#ffff00"},
                    {CustomRoles.Clumsy, "#696969"},
                    {CustomRoles.Revenger, "#00ffff"},
                    {CustomRoles.Management, "#80ffdd"},
                    {CustomRoles.Sending, "#883fd1"},
                    {CustomRoles.InfoPoor, "#556b2f"},
                    {CustomRoles.TieBreaker, "#204d42"},
                    {CustomRoles.NonReport, "#883fd1"},
                    {CustomRoles.Loyalty, "#b8fb4f"},
                    {CustomRoles.PlusVote, "#204d42"},
                    {CustomRoles.Guarding, "#8cffff"},
                    {CustomRoles.AddBait, "#00f7ff"},
                    {CustomRoles.Refusing, "#61b26c"},
                    {CustomRoles.CompreteCrew, "#ffff00"},

                    //TOH_Y
                    //Crewmate
                    {CustomRoles.Bakery, "#b58428"},//TOH_Y01_1
                    {CustomRoles.NBakery, "#b58428"},//TOH_Y01_1
                    {CustomRoles.Hunter, "#f8cd46"},
                    {CustomRoles.TaskManager, "#80ffdd"},
                    {CustomRoles.Nekomata, "#00ffff"},
                    {CustomRoles.Express, "#00ffff"},
                    {CustomRoles.Chairman, "#204d42"},
                    {CustomRoles.SeeingOff, "#883fd1"},
                    {CustomRoles.Rainbow, "#ffff00"},//TOH_Y01_8
                    {CustomRoles.SillySheriff, "#f8cd46"},
                    {CustomRoles.Sympathizer, "#f08080"},
                    {CustomRoles.Blinder, "#883fd1"},
                    {CustomRoles.Medic, "#6495ed"},
                    {CustomRoles.Potentialist, "#ffffff"},
                    {CustomRoles.GrudgeSheriff, "#f8cd46"},
                    {CustomRoles.CandleLighter, "#ff7f50"},
                    {CustomRoles.Psychic, "#883fd1"},

                    //Neutral
                    {CustomRoles.AntiComplete, "#ec62a5"},//TOH_Y01_13
                    {CustomRoles.Workaholic, "#008b8b"},//TOH_Y01_14
                    {CustomRoles.DarkHide, "#483d8b"},//TOH_Y
                    {CustomRoles.OSchrodingerCat, "#00ff00"},
                    {CustomRoles.DSchrodingerCat, "#483d8b"},
                    {CustomRoles.LoveCutter, "#c71585"},
                    {CustomRoles.PlatonicLover, "#ff6be4"},
                    {CustomRoles.Lawyer, "#daa520"},
                    {CustomRoles.Pursuer, "#daa520"},
                    {CustomRoles.JClient, "#00b4eb"},
                    {CustomRoles.Totocalcio, "#00ff00"},

                    //NeutralDummy
                    {CustomRoles.Neutral, "#696969"},

                    //CatchCat
                    {CustomRoles.CatRedLeader, "#ff0000"},
                    {CustomRoles.CatBlueLeader, "#00b4eb"},
                    {CustomRoles.CatYellowLeader, "#f8cd46"},
                    {CustomRoles.CatNoCat, "#696969"},
                    {CustomRoles.CatRedCat, "#ff0000"},
                    {CustomRoles.CatBlueCat, "#00b4eb"},
                    {CustomRoles.CatYellowCat, "#f8cd46"},

                    //OneNight
                    {CustomRoles.ONWerewolf, "#be3964"},
                    {CustomRoles.ONBigWerewolf, "#be3964"},
                    {CustomRoles.ONMadman, "#8b3073"},
                    {CustomRoles.ONMadFanatic, "#8b3073"},
                    {CustomRoles.ONVillager, "#3c7b9a"},
                    {CustomRoles.ONDiviner, "#865575"},
                    {CustomRoles.ONPhantomThief, "#696969"},
                    {CustomRoles.ONHunter, "#9fcc5b"},
                    {CustomRoles.ONMayor, "#9292b2"},
                    {CustomRoles.ONBakery, "#f19801"},
                    {CustomRoles.ONTrapper, "#5a8fd0"},
                    {CustomRoles.ONHangedMan, "#ca8134"},

                    {CustomRoles.Engineer1, "#8cffff"},
                    {CustomRoles.Engineer2, "#8cffff"},
                    {CustomRoles.Engineer3, "#8cffff"},
                    {CustomRoles.Scientist1, "#8cffff"},
                    {CustomRoles.Scientist2, "#8cffff"},
                    {CustomRoles.Scientist3, "#8cffff"},
                    
                    {CustomRoles.NotAssigned, "#ffffff"}
                };
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    switch (role.GetCustomRoleTypes())
                    {
                        case CustomRoleTypes.Impostor:
                        case CustomRoleTypes.Madmate:
                            roleColors.TryAdd(role, "#ff1919");
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                TownOfHost.Logger.Error("エラー:Dictionaryの値の重複を検出しました", "LoadDictionary");
                TownOfHost.Logger.Exception(ex, "LoadDictionary");
                hasArgumentException = true;
                ExceptionMessage = ex.Message;
                ExceptionMessageIsShown = false;
            }
            TownOfHost.Logger.Info($"{Application.version}", "AmongUs Version");

            var handler = TownOfHost.Logger.Handler("GitVersion");
            handler.Info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}");
            handler.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}");
            handler.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}");
            handler.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}");
            handler.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}");
            handler.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}");
            handler.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}");

            ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();

            Harmony.PatchAll();
        }
    }
    public enum CustomRoles
    {
        //Default
        Crewmate = 0,
        //Impostor(Vanilla)
        Impostor,
        Shapeshifter,
        //Impostor
        BountyHunter,
        EvilWatcher,
        FireWorks,
        Mafia,
        SerialKiller,
        ShapeMaster,
        Sniper,
        Vampire,
        Witch,
        Warlock,
        Mare,
        Puppeteer,
        TimeThief,
        EvilTracker,
        ShapeKiller,
        //TOH_YImpostor
        Evilneko,
        AntiAdminer,
        CursedWolf,
        Greedier,
        Ambitioner,
        Scavenger,
        EvilDiviner,
        Telepathisters,
        NormalImpostor,

        //Madmate
        MadGuardian,
        Madmate,
        MadSnitch,
        SKMadmate,
        MSchrodingerCat,//インポスター陣営のシュレディンガーの猫
        //TOH_YMadmate
        MadDictator,
        MadNatureCalls,
        MadBrackOuter,
        MadSheriff,

        //両陣営
        Watcher,
        //Crewmate(Vanilla)
        Engineer,
        GuardianAngel,
        Scientist,
        //Crewmate
        Bait,
        Lighter,
        Mayor,
        NiceWatcher,
        SabotageMaster,
        Sheriff,
        Snitch,
        SpeedBooster,
        Trapper,
        Dictator,
        Doctor,
        Seer,
        TimeManager,
        FortuneTeller,
        CSchrodingerCat,//クルー陣営のシュレディンガーの猫
        //TOH_YCrewmate
        Bakery,
        NBakery,
        Hunter,
        TaskManager,
        Nekomata,
        Express,
        Chairman,
        SeeingOff,
        Rainbow,
        SillySheriff,
        Sympathizer,
        Blinder,
        Medic,
        Potentialist,
        GrudgeSheriff,
        CandleLighter,
        Psychic,

        //Neutral
        Arsonist,
        Egoist,
        EgoSchrodingerCat,//エゴイスト陣営のシュレディンガーの猫
        Jester,
        Opportunist,
        OSchrodingerCat,//オポチュニスト陣営のシュレディンガーの猫
        SchrodingerCat,//無所属のシュレディンガーの猫
        Terrorist,
        Executioner,
        Jackal,
        JClient,//Jクライアント
        JSchrodingerCat,//ジャッカル陣営のシュレディンガーの猫
        //TOH_YNeutral オポシュレ猫のみ上に移動
        AntiComplete,
        Workaholic,
        DarkHide,
        DSchrodingerCat,//ダークハイド陣営のシュレディンガーの猫
        LoveCutter,
        PlatonicLover,
        Lawyer,
        Pursuer,
        Totocalcio,

        //HideAndSeek
        HASFox,
        HASTroll,
        //CatchCat
        CatRedLeader,
        CatBlueLeader,
        CatYellowLeader,
        CatNoCat,
        CatRedCat,
        CatBlueCat,
        CatYellowCat,

        //OneNight        
        ONWerewolf,
        ONBigWerewolf,
        ONMadman,
        ONMadFanatic,
        ONVillager,
        ONDiviner,
        ONPhantomThief,
        ONHunter,
        ONMayor,
        ONBakery,
        ONTrapper,
        ONHangedMan,

        //GM
        GM,

        //AddOnOnly
        Impostor1,
        Impostor2,
        Impostor3,
        Shapeshifter1,
        Shapeshifter2,
        Shapeshifter3,
        Madmate1,
        Madmate2,
        Madmate3,
        Crewmate1,
        Crewmate2,
        Crewmate3,
        Engineer1,
        Engineer2,
        Engineer3,
        Scientist1,
        Scientist2,
        Scientist3,

        // Sub-roll after 500
        NotAssigned = 500,
        LastImpostor,
        Lovers,
        Workhorse,
        AddWatch,
        AddLight,
        Sunglasses,
        AddSeer,
        Autopsy,
        VIP,
        Clumsy,
        Revenger,
        Management,
        Sending,
        InfoPoor,
        TieBreaker,
        NonReport,
        Loyalty,
        PlusVote,
        Guarding,
        AddBait,
        Refusing,
        CompreteCrew,

        //Dummy
        Neutral = 900
    }
    //WinData
    public enum CustomWinner
    {
        Draw = -1,
        Default = -2,
        None = -3,
        Impostor = CustomRoles.Impostor,
        Crewmate = CustomRoles.Crewmate,
        Jester = CustomRoles.Jester,
        Terrorist = CustomRoles.Terrorist,
        Lovers = CustomRoles.Lovers,
        Executioner = CustomRoles.Executioner,
        Arsonist = CustomRoles.Arsonist,
        Egoist = CustomRoles.Egoist,
        Jackal = CustomRoles.Jackal,
        HASTroll = CustomRoles.HASTroll,
        //TOH_Y
        AntiComplete = CustomRoles.AntiComplete,
        Workaholic = CustomRoles.Workaholic,
        DarkHide = CustomRoles.DarkHide,
        LoveCutter = CustomRoles.LoveCutter,
        Lawyer = CustomRoles.Lawyer,
        NBakery = CustomRoles.NBakery,

        //CatchCat
        RedL = CustomRoles.CatRedLeader,
        BlueL = CustomRoles.CatBlueLeader,
        YellowL = CustomRoles.CatYellowLeader,

        //OneNight
        HangedMan = CustomRoles.ONHangedMan,
    }
    public enum AdditionalWinners
    {
        None = -1,
        Opportunist = CustomRoles.Opportunist,
        SchrodingerCat = CustomRoles.SchrodingerCat,
        Executioner = CustomRoles.Executioner,
        HASFox = CustomRoles.HASFox,
        //TOH_Y
        Lovers = CustomRoles.Lovers,
        Lawyer = CustomRoles.Lawyer,
        Pursuer = CustomRoles.Pursuer,
        Totocalcio = CustomRoles.Totocalcio,

        //CatchCat
        RedC = CustomRoles.CatRedCat,
        BlueC = CustomRoles.CatBlueCat,
        YellowC = CustomRoles.CatYellowCat,
    }
    /*public enum CustomRoles : byte
    {
        Default = 0,
        HASTroll = 1,
        HASHox = 2
    }*/
    public enum SuffixModes
    {
        None = 0,
        TOH,
        Streaming,
        Recording,
        RoomHost,
        OriginalName
    }
    public enum VoteMode
    {
        Default,
        Suicide,
        SelfVote,
        Skip
    }
    public enum TieMode
    {
        Default,
        All,
        Random
    }
    public enum SyncColorMode
    {
        None,
        Clone,
        fif_fif,
        ThreeCornered,
        Twin,
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

using TownOfHostY.Attributes;
using TownOfHostY.Roles.Core;

[assembly: AssemblyFileVersionAttribute(TownOfHostY.Main.PluginVersion)]
[assembly: AssemblyInformationalVersionAttribute(TownOfHostY.Main.PluginVersion)]
namespace TownOfHostY
{
    [BepInPlugin(PluginGuid, "Town Of Host_Y", PluginVersion)]
    [BepInIncompatibility("jp.ykundesu.supernewroles")]
    [BepInIncompatibility("com.emptybottle.townofhost")]
    [BepInProcess("Among Us.exe")]
    public class Main : BasePlugin
    {
        // == プログラム設定 / Program Config ==
        // modの名前 / Mod Name (Default: Town Of Host)
        public static readonly string ModName = "Town Of Host_Y";
        // modの色 / Mod Color (Default: #00bfff)
        public static readonly string ModColor = "#ffff00";
        // 公開ルームを許可する / Allow Public Room (Default: true)
        public static readonly bool AllowPublicRoom = false;
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
        public const string PluginGuid = "com.yumenopai.townofhosty";
        public const string PluginVersion = "503.13.1";
        // サポートされている最低のAmongUsバージョン
        public static readonly string LowestSupportedVersion = "2023.7.11";
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
        public static Dictionary<byte, string> AllPlayerNames;
        public static Dictionary<(byte, byte), string> LastNotifyNames;
        public static Dictionary<byte, Color32> PlayerColors = new();
        public static Dictionary<byte, CustomDeathReason> AfterMeetingDeathPlayers = new();
        public static Dictionary<CustomRoles, String> roleColors;
        public static Dictionary<CustomColor, String> customColors;
        public static List<byte> ResetCamPlayerList;
        public static List<byte> winnerList;
        public static List<int> clientIdList;
        public static List<(string, byte, string, bool)> MessagesToSend;
        public static bool isChatCommand = false;
        public static List<PlayerControl> LoversPlayers = new();
        public static bool isLoversDead = true;
        public static Dictionary<byte, float> AllPlayerKillCooldown = new();

        /// <summary>
        /// 基本的に速度の代入は禁止.スピードは増減で対応してください.
        /// </summary>
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public const float MinSpeed = 0.0001f;
        public static int AliveImpostorCount;
        public static int SKMadmateNowCount;
        public static Dictionary<byte, bool> CheckShapeshift = new();
        public static Dictionary<byte, byte> ShapeshiftTarget = new();
        public static bool VisibleTasksCount;
        public static string nickName = "";
        public static bool introDestroyed = false;
        public static float DefaultCrewmateVision;
        public static float DefaultImpostorVision;
        public static bool IsValentine = DateTime.Now.Month == 3 && DateTime.Now.Day is 9 or 10 or 11 or 12 or 13 or 14 or 15;
        public static bool IsChristmas = DateTime.Now.Month == 12 && DateTime.Now.Day is 23 or 24 or 25 or 26;
        public static bool IsAprilFool = DateTime.Now.Month == 4 && DateTime.Now.Day is 1 or 2 or 3;
        public static bool IsInitialRelease = DateTime.Now.Month == 11 && DateTime.Now.Day is 2;
        public static bool IsOneNightRelease = DateTime.Now.Month is 7 or 8;
        public const float RoleTextSize = 2f;

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
            TownOfHostY.Logger.Enable();
            TownOfHostY.Logger.Disable("NotifyRoles");
            TownOfHostY.Logger.Disable("SendRPC");
            TownOfHostY.Logger.Disable("ReceiveRPC");
            TownOfHostY.Logger.Disable("SwitchSystem");
            TownOfHostY.Logger.Disable("CustomRpcSender");
            //TownOfHost.Logger.isDetail = true;

            // 認証関連-初期化
            DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

            // 認証関連-認証
            DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

            winnerList = new();
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte, string, bool)>();

            Preset1 = Config.Bind("Preset Name Options", "Preset1", "Preset_1");
            Preset2 = Config.Bind("Preset Name Options", "Preset2", "Preset_2");
            Preset3 = Config.Bind("Preset Name Options", "Preset3", "Preset_3");
            Preset4 = Config.Bind("Preset Name Options", "Preset4", "Preset_4");
            Preset5 = Config.Bind("Preset Name Options", "Preset5", "Preset_5");
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            BetaBuildURL = Config.Bind("Other", "BetaBuildURL", "");
            MessageWait = Config.Bind("Other", "MessageWait", 0.5f);
            LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);
            LastShapeshifterCooldown = Config.Bind("Other", "LastShapeshifterCooldown", (float)30);

            PluginModuleInitializerAttribute.InitializeAll();

            IRandom.SetInstance(new NetRandomWrapper());

            hasArgumentException = false;
            ExceptionMessage = "";
            try
            {
                roleColors = new Dictionary<CustomRoles, string>()
                {
                    // マッドメイト役職
                    {CustomRoles.MSchrodingerCat, "#ff1919"},
                    {CustomRoles.SKMadmate, "#ff1919"},
                    //特殊クルー役職
                    {CustomRoles.CSchrodingerCat, "#ffffff"}, //シュレディンガーの猫の派生
                    //ニュートラル役職
                    {CustomRoles.EgoSchrodingerCat, "#5600ff"},
                    {CustomRoles.JSchrodingerCat, "#00b4eb"},
                    {CustomRoles.DSchrodingerCat, "#483d8b"},
                    {CustomRoles.OSchrodingerCat, "#00ff00"},
                    //HideAndSeek
                    {CustomRoles.HASFox, "#e478ff"},
                    {CustomRoles.HASTroll, "#00ff00"},
                    // GM
                    {CustomRoles.GM, "#ff5b70"},
                    //サブ役職
                    {CustomRoles.LastImpostor, "#ff1919"},
                    {CustomRoles.Lovers, "#ff6be4"},
                    {CustomRoles.Workhorse, "#00ffff"},
                    {CustomRoles.CompreteCrew, "#ffff00"},
                    {CustomRoles.AddWatch, "#800080"},
                    {CustomRoles.Sunglasses, "#883fd1"},
                    {CustomRoles.AddLight, "#eee5be"},
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
                    {CustomRoles.Archenemy, "#ff6347"},

                    {CustomRoles.NotAssigned, "#ffffff"}
                };

                var type = typeof(RoleBase);
                var roleClassArray =
                CustomRoleManager.AllRolesClassType = Assembly.GetAssembly(type)
                    .GetTypes()
                    .Where(x => x.IsSubclassOf(type)).ToArray();

                foreach (var roleClassType in roleClassArray)
                    roleClassType.GetField("RoleInfo")?.GetValue(type);
            }
            catch (ArgumentException ex)
            {
                TownOfHostY.Logger.Error("エラー:Dictionaryの値の重複を検出しました", "LoadDictionary");
                TownOfHostY.Logger.Exception(ex, "LoadDictionary");
                hasArgumentException = true;
                ExceptionMessage = ex.Message;
                ExceptionMessageIsShown = false;
            }
            TownOfHostY.Logger.Info($"{Application.version}", "AmongUs Version");

            var handler = TownOfHostY.Logger.Handler("GitVersion");
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
    public enum CustomDeathReason
    {
        Kill,
        Vote,
        Suicide,
        Spell,
        FollowingSuicide,
        Bite,
        Bombed,
        Misfire,
        Torched,
        Sniped,
        Revenge,
        Execution,
        Disconnected,
        Fall,
        Poisoning,
        Win,
        etc = -1
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

        AntiComplete = CustomRoles.AntiComplete,
        DarkHide = CustomRoles.DarkHide,
        NBakery = CustomRoles.Bakery,
        Workaholic = CustomRoles.Workaholic,
        LoveCutter = CustomRoles.LoveCutter,
        Lawyer = CustomRoles.Lawyer,

        HASTroll = CustomRoles.HASTroll,
    }
    public enum AdditionalWinners
    {
        None = -1,
        Opportunist = CustomRoles.Opportunist,
        OSchrodingerCat = CustomRoles.OSchrodingerCat,
        SchrodingerCat = CustomRoles.SchrodingerCat,
        Executioner = CustomRoles.Executioner,
        Lovers = CustomRoles.Lovers,
        Lawyer = CustomRoles.Lawyer,
        Pursuer = CustomRoles.Lawyer,
        Totocalcio = CustomRoles.Totocalcio,
        Duelist = CustomRoles.Duelist,
        Archenemy = CustomRoles.Archenemy,
        HASFox = CustomRoles.HASFox,
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
        TOH_Y,
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
    public enum AddonShowMode
    {
        Default,
        All,
        TOH
    }
    public enum NameChange
    {
        None,
        //Crew,
        Color
    }
    public enum CustomColor
    {
        Coral,
        LightCoral,
        RoyalBlue,
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Text;

using AmongUs.GameOptions;
using TownOfHostY.Attributes;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Impostor;
using TownOfHostY.Roles.Neutral;
using TownOfHostY.Roles.Unit;
using TownOfHostY.Roles.AddOns.Common;

namespace TownOfHostY.Roles.Core;

public static class CustomRoleManager
{
    public static Type[] AllRolesClassType;
    public static Dictionary<CustomRoles, SimpleRoleInfo> AllRolesInfo = new(CustomRolesHelper.AllRoles.Length);
    public static Dictionary<byte, RoleBase> AllActiveRoles = new(15);

    public static SimpleRoleInfo GetRoleInfo(this CustomRoles role) => AllRolesInfo.ContainsKey(role) ? AllRolesInfo[role] : null;
    public static RoleBase GetRoleClass(this PlayerControl player) => GetByPlayerId(player.PlayerId);
    public static RoleBase GetByPlayerId(byte playerId) => AllActiveRoles.TryGetValue(playerId, out var roleBase) ? roleBase : null;
    public static void Do<T>(this List<T> list, Action<T> action) => list.ToArray().Do(action);
    // == CheckMurder関連処理 ==
    public static Dictionary<byte, MurderInfo> CheckMurderInfos = new();
    /// <summary>
    ///
    /// </summary>
    /// <param name="attemptKiller">実際にキルを行ったプレイヤー 不変</param>
    /// <param name="attemptTarget">>Killerが実際にキルを行おうとしたプレイヤー 不変</param>
    public static bool OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget, bool killerMyselfInvoke = false)
        => OnCheckMurder(attemptKiller, attemptTarget, attemptKiller, attemptTarget, killerMyselfInvoke);
    /// <summary>
    ///
    /// </summary>
    /// <param name="attemptKiller">実際にキルを行ったプレイヤー 不変</param>
    /// <param name="attemptTarget">>Killerが実際にキルを行おうとしたプレイヤー 不変</param>
    /// <param name="appearanceKiller">見た目上でキルを行うプレイヤー 可変</param>
    /// <param name="appearanceTarget">見た目上でキルされるプレイヤー 可変</param>
    /// <param name="killerMyselfInvoke">自身で呼び出すときはxxいくつかの処理をスキップ</param>
    public static bool OnCheckMurder(
        PlayerControl attemptKiller, PlayerControl attemptTarget,
        PlayerControl appearanceKiller, PlayerControl appearanceTarget,
        bool killerMyselfInvoke = false)
    {
        Logger.Info($"Attempt  :{attemptKiller.GetNameWithRole()} => {attemptTarget.GetNameWithRole()}", "CheckMurder");
        if (appearanceKiller != attemptKiller || appearanceTarget != attemptTarget)
            Logger.Info($"Apperance:{appearanceKiller.GetNameWithRole()} => {appearanceTarget.GetNameWithRole()}", "CheckMurder");

        var info = new MurderInfo(attemptKiller, attemptTarget, appearanceKiller, appearanceTarget);

        appearanceKiller.ResetKillCooldown();

        // 無効なキルをブロックする処理 必ず最初に実行する
        if (!CheckMurderPatch.CheckForInvalidMurdering(info, killerMyselfInvoke)) return false;

        // インポスター同士のキル無効
        if (attemptKiller.GetCustomRole().IsImpostor() && attemptTarget.GetCustomRole().IsImpostor() && !attemptKiller.Is(CustomRoles.StrayWolf)) return false;
        // ゴッドファーザーのロックキル
        if (CustomRoles.Godfather.IsPresent() && GodfatherAndJanitor.JanitorTarget.Contains(attemptKiller.PlayerId)) return false;
        // ペンギンの拉致られ中キル
        if (!Penguin.CanKilledByTarget(attemptKiller)) return false;

        var killerRole = attemptKiller.GetRoleClass();
        var targetRole = attemptTarget.GetRoleClass();

        // キラーがキル能力持ちなら
        if (killerRole is IKiller killer)
        {
            if (killer.IsKiller)
            {
                // イビルディバイナーのみ占いのためここで先に処理
                if (killerRole is EvilDiviner && !EvilDiviner.OnCheckMurder(info)) return false;
                // ガーディング属性によるガード
                if (Guarding.OnCheckMurder(info)) { }
                // メディックの対象プレイヤー
                else if (Medic.GuardPlayerCheckMurder(info)) { }
                // 背徳者の対象プレイヤー
                else if (Immoralist.GuardPlayerCheckMurder(info)) { }
                // ターゲットのキルチェック処理実行
                else if (targetRole != null)
                {
                    if (!targetRole.OnCheckMurderAsTarget(info)) return false;
                }
            }
            // キラーのキルチェック処理実行
            if (!killerMyselfInvoke)
            {
                killer.OnCheckMurderAsKiller(info);
            }
        }
        // CC
        if (Options.IsCCMode)
        {
            CatchCat.LeaderPlayer.OnCheckMurder(info);
            CatchCat.CatPlayer.OnCheckMurder(info);
        }
        // ON
        //if (Options.IsONMode && attemptTarget.Is(CustomRoles.ONPhantomThief))
        //{
        //    ONPhantomThief.OnCheckMurderTarget(info);
        //}

        //キル可能だった場合のみMurderPlayerに進む
        if (info.CanKill && info.DoKill)
        {
            //MurderPlayer用にinfoを保存
            CheckMurderInfos[appearanceKiller.PlayerId] = info;
            appearanceKiller.RpcMurderPlayer(appearanceTarget);
            return true;
        }
        else
        {
            if (!info.CanKill) Logger.Info($"{appearanceTarget.GetNameWithRole()}をキル出来ない。", "CheckMurder");
            if (!info.DoKill) Logger.Info($"{appearanceKiller.GetNameWithRole()}はキルしない。", "CheckMurder");
            return false;
        }
    }
    /// <summary>
    /// MurderPlayer実行後の各役職処理
    /// </summary>
    /// <param name="appearanceKiller">見た目上でキルを行うプレイヤー 可変</param>
    /// <param name="appearanceTarget">見た目上でキルされるプレイヤー 可変</param>
    public static void OnMurderPlayer(PlayerControl appearanceKiller, PlayerControl appearanceTarget)
    {
        //MurderInfoの取得
        if (CheckMurderInfos.TryGetValue(appearanceKiller.PlayerId, out var info))
        {
            //参照出来たら削除
            CheckMurderInfos.Remove(appearanceKiller.PlayerId);
        }
        else
        {
            //CheckMurderを経由していない場合はappearanceで処理
            info = new MurderInfo(appearanceKiller, appearanceTarget, appearanceKiller, appearanceTarget);
        }

        (var attemptKiller, var attemptTarget) = info.AttemptTuple;

        Logger.Info($"Real Killer={attemptKiller.GetNameWithRole()}", "MurderPlayer");

        //キラーの処理
        (attemptKiller.GetRoleClass() as IKiller)?.OnMurderPlayerAsKiller(info);

        //ターゲットの処理
        var targetRole = attemptTarget.GetRoleClass();
        if (targetRole != null)
            targetRole.OnMurderPlayerAsTarget(info);

        //その他視点の処理があれば実行
        foreach (var onMurderPlayer in OnMurderPlayerOthers.ToArray())
        {
            onMurderPlayer(info);
        }

        if (info.IsMeeting)
        {
            Lovers.VoteSuicide(attemptTarget.PlayerId);
            Janitor.VoteSuicide(attemptTarget.PlayerId);
        }
        else
        {
            Lovers.KillSuicide(attemptTarget.PlayerId);
            Janitor.KillSuicide(attemptTarget.PlayerId);
        }

        //TargetDeadArrow
        if (!info.IsMeeting) TargetDeadArrow.UpdateDeadBody();
        //WinTask
        VentEnterTask.TaskWinCountAllComplete(attemptTarget.PlayerId);

        //以降共通処理
        var targetState = PlayerState.GetByPlayerId(attemptTarget.PlayerId);
        if (targetState.DeathReason == CustomDeathReason.etc)
        {
            //死因が設定されていない場合は死亡判定
            targetState.DeathReason = CustomDeathReason.Kill;
        }

        targetState.SetDead();
        attemptTarget.SetRealKiller(attemptKiller, true);

        //ホストの死後タスク免除
        if (attemptTarget == PlayerControl.LocalPlayer && Options.HostGhostIgnoreTasks.GetBool()
            && !attemptTarget.Is(CustomRoles.Gang) && Utils.HasTasks(attemptTarget.Data))
        {
            var taskState = PlayerControl.LocalPlayer.GetPlayerTaskState();
            taskState.CompletedTasksCount = taskState.AllTasksCount;
            Logger.Info($"{PlayerControl.LocalPlayer.GetNameWithRole()}: TaskCounts = {taskState.CompletedTasksCount}/{taskState.AllTasksCount}", "OnMurderPlayer/HostGhostIgnoreTasks");

            foreach (var task in PlayerControl.LocalPlayer.Data.Tasks)
            {
                if (!task.Complete)
                {
                    task.Complete = true;
                    GameData.Instance.CompletedTasks++;
                }
            }
            Logger.Info($"{PlayerControl.LocalPlayer.GetNameWithRole()}.Dead TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "OnMurderPlayer/HostGhostIgnoreTasks");
        }

        Utils.CountAlivePlayers(true);

        Utils.TargetDies(info);

        Utils.SyncAllSettings();
        Utils.NotifyRoles();
    }
    /// <summary>
    /// その他視点からのMurderPlayer処理
    /// 初期化時にOnMurderPlayerOthers+=で登録
    /// </summary>
    public static HashSet<Action<MurderInfo>> OnMurderPlayerOthers = new();

    public static void OnFixedUpdate(PlayerControl player)
    {
        if (GameStates.IsInTask)
        {
            player.GetRoleClass()?.OnFixedUpdate(player);
            //その他視点処理があれば実行
            foreach (var onFixedUpdate in OnFixedUpdateOthers)
            {
                onFixedUpdate(player);
            }
        }
    }
    /// <summary>
    /// タスクターンに常時呼ばれる関数
    /// 他役職への干渉用
    /// Host以外も呼ばれるので注意
    /// 初期化時にOnFixedUpdateOthers+=で登録
    /// </summary>
    public static HashSet<Action<PlayerControl>> OnFixedUpdateOthers = new();

    public static bool OnSabotage(PlayerControl player, SystemTypes systemType)
    {
        bool cancel = false;
        foreach (var roleClass in AllActiveRoles.Values)
        {
            if (!roleClass.OnSabotage(player, systemType))
            {
                cancel = true;
            }
        }
        return !cancel;
    }
    // ==初期化関連処理 ==
    [GameModuleInitializer]
    public static void Initialize()
    {
        AllRolesInfo.Do(kvp => kvp.Value.IsEnable = kvp.Key.IsEnable());
        AllActiveRoles.Clear();
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        CheckMurderInfos.Clear();
        OnMurderPlayerOthers.Clear();
        OnFixedUpdateOthers.Clear();
    }
    public static void CreateInstance()
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            CreateInstance(pc.GetCustomRole(), pc);
        }
    }
    public static void CreateInstance(CustomRoles role, PlayerControl player)
    {
        if (AllRolesInfo.TryGetValue(role, out var roleInfo))
        {
            roleInfo.CreateInstance(player).Add();
        }
        //AddonAdd
        {
            foreach (var subRole in player.GetCustomSubRoles())
            {
                SubRoleAdd(player.PlayerId, subRole);
            }
        }
        if (player.Data.Role.Role == RoleTypes.Shapeshifter)
        {
            Main.CheckShapeshift.TryAdd(player.PlayerId, false);
        }
    }
    public static void SubRoleAdd(byte playerId, CustomRoles subRole)
    {
        switch (subRole)
        {
            case CustomRoles.Lovers: Lovers.Add(playerId); break;
            case CustomRoles.ChainShifterAddon: ChainShifterAddon.Add(playerId); break;

            case CustomRoles.AddWatch: AddWatch.Add(playerId); break;
            case CustomRoles.AddLight: AddLight.Add(playerId); break;
            case CustomRoles.AddSeer: AddSeer.Add(playerId); break;
            case CustomRoles.Autopsy: Autopsy.Add(playerId); break;
            case CustomRoles.VIP: VIP.Add(playerId); break;
            case CustomRoles.Revenger: Revenger.Add(playerId); break;
            case CustomRoles.Management: Management.Add(playerId); break;
            case CustomRoles.Sending: Sending.Add(playerId); break;
            case CustomRoles.TieBreaker: TieBreaker.Add(playerId); break;
            case CustomRoles.Loyalty: Loyalty.Add(playerId); break;
            case CustomRoles.PlusVote: PlusVote.Add(playerId); break;
            case CustomRoles.Guarding: Guarding.Add(playerId); break;
            case CustomRoles.AddBait: AddBait.Add(playerId); break;
            case CustomRoles.Refusing: Refusing.Add(playerId); break;
            case CustomRoles.Revealer: Revealer.Add(playerId); break;

            case CustomRoles.Sunglasses: Sunglasses.Add(playerId); break;
            case CustomRoles.Clumsy: Clumsy.Add(playerId); break;
            case CustomRoles.InfoPoor: InfoPoor.Add(playerId); break;
            case CustomRoles.NonReport: NonReport.Add(playerId); break;
        }
    }
    /// <summary>
    /// 受信したRPCから送信先を読み取ってRoleClassに配信する
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="rpcType"></param>
    public static void DispatchRpc(MessageReader reader, CustomRPC rpcType)
    {
        var playerId = reader.ReadByte();
        GetByPlayerId(playerId)?.ReceiveRPC(reader, rpcType);
    }
    //NameSystem
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> MarkOthers = new();
    public static HashSet<Func<PlayerControl, PlayerControl, bool, bool, string>> LowerOthers = new();
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> SuffixOthers = new();
    /// <summary>
    /// seer,seenが役職であるかに関わらず発動するMark
    /// 登録されたすべてを結合する。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>結合したMark</returns>
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        var sb = new StringBuilder(100);
        foreach (var marker in MarkOthers)
        {
            sb.Append(marker(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }
    /// <summary>
    /// seer,seenが役職であるかに関わらず発動するLowerText
    /// 登録されたすべてを結合する。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <param name="isForHud">ModでHudとして表示する場合</param>
    /// <returns>結合したLowerText</returns>
    public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        var sb = new StringBuilder(100);
        foreach (var lower in LowerOthers)
        {
            sb.Append(lower(seer, seen, isForMeeting, isForHud));
        }
        return sb.ToString();
    }
    /// <summary>
    /// seer,seenが役職であるかに関わらず発動するSuffix
    /// 登録されたすべてを結合する。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>結合したSuffix</returns>
    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        var sb = new StringBuilder(100);
        foreach (var suffix in SuffixOthers)
        {
            sb.Append(suffix(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }
    /// <summary>
    /// オブジェクトの破棄
    /// </summary>
    public static void Dispose()
    {
        Logger.Info($"Dispose ActiveRoles", "CustomRoleManager");
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        CheckMurderInfos.Clear();
        OnMurderPlayerOthers.Clear();
        OnFixedUpdateOthers.Clear();

        AllActiveRoles.Values.ToArray().Do(roleClass => roleClass.Dispose());
    }
}
public class MurderInfo
{
    /// <summary>実際にキルを行ったプレイヤー 不変</summary>
    public PlayerControl AttemptKiller { get; }
    /// <summary>Killerが実際にキルを行おうとしたプレイヤー 不変</summary>
    public PlayerControl AttemptTarget { get; }
    /// <summary>見た目上でキルを行うプレイヤー 可変</summary>
    public PlayerControl AppearanceKiller { get; set; }
    /// <summary>見た目上でキルされるプレイヤー 可変</summary>
    public PlayerControl AppearanceTarget { get; set; }

    /// <summary>
    /// targetがキル出来るか
    /// </summary>
    public bool CanKill = true;
    /// <summary>
    /// Killerが実際にキルするか
    /// </summary>
    public bool DoKill = true;
    /// <summary>
    ///転落死など事故の場合(キラー不在)
    /// </summary>
    public bool IsAccident = false;

    // 分解用 (killer, target) = info.AttemptTuple; のような記述でkillerとtargetをまとめて取り出せる
    public (PlayerControl killer, PlayerControl target) AttemptTuple => (AttemptKiller, AttemptTarget);
    public (PlayerControl killer, PlayerControl target) AppearanceTuple => (AppearanceKiller, AppearanceTarget);
    /// <summary>
    /// 本来の自殺
    /// </summary>
    public bool IsSuicide => AttemptKiller.PlayerId == AttemptTarget.PlayerId;
    /// <summary>
    /// 遠距離キル代わりの疑似自殺
    /// </summary>
    public bool IsFakeSuicide => AppearanceKiller.PlayerId == AppearanceTarget.PlayerId;
    /// <summary>
    ///会議中キルの場合
    /// </summary>
    public bool IsMeeting => GameStates.IsMeeting;
    public MurderInfo(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearancetarget)
    {
        AttemptKiller = attemptKiller;
        AttemptTarget = attemptTarget;
        AppearanceKiller = appearanceKiller;
        AppearanceTarget = appearancetarget;
    }
}

public enum CustomRoles
{
    NotAssigned = -1,

    //Default
    Crewmate = 0,
    //Impostor(Vanilla)
    Impostor,
    Shapeshifter,
    Phantom,
    //Impostor
    NormalImpostor,
    NormalShapeshifter,
    NormalPhantom,
    CustomImpostor,
    EvilWatcher,
    BountyHunter,
    FireWorks,
    Mafia,
    SerialKiller,
    ShapeMaster,
    Sniper,
    Vampire,
    Witch,
    Warlock,
    Mare,
    Penguin,
    Puppeteer,
    TimeThief,
    EvilTracker,
    Stealth,
    NekoKabocha,
    Insider,
    EvilNekomata,
    AntiAdminer,
    CursedWolf,
    Greedier,
    Ambitioner,
    Scavenger,
    EvilDiviner,
    Telepathisters,
    ShapeKiller,
    StrayWolf,
    EvilIgnition,
    Escalationer,
    EvilDyer,
    BestieWolf,
    EvilGuesser,
    SelfBomber,
    GrudgeCharger,
    Charger,
    Chaser,
    CharismaStar,
    EvilHacker,
    Telekinetic,
    Administer,

    Godfather,
    Janitor,
    Jack,
    //Madmate
    Madmate,
    MadGuardian,
    MadSnitch,
    MadSheriff,
    MadDictator,
    MadNatureCalls,
    MadBrackOuter,
    MadNimrod,
    MadScientist,
    MadJester,
    MadGuesser,
    MadConnecter,

    MadDilemma,
    jO,
    SKMadmate,
    //Crewmate(Vanilla)
    Engineer,
    GuardianAngel,
    Scientist,
    Tracker,
    Noisemaker,
    //Crewmate
    NormalEngineer,
    NormalScientist,
    NormalTracker,
    NormalNoisemaker,
    CustomCrewmate,
    NiceWatcher,
    Bait,
    Lighter,
    Mayor,
    SabotageMaster,
    Sheriff,
    Snitch,
    SpeedBooster,
    Trapper,
    Dictator,
    Doctor,
    Seer,
    TimeManager,
    Bakery,
    TaskManager,
    SillySheriff,
    GrudgeSheriff,
    Hunter,
    Nekomata,
    Chairman,
    Express,
    SeeingOff,
    Rainbow,
    Sympathizer,
    Blinder,
    Medic,
    CandleLighter,
    FortuneTeller,
    Psychic,
    Nimrod,
    Detector,
    LoyalDoggy,
    Rabbit,
    VentManager,
    NiceGuesser,
    Elder,

    Counselor,
    Lantern,
    Potentialist,
    //Neutral
    Arsonist,
    Egoist,
    Jester,
    Opportunist,
    SchrodingerCat,
    SchrodingerCatKiller,
    Terrorist,
    Executioner,
    Jackal,
    JSidekick,
    JClient,
    AntiComplete,
    Workaholic,
    DarkHide,
    LoveCutter,
    PlatonicLover,
    Lawyer,
    Pursuer,
    Totocalcio,
    Duelist,
    God,
    FoxSpirit,
    Immoralist,
    Ogre,
    Pirate,
    Gang,
    ChainShifter,

    GM,
    CounselorAndMadDilemma,
    GodfatherAndJanitor,
    JackOLantern,
    MaxMain,
    /************/

    //ON
    ONStart = 400,

    MaxON,
    /************/

    //CC
    CCStart = 480,
    CCRedLeader,
    CCBlueLeader,
    CCYellowLeader,
    CCNoCat,
    CCRedCat,
    CCBlueCat,
    CCYellowCat,

    MaxCC,
    /************/

    //HideAndSeek
    HASStart = 490,
    HASFox,
    HASTroll,

    MaxHAS,
    /************/

    // Addon
    StartAddon = 500,
    LastImpostor,
    CompleteCrew,
    Lovers,
    Workhorse,

    AddWatch,
    AddLight,
    AddSeer,
    Autopsy,
    VIP,
    Revenger,
    Management,
    Sending,
    TieBreaker,
    Loyalty,
    PlusVote,
    Guarding,
    AddBait,
    Refusing,
    Revealer,

    Sunglasses,
    Clumsy,
    InfoPoor,
    NonReport,

    Archenemy,
    ChainShifterAddon,

    MaxAddon,

    //dummy
    DummyNext = 10000,
}
public enum CustomRoleTypes
{
    Impostor,
    Madmate,
    Crewmate,
    Neutral,
    Unit
}
public enum HasTask
{
    True,
    False,
    ForRecompute
}
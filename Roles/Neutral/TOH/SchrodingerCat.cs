using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral;

public sealed class SchrodingerCat : RoleBase, IAdditionalWinner, IDeathReasonSeeable, IKillFlashSeeable//MAD時に使用
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SchrodingerCat),
            player => new SchrodingerCat(player),
            CustomRoles.SchrodingerCat,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuTOH + 300,
            SetupOptionItem,
            "シュレディンガーの猫",
            "#696969",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public SchrodingerCat(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        canWinTheCrewmateBeforeChange = OptionCanWinTheCrewmateBeforeChange.GetBool();
        changeTeamWhenExile = OptionChangeTeamWhenExile.GetBool();
        CanSeeKillableTeammate = OptionCanSeeKillableTeammate.GetBool();
        ConsumeBullet = OptionConsumeBullet.GetBool();
        changeKiller = OptionChangeKiller.GetBool();
        DeadDelay = OptionDeadDelay.GetFloat();
        RevengeOnExile = OptionRevengeOnExile.GetBool();
        taskTrigger = OptionTaskTrigger.GetInt();
    }
    static OptionItem OptionCanWinTheCrewmateBeforeChange;
    static OptionItem OptionChangeTeamWhenExile;
    static OptionItem OptionCanSeeKillableTeammate;
    static OptionItem OptionConsumeBullet;
    static OptionItem OptionChangeKiller;
    static OptionItem OptionDeadDelay;
    static OptionItem OptionRevengeOnExile;
    static OptionItem OptionTaskTrigger;

    enum OptionName
    {
        CanBeforeSchrodingerCatWinTheCrewmate,
        SchrodingerCatExiledTeamChanges,
        SchrodingerCatCanSeeKillableTeammate,
        SchrodingerCatConsumeBullet,
        SchrodingerCatChangeKiller,
        SchrodingerCatDeadDelay,
        SchrodingerCatRevengeOnExile,
        SchrodingerCatTaskTrigger,
    }
    static bool canWinTheCrewmateBeforeChange;
    static bool changeTeamWhenExile;
    public static bool CanSeeKillableTeammate;
    public static bool ConsumeBullet;
    static bool changeKiller;
    public static float DeadDelay;
    public static bool RevengeOnExile;
    static int taskTrigger;

    /// <summary>
    /// 自分をキルしてきた人のロール
    /// </summary>
    private ISchrodingerCatOwner owner = null;
    private TeamType _team = TeamType.None;
    /// <summary>
    /// 現在の所属陣営<br/>
    /// 変更する際は特段の事情がない限り<see cref="RpcSetTeam"/>を使ってください
    /// </summary>
    public TeamType Team
    {
        get => _team;
        private set
        {
            logger.Info($"{Player.GetRealName()}の陣営を{value}に変更");
            _team = value;
        }
    }
    public bool AmMadmate => Team == TeamType.Mad;
    public Color DisplayRoleColor => GetCatColor(Team);
    private static LogHandler logger = Logger.Handler(nameof(SchrodingerCat));

    public static void SetupOptionItem()
    {
        OptionCanWinTheCrewmateBeforeChange = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanBeforeSchrodingerCatWinTheCrewmate, false, false);
        OptionChangeTeamWhenExile = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SchrodingerCatExiledTeamChanges, false, false);
        OptionCanSeeKillableTeammate = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SchrodingerCatCanSeeKillableTeammate, false, false);
        OptionConsumeBullet = BooleanOptionItem.Create(RoleInfo, 13, OptionName.SchrodingerCatConsumeBullet, false, false);
        OptionChangeKiller = BooleanOptionItem.Create(RoleInfo, 14, OptionName.SchrodingerCatChangeKiller, false, false);
        OptionDeadDelay = FloatOptionItem.Create(RoleInfo, 15, OptionName.SchrodingerCatDeadDelay, new(2.5f, 180f, 2.5f), 15f, false, OptionChangeKiller)
            .SetValueFormat(OptionFormat.Seconds);
        OptionRevengeOnExile = BooleanOptionItem.Create(RoleInfo, 16, OptionName.SchrodingerCatRevengeOnExile, false, false);
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 17, OptionName.SchrodingerCatTaskTrigger, new(0, 99, 1), 10, false).SetValueFormat(OptionFormat.Pieces);
    }
    private bool KnownImpostor()
    {
        return MyTaskState.HasCompletedEnoughCountOfTasks(taskTrigger);
    }
    private void CheckAndAddNameColorToImpostors()
    {
        if (!KnownImpostor()) return;

        foreach (var impostor in Main.AllPlayerControls.Where(pc => !pc.Data.Disconnected && pc.GetCustomRole().IsImpostor()))
        {
            NameColorManager.Add(impostor.PlayerId, Player.PlayerId, Utils.GetRoleColorCode(CustomRoles.SchrodingerCat));
        }
    }
    public override void Add()
    {
        CheckAndAddNameColorToImpostors();
    }
    public override bool OnCompleteTask()
    {
        CheckAndAddNameColorToImpostors();
        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        owner?.ApplySchrodingerCatOptions(opt);
    }
    /// <summary>
    /// マッド猫用のオプション構築
    /// </summary>
    public static void ApplyMadCatOptions(IGameOptions opt)
    {
        if (Options.MadmateHasImpostorVision.GetBool())
        {
            opt.SetVision(true);
        }
        if (Options.MadmateCanSeeOtherVotes.GetBool())
        {
            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
        }
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var killer = info.AttemptKiller;

        //自殺ならスルー
        if (info.IsSuicide) return true;

        if (Team == TeamType.None && !killer.Is(CustomRoles.Pirate))
        {
            info.CanKill = false;
            ChangeTeamOnKill(killer);
            return false;
        }
        return true;
    }
    /// <summary>
    /// キルしてきた人に応じて陣営の状態を変える
    /// </summary>
    private void ChangeTeamOnKill(PlayerControl killer)
    {
        killer.RpcProtectedMurderPlayer(Player);
        if (killer.GetRoleClass() is ISchrodingerCatOwner catOwner)
        {
            var team = catOwner.SchrodingerCatChangeTo;

            catOwner.OnSchrodingerCatKill(this);
            RpcSetTeam(team);
            owner = catOwner;

            if (changeKiller)
            {
                killer.SetKillCooldown(250f);
                killer.RpcResetAbilityCooldown();
                SchrodingerCatKiller.SetCatKiller(Player, killer);
            }
        }
        else
        {
            logger.Warn($"未知のキル役職からのキル: {killer.GetNameWithRole()}");
        }

        RevealNameColors(killer);

        Utils.NotifyRoles();
        Utils.MarkEveryoneDirtySettings();
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        SchrodingerCatKiller.CheckCatKiller();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        SchrodingerCatKiller.FixedUpdate(player);
    }
    /// <summary>
    /// キルしてきた人とオプションに応じて名前の色を開示する
    /// </summary>
    private void RevealNameColors(PlayerControl killer)
    {
        if (CanSeeKillableTeammate)
        {
            var killerRoleId = killer.GetCustomRole();
            var killerTeam = Main.AllPlayerControls.Where(player => (AmMadmate && player.Is(CustomRoleTypes.Impostor)) || player.Is(killerRoleId));
            foreach (var member in killerTeam)
            {
                NameColorManager.Add(member.PlayerId, Player.PlayerId, RoleInfo.RoleColorCode);
                NameColorManager.Add(Player.PlayerId, member.PlayerId);
            }
        }
        else
        {
            NameColorManager.Add(killer.PlayerId, Player.PlayerId, RoleInfo.RoleColorCode);
            NameColorManager.Add(Player.PlayerId, killer.PlayerId);
        }
    }
    public override void OverrideShowMainRoleText(ref Color roleColor, ref string roleText)
    {
        switch(Team)
        {
            case TeamType.None:// 陣営変化前なら上書き不要
                return;
            case TeamType.Crew: roleText = "(Crew)" + roleText; break;
            case TeamType.Mad: roleText = "(Impo)" + roleText; break;
            case TeamType.Jackal: roleText = "(Jack)" + roleText; break;
            case TeamType.Egoist: roleText = "(Ego)" + roleText; break;
            case TeamType.DarkHide: roleText = "(Dark)" + roleText; break;
            case TeamType.Opportunist: roleText = "(Oppo)" + roleText; break;
            case TeamType.Ogre: roleText = "(Ogre)" + roleText; break;
        }

        // 色を変更
        roleColor = DisplayRoleColor;
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (exiled.PlayerId != Player.PlayerId || Team != TeamType.None || !changeTeamWhenExile)
        {
            return;
        }
        ChangeTeamRandomly();
    }
    /// <summary>
    /// ゲームに存在している陣営の中からランダムに自分の陣営を変更する
    /// </summary>
    private void ChangeTeamRandomly()
    {
        var rand = IRandom.Instance;
        // 追加時にキャパ数を変更
        List<TeamType> candidates = new(5)
        {
            TeamType.Crew,
            TeamType.Mad,
        };
        if (CustomRoles.Egoist.IsPresent())
        {
            candidates.Add(TeamType.Egoist);
        }
        if (CustomRoles.Jackal.IsPresent())
        {
            candidates.Add(TeamType.Jackal);
        }
        if (CustomRoles.DarkHide.IsPresent())
        {
            candidates.Add(TeamType.DarkHide);
        }
        if (CustomRoles.Ogre.IsPresent())
        {
            candidates.Add(TeamType.Ogre);
        }
        var team = candidates[rand.Next(candidates.Count)];
        RpcSetTeam(team);
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        bool? won = Team switch
        {
            TeamType.None => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && canWinTheCrewmateBeforeChange,
            TeamType.Mad => CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor,
            TeamType.Crew => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate,
            TeamType.Jackal => CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal,
            TeamType.Egoist => CustomWinnerHolder.WinnerTeam == CustomWinner.Egoist,
            TeamType.DarkHide => CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide,
            TeamType.Ogre => CustomWinnerHolder.AdditionalWinnerRoles.Contains(CustomRoles.Ogre),
            TeamType.Opportunist => Player.IsAlive(),
            _ => null,
        };
        if (!won.HasValue)
        {
            logger.Warn($"不明な猫の勝利チェック: {Team}");
            return false;
        }
        return won.Value;
    }
    public void RpcSetTeam(TeamType team)
    {
        Team = team;
        if (AmongUsClient.Instance.AmHost)
        {
            using var sender = CreateSender(CustomRPC.SetSchrodingerCatTeam);
            sender.Writer.Write((byte)team);
        }
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetSchrodingerCatTeam)
        {
            return;
        }
        Team = (TeamType)reader.ReadByte();
    }

    // マッド属性化までの間マッド状態時に特別扱いするための応急処置的個別実装
    // マッドが属性化したらマッド状態のシュレ猫にマッド属性を付与することで削除
    // 上にあるApplyMadCatOptions，MeetingHudPatchにある道連れ処理，ShipStatusPatchにあるサボ直しキャンセル処理も同様 - Hyz-sui
    public bool CheckSeeDeathReason(PlayerControl seen) => AmMadmate && Options.MadmateCanSeeDeathReason.GetBool();
    public bool CheckKillFlash(MurderInfo info) => AmMadmate && Options.MadmateCanSeeKillFlash.GetBool();

    /// <summary>
    /// 陣営状態
    /// </summary>
    public enum TeamType : byte
    {
        /// <summary>
        /// どこの陣営にも属していない状態
        /// </summary>
        None = 0,

        // 10-49 シェリフキルオプションを作成しない変化先

        /// <summary>
        /// インポスター陣営に所属する状態
        /// </summary>
        Mad = 10,
        Impostor,
        /// <summary>
        /// クルー陣営に所属する状態
        /// </summary>
        Crew,

        // 50- シェリフキルオプションを作成する変化先

        /// <summary>
        /// ジャッカル陣営に所属する状態
        /// </summary>
        Jackal = 50,
        /// <summary>
        /// エゴイスト陣営に所属する状態
        /// </summary>
        Egoist,
        /// <summary>
        /// ダークハイド陣営に所属する状態
        /// </summary>
        DarkHide,
        /// <summary>
        /// オポチュニスト陣営に所属する状態
        /// </summary>
        Opportunist,
        /// <summary>
        /// 鬼陣営に所属する状態
        /// </summary>
        Ogre,
    }
    public static Color GetCatColor(TeamType catType)
    {
        Color? color = catType switch
        {
            TeamType.None => RoleInfo.RoleColor,
            TeamType.Mad => Utils.GetRoleColor(CustomRoles.Madmate),
            TeamType.Crew => Utils.GetRoleColor(CustomRoles.Crewmate),
            TeamType.Jackal => Utils.GetRoleColor(CustomRoles.Jackal),
            TeamType.Egoist => Utils.GetRoleColor(CustomRoles.Egoist),
            TeamType.DarkHide => Utils.GetRoleColor(CustomRoles.DarkHide),
            TeamType.Opportunist => Utils.GetRoleColor(CustomRoles.Opportunist),
            TeamType.Ogre => Utils.GetRoleColor(CustomRoles.Ogre),
            _ => null,
        };
        if (!color.HasValue)
        {
            logger.Warn($"不明な猫に対する色の取得: {catType}");
            return Utils.GetRoleColor(CustomRoles.Crewmate);
        }
        return color.Value;
    }
}

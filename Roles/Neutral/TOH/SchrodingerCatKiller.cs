using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Roles.Neutral.SchrodingerCat;

namespace TownOfHostY.Roles.Neutral;

public sealed class SchrodingerCatKiller : RoleBase, IKiller, IAdditionalWinner, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SchrodingerCatKiller),
            player => new SchrodingerCatKiller(player),
            CustomRoles.SchrodingerCatKiller,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuTOH + 390,
            SetupOptionItem,
            "シュレディンガーの猫",
            "#696969"
        );
    public SchrodingerCatKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        planedCats = new();
        changeCheckList = new();
    }

    private static Dictionary<byte, (PlayerControl cat, PlayerControl owner, CustomRoles ownerRole,
        TeamType team, CountTypes countType)> planedCats = new();
    private static Dictionary<byte, bool> changeCheckList = new();

    private TeamType _team = TeamType.None;

    public TeamType SchrodingerCatChangeTo
        => Team == TeamType.Impostor ? TeamType.Mad : Team;
    public TeamType Team
    {
        get => _team;
        private set
        {
            logger.Info($"{Player.GetRealName()}の陣営を{value}に変更");
            _team = value;
        }
    }
    public Color DisplayRoleColor => GetCatColor(Team);
    private static LogHandler logger = Logger.Handler(nameof(SchrodingerCatKiller));

    public static void SetupOptionItem()
    {
        if (Options.CustomRoleSpawnChances.TryGetValue(CustomRoles.SchrodingerCatKiller, out var spawnOption))
        {
            spawnOption.SetGameMode(CustomGameMode.HideMenu);
        }
    }
    public bool CanKill => true;
    //public float CalculateKillCooldown() => KillCooldown; //キルクールダウンはdefault固定
    public bool CanUseSabotageButton()
    {
        switch (Team)
        {
            case TeamType.Impostor: return true;
            case TeamType.Jackal: return Jackal.CanUseSabotage;
            case TeamType.Egoist: return true;
        }
        return false;
    }
    public bool CanUseImpostorVentButton()
    {
        switch (Team)
        {
            case TeamType.Impostor: return true;
            case TeamType.Jackal: return Jackal.CanVent;
            case TeamType.Egoist: return true;
        }
        return false;
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(true);
    public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
    public override void OverrideShowMainRoleText(ref Color roleColor, ref string roleText)
    {
        switch(Team)
        {
            case TeamType.Impostor: roleText = "(Impo)" + roleText; break;
            case TeamType.Jackal: roleText = "(Jack)" + roleText; break;
            case TeamType.Egoist: roleText = "(Ego)" + roleText; break;
            //case TeamType.DarkHide: roleText = "(Dark)" + roleText; break;
            //case TeamType.Opportunist: roleText = "(Oppo)" + roleText; break;
            //case TeamType.Ogre: roleText = "(Ogre)" + roleText; break;
        }

        // 色を変更
        roleColor = DisplayRoleColor;
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        bool? won = Team switch
        {
            TeamType.Impostor => CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor,
            TeamType.Jackal => CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal,
            TeamType.Egoist => CustomWinnerHolder.WinnerTeam == CustomWinner.Egoist,
            //TeamType.DarkHide => CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide,
            //TeamType.Ogre => CustomWinnerHolder.AdditionalWinnerRoles.Contains(CustomRoles.Ogre),
            //TeamType.Opportunist => Player.IsAlive(),
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
    public static Color GetCatColor(TeamType catType)
    {
        Color? color = catType switch
        {
            TeamType.None => RoleInfo.RoleColor,
            TeamType.Mad => Utils.GetRoleColor(CustomRoles.Madmate),
            TeamType.Impostor => Utils.GetRoleColor(CustomRoles.Impostor),
            TeamType.Crew => Utils.GetRoleColor(CustomRoles.Crewmate),
            TeamType.Jackal => Utils.GetRoleColor(CustomRoles.Jackal),
            TeamType.Egoist => Utils.GetRoleColor(CustomRoles.Egoist),
            //TeamType.DarkHide => Utils.GetRoleColor(CustomRoles.DarkHide),
            //TeamType.Opportunist => Utils.GetRoleColor(CustomRoles.Opportunist),
            //TeamType.Ogre => Utils.GetRoleColor(CustomRoles.Ogre),
            _ => null,
        };
        if (!color.HasValue)
        {
            logger.Warn($"不明な猫に対する色の取得: {catType}");
            return Utils.GetRoleColor(CustomRoles.Crewmate);
        }
        return color.Value;
    }
    public static void SetCatKiller(PlayerControl cat, PlayerControl owner)
    {
        if (cat == null || owner == null) return;

        if (!(owner.GetRoleClass() is ISchrodingerCatOwner catOwner)) return;
        var team = catOwner.SchrodingerCatChangeTo;
        switch (team)
        {
            case TeamType.Mad:
                team = TeamType.Impostor;
                break;
            case TeamType.Crew:
            case TeamType.Impostor:
            case TeamType.Jackal:
            case TeamType.Egoist:
            //case TeamType.DarkHide:   ダークハイドは対象外
            //case TeamType.Opportunist:   オポチュニストは対象外
            //case TeamType.Ogre:   鬼は対象外
                break;
            default:
                return;
        }

        var ownerRole = owner.GetCustomRole();
        var countType = owner.GetCountTypes();
        planedCats.Add(cat.PlayerId, (cat, owner, ownerRole, team, countType));

        logger.Info($"SetCatKiller owner: {owner?.name}({ownerRole}), cat:{cat?.name} =>SchrodingerCatKiller({team})");

        if (GameStates.IsMeeting ||
            team == TeamType.Crew) CheckCatKiller(cat.PlayerId);
        else _ = new LateTask(() =>
        {
            CheckCatKiller(cat.PlayerId);
            logger.Info($"{DeadDelay}");
        }, DeadDelay, "ChangeCatKiller");
    }
    public static void CheckCatKiller()
    {
        foreach (var playerId in planedCats.Keys)
        {
            CheckCatKiller(playerId, true);
        }
    }

    public static void FixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask) return;

        if (changeCheckList.Count == 0) return;

        foreach (var check in changeCheckList)
        {
            var change = CheckCatKiller(check.Key);
            changeCheckList[check.Key] = change;
        }
        changeCheckList = changeCheckList.Where(x => !x.Value).ToDictionary(x => x.Key, y => y.Value);
    }
    private static bool CheckCatKiller(byte playerId, bool force = false)
    {
        if (!planedCats.TryGetValue(playerId, out var change)) return true;
        (var cat, var owner, var ownerRole, var team, var countType) = change;

        if (!force && GameStates.IsInTask && (owner.inMovingPlat || owner.onLadder))
        {
            logger.Info($"LoopAssignCatKiller owner: {owner?.name}, inTask: {GameStates.IsInTask}, movingPlat: {owner.inMovingPlat}, ladder: {owner.onLadder}");
            changeCheckList[playerId] = false;
            return false;
        }

        planedCats.Remove(playerId);
        ChangeCatKiller(cat, owner, ownerRole, team, countType);

        return true;
    }
    private static void ChangeCatKiller(PlayerControl cat, PlayerControl owner, CustomRoles ownerRole, TeamType team, CountTypes countType)
    {
        if (cat == null || !cat.IsAlive()) return;
        if (owner == null || !owner.IsAlive()) return;

        PlayerState.GetByPlayerId(owner.PlayerId).DeathReason = CustomDeathReason.Kill;
        owner.SetRealKiller(owner);
        owner.RpcMurderPlayer(owner);
        logger.Info($"OwnerDead owner: {owner?.name} cat: {cat?.name}");

        //ゲームが終了しないようにCountTypeのみ先にセットする
        PlayerState.GetByPlayerId(cat.PlayerId).SetCountType(countType);
        logger.Info($"ChangeCountType cat: {cat?.name} countType: => {countType}");

        if (team == TeamType.Crew) return;

        _ = new LateTask(() => AssignCatKiller(cat, ownerRole, team, countType), 0.1f, "AssignCatKiller");
    }
    private static void AssignCatKiller(PlayerControl cat, CustomRoles ownerRole, TeamType team, CountTypes countType)
    {
        logger.Info($"AssignCatKiller cat:{cat?.name} =>SchrodingerCatKiller({team}) by {ownerRole}");

        RoleTypes roleTypes;
        foreach (var pc in Main.AllPlayerControls.Where(x => x != null && !x.Data.Disconnected))
        {
            var sameTeam = pc.GetRoleClass() is ISchrodingerCatOwner killer && killer.SchrodingerCatChangeTo == team;
            //シュレ猫視点
            roleTypes = RoleTypes.Scientist;
            if (pc.PlayerId == cat.PlayerId) roleTypes = RoleTypes.Impostor;
            else if (!pc.IsAlive()) roleTypes = RoleTypes.CrewmateGhost;
            else if (sameTeam) roleTypes = RoleTypes.Impostor;
            else if (pc.GetCustomRole().GetRoleTypes() == RoleTypes.Noisemaker) roleTypes = RoleTypes.Noisemaker;

            if (cat.PlayerId == PlayerControl.LocalPlayer.PlayerId) pc.SetRoleEx(roleTypes);
            else pc.RpcSetRoleDesync(roleTypes, cat.GetClientId());

            if (pc.PlayerId == cat.PlayerId) continue;

            //他クルー視点
            roleTypes = RoleTypes.Scientist;
            if (sameTeam) roleTypes = RoleTypes.Impostor;
            else if (team == TeamType.Impostor &&
                     !pc.GetCustomRole().GetRoleInfo().IsDesyncImpostor) roleTypes = RoleTypes.Impostor;

            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) cat.SetRoleEx(roleTypes);
            else cat.RpcSetRoleDesync(roleTypes, pc.GetClientId());

            if (sameTeam)
            {
                NameColorManager.Add(cat.PlayerId, pc.PlayerId, Utils.GetRoleColorCode(ownerRole));
                NameColorManager.Add(pc.PlayerId, cat.PlayerId, Utils.GetRoleColorCode(ownerRole));
            }
        }
        cat.RpcSetCustomRole(CustomRoles.SchrodingerCatKiller);
        PlayerState.GetByPlayerId(cat.PlayerId).SetCountType(countType);

        var catRole = (SchrodingerCatKiller)cat.GetRoleClass();
        catRole.Team = team;

        cat.SetKillCooldown();
        PlayerGameOptionsSender.SetDirty(cat.PlayerId);
        Utils.NotifyRoles(SpecifySeer: cat);
    }
}

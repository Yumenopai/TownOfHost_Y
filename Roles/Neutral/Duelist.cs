using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral;
public sealed class Duelist : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Duelist),
            player => new Duelist(player),
            CustomRoles.Duelist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            60700,
            null,
            "決闘者",
            "#ff6347"
        );
    public Duelist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        Duelists.Add(this);
        Archenemy = null;
    }
    public override void OnDestroy()
    {
        Duelists.Remove(this);
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
    }

    private static HashSet<Duelist> Duelists = new(15);
    PlayerControl Archenemy;

    public override void Add()
    {

    }
    public override (byte? votedForId, int? numVotes, bool doVote) OnVote(byte voterId, byte sourceVotedForId)
    {
        var (votedForId, numVotes, doVote) = base.OnVote(voterId, sourceVotedForId);
        if (MeetingStates.FirstMeeting && voterId == Player.PlayerId && Player.IsAlive())
        {
            if (sourceVotedForId != Player.PlayerId && sourceVotedForId < 253)
            {
                numVotes = 0;//投票を見えなくする
                var VotedForPC = Utils.GetPlayerById(sourceVotedForId);
                VotedForPC.RpcSetCustomRole(CustomRoles.Archenemy);
                Archenemy = VotedForPC;
                Utils.NotifyRoles();
            }
            else
            {
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            }
        }
        return (votedForId, numVotes, doVote);
    }


    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer == Player && seen == Archenemy)
            return Utils.ColorString(RoleInfo.RoleColor, "χ");
        return string.Empty;
    }
    public string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (seer == Archenemy && seen == Player)
            return Utils.ColorString(RoleInfo.RoleColor, "χ");
        return string.Empty;
    }

    public bool CheckWin(out AdditionalWinners winnerType)
    {
        winnerType = AdditionalWinners.Duelist;
        return Player.IsAlive() && !Archenemy.IsAlive();
    }

    public static bool ArchenemyCheckWin(PlayerControl pc)
    {
        foreach (var duelist in Duelists)
        {
            if (pc == duelist.Archenemy && !duelist.Player.IsAlive() && pc.IsAlive()) return true;
        }
        return false;
    }
    public static bool CheckNotify(PlayerControl pc)
    {
        foreach (var duelist in Duelists)
        {
            if (pc == duelist.Archenemy || pc == duelist.Player) return true;
        }
        return false;
    }
}
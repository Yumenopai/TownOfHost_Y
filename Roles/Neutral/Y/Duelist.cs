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
            (int)Options.offsetId.NeuY + 700,
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
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (MeetingStates.FirstMeeting && voterId == Player.PlayerId && Player.IsAlive())
        {
            if (sourceVotedForId != Player.PlayerId && sourceVotedForId < 253)
            {
                numVotes = 0;//投票を見えなくする
                var VotedForPC = Utils.GetPlayerById(sourceVotedForId);
                VotedForPC.SetCustomRole(CustomRoles.Archenemy);
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

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (!MeetingStates.FirstMeeting || !isForMeeting || !Player.IsAlive()) return string.Empty;

        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return Translator.GetString("DuelistVote").Color(RoleInfo.RoleColor);
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

    public bool CheckWin(ref CustomRoles winnerRole)
    {
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
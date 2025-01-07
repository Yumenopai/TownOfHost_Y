using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using static TownOfHostY.Roles.Unit.CounselorAndMadDilemma;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Counselor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Counselor),
            player => new Counselor(player),
            CustomRoles.Counselor,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.UnitMix + 0,//使用しない
            null,
            "カウンセラー",
            "#ffc0cb"
        );
    public Counselor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskTrigger = OptionTaskTrigger.GetInt();
        ChallengeMaxCount = OptionChallengeMaxCount.GetInt();
        //ResetAddonChangeCrew = OptionResetAddonChangeCrew.GetBool();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public override void OnDestroy()
    {
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
    }

    public static int ChallengeMaxCount;
    public static int TaskTrigger;
    //public static bool ResetAddonChangeCrew;
    int counselCount = 0;
    (bool, PlayerControl) Reserved = (false, null);

    public override void Add()
    {
        counselCount = ChallengeMaxCount;
        Reserved = (false, null);
    }

    public bool CanAbilityVote() => Player.IsAlive() && counselCount > 0 && TaskFinished()
        && Main.AllPlayerControls.Where(pc => pc.Is(CustomRoles.MadDilemma)).Any();
    public bool TaskFinished() => IsTaskFinished || MyTaskState.CompletedTasksCount >= TaskTrigger;

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (!isIntentional || !CanAbilityVote() || voterId != Player.PlayerId || sourceVotedForId == Player.PlayerId || sourceVotedForId >= 253)
        {
            if (voterId == Player.PlayerId) Reserved = (false, null);
            return baseVote;
        }

        counselCount--;
        var VotedForPC = Utils.GetPlayerById(sourceVotedForId);
        if (VotedForPC.Is(CustomRoles.MadDilemma))
        {
            Reserved = (true, VotedForPC);
            Logger.Info($"{Player.GetNameWithRole()}：Counsel⇒{VotedForPC.GetNameWithRole()}", "Counselor");
        }
        else
        {
            Logger.Info($"{Player.GetNameWithRole()}：Counsel失敗({VotedForPC.GetNameWithRole()}) 残り{counselCount}回", "Counselor");
        }
        return baseVote;
    }
    public static void AfterMeetingTask()
    {
        foreach(var pc in Main.AllPlayerControls.Where(pc=> pc.Is(CustomRoles.Counselor)))
        {
            if (pc.GetRoleClass() is not Counselor counselor || !counselor.Reserved.Item1) return;

            counselor.Reserved.Item2.RpcSetCustomRole(CustomRoles.Crewmate);
            //if (ResetAddonChangeCrew)
            //    PlayerState.GetByPlayerId(counselor.Reserved.Item2.PlayerId).SubRoles.Clear();
        }
    }

    public override string GetProgressText(bool comms = false)
    {
        if (!CanAbilityVote()) return string.Empty;

        return $"[{counselCount}]".Color(RoleInfo.RoleColor);
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (!CanAbilityVote() || !isForMeeting) return string.Empty;
        
        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        var color = counselCount > 0 ? RoleInfo.RoleColor : Color.gray;
        return Translator.GetString("DoCounsel").Color(color);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        if (!Reserved.Item1) return string.Empty;
        //seenが省略の場合seer
        seen ??= seer;
        if (seer == Player && (seen == Player || seen == Reserved.Item2)) return "○".Color(RoleInfo.RoleColor);

        return string.Empty;
    }
    public string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (!Reserved.Item1) return string.Empty;
        //seenが省略の場合seer
        seen ??= seer;
        if (seer == Reserved.Item2 && seen == Reserved.Item2) return "○".Color(RoleInfo.RoleColor);

        return string.Empty;
    }
}

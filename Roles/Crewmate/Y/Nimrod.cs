using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Nimrod : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Nimrod),
            player => new Nimrod(player),
            CustomRoles.Nimrod,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1300,
            null,
            "ニムロッド",
            "#9fcc5b"
        );
    public Nimrod(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        playerIdList = new();
        ExecutionMeetingPlayerId = byte.MaxValue;
    }
    public override void OnDestroy()
    {
        playerIdList.Clear();
    }

    public static List<byte> playerIdList = new();
    private static byte ExecutionMeetingPlayerId = byte.MaxValue;

    public override void Add()
    {
        playerIdList.Add(Player.PlayerId);
    }
    public static bool IsExecutionMeeting()
        => ExecutionMeetingPlayerId != byte.MaxValue;

    public static NetworkedPlayerInfo VoteChange(NetworkedPlayerInfo Exiled)
    {
        if (Exiled == null || !playerIdList.Contains(Exiled.PlayerId)) return Exiled;

        // 会議終了後にニムロッド会議を開く
        _ = new LateTask(() =>
        {
            if (!Exiled.IsDead)
            {
                // ニムロッド会議
                ExecutionMeetingPlayerId = Exiled.PlayerId;
                Logger.Info($"{Utils.GetPlayerById(ExecutionMeetingPlayerId).GetNameWithRole()} : ニムロッド会議", "Nimrod");
                // バニラ側の表示更新
                Utils.NotifyRoles(true, ForceLoop: true);
                Utils.GetPlayerById(Exiled.PlayerId).ReportDeadBody(Exiled);
            }
        }, 14.5f, "NimrodExiled");

        // 一旦誰も追放されずに終わる
        return null;
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (ExecutionMeetingPlayerId != Player.PlayerId || voterId != Player.PlayerId)
        {
            return baseVote;
        }

        // 誰かに投票していたら、その人を追放する
        if (sourceVotedForId <= 15)
        {
            Utils.GetPlayerById(sourceVotedForId).SetRealKiller(Player);
            PlayerState.GetByPlayerId(sourceVotedForId).DeathReason = CustomDeathReason.Execution;
            Logger.Info($"{Utils.GetPlayerById(ExecutionMeetingPlayerId).GetNameWithRole()} : ニムロッド追放→{Utils.GetPlayerById(sourceVotedForId).GetNameWithRole()}", "Nimrod");
        }
        // 会議の強制終了
        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
        return (votedForId, numVotes, false);
    }
    public override void AfterMeetingTasks()
    {
        if (!IsExecutionMeeting()) return;

        // ニムロッド会議終了の共通処理
        FinishNimrodMeeting();
    }
    private static void FinishNimrodMeeting()
    {
        // 自身は死亡する
        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, ExecutionMeetingPlayerId);
        // ニムロッド会議を解除する
        ExecutionMeetingPlayerId = byte.MaxValue;
        Logger.Info($"{Utils.GetPlayerById(ExecutionMeetingPlayerId).GetNameWithRole()} : ニムロッド会議の解除", "Nimrod");
    }

    public override void OnStartMeeting()
    {
        if (!IsExecutionMeeting()) return;

        Utils.SendMessage(Translator.GetString("IsNimrodMeetingText"),
            title: $"<color={RoleInfo.RoleColorCode}>{Translator.GetString("IsNimrodMeetingTitle")}</color>");
    }
    public static (string, int) AddMeetingDisplay()
    {
        if (!IsExecutionMeeting()) return ("", 0);

        string text = Translator.GetString("MDisplay.NimrodTitle").Color(RoleInfo.RoleColor);
        text += "\n";
        return (text, 1);
    }
}

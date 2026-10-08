using System.Linq;
using AmongUs.GameOptions;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral;

public sealed class Vendetta : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Vendetta),
            player => new Vendetta(player),
            CustomRoles.Vendetta,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuY + 1200,
            SetupOptionItem,
            "アベンジャー",
            "#e68ae6",
            true,
            assignInfo: new RoleAssignInfo(CustomRoles.Vendetta, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public Vendetta(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        SelfKillRequired = OptionSelfKillRequired.GetBool();
    }

    enum OptionName
    {
        VendettaSelfKillRequired
    }

    private static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionSelfKillRequired;
    private static bool HasImpostorVision;
    private static bool SelfKillRequired;

    PlayerControl Target = null;
    private bool isChooseTarget = false;
    private bool killedThisTurn = false;
    private bool selfKill = false;

    private static void SetupOptionItem()
    {
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.ImpostorVision, false, false);
        OptionSelfKillRequired = BooleanOptionItem.Create(RoleInfo, 11, OptionName.VendettaSelfKillRequired, false, false);
    }

    public override void Add()
    {
        isChooseTarget = false;
        killedThisTurn = false;
        selfKill = false;
    }

    public float CalculateKillCooldown() => CanUseKillButton() ? 0.1f : 0f;
    public bool CanUseKillButton() => isChooseTarget && Player.IsAlive() && !Target;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (SelfKillRequired && !selfKill)
        {
            Logger.Info($"Didn't kill target. {Player.name} is loser.", "Vendetta");
            return false;
        }

        return isChooseTarget && Player.IsAlive() && !Target.IsAlive();
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
                Target = VotedForPC;
                isChooseTarget = true;
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
        if (!MeetingStates.FirstMeeting || !isForMeeting || !Player.IsAlive())
        {
            return string.Empty;
        }

        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return Translator.GetString("VendettaVote").Color(RoleInfo.RoleColor);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer == Player && seen == Target)
        {
            return Utils.ColorString(RoleInfo.RoleColor, "χ");
        }

        return string.Empty;
    }

    public override void AfterMeetingTasks()
    {
        killedThisTurn = false;

        if (!Player.IsAlive() || isChooseTarget) return;
        
        Main.AfterMeetingDeathPlayers.TryAdd(Player.PlayerId, CustomDeathReason.Suicide);
        Logger.Info($"Didn't select target. {Player.name} is suicide.", "Vendetta");
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (killedThisTurn)
        {
            info.DoKill = false;
            Logger.Info($"{killer.GetNameWithRole()} : 既にこのターンにキルしています", "Vendetta");
            return;
        }

        // 通常キル
    }

    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        killedThisTurn = true;

        (var killer, var target) = info.AttemptTuple;

        if (target == Target)
        {
            selfKill = true;
        }
    }
}

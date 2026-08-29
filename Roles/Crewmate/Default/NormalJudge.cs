using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;

public sealed class NormalJudge : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(NormalJudge),
            player => new NormalJudge(player),
            CustomRoles.NormalJudge,
            () => RoleTypes.Judge,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewDefault + 500,
            SetupOptionItem,
            "ジャッジ",
            "#8cffff"
        );
    public NormalJudge(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        judgeTaskRequirement = OptionJudgeTaskRequirementPercentage.GetFloat();
        limitAbility = OptionAbilityCount.GetInt();
    }

    private static OptionItem OptionJudgeTaskRequirementPercentage;
    private static OptionItem OptionAbilityCount;
    private static OptionItem OptionCanJudgeMadmate;
    private static OptionItem OptionCanJudgeNeutrals;
    private static float judgeTaskRequirement;
    private bool isMisfire = false;
    private int limitAbility;

    enum OptionName
    {
        JudgeTaskRequirementPercentage,
        JudgeAbilityCount,
        JudgeCanJudgeMadmate,
        JudgeCanJudgeNeutrals,
    }
    private static void SetupOptionItem()
    {
        OptionJudgeTaskRequirementPercentage = FloatOptionItem.Create(RoleInfo, 3, OptionName.JudgeTaskRequirementPercentage, new(0f, 100f, 5f), 50f, false)
            .SetValueFormat(OptionFormat.Percent);
        OptionAbilityCount = IntegerOptionItem.Create(RoleInfo, 4, OptionName.JudgeAbilityCount, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanJudgeMadmate = BooleanOptionItem.Create(RoleInfo, 5, OptionName.JudgeCanJudgeMadmate, true, false);
        OptionCanJudgeNeutrals = BooleanOptionItem.Create(RoleInfo, 6, OptionName.JudgeCanJudgeNeutrals, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.JudgeTaskRequirementPercentage = judgeTaskRequirement;
    }

    public override bool CallJudgeVote(PlayerControl voter, PlayerControl votefor, ref byte ExilePlayerid)
        => Vanilla.Judge.JudgeOverrule(voter, votefor, ref ExilePlayerid, ref isMisfire, ref limitAbility,
            OptionCanJudgeMadmate.GetBool(), OptionCanJudgeNeutrals.GetBool());

    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
        => Vanilla.Judge.SetMisfireDeathReason(this, exiled, ref isMisfire);

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(limitAbility > 0 ? Color.yellow : Color.gray, $"({limitAbility})");
}

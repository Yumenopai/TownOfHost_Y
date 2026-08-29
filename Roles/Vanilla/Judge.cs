using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Vanilla;

public sealed class Judge : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Judge),
            player => new Judge(player),
            RoleTypes.Judge,
            "#8cffff"
        );
    public Judge(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private bool isMisfire = false;
    private int limitAbility = 1;

    public override bool CallJudgeVote(PlayerControl voter, PlayerControl votefor, ref byte ExilePlayerid)
        => JudgeOverrule(voter, votefor, ref ExilePlayerid, ref isMisfire, ref limitAbility,
            canJudgeMadmate: true, canJudgeNeutral: false);

    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
        => SetMisfireDeathReason(this, exiled, ref isMisfire);

    /// <summary>
    /// ジャッジの裁決の共通判定。<br/>
    /// バニラ同様、裁決先がキル対象なら相手を、そうでなければジャッジ自身を追放する。
    /// </summary>
    /// <param name="isMisfire">誤爆したかどうか。死因の設定に使う</param>
    /// <param name="limitAbility">残りの裁決回数。バニラ側の使用回数管理は
    /// AntiBlackout 有効時に消費されないため、ホスト側でも数える</param>
    public static bool JudgeOverrule(PlayerControl voter, PlayerControl votefor, ref byte ExilePlayerid,
                                     ref bool isMisfire, ref int limitAbility,
                                     bool canJudgeMadmate, bool canJudgeNeutral)
    {
        ExilePlayerid = byte.MaxValue;
        if (voter == null || votefor == null) return false;
        if (!votefor.IsAlive()) return false;
        if (limitAbility <= 0)
        {
            Logger.Info($"{voter.GetNameWithRole()} は裁決回数を使い切っています", "Judge");
            return false;
        }
        limitAbility--;

        var cRole = votefor.GetCustomRole();
        var canJudge = cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => true,
            CustomRoleTypes.Madmate => canJudgeMadmate,
            CustomRoleTypes.Neutral => canJudgeNeutral,
            _ => false,
        };

        if (canJudge)
        {
            ExilePlayerid = votefor.PlayerId;
            votefor.SetRealKiller(voter);
        }
        else
        {
            // 誤爆。ジャッジ自身が追放される
            isMisfire = true;
            ExilePlayerid = voter.PlayerId;
        }
        return true;
    }

    /// <summary>
    /// 死因の設定。<br/>
    /// ExileControllerWrapUpPatch が死因を Vote で上書きした後に呼ばれるため、ここで設定する。
    /// </summary>
    public static void SetMisfireDeathReason(RoleBase roleClass, NetworkedPlayerInfo exiled, ref bool isMisfire)
    {
        if (!isMisfire || exiled == null) return;
        isMisfire = false;
        if (exiled.PlayerId != roleClass.Player.PlayerId) return;
        roleClass.MyState.DeathReason = CustomDeathReason.Misfire;
    }
}

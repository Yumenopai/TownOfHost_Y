using System.Text;
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Utils;
using static TownOfHostY.Roles.Impostor.GotFather_Janitor;

namespace TownOfHostY.Roles.Impostor;

public sealed class Janitor : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Janitor),
            player => new Janitor(player),
            CustomRoles.Janitor,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 200,
            null,
            "ジャニター"
        );
    public Janitor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CleanCooldown = OptionCleanCooldown.GetFloat();
    }
    private static float CleanCooldown;
    public float CalculateKillCooldown() => CleanCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple; // 殺害を試みたキラーとターゲットを取得
        if (killer.Is(CustomRoles.Janitor))
        {
            info.DoKill = false;// Janitorはキルを防ぐ
            if (JanitorChance)
            {
                var targetPlayerState = PlayerState.GetByPlayerId(target.PlayerId); // ターゲットの状態を取得



                // ターゲットを死亡状態に設定し、追放する処理
                targetPlayerState.SetDead();
                Utils.GetPlayerById(target.PlayerId)?.RpcExileV2();
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Clean;
                killer.SetKillCooldown();
                JanitorChance = false;
                JanitorTarget = 0;
            }
        }
        else if (killer.Is(CustomRoles.GotFather))
        {
            JanitorTarget = target.PlayerId;
            Logger.CurrentMethod();
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting)
        {
            return GetArrows(seen);
        }
        else
        {
            return GetArrows(seen);
        }
    }
    private string GetArrows(PlayerControl seen)
    {

        var janitorTarget = GetPlayerById(JanitorTarget); // JanitorTarget のプレイヤーを取得
        var sb = new StringBuilder(80);//矢印の文字列を構築するためのインスタンスを作成
        if (JanitorChance)// && janitorTarget != null
        {
            sb.Append($"<color={GetRoleColorCode(CustomRoles.Impostor)}>");
            sb.Append(TargetArrow.GetArrows(Player, janitorTarget.PlayerId));
            sb.Append($"</color>");
            Logger.CurrentMethod();
        }
        return sb.ToString();
    }
    public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
    {
        if (JanitorChance && JanitorTarget != 0)
        {
            var janitorTarget = GetPlayerById(JanitorTarget); // JanitorTarget のプレイヤーを取得
            var targetPlayerState = PlayerState.GetByPlayerId(janitorTarget.PlayerId); // ターゲットの状態を取得

            // ターゲットを死亡状態に設定し、追放する処理
            targetPlayerState.SetDead();
            Utils.GetPlayerById(janitorTarget.PlayerId)?.RpcExileV2();
            PlayerState.GetByPlayerId(janitorTarget.PlayerId).DeathReason = CustomDeathReason.Clean;
        }
    }
}
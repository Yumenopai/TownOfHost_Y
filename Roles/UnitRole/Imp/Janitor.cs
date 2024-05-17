using System.Text;
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Utils;
using static TownOfHostY.Roles.Impostor.Gotfather_Janitor;
using System.Collections.Generic;

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
        JanitorTarget.Clear();
        var playerId = player.PlayerId;
        var trackerId = Player.PlayerId;
    }
    private static float CleanCooldown;
    public float CalculateKillCooldown() => CleanCooldown;
    public HashSet<byte> JanitorTarget = new(3);
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (JanitorChance)
        {
            var (killer, target) = info.AttemptTuple; // 殺害を試みたキラーとターゲットを取得
            if (killer.Is(CustomRoles.Janitor))
            {
                info.DoKill = false;// Janitorはキルを防ぐ
                var targetPlayerState = PlayerState.GetByPlayerId(target.PlayerId); // ターゲットの状態を取得


                // ターゲットを死亡状態に設定し、追放する処理
                targetPlayerState.SetDead();
                Utils.GetPlayerById(target.PlayerId)?.RpcExileV2();
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Clean;
                killer.SetKillCooldown();
                JanitorChance = false;
                JanitorTarget.Clear();
            }
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
        JanitorTarget.RemoveWhere(id => PlayerState.GetByPlayerId(id) == null);

        var sb = new StringBuilder(80);//矢印の文字列を構築するためのインスタンスを作成
        if (JanitorChance)// && janitorTarget != null
        {
            sb.Append($"<color={GetRoleColorCode(CustomRoles.Impostor)}>");
            foreach (var impostorId in JanitorTarget)
            {
                sb.Append(TargetArrow.GetArrows(Player, impostorId));
            }
            sb.Append($"</color>");
        }
        return sb.ToString();
    }
    public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
    {
        if (JanitorChance)
        {
            var meetingKillTarget = GetPlayerById(MeetingKillTarget); // MeetingKillTarget のプレイヤーを取得
            var targetPlayerState = PlayerState.GetByPlayerId(meetingKillTarget.PlayerId); // ターゲットの状態を取得

            // ターゲットを死亡状態に設定し、追放する処理
            targetPlayerState.SetDead();
            Utils.GetPlayerById(meetingKillTarget.PlayerId)?.RpcExileV2();
            PlayerState.GetByPlayerId(meetingKillTarget.PlayerId).DeathReason = CustomDeathReason.Clean;
            JanitorChance = false;
            MeetingKillTarget = 0;
            JanitorTarget.Clear();
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        base.OnFixedUpdate(player);
        if (JanitorChance)
        {
            foreach (var targetId in JanitorTarget)
            {
                // Janitorからターゲットへの矢印を追加
                TargetArrow.Add(player.PlayerId, targetId);

            }
        }
    }
}
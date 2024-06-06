using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Roles.Impostor.Gotfather_Janitor;
using System.Collections.Generic;
using System.Linq;
using Hazel;

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
        var trackerId = Player.PlayerId;//いらないかも
    }
    private static float CleanCooldown;
    public float CalculateKillCooldown() => CleanCooldown;
    public HashSet<byte> JanitorTarget = new(15);
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
            }
        }
    }
    public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
    {
        if (JanitorChance || JanitorTarget.Count > 0) // JanitorChanceがtrueかつJanitorTargetが空でない場合
        {

            foreach (var targetId in JanitorTarget)
            {
                var targetPlayer = Utils.GetPlayerById(targetId);
                if (targetPlayer != null)
                {
                    var targetPlayerState = PlayerState.GetByPlayerId(targetId);
                    targetPlayerState.SetDead();                                // プレイヤーを死亡状態に設定
                    targetPlayer.RpcExileV2();                                  // プレイヤーを追放する
                    targetPlayerState.DeathReason = CustomDeathReason.Clean;    // 死因をJanitorによる清掃とする
                }
            }

            // Janitorのチャンスをリセットし、JanitorTargetをクリアする
            JanitorChance = false;
            JanitorTarget.Clear();
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!JanitorChance || isForMeeting)// JanitorChanceがtrueで、ミーティング以外の場合に矢印を表示
        {
            return "";
        }
        else
        {
            var targetId = JanitorTarget.FirstOrDefault();
            var target = Utils.GetPlayerById(targetId);
            if (target == null) return "";
            return TargetArrow.GetArrows(Player, target.PlayerId);
        }
    }
}
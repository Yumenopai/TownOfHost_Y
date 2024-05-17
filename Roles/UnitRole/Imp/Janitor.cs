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
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting)
        {
            return ""; // 会議の時は何も表示しない
        }
        else
        {
            var sb = new StringBuilder(80); // 矢印の文字列を構築するためのインスタンスを作成
            if (JanitorChance)
            {
                sb.Append($"<color={GetRoleColorCode(CustomRoles.Impostor)}>");
                foreach (var targetId in JanitorTarget)
                {
                    sb.Append(TargetArrow.GetArrows(Player, targetId));
                }
                sb.Append($"</color>");
            }
            return sb.Length == 0 ? string.Empty : sb.ToString();
        }
    }
    public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
    {
        if (JanitorChance || JanitorTarget.Count > 0) // JanitorChanceがtrueかつJanitorTargetが空でない場合
        {

            Logger.CurrentMethod();
            foreach (var targetId in JanitorTarget)
            {
                Logger.CurrentMethod();
                var targetPlayer = Utils.GetPlayerById(targetId);
                if (targetPlayer != null)
                {
                    Logger.CurrentMethod();
                    var targetPlayerState = PlayerState.GetByPlayerId(targetId);
                    targetPlayerState.SetDead(); // プレイヤーを死亡状態に設定
                    targetPlayer.RpcExileV2(); // プレイヤーを追放する
                    targetPlayerState.DeathReason = CustomDeathReason.Clean; // 死因をJanitorによる清掃とする
                    Logger.CurrentMethod();
                }
            }

            // Janitorのチャンスをリセットし、JanitorTargetをクリアする
            JanitorChance = false;
            JanitorTarget.Clear();
            Logger.CurrentMethod();
        }
    }
}
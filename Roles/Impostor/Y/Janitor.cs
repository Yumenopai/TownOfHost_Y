using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using UnityEngine;

using static TownOfHostY.Utils;
namespace TownOfHostY.Roles.Impostor;
public sealed class Janitor : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Janitor),
            player => new Janitor(player),
            CustomRoles.Janitor,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1600,//仮
            SetUpOptionItem,
            "ジャニター"
        );
    public Janitor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CleanCooldown = OptionCleanCooldown.GetFloat();
        LookJanitor = OptionLookJanitor.GetFloat();
        JanitorTarget = byte.MaxValue;
        JanitorChance = false;
        CleanPlayer.Clear();
    }
    private static OptionItem OptionCleanCooldown;
    private static OptionItem OptionLookJanitor;
    enum OptionName
    {
        JanitorCleanCooldown,
        LookJanitor,
    }
    private static float CleanCooldown;
    private static float LookJanitor;
    public static byte JanitorTarget;
    private static bool JanitorChance;
    Dictionary<byte, object> CleanPlayer = new(14);
    private static void SetUpOptionItem()
    {
        OptionCleanCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.JanitorCleanCooldown, new(5.0f, 900f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);//掃除のクールダウン
        OptionLookJanitor = FloatOptionItem.Create(RoleInfo, 11, OptionName.LookJanitor, new(1.0f, 5f, 0.5f), 2f, false)
        .SetValueFormat(OptionFormat.Multiplier);//Janitorの距離
    }
    public float CalculateKillCooldown() => CleanCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (JanitorChance && JanitorTarget == target.PlayerId)
        {
            // キルをキャンセルし、Janitor の処理を実行
            info.DoKill = false;
            target.RpcSetPet("");
            target.Data.IsDead = true;
            AntiBlackout.SendGameData();
            Utils.NotifyRoles(ForceLoop: true);
            JanitorChance = false;
            killer.SetKillCooldown();

            // CleanPlayer にターゲットが含まれていない場合は追加
            if (!CleanPlayer.ContainsKey(target.PlayerId))
            {
                CleanPlayer.Add(target.PlayerId, null);
            }

            // 遅延タスクでターゲットを元に戻す処理を実行
            new LateTask(() =>
            {
                target.Data.IsDead = false;
                AntiBlackout.SendGameData();
                target.SetKillCooldown(2.5f);
                target.RpcResetAbilityCooldown();
                BackBody(target);
            }, 5, "Return Body");
        }
    }
    public static bool GuardPlayerCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        JanitorChance = true;
        if (!killer.Is(CustomRoles.Impostor) || killer.GetCustomRole().IsDirectKillRole())
        {// Impostor でない場合や、直接キルする役職である場合は処理を行わない
            return false;
        }

        foreach (var player in Main.AllAlivePlayerControls)
        {
            var distance = Vector2.Distance(killer.transform.position, player.transform.position);
            if (distance <= LookJanitor && player.Is(CustomRoles.Janitor))
            {
                killer.RpcProtectedMurderPlayer(target); //killer側のみ。斬られた側は見れない。
                player.RpcProtectedMurderPlayer(target); //Janitor側にも見えるかも？
                info.CanKill = false;
                JanitorTarget = target.PlayerId;
                break; // Janitorが見つかったらループを終了
            }
        }
        killer.SetKillCooldown();
        return true;
    }
    public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
    {
        foreach (var targetId in CleanPlayer.Keys)
        {
            var target = Utils.GetPlayerById(targetId);
            target.MyPhysics.RpcBootFromVent(GetNearestVent().Id);//[target]を付近のベントへ飛ばす。
            JanitorChance = false;
            BackBody(target);
            KillClean(target);
        }
        CleanPlayer.Clear();
    }
    Vent GetNearestVent()
    {
        var vents = ShipStatus.Instance.AllVents.OrderBy(v => (Player.transform.position - v.transform.position).magnitude);
        return vents.First();

    }
    public void BackBody(PlayerControl target)
    {
        Utils.NotifyRoles(ForceLoop: true);
        target.RpcResetAbilityCooldown();
    }
    public void KillClean(PlayerControl target)//完全に実態化して全員から見えるようにする処理。
    {
        target.RpcMurderPlayer(target);
        var playerState = PlayerState.GetByPlayerId(target.PlayerId);
        PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Clean;
        playerState.SetDead();
    }
}
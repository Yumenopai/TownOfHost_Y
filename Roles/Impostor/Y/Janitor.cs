using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHostY.Roles.Impostor;
public sealed class Janitor : RoleBase, IImpostor, IKillFlashSeeable
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
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = CleanCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        info.DoKill = false;
        if (JanitorChance)
        {
            if (JanitorTarget == target.PlayerId)
            {
                target.RpcSetPet("");
                target.Data.IsDead = true;
                AntiBlackout.SendGameData();
                Utils.NotifyRoles(ForceLoop: true);

                if (!CleanPlayer.ContainsKey(target.PlayerId))
                {
                    CleanPlayer.Add(target.PlayerId, null);
                }
                _ = new LateTask(() =>//幽霊から元に戻す処理。
                {
                    target.Data.IsDead = false;
                    AntiBlackout.SendGameData();
                    target.SetKillCooldown(2.5f);
                    target.RpcResetAbilityCooldown();
                    BackBody(target);
                }, 5, "");
            }
        }
    }
    public static bool GuardPlayerCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (!killer.Is(CustomRoles.Impostor)) return false;
        if (killer.GetCustomRole().IsDirectKillRole()) return false;//直接キルする役職のチェック
        foreach (var player in Main.AllAlivePlayerControls)
        {
            var distance = Vector2.Distance(killer.transform.position, player.transform.position);
            if (distance <= LookJanitor)
            {
                if (player.Is(CustomRoles.Janitor))
                {

                    killer.RpcProtectedMurderPlayer(target); //killer側のみ。斬られた側は見れない。
                    info.CanKill = false;
                    JanitorTarget = target.PlayerId;
                    JanitorChance = true;
                    break; // Janitorが見つかったらループを終了
                }
            }
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        JanitorChance = false;
    }
    public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
    {
        foreach (var targetId in CleanPlayer.Keys)
        {
            var target = Utils.GetPlayerById(targetId);
            target.MyPhysics.RpcBootFromVent(GetNearestVent().Id);//[target]を付近のベントへ飛ばす。
            BackBody(target);
            KillClean(target);
            JanitorChance = false;
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
        target.SetKillCooldown(Options.DefaultKillCooldown);
        target.RpcResetAbilityCooldown();
    }
    public void KillClean(PlayerControl target)//完全に実態化して全員から見えるようにする処理。
    {
        target.RpcMurderPlayer(target);
        var playerState = PlayerState.GetByPlayerId(target.PlayerId);
        //playerState.DeathReason = CustomDeathReason.○○;死亡理由をここに、のちほど。
        playerState.SetDead();
    }
}
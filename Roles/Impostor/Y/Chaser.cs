using System.Linq;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHostY.Roles.Impostor;
public sealed class Chaser : RoleBase, IImpostor, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Chaser),
            player => new Chaser(player),
            CustomRoles.Chaser,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1700,//仮
            SetUpOptionItem,
            "チェイサー"
        );
    public Chaser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        ChaseCooldown = OptionChaseCooldown.GetFloat();
        ReturnPosition = OptionReturnPosition.GetBool();
        PositionTime = OptionPositionTime.GetFloat();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionChaseCooldown;
    private static OptionItem OptionReturnPosition;
    private static OptionItem OptionPositionTime;
    private static float KillCooldown;
    private static float ChaseCooldown;
    public static bool ReturnPosition;
    private static float PositionTime;
    private static bool IsVentWarp;//飛ばすフラグ
    private Vector2 lastTransformPosition;
    enum OptionName
    {
        ChaserChaseCooldown,
        ChaserReturnPosition,
        ChaserPositionTime,
    }
    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionChaseCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ChaserChaseCooldown, new(1f, 180f, 1f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionReturnPosition = BooleanOptionItem.Create(RoleInfo, 12, OptionName.ChaserReturnPosition, false, false);
        OptionPositionTime = FloatOptionItem.Create(RoleInfo, 13, OptionName.ChaserPositionTime, new(1f, 99f, 1f), 10f, false, OptionReturnPosition)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool OnCheckShapeshift(PlayerControl target, ref bool animate)
    {
        IsVentWarp = true;
        lastTransformPosition = Player.transform.position;          // 変身前の位置を記録する

        Player.MyPhysics.RpcExitVent(GetNearestVent(target).Id);//ベントの位置へ飛ばす。
        animate = false;
        if (ReturnPosition)
        {
            MyState.CanUseMovingPlatform = false;                   //元の位置に戻る時はぬーんを使わせない。

            _ = new LateTask(() =>
            {
                if (Player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {//梯子をもし使っている場合
                    Logger.Info($"梯子を使っていたため元の位置に戻れませんでした。", "Chaser");
                    return;
                }
                else
                {
                    var returnVent = GetReturnNearestVent();
                    if (IsVentWarp)
                    {
                        Player.MyPhysics.RpcExitVent(returnVent.Id);        // 変身前の位置にプレイヤーを戻す
                        IsVentWarp = false;
                    }
                    lastTransformPosition = default;                            //位置をリセットする。
                    MyState.CanUseMovingPlatform = true;                        //元通りぬーんを使えるようにする。

                }
            }, PositionTime, "ReturnPosition");
        }
        return false;
    }
    Vent GetNearestVent(PlayerControl target)
    {
        var vents = ShipStatus.Instance.AllVents.OrderBy(v => (target.transform.position - v.transform.position).magnitude);
        return vents.First();
    }
    Vent GetReturnNearestVent()
    {
        var vents = ShipStatus.Instance.AllVents.OrderBy(v => (lastTransformPosition - (Vector2)v.transform.position).magnitude);
        return vents.First();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = ChaseCooldown;
    }
    public float CalculateKillCooldown() => KillCooldown;
}
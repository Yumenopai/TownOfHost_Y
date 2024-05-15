using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
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

            var targetPlayerState = PlayerState.GetByPlayerId(target.PlayerId); // ターゲットの状態を取得

            // キルを防ぐ
            info.DoKill = false;

            // ターゲットを死亡状態に設定し、追放する処理
            targetPlayerState.SetDead();
            Utils.GetPlayerById(target.PlayerId)?.RpcExileV2();
            PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Clean;
            killer.SetKillCooldown();
        }
    }
}
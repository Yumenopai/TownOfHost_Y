using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class Telekinetic : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Telekinetic),
            player => new Telekinetic(player),
            CustomRoles.Telekinetic,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 2100,
            SetupOptionItem,
            "テレキネス"
        );
    public Telekinetic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        ChangeDeathReason = OptionChangeDeathReason.GetBool();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionChangeDeathReason;
    enum OptionName
    {
        TelekineticChangeDeathReason,
    }
    private static float KillCooldown;
    private static bool ChangeDeathReason;

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionChangeDeathReason = BooleanOptionItem.Create(RoleInfo, 11, OptionName.TelekineticChangeDeathReason, false, false);
    }

    public float CalculateKillCooldown() => KillCooldown;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        // 通常のキルは起こさない
        info.CanKill = false;

        // キルモーションなし（相手の自爆）を起こす
        if (CustomRoleManager.OnCheckMurder(Player, target, target, target, true))
        {
            // 死因変更
            if (ChangeDeathReason) {
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Telekinesis;
            }
            // キラーセット
            target.SetRealKiller(killer);
        }

        // 自身は全くキルしないことになるのでキルクールセットする
        killer.SetKillCooldown();
    }
}
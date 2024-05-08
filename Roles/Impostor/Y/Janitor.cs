using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using UnityEngine;

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
    private static void SetUpOptionItem()
    {
        OptionCleanCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.JanitorCleanCooldown, new(5.0f, 900f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionLookJanitor = FloatOptionItem.Create(RoleInfo, 11, OptionName.LookJanitor, new(1.0f, 5f, 0.5f), 2f, false)
        .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = CleanCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        info.DoKill = false;
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
                    break; // Janitorが見つかったらループを終了
                }
            }
        }
        return true;
    }
}
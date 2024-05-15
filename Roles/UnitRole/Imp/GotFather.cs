using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using UnityEngine;
using static TownOfHostY.Roles.Impostor.GotFather_Janitor;

namespace TownOfHostY.Roles.Impostor;
public sealed class GotFather : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GotFather),
            player => new GotFather(player),
            CustomRoles.GotFather,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 300,
            null,
            "ゴットファーザー"
        );
    public GotFather(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        GotFatherKillCooldown = OptionGotFatherKillCooldown.GetFloat();
        LookJanitor = OptionLookJanitor.GetFloat();
        JanitorChance = false;
    }
    private static float GotFatherKillCooldown;
    private static float LookJanitor;
    public float CalculateKillCooldown() => GotFatherKillCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (killer.Is(CustomRoles.GotFather))
        {
            foreach (var player in Main.AllAlivePlayerControls)
            {
                var distance = Vector2.Distance(killer.transform.position, player.transform.position);
                if (distance <= LookJanitor && player.Is(CustomRoles.Janitor))
                {
                    info.DoKill = false;
                    JanitorChance = true;
                    JanitorTarget = target.PlayerId;
                    break;
                }
            }
        }
    }
}

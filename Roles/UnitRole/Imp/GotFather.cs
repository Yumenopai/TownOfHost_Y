using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using UnityEngine;
using static TownOfHostY.Roles.Impostor.Gotfather_Janitor;

namespace TownOfHostY.Roles.Impostor;
public sealed class Gotfather : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Gotfather),
            player => new Gotfather(player),
            CustomRoles.Gotfather,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 300,
            null,
            "ゴットファーザー"
        );
    public Gotfather(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        GotfatherKillCooldown = OptionGotfatherKillCooldown.GetFloat();
        LookJanitor = OptionLookJanitor.GetFloat();
        JanitorChance = false;
        JanitorTarget.Clear();
    }
    private static float GotfatherKillCooldown;
    private static float LookJanitor;
    public HashSet<byte> JanitorTarget = new(3);
    public float CalculateKillCooldown() => GotfatherKillCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (killer.Is(CustomRoles.Gotfather))
        {
            foreach (var player in Main.AllAlivePlayerControls)
            {
                var distance = Vector2.Distance(killer.transform.position, player.transform.position);
                if (distance <= LookJanitor && player.Is(CustomRoles.Janitor))
                {
                    info.DoKill = false;
                    JanitorChance = true;

                    var targetId = target.PlayerId;
                    MeetingKillTarget = target.PlayerId;
                    JanitorTarget.Add(targetId);
                    TargetArrow.Add(killer.PlayerId, player.PlayerId);
                    break;
                }
            }
        }
    }
}

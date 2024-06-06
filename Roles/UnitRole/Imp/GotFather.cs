using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
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
    public HashSet<byte> JanitorTarget = new(15);
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
                    var playerId = player.PlayerId;
                    JanitorTarget.Add(targetId);
                    TargetArrow.Add(playerId, targetId);
                    SendRPC(targetId);
                    break;
                }
            }
        }
    }
    private void SendRPC(byte targetId)
    {
        using var sender = CreateSender(CustomRPC.SetJanitorTarget);
        sender.Writer.Write(targetId);
        Logger.CurrentMethod();
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        Logger.CurrentMethod();
        if (rpcType != CustomRPC.SetJanitorTarget) return;
        byte targetId = reader.ReadByte();
        if (JanitorChance) TargetArrow.Add(Player.PlayerId, targetId);
    }
}

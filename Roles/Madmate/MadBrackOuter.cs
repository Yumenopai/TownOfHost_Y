using AmongUs.GameOptions;
using Hazel;
using InnerNet;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;
public sealed class MadBrackOuter : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadBrackOuter),
            player => new MadBrackOuter(player),
            CustomRoles.MadBrackOuter,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            10500,
            null,
            "マッドブラックアウター",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadBrackOuter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
    }

    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, Player.GetClientId());
        SabotageFixWriter.Write((byte)SystemTypes.Electrical);
        SabotageFixWriter.WriteNetObject(Player);
        AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);

        foreach (var target in Main.AllPlayerControls)
        {
            if (target == Player || target.Data.Disconnected) continue;
            SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, target.GetClientId());
            SabotageFixWriter.Write((byte)SystemTypes.Electrical);
            SabotageFixWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
        }
        return true;
    }

    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
}
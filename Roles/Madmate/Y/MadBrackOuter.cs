using AmongUs.GameOptions;
using Hazel;
using InnerNet;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Madmate;

public sealed class MadBrackOuter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadBrackOuter),
            player => new MadBrackOuter(player),
            CustomRoles.MadBrackOuter,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            (int)Options.offsetId.MadY + 200,
            SetupOptionItem,
            "マッドブラックアウター",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadBrackOuter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public static void SetupOptionItem()
    {
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 10, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        Player.RpcDesyncUpdateSystem(SystemTypes.Electrical, 0);

        foreach (var target in Main.AllPlayerControls)
        {
            if (target == Player || target.Data.Disconnected) continue;
            target.RpcDesyncUpdateSystem(SystemTypes.Electrical, 0);
        }
        return true;
    }
}
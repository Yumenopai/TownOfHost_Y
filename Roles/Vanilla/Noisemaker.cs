using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Vanilla;

public sealed class Noisemaker : RoleBase
{
    public Noisemaker(PlayerControl player) : base(RoleInfo, player) { }
    public readonly static SimpleRoleInfo RoleInfo = SimpleRoleInfo.CreateForVanilla(typeof(Noisemaker), player => new Noisemaker(player), RoleTypes.Noisemaker, "#8cffff");
}

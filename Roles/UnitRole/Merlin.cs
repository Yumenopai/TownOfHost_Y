using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Merlin : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Merlin),
            player => new Merlin(player),
            CustomRoles.Merlin,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 200,
            null,
            "マーリン",
            "#00ffff"
        );
    public Merlin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override void Add()
    {
        foreach (var impostor in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
        {
            NameColorManager.Add(Player.PlayerId, impostor.PlayerId);
        }
    }

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (seen.Is(CustomRoles.Assassin)) enabled = true;
    }
}
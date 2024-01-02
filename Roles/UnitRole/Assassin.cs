using System;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Translator;
using static TownOfHostY.Options;

namespace TownOfHostY.Roles.Impostor;
public sealed class Assassin : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Assassin),
            player => new Assassin(player),
            CustomRoles.Assassin,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 0,
            null,
            "アサシン"
        );
    public Assassin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
}
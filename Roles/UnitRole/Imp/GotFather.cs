using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Roles.Impostor.GotFather_Janitor;

namespace TownOfHostY.Roles.Impostor;
public sealed class GotFather : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GotFather),
            player => new GotFather(player),
            CustomRoles.GotFather,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 0,//使用しない
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

    }
    private static float GotFatherKillCooldown;
}

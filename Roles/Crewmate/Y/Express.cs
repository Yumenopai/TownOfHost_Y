using System.Linq;
using System.Collections.Generic;

using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Express : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Express),
            player => new Express(player),
            CustomRoles.Express,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 400,
            SetupOptionItem,
            "エクスプレス",
            "#00ffff"
        );
    public Express(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Speed = OptionSpeed.GetFloat();
    }

    private static OptionItem OptionSpeed;
    enum OptionName
    {
        ExpressSpeed
    }
    private static float Speed;

    private static void SetupOptionItem()
    {
        OptionSpeed = FloatOptionItem.Create(RoleInfo, 10, OptionName.ExpressSpeed, new(1.5f, 10f, 0.25f), 3.0f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Add()
    {
        // スピード設定
        Main.AllPlayerSpeed[Player.PlayerId] = Speed;
    }
    public override void OnDestroy()
    {
        Main.AllPlayerSpeed[Player.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
    }
}

using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class Janitor : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Janitor),
            player => new Janitor(player),
            CustomRoles.Janitor,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1600,//仮
            SetUpOptionItem,
            "ジャニター"
        );
    public Janitor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CleanCooldown = OptionCleanCooldown.GetFloat();
        //ShapeTarget = null;
    }
    private static OptionItem OptionCleanCooldown;
    enum OptionName
    {
        JanitorCleanCooldown,
    }
    private static float CleanCooldown;
    private static void SetUpOptionItem()
    {
        OptionCleanCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.JanitorCleanCooldown, new(10f, 900f, 2.5f), 60f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
}
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class VentManager : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(VentManager),
            player => new VentManager(player),
            CustomRoles.VentManager,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1700,
            SetupOptionItem,
            "ベントマネージャー",
            "#00ffff",
            assignInfo: new(CustomRoles.VentManager, CustomRoleTypes.Crewmate)
            {
               IsInitiallyAssignableCallBack = () => (MapNames)Main.NormalOptions.MapId is not MapNames.Polus and not MapNames.Fungle
            }
        );
    public VentManager(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    enum OptionName
    {
        FoxSpiritTaskCount,
    }

    public static OptionItem TaskCount;
    private static void SetupOptionItem()
    {
        TaskCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.FoxSpiritTaskCount, new(1, 30, 1), 15, false).SetValueFormat(OptionFormat.Pieces);
    }
    public static (bool, int, int) TaskData => (false, 0, TaskCount.GetInt());
}
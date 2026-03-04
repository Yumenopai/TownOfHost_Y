using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class NormalViper : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(NormalViper),
            player => new NormalViper(player),
            CustomRoles.NormalViper,
            () => RoleTypes.Viper,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpDefault + 300,
            SetupOptionItem,
            "バイパー"
        );
    public NormalViper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        viperDissolveTime = OptionViperDissolveTime.GetFloat();
        
    }
    private static OptionItem OptionViperDissolveTime;
    enum OptionName
    {
        ViperDissolveTime,
    }
    private static float viperDissolveTime;
   
    public static void SetupOptionItem()
    {
        OptionViperDissolveTime = FloatOptionItem.Create(RoleInfo, 3, OptionName.ViperDissolveTime, new(5f, 90f, 5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ViperDissolveTime = viperDissolveTime;
    }
}
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;

public sealed class NormalDetective : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(NormalDetective),
            player => new NormalDetective(player),
            CustomRoles.NormalDetective,
            () => RoleTypes.Detective,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewDefault + 400,
            SetupOptionItem,
            "探偵",
            "#8cffff"
        );
    public NormalDetective(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        detectiveSuspectLimit = OptionDetectiveSuspectLimit.GetFloat();
    }
    private static OptionItem OptionDetectiveSuspectLimit;
    enum OptionName
    {
        DetectiveSuspectLimit

    }
    private static float detectiveSuspectLimit;

    private static void SetupOptionItem()
    {
        OptionDetectiveSuspectLimit = FloatOptionItem.Create(RoleInfo, 3, OptionName.DetectiveSuspectLimit, new(0f, 180f, 5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
       
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.DetectiveSuspectLimit = detectiveSuspectLimit;
        
    }
}
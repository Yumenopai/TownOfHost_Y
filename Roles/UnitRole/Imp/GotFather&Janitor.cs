using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class GotFather_Janitor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GotFather_Janitor),
            player => new GotFather_Janitor(player),
            CustomRoles.GotFather_Janitor,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Unit,
            (int)Options.offsetId.UnitMix + 0,
            SetupOptionItem,
            "ゴットファーザー&ジャニター",
            assignInfo: new RoleAssignInfo(CustomRoles.GotFather_Janitor, CustomRoleTypes.Unit)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.GotFather, CustomRoles.Janitor }
            }
        );
    public GotFather_Janitor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        GotFatherKillCooldown = OptionGotFatherKillCooldown.GetFloat();
        CleanCooldown = OptionCleanCooldown.GetFloat();
        LookJanitor = OptionLookJanitor.GetFloat();
        LastImpostorCanKill = OptionLastImpostorCanKill.GetBool();
    }
    private static OptionItem OptionGotFatherKillCooldown;
    private static OptionItem OptionCleanCooldown;
    private static OptionItem OptionLookJanitor;
    public static OptionItem OptionLastImpostorCanKill;
    enum OptionName
    {
        GotFatherKillCooldown,
        JanitorCleanCooldown,
        LookJanitor,
        LastImpostorCanKill,
    }
    private static float GotFatherKillCooldown;
    private static float CleanCooldown;
    private static float LookJanitor;
    public static bool LastImpostorCanKill;
    private static void SetupOptionItem()
    {
        OptionGotFatherKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.GotFatherKillCooldown, new(5.0f, 900f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCleanCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.JanitorCleanCooldown, new(5.0f, 900f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);//掃除のクールダウン
        OptionLookJanitor = FloatOptionItem.Create(RoleInfo, 12, OptionName.LookJanitor, new(1.0f, 20f, 0.5f), 10f, false)
        .SetValueFormat(OptionFormat.Multiplier);//Janitorの距離
        OptionLastImpostorCanKill = BooleanOptionItem.Create(RoleInfo, 13, OptionName.LastImpostorCanKill, false, false);
    }
}

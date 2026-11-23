using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class Escalationer : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Escalationer),
            player => new Escalationer(player),
            CustomRoles.Escalationer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1200,
            SetupOptionItem,
            "エスカレーショナー"
        );
    public Escalationer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        SpeedUpRate = OptionSpeedUpRate.GetFloat();
        KillCoolDecrease = OptionKillCoolDecrease.GetFloat();
        SpeedAndCoolReset = OptionSpeedAndCoolReset.GetBool();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionSpeedUpRate;
    private static OptionItem OptionKillCoolDecrease;
    private static OptionItem OptionSpeedAndCoolReset;
    enum OptionName
    {
        EscalationerSpeedUpRate,
        EscalationerKillCoolDecrease,
        EscalationerSpeedAndCoolReset,
    }
    private static float KillCooldown;
    private static float SpeedUpRate;
    private static float KillCoolDecrease;
    private static bool SpeedAndCoolReset;
    float nowKillcool = 0f;

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillCoolDecrease = FloatOptionItem.Create(RoleInfo, 11, OptionName.EscalationerKillCoolDecrease, new(0f, 60f, 0.5f), 1.0f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSpeedUpRate = FloatOptionItem.Create(RoleInfo, 12, OptionName.EscalationerSpeedUpRate, new(0.1f, 2f, 0.05f), 0.15f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionSpeedAndCoolReset = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EscalationerSpeedAndCoolReset, false, false);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        nowKillcool = KillCooldown;
    }
    public float CalculateKillCooldown() => nowKillcool;

    public override void OnStartMeeting()
    {
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            nowKillcool = System.Math.Max(0f, nowKillcool - KillCoolDecrease);
            Logger.Info($"{killer.GetNameWithRole()}:キルクール減少:{nowKillcool}", "Escalationer");
            killer.ResetKillCooldown();

            foreach (var player in Main.AllPlayerControls)
            {
                Main.AllPlayerSpeed[player.PlayerId] += SpeedUpRate;
            }
            Utils.SyncAllSettings();
        }
    }

    public override void AfterMeetingTasks()
    {
        if (!SpeedAndCoolReset) return;

        nowKillcool = KillCooldown;
        Player.ResetKillCooldown();

        foreach (var player in Main.AllPlayerControls)
        {
            Main.AllPlayerSpeed[player.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
        }
        Utils.SyncAllSettings();
    }
}

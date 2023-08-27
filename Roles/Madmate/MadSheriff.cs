using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Madmate;
public sealed class MadSheriff : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadSheriff),
            player => new MadSheriff(player),
            CustomRoles.MadSheriff,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Madmate,
            5600,
            SetupOptionItem,
            "マッドシェリフ",
            requireResetCam: true
        );
    public MadSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        MisfireKillsTarget = OptionMisfireKillsTarget.GetBool();
        CanVent = OptionCanVent.GetBool();
    }

    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionMisfireKillsTarget;
    private static OptionItem OptionCanVent;

    enum OptionName
    {
        SheriffMisfireKillsTarget,
    }
    private static float KillCooldown;
    private static bool MisfireKillsTarget;
    public static bool CanVent;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, false, false);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? KillCooldown : 0f;
    public bool CanUseKillButton() => Player.IsAlive();
    public override bool OnInvokeSabotage(SystemTypes systemType) => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(Options.AddOnRoleOptions[(CustomRoles.MadSheriff, CustomRoles.AddLight)].GetBool());
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide) return;
        (var killer, var target) = info.AttemptTuple;
        // ガード持ちに関わらず能力発動する直接キル役職

        PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Misfire;
        killer.RpcMurderPlayerV2(killer);

        if (!MisfireKillsTarget) info.DoKill = false;
    }
}
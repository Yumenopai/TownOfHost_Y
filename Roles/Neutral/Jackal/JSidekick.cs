using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Modules;
using static UnityEngine.RemoteConfigSettingsHelper;

namespace TownOfHostY.Roles.Neutral;

public sealed class JSidekick : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JSidekick),
            player => new JSidekick(player),
            CustomRoles.JSidekick,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuJackal + 200,
            SetupOptionItem,
            "サイドキック",
            "#00b4eb",
            true,
            countType: CountTypes.Jackal
        );
    public JSidekick(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False)
    {
        promoted = false;
    }
    private static void SetupOptionItem()
    {
        if (Options.CustomRoleSpawnChances.TryGetValue(CustomRoles.JSidekick, out var spawnOption))
        {
            spawnOption.SetGameMode(CustomGameMode.HideMenu);
        }
    }
    private bool promoted = false;
    public bool Promoted {  get { return promoted; } }
    public bool CanUseKillButton() => promoted;
    public float CalculateKillCooldown() => Jackal.KillCooldown;
    public bool CanUseSabotageButton() => Jackal.CanUseSabotage;
    public bool CanUseImpostorVentButton() => Jackal.CanVent;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(Jackal.HasImpostorVision);
    public override bool OnInvokeSabotage(SystemTypes systemType) => Jackal.CanUseSabotage;
    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;
    public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);

    public void BePromoted()
    {
        if (Player == null) return;

        Logger.Info($"BePromoted sidekick: {Player?.name}", "JSidekick");
        promoted = true;

        Player.SetKillCooldown();
        PlayerGameOptionsSender.SetDirty(Player.PlayerId);
        Utils.NotifyRoles(SpecifySeer: Player);
    }

}
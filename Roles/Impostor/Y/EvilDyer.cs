using AmongUs.GameOptions;
using HarmonyLib;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class EvilDyer : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilDyer),
            player => new EvilDyer(player),
            CustomRoles.EvilDyer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1100,
            SetupOptionItem,
            "イビル真っ赤",
            assignInfo: new(CustomRoles.EvilDyer, CustomRoleTypes.Impostor)
            {
                IsInitiallyAssignableCallBack = () => !Options.IsSyncColorMode
            }
        );
    public EvilDyer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        killCooldown = OptionKillCooldown.GetFloat();
        dyerTime = OptionDyerTime.GetFloat();

        IsColorCamouflage = false;
    }
    public static bool IsColorCamouflage = false;
    // 0 = 赤
    private static NetworkedPlayerInfo.PlayerOutfit CamouflageRedOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set("", 0, "", "", "", "");

    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionDyerTime;
    float killCooldown;
    float dyerTime;
    enum OptionName
    {
        EvilDyerDyeTime,
    }
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDyerTime = FloatOptionItem.Create(RoleInfo, 11, OptionName.EvilDyerDyeTime, new(0.5f, 30f, 0.5f), 7f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public float CalculateKillCooldown() => killCooldown;

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (IsColorCamouflage && AmongUsClient.Instance.AmHost)
        {
            Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(false, pc));
            Utils.NotifyRoles(NoCache: true);
        }
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        (var killer, var target) = info.AttemptTuple;

        if (AmongUsClient.Instance.AmHost)
        {
            Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(true, pc, CamouflageRedOutfit));
            IsColorCamouflage = true;
            Utils.NotifyRoles(NoCache: true);

            _ = new LateTask(() =>
            {
                Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(false, pc));
                IsColorCamouflage = false;
                Utils.NotifyRoles(NoCache: true);
            }, dyerTime, "EvilDyer IsCamouflage");
        }
    }
}

using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostY.Attributes;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using System.Linq;

namespace TownOfHostY.Roles.Impostor;
public sealed class EvilIgnition : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(EvilIgnition),
            player => new EvilIgnition(player),
            CustomRoles.EvilIgnition,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1000,
            SetupOptionItem,
            "イビルイグニッション"
        );
    public EvilIgnition(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        IgnitionMaxCount = OptionIgnitionMaxCount.GetInt();
        FalseIgnition = OptionFalseIgnition.GetBool();
        IsCanBombTarget = OptionIsCanBombTarget.GetBool();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionIgnitionMaxCount;
    private static OptionItem OptionFalseIgnition;
    private static OptionItem OptionIsCanBombTarget;
    enum OptionName
    {
        EvilFireIgnitionMaxCount,
        EvilFireFalseIgnition,
        EvilFireIsCanBombTarget,
    }
    private static float KillCooldown;
    private static int IgnitionMaxCount;
    private static bool FalseIgnition;//オフなら使用回数減る
    private static bool IsCanBombTarget;

    int ignitionLimit;
    static bool OccurredBombed;

    [GameModuleInitializer]
    public static void Init()
    {
        OccurredBombed = false;
    }

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionIgnitionMaxCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.EvilFireIgnitionMaxCount, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionFalseIgnition = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EvilFireFalseIgnition, false, false);
        OptionIsCanBombTarget = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EvilFireIsCanBombTarget, false, false);
    }
    public override void Add()
    {
        ignitionLimit = IgnitionMaxCount;
        Player.AddDoubleTrigger();
    }

    public float CalculateKillCooldown() => KillCooldown;
    public override string GetProgressText(bool comms = false) => Utils.ColorString(ignitionLimit > 0 ? Palette.ImpostorRed : Color.gray, $"[{ignitionLimit}]");
    public static bool CanBombTarget()
    {
        if (Main.AllPlayerControls.Where(pc=> pc.Is(CustomRoles.EvilIgnition)).Any())
        {
            return IsCanBombTarget;
        }
        return false;
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide) return;
        if (ignitionLimit <= 0) return;
        if (!info.CanKill) return;

        (var killer, var target) = info.AttemptTuple;
        info.CanKill = killer.CheckDoubleTrigger(target, () => { IgnitionKill(killer, target); });
    }
    private void IgnitionKill(PlayerControl killer, PlayerControl target)
    {
        if (!killer.IsAlive() || !target.IsAlive()) return;

        var killerId = killer.PlayerId;
        var targetId = target.PlayerId;

        target.SetRealKiller(killer);
        killer.RpcMurderPlayer(target, true);
        Logger.Info($"{killer.GetNameWithRole()}：着火 着火先→{target.GetNameWithRole()} || 残り{ignitionLimit}回", "EvilFire");

        //爆破処理はホストのみ
        if (AmongUsClient.Instance.AmHost)
        {
            (float d, PlayerControl pc) nearTarget = (2.5f, null);
            foreach (var fire in Main.AllAlivePlayerControls)
            {
                if (fire == killer || fire == target) continue;
                var pos = target.transform.position;
                var dis = Vector2.Distance(pos, fire.transform.position);

                if (dis < nearTarget.d)
                {
                    nearTarget = (dis, fire);
                }
            }
            if (nearTarget.pc == null)
            {
                if(!FalseIgnition) ignitionLimit--;
            }
            else
            {
                ignitionLimit--;
                OccurredBombed = true;
                Logger.Info($"{killer.GetNameWithRole()}：着火爆破 爆破先→{nearTarget.pc.GetNameWithRole()}", "EvilFire");

                PlayerState.GetByPlayerId(nearTarget.pc.PlayerId).DeathReason = CustomDeathReason.IgnitionBombed;
                nearTarget.pc.SetRealKiller(killer);
                nearTarget.pc.RpcMurderPlayer(nearTarget.pc, true);
                killer.MarkDirtySettings();
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        OccurredBombed = false;
    }

    public static (string, int) AddMeetingDisplay()
    {
        if (!CanBombTarget() || !OccurredBombed) return ("", 0);

        string text = "●Bombed!!\n".Color(Palette.ImpostorRed);
        return (text, 1);
    }
}

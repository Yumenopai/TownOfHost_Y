using AmongUs.GameOptions;
using UnityEngine;

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

    static int IgnitionCount;
    static bool OccurredBombed;

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
        IgnitionCount = IgnitionMaxCount;
        Player.AddDoubleTrigger();

        OccurredBombed = false;
    }

    public float CalculateKillCooldown() => KillCooldown;
    public override string GetProgressText(bool comms = false) => Utils.ColorString(IgnitionCount > 0 ? Palette.ImpostorRed : Color.gray, $"[{IgnitionCount}]");
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
        if (IgnitionCount <= 0) return;

        (var killer, var target) = info.AttemptTuple;
        info.CanKill = killer.CheckDoubleTrigger(target, () => { IgnitionKill(killer, target); });
    }
    public static void IgnitionKill(PlayerControl killer, PlayerControl target)
    {
        var killerId = killer.PlayerId;
        var targetId = target.PlayerId;

        target.SetRealKiller(killer);
        killer.RpcMurderPlayer(target, true);
        Logger.Info($"{killer.GetNameWithRole()}：発火 発火先→{target.GetNameWithRole()} || 残り{IgnitionCount}回", "EvilFire");

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
                if(!FalseIgnition) IgnitionCount--;
            }
            else
            {
                IgnitionCount--;
                OccurredBombed = true;
                Logger.Info($"{killer.GetNameWithRole()}：発火爆破 爆破先→{nearTarget.pc.GetNameWithRole()}", "EvilFire");

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
        if (!IsCanBombTarget || !OccurredBombed) return ("", 0);

        string text = "●Bombed!!\n".Color(Palette.ImpostorRed);
        return (text, 1);
    }
}
using UnityEngine;

using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Translator;
using System.Linq;

namespace TownOfHostY.Roles.Impostor;

public sealed class SelfBomber : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SelfBomber),
            player => new SelfBomber(player),
            CustomRoles.SelfBomber,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1500,
            SetupCustomOption,
            "自爆魔"
        );
    public SelfBomber(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        BombRadius = OptionBombRadius.GetFloat();
        BombCooldown = OptionBombCooldown.GetFloat();
    }

    static OptionItem OptionBombRadius;
    static OptionItem OptionBombCooldown;
    enum OptionName
    {
        SelfBomberBombRadius,
        SelfBomberBombCooldown,
    }

    float BombRadius;
    float BombCooldown;

    public static void SetupCustomOption()
    {
        OptionBombRadius = FloatOptionItem.Create(RoleInfo, 10, OptionName.SelfBomberBombRadius, new(0.5f, 3f, 0.5f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionBombCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.SelfBomberBombCooldown, new(5f, 180f, 2.5f), 15.0f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = BombCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Player == null || !Player.IsAlive()) return;
        if (Player.PlayerId != info.AttemptKiller.PlayerId) return;

        Player.RpcResetAbilityCooldown();
        Logger.Info($"ResetBombTimer(kill) bomber: {Player?.name}", "SelfBomber");
    }
    public override void AfterMeetingTasks()
    {
        if (Player == null || !Player.IsAlive()) return;

        Player.RpcResetAbilityCooldown();
        Logger.Info($"ResetBombTimer(afterMeeting) bomber: {Player?.name}", "SelfBomber");
    }
    public override bool OnCheckVanish(ref float killCooldown, ref bool canResetAbilityCooldown)
    {
        if (!AmongUsClient.Instance.AmHost) return false; // 爆破処理はホストのみ

        if (Player == null || !Player.IsAlive()) return false;
        Logger.Info($"BombFire bomber: {Player?.name}", "SelfBomber");

        var allKill = true;
        var pos = Player.transform.position;
        foreach (var fireTarget in Main.AllAlivePlayerControls)
        {
            if (fireTarget.inVent) { allKill = false; continue;}

            var dis = Vector2.Distance(pos, fireTarget.transform.position);
            if (dis > BombRadius) { allKill = false; continue; }

            PlayerState.GetByPlayerId(fireTarget.PlayerId).DeathReason = CustomDeathReason.Bombed;
            fireTarget.SetRealKiller(Player);
            fireTarget.RpcMurderPlayer(fireTarget);
        }

        if (allKill && Main.AllAlivePlayerControls.Count() < 1 &&
            CustomWinnerHolder.WinnerTeam == CustomWinner.Default)
        {
            // 自爆で全員死亡の場合、勝利陣営がない場合はインポスター陣営勝利
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
        }
        Player.MarkDirtySettings();

        Utils.NotifyRoles();

        return false;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        return seer != null && seer.IsAlive() && !isForMeeting && !isForHud ? GetString("SelfBomberFirePhase") : "";
    }
    public override string GetAbilityButtonText()
    {
        return Player != null && Player.IsAlive() ? GetString("SelfBomberExplosionButtonText") : "";
    }
}
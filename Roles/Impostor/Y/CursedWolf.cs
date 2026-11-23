using AmongUs.GameOptions;
using Hazel;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class CursedWolf : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(CursedWolf),
            player => new CursedWolf(player),
            CustomRoles.CursedWolf,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 200,
            SetupOptionItem,
            "呪狼"
        );
    public CursedWolf(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        GuardSpellTimes = OptionGuardSpellTimes.GetInt();
        nowKillMotion = (KillMotionOption)OptionKillMotion.GetValue();
    }
    public static OptionItem OptionKillMotion;
    private static OptionItem OptionGuardSpellTimes;
    enum OptionName
    {
        CursedWolfGuardSpellMotion,
        CursedWolfGuardSpellTimes,
    }
    enum KillMotionOption
    {
        MotionKill,
        MotionSuicide,
    };
    KillMotionOption nowKillMotion;

    private static int GuardSpellTimes;
    int SpellCount;

    public static void SetupOptionItem()
    {
        OptionKillMotion = StringOptionItem.Create(RoleInfo, 11, OptionName.CursedWolfGuardSpellMotion, EnumHelper.GetAllNames<KillMotionOption>(), 0, false);
        OptionGuardSpellTimes = IntegerOptionItem.Create(RoleInfo, 10, OptionName.CursedWolfGuardSpellTimes, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;

        SpellCount = GuardSpellTimes;
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{SpellCount}回", "CursedWolf");
    }



    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;
        if (SpellCount <= 0) return true;

        // ガード
        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        SpellCount--;
        Logger.Info($"{target.GetNameWithRole()} : 残り{SpellCount}回", "CursedWolf");

        //切り返す
        switch (nowKillMotion)
        {
            case KillMotionOption.MotionKill://自身がキル
                target.RpcMurderPlayer(killer);
                break;
            case KillMotionOption.MotionSuicide://相手が自爆
                killer.RpcMurderPlayer(killer);
                break;
        }
        PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Spell;
        // 自身は斬られない
        info.CanKill = false;
        return true;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(Palette.ImpostorRed, $"〔{SpellCount}〕");
}
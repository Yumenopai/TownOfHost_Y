using System.Collections.Generic;
using System.Text;

using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Translator;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Medic : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Medic),
            player => new Medic(player),
            CustomRoles.Medic,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            40900,
            SetupOptionItem,
            "メディック",
            "#6495ed",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Medic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskTrigger = OptionTaskTrigger.GetInt();
    }

    private static OptionItem OptionTaskTrigger;
    enum OptionName
    {
        TaskTrigger,
    }

    public static List<Medic> Medics = new();
    public static int TaskTrigger;

    PlayerControl GuardPlayer = null;
    bool UseVent = false;
    private static void SetupOptionItem()
    {
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TaskTrigger, new(0, 20, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
    }
    public override void Add()
    {
        Medics.Add(this);
        GuardPlayer = null;
        UseVent = true;

        Player.AddVentSelect();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = CanUseVent() ? 0f : 255f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public bool CanUseVent()
    => Player.IsAlive()
    && UseVent
    && MyTaskState.CompletedTasksCount >= TaskTrigger;

    /// <summary>
    /// 使用する時true
    /// </summary>
    public static bool GuardPlayerCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        // メディックに守られていなければなにもせず返す
        if (!IsGuard(target)) return false;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return false;

        killer.RpcGuardAndKill(target); //killer側のみ。斬られた側は見れない。

        foreach (var medic in Medics)
        {
            if (medic.GuardPlayer == target)
            {
                medic.GuardPlayer = null; break;
            }
        }
        return true;
    }
    public static bool IsGuard(PlayerControl target)
    {
        foreach (var medic in Medics)
        {
            if(target == medic.GuardPlayer) return true;
        }
        return false;
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseVent()) return false;

        GuardPlayer = Player.VentPlayerSelect(() =>
        {
            UseVent = false;
        });

        Utils.NotifyRoles(SpecifySeer: Player);
        return true;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (GuardPlayer != null && seen == GuardPlayer)
        {
            return Utils.ColorString(RoleInfo.RoleColor, "Σ");
        }
        return string.Empty;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || !CanUseVent() || isForMeeting) return string.Empty;

        var str = new StringBuilder();
        if (GuardPlayer == null)
            str.Append(GetString(isForHud ? "SelectPlayerTagBefore" : "SelectPlayerTagMiniBefore"));
        else
        {
            str.Append(GetString(isForHud ? "SelectPlayerTag" : "SelectPlayerTagMini"));
            str.Append(GuardPlayer.GetRealName(Options.GetNameChangeModes() == NameChange.Crew));
        }
        return str.ToString();
    }

    public override string GetAbilityButtonText() => GetString("ChangeButtonText");
}
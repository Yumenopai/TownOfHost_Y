using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral;
public sealed class Pirate : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Pirate),
            player => new Pirate(player),
            CustomRoles.Pirate,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuY + 1000,
            SetupOptionItem,
            "海賊",
            "#cc4b33",
            true,
            countType: CountTypes.Pirate,
            assignInfo: new RoleAssignInfo(CustomRoles.Pirate, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public Pirate(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCoolDown = OptionKillCoolDown.GetFloat();
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        LimitTurn = OptionLimitTurn.GetInt();
        CanImpostorBeGang = OptionCanImpostorBeGang.GetBool();
        CanMadmateBeGang = OptionCanMadmateBeGang.GetBool();
        CanNeutralBeGang = OptionCanNeutralBeGang.GetBool();

        if (BuffAddonAssignTarget.GetValue() == 0)//Random
        {
            int chance = IRandom.Instance.Next(0, BuffAddonRoles.Length);
            grantAddonRole = BuffAddonRoles[chance];
            Logger.Info($"ランダム付与属性決定：{grantAddonRole}", "Pirate");
        }
        else
        {
            grantAddonRole = BuffAddonRoles[BuffAddonAssignTarget.GetValue() - 1];
            Logger.Info($"付与属性：{grantAddonRole}", "Pirate");
        }

        TurnNumber = 1;
    }
    private static HashSet<Pirate> Pirates = new(15);

    private static OptionItem OptionKillCoolDown;
    private static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionLimitTurn;
    private static OptionItem OptionCanImpostorBeGang;
    private static OptionItem OptionCanMadmateBeGang;
    private static OptionItem OptionCanNeutralBeGang;
    private static OptionItem BuffAddonAssignTarget;
    private static Options.OverrideTasksData Tasks;
    enum OptionName
    {
        PirateLimitTurn,
        PirateImpostorCanBeGang,
        PirateMadmateCanBeGang,
        PirateNeutralCanBeGang,
        PirateBuffAddonAssignTarget,
    }
    private static float KillCoolDown;
    private static bool HasImpostorVision;
    private static int LimitTurn;
    private static bool CanImpostorBeGang;
    private static bool CanMadmateBeGang;
    private static bool CanNeutralBeGang;
    private static int TurnNumber;
    public static CustomRoles grantAddonRole = CustomRoles.NotAssigned;

    static CustomRoles[] BuffAddonRoles = CustomRolesHelper.AllAddOnRoles.Where(role => role.IsBuffAddOn() && role != CustomRoles.Loyalty).ToArray();
    static string[] buffRoleArrays = BuffAddonRoles.Select(role => role.ToString()).ToArray();
    static string[] randArrays = { "Random" };
    static string[] selectStringArray = randArrays.Concat(buffRoleArrays).ToArray();
    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.ImpostorVision, true, false);
        OptionLimitTurn = IntegerOptionItem.Create(RoleInfo, 12, OptionName.PirateLimitTurn, new(1, 30, 1), 3, false)
            .SetValueFormat(OptionFormat.Turns);
        OptionCanImpostorBeGang = BooleanOptionItem.Create(RoleInfo, 13, OptionName.PirateImpostorCanBeGang, false, false);
        OptionCanMadmateBeGang = BooleanOptionItem.Create(RoleInfo, 14, OptionName.PirateMadmateCanBeGang, true, false);
        OptionCanNeutralBeGang = BooleanOptionItem.Create(RoleInfo, 15, OptionName.PirateNeutralCanBeGang, true, false);

        BuffAddonAssignTarget = StringOptionItem.Create(RoleInfo, 16, OptionName.PirateBuffAddonAssignTarget, selectStringArray, 0, false);

        // 20-23を使用
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20, Options.CustomRoleSpawnChances[RoleInfo.RoleName], CustomRoles.Gang);
    }

    public PlayerControl Gang;
    bool isMadeGang = false;

    public override void Add()
    {
        var playerId = Player.PlayerId;
        Gang = null;
        Pirates.Add(this);

        isMadeGang = false;
    }
    public float CalculateKillCooldown() => KillCoolDown;
    public bool CanUseKillButton()
    {
        if (!isMadeGang) return true;
        if (Gang == null) return true;

        var state = Gang.GetPlayerTaskState();
        int rate = state.CompletedTasksCount * 100 / state.AllTasksCount;
        return rate >= 50;
    }
    public bool CanUseImpostorVentButton()
    {
        if (!isMadeGang) return false;
        if (Gang == null) return true;

        var state = Gang.GetPlayerTaskState();
        int rate = state.CompletedTasksCount * 100 / state.AllTasksCount;
        return rate >= 25;
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // ガード持ちに関わらず能力発動する直接キル役職

        if (isMadeGang) return;

        if (!CanImpostorBeGang && target.Is(CustomRoleTypes.Impostor))
        {
            DontKill(info);
            return;
        }
        if (!CanMadmateBeGang && target.Is(CustomRoleTypes.Madmate))
        {
            DontKill(info);
            return;
        }
        if (!CanNeutralBeGang && target.Is(CustomRoleTypes.Neutral))
        {
            DontKill(info);
            return;
        }

        Gang = target;
        Logger.Info($"{killer.GetNameWithRole()} : 一味にした[{target.GetNameWithRole()}]", "Pirate");

        Utils.NotifyRoles(SpecifySeer: killer);
        isMadeGang = true;
    }
    private void DontKill(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        killer.RpcProtectedMurderPlayer(target);
        info.CanKill = false;
    }

    // 一味の対象海賊
    public static PlayerControl PirateOfGang(PlayerControl target)
    {
        foreach (var pirate in Pirates.ToArray())
        {
            if (pirate.Gang == target) return pirate.Player;
        }
        return null;
    }

    // Gang task
    public static bool TargetSetGhostAndTask(PlayerControl target)
    {
        foreach (var pirate in Pirates.ToArray())
        {
            if (pirate.Gang != target) continue;

            if (AmongUsClient.Instance.AmHost)
            {
                target.SetCustomRole(CustomRoles.Gang);
                target.GetPlayerTaskState().CompletedTasksCount = 0;
                target.GetPlayerTaskState().AllTasksCount = target.Data.Tasks.Count;
                target.Data.RpcSetTasks(Array.Empty<byte>()); //タスクを再配布
                target.SyncSettings();
                Utils.NotifyRoles();
            }
            return true;
        }
        return false;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (seer == Player && Gang == seen) return Utils.ColorString(RoleInfo.RoleColor, "▲");

        return string.Empty;
    }
    public override void OnStartMeeting() => TurnNumber++;
    public override string GetProgressText(bool comms = false)
    {
        if (LimitTurn < TurnNumber || !Player.IsAlive() || isMadeGang) return string.Empty;

        return $"[{TurnNumber}/{LimitTurn}]".Color(RoleInfo.RoleColor);
    }
    public override void AfterMeetingTasks()
    {
        if (LimitTurn >= TurnNumber || !Player.IsAlive() || isMadeGang) return;

        Main.AfterMeetingDeathPlayers.TryAdd(Player.PlayerId, CustomDeathReason.Suicide);
        Logger.Info($"PlatonicLover:dead, Turn:{TurnNumber} > {LimitTurn}", "Pirate");
    }
}

using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TownOfHostY.Attributes;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Translator;

namespace TownOfHostY.Roles.Crewmate;

public sealed class Psychic : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Psychic),
            player => new Psychic(player),
            CustomRoles.Psychic,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1200,
            SetupOptionItem,
            "霊媒師",
            "#883fd1",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Psychic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCooldown.GetFloat();
        TaskTrigger = OptionTaskTrigger.GetInt();

    }
    private static OptionItem OptionCooldown;
    public static OptionItem MaxCheckRole;
    public static OptionItem ConfirmCamp;
    public static OptionItem KillerOnly;
    private static OptionItem OptionTaskTrigger;
    enum OptionName
    {
        PsychicMaxCheckRole,
        FortuneTellerConfirmCamp,
        FortuneTellerKillerOnly,
        TaskTrigger,
    }

    public static float Cooldown;
    public static int TaskTrigger;
    int divinationLimit = 0;
    bool IsCoolTimeOn = true;

    readonly HashSet<byte> revealedRolePlayers = new();

    [GameModuleInitializer]
    public static void Init() => VentSelect.Init();

    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 14, OptionName.TaskTrigger, new(0, 20, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        MaxCheckRole = IntegerOptionItem.Create(RoleInfo, 11, OptionName.PsychicMaxCheckRole, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        ConfirmCamp = BooleanOptionItem.Create(RoleInfo, 12, OptionName.FortuneTellerConfirmCamp, false, false);
        KillerOnly = BooleanOptionItem.Create(RoleInfo, 13, OptionName.FortuneTellerKillerOnly, false, false);
    }
    public override void Add()
    {
        IsCoolTimeOn = true;
        divinationLimit = MaxCheckRole.GetInt();
        revealedRolePlayers.Clear();
        VentSelect.ClearSelect(Player.PlayerId);
    }
    public override void OnDestroy() => VentSelect.ClearSelect(Player.PlayerId);
    public override void OnStartMeeting()
    {
        IsCoolTimeOn = true;
        VentSelect.ClearSelect();
    }

    // 通報時にも IsCoolTimeOn をリセットして SyncAllSettings 時に
    // クールダウン0が送信されないようにする
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        IsCoolTimeOn = true;
        VentSelect.ClearSelect();
    }

    public override bool OnCompleteTask()
    {
        if (!Player.IsAlive()) return true;

        if (MyTaskState.CompletedTasksCount == TaskTrigger || IsTaskFinished)
        {
            Player.MarkDirtySettings();
        }

        return true;
    }

    public override void AfterMeetingTasks() => Player.RpcResetAbilityCooldown();

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = CanUseVent() ? (IsCoolTimeOn ? Cooldown : 0f) : 255f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public bool CanUseVent()
        => Player.IsAlive()
        && divinationLimit > 0
        && (MyTaskState.CompletedTasksCount >= TaskTrigger || IsTaskFinished);

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseVent()) return false;

        if (!VentSelect.PlayerSelect(Player, GetSelectableTargets())) return false;

        IsCoolTimeOn = false;
        Player.MarkDirtySettings();

        Utils.NotifyRoles(SpecifySeer: Player);
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;

        if (VentSelect.OnFixedUpdate(Player, out var selectedId))
        {
            if (selectedId < 0) return;

            revealedRolePlayers.Add((byte)selectedId);
            if (divinationLimit > 0)
            {
                divinationLimit--;
            }
            IsCoolTimeOn = true;
            player.SyncSettings();
            player.RpcResetAbilityCooldown();
            Utils.NotifyRoles(SpecifySeer: Player);
        }
    }

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref string roleText)
    {
        if (!revealedRolePlayers.Contains(seen.PlayerId)) return;
        if (KillerOnly.GetBool() &&
            !(seen.GetCustomRole().IsImpostor() || seen.IsNeutralKiller() || seen.IsCrewKiller()
            || seen.Is(CustomRoles.MadSheriff) || seen.Is(CustomRoles.GrudgeSheriff))) return;

        enabled = true;

        if (!ConfirmCamp.GetBool()) return;   //役職表示

        //陣営表示
        if (seen.GetCustomRole().IsImpostor() || seen.GetCustomRole().IsMadmate())
        {
            roleText = GetString("TeamImpostor").Color(Palette.ImpostorRed);
        }
        else if (seen.GetCustomRole().IsNeutral())
        {
            roleText = GetString("Neutral").Color(Palette.Orange);
        }
        else
        {
            roleText = GetString("TeamCrewmate").Color(Palette.CrewmateBlue);
        }
    }
    public bool KnowTargetRoleColor(PlayerControl target)
    {
        if (!revealedRolePlayers.Contains(target.PlayerId)) return false;
        if (!ConfirmCamp.GetBool()) return false;   //役職表示
        if (KillerOnly.GetBool() &&
        !(target.GetCustomRole().IsImpostor() || target.IsNeutralKiller() || target.IsCrewKiller()
        || target.Is(CustomRoles.MadSheriff) || target.Is(CustomRoles.GrudgeSheriff))) return false;
        return true;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seer) || !Is(seen) || !CanUseVent() || isForMeeting) return string.Empty;

        var target = VentSelect.GetSelectedPlayer(seer);
        if (target == null) return GetString(isForHud ? "SelectPlayerTagBefore" : "SelectPlayerTagMiniBefore");

        return GetString(isForHud ? "SelectPlayerTag" : "SelectPlayerTagMini")
            + target.GetRealName(Options.GetNameChangeModes() == NameChange.Crew);
    }

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(RoleInfo.RoleColor, $"[{divinationLimit}]");
    public override string GetAbilityButtonText() => GetString("ChangeButtonText");

    private IEnumerable<PlayerControl> GetSelectableTargets()
        => Main.AllDeadPlayerControls.Where(target => !revealedRolePlayers.Contains(target.PlayerId));

    //死人がターゲットの特別なセレクト
    private static class VentSelect
    {
        private static readonly Dictionary<byte, PlayerControl> SelectedTargets = new();
        private static readonly HashSet<byte> FixedSelections = new();
        private static readonly Dictionary<byte, float> SelectionTimers = new();

        public static void Init()
        {
            SelectedTargets.Clear();
            FixedSelections.Clear();
            SelectionTimers.Clear();
        }

        public static void ClearSelect()
        {
            SelectedTargets.Clear();
            FixedSelections.Clear();
            SelectionTimers.Clear();
        }

        public static void ClearSelect(byte playerId)
        {
            SelectedTargets.Remove(playerId);
            FixedSelections.Remove(playerId);
            SelectionTimers.Remove(playerId);
        }

        public static bool PlayerSelect(PlayerControl player, IEnumerable<PlayerControl> candidates)
        {
            if (player == null) return false;

            var playerId = player.PlayerId;
            SelectionTimers.Remove(playerId); // 選択中ならタイマーをリセット

            if (FixedSelections.Contains(playerId)) return false;

            var targets = candidates?.Where(target => target != null && target != player).ToList() ?? new();
            if (targets.Count == 0)
            {
                SelectedTargets.Remove(playerId);
                return false;
            }

            var currentIndex = SelectedTargets.TryGetValue(playerId, out var currentTarget)
                ? targets.IndexOf(currentTarget)
                : -1;
            var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % targets.Count;
            var selectedTarget = targets[nextIndex];
            SelectedTargets[playerId] = selectedTarget;

            SelectionTimers[playerId] = 3f;
            Logger.Info($"{player.name} PlayerSelectNow:{selectedTarget.name}", "VentSelect");
            return true;
        }

        public static bool OnFixedUpdate(PlayerControl player, out int selectedId)
        {
            selectedId = -1;

            if (player == null) return false;
            if (!GameStates.IsInTask) return false;

            var playerId = player.PlayerId;
            if (!SelectionTimers.ContainsKey(playerId)) return false;

            SelectionTimers[playerId] -= Time.fixedDeltaTime;
            if (SelectionTimers[playerId] > 0) return false;

            if (!SelectedTargets.TryGetValue(playerId, out var target) || target == null || target.Data == null || target.Data.Disconnected)
            {
                ClearSelect(playerId);
                return false;
            }

            FixedSelections.Add(playerId);
            selectedId = target.PlayerId;
            SelectionTimers.Remove(playerId);

            Logger.Info($"{player.name} PlayerDecision:{target.name}", "VentSelect");

            player.RpcProtectedMurderPlayer(); // 選択確定のフィードバック

            return true;
        }

        public static PlayerControl GetSelectedPlayer(PlayerControl player)
            => player != null && SelectedTargets.TryGetValue(player.PlayerId, out var target) ? target : null;
    }
}

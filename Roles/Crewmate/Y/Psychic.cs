using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

using AmongUs.GameOptions;
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

        VentSelect.Init();
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

    public static float Cooldown = 30;
    public static int TaskTrigger;
    int DivinationLeftCount = 0;
    bool IsCoolTimeOn = true;

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
        DivinationLeftCount = MaxCheckRole.GetInt();
    }
    public override void OnStartMeeting()
    {
        IsCoolTimeOn = true;
        VentSelect.ClearSelect();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = CanUseVent() ? (IsCoolTimeOn ? Cooldown : 0f) : 255f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public bool CanUseVent()
        => Player.IsAlive()
        && DivinationLeftCount > 0
        && MyTaskState.CompletedTasksCount >= TaskTrigger;

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseVent()) return false;

        IsCoolTimeOn = false;
        Player.MarkDirtySettings();

        VentSelect.PlayerSelect(Player);

        Utils.NotifyRoles(SpecifySeer: Player);
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;

        if (VentSelect.OnFixedUpdate(Player, out var selectedId))
        {
            player.MarkDirtySettings();
            Utils.NotifyRoles(SpecifySeer: Player);
        }
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref string roleText)
    {
        if (!VentSelect.IsShowTargetRole(Player, seen)) return;
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
        if (!VentSelect.IsShowTargetRole(Player, target)) return false;
        if (!ConfirmCamp.GetBool()) return false;   //役職表示
        if (KillerOnly.GetBool() &&
        !(target.GetCustomRole().IsImpostor() || target.IsNeutralKiller() || target.IsCrewKiller()
        || target.Is(CustomRoles.MadSheriff) || target.Is(CustomRoles.GrudgeSheriff))) return false;
        return true;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || !CanUseVent() || isForMeeting) return string.Empty;

        return VentSelect.GetCheckPlayerText(seer, isForHud);
    }

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(RoleInfo.RoleColor, $"[{DivinationLeftCount}]");
    public override string GetAbilityButtonText() => GetString("ChangeButtonText");


    //死人がターゲットの特別なセレクト
    public static class VentSelect
    {
        public static Dictionary<byte, PlayerControl> SelectPlayer = new();
        public static Dictionary<byte, bool> SelectFix = new();
        public static Dictionary<byte, float> TergetFixTimer = new();
        public static Dictionary<byte, List<byte>> CheckRolePlayer = new();
        public static void Init()
        {
            SelectPlayer = new();
            SelectFix = new();
            TergetFixTimer = new();
            CheckRolePlayer = new();
        }
        public static void ClearSelect()
        {
            SelectPlayer.Clear();
            SelectFix.Clear();
            TergetFixTimer.Clear();
        }
        public static IEnumerable<PlayerControl> SelectList(byte playerId)
                    => Main.AllDeadPlayerControls.Where(x => !CheckRolePlayer[playerId].Contains(x.PlayerId));
        public static void PlayerSelect(PlayerControl player)
        {
            if (player == null) return;

            var playerId = player.PlayerId;
            if (TergetFixTimer.ContainsKey(playerId)) //タイマーリセット
                TergetFixTimer.Remove(playerId);

            if (SelectFix.TryGetValue(playerId, out var fix) && fix) return;

            PlayerControl first = null;
            SelectPlayer.TryGetValue(playerId, out var selectedPlayer);
            var preSelected = false;
            var selected = false;
            if (!CheckRolePlayer.ContainsKey(playerId)) CheckRolePlayer.Add(playerId, new());
            foreach (var target in SelectList(playerId))
            {
                if (target == player) continue;

                if (first == null) first = target;

                if (preSelected)
                {
                    SelectPlayer[playerId] = target;
                    selected = true;
                    Logger.Info($"{player.name} PlayerSelectNow:{target.name}, nextTarget", "player");
                    break;
                }

                if (target == selectedPlayer) preSelected = true;
            }
            if (first == null)
            {
                SelectPlayer[playerId] = null;
                Logger.Info($"{player.name} PlayerSelectNow:null, ターゲットなし", "player");
                return;
            }
            if (!selected)
            {
                SelectPlayer[playerId] = first;
                Logger.Info($"{player.name} PlayerSelectNow:{first?.name}, firstTarget", "player");
            }

            TergetFixTimer.Add(player.PlayerId, 3f);
        }
        public static bool OnFixedUpdate(PlayerControl player, out int selectedId)
        {
            selectedId = -1;

            if (player == null) return false;
            if (!GameStates.IsInTask) return false;

            var playerId = player.PlayerId;
            if (!TergetFixTimer.ContainsKey(playerId)) return false;

            TergetFixTimer[playerId] -= Time.fixedDeltaTime;
            if (TergetFixTimer[playerId] > 0) return false;

            //以下ターゲット確定
            SelectFix[playerId] = true;
            selectedId = SelectPlayer[playerId].PlayerId;

            if (!CheckRolePlayer.ContainsKey(playerId)) CheckRolePlayer.Add(playerId, new());
            CheckRolePlayer[playerId].Add(SelectPlayer[playerId].PlayerId);
            TergetFixTimer.Remove(playerId);

            Logger.Info($"{player.name} PlayerDecision:{SelectPlayer[playerId].name}", "player");

            player.RpcProtectedMurderPlayer();   //設定完了のパリン
            player.RpcResetAbilityCooldown();

            return true;
        }
        public static bool IsShowTargetRole(PlayerControl seer, PlayerControl target)
        {
            var IsWatch = false;
            CheckRolePlayer.Do(x =>
            {
                if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                    IsWatch = true;
            });
            return IsWatch;
        }
        public static string GetCheckPlayerText(PlayerControl psychic, bool isForHud)
        {
            if (psychic == null) return "";
            var psychicId = psychic.PlayerId;

            var str = new StringBuilder();
            SelectPlayer.TryGetValue(psychicId, out var target);
            if (target == null)
                str.Append(GetString(isForHud ? "SelectPlayerTagBefore" : "SelectPlayerTagMiniBefore"));
            else
            {
                str.Append(GetString(isForHud ? "SelectPlayerTag" : "SelectPlayerTagMini"));
                str.Append(target.GetRealName(Options.GetNameChangeModes() == NameChange.Crew));
            }
            return str.ToString();
        }
    }
}
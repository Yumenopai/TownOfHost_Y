using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class GrudgeSheriff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(GrudgeSheriff),
            player => new GrudgeSheriff(player),
            CustomRoles.GrudgeSheriff,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            36100,
            SetupOptionItem,
            "グラージシェリフ",
            "#f8cd46",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public GrudgeSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        KillCooldown = OptionKillCooldown.GetFloat();

    }

    private static OptionItem OptionKillCooldown;
    private static OptionItem MisfireKillsTarget;
    private static OptionItem ShotLimitOpt;
    private static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        SheriffCanKillNeutrals,
        SheriffCanKill,
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    PlayerControl KillWaitPlayerSelect = null;
    PlayerControl KillWaitPlayer = null;
    bool IsCoolTimeOn = true;

    public int ShotLimit = 0;
    public static float KillCooldown = 30;
    public static readonly string[] KillOption =
    {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 12, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffCanKillAllAlive, true, false);
        SetUpKillTargetOption(CustomRoles.Madmate, 13);
        CanKillNeutrals = StringOptionItem.Create(RoleInfo, 14, OptionName.SheriffCanKillNeutrals, KillOption, 0, false);
        SetUpNeutralOptions(30);
    }
    public static void SetUpNeutralOptions(int idOffset)
    {
        foreach (var neutral in CustomRolesHelper.AllRoles.Where(x => x.IsNeutral()).ToArray())
        {
            if (neutral is CustomRoles.SchrodingerCat
                        or CustomRoles.HASFox
                        or CustomRoles.HASTroll) continue;
            SetUpKillTargetOption(neutral, idOffset, true, CanKillNeutrals);
            idOffset++;
        }
    }
    public static void SetUpKillTargetOption(CustomRoles role, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        if (parent == null) parent = RoleInfo.RoleOption;
        var roleName = Utils.GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
        KillTargetOptions[role] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
        KillTargetOptions[role].ReplacementDictionary = replacementDic;
    }
    public override void Add()
    {
        KillWaitPlayerSelect = null;
        KillWaitPlayer = null;
        IsCoolTimeOn = true;
        ShotLimit = ShotLimitOpt.GetInt();

        Player.AddVentSelect();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = CanUseKillButton() ? (IsCoolTimeOn ? KillCooldown : 0f) : 255f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseKillButton()) return false;

        IsCoolTimeOn = false;
        Player.MarkDirtySettings();

        KillWaitPlayerSelect = Player.VentPlayerSelect(() =>
        {
            KillWaitPlayer = KillWaitPlayerSelect;
            IsCoolTimeOn = true;
            Player.MarkDirtySettings();
        });

        Utils.NotifyRoles(SpecifySeer: Player);
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;
        if (KillWaitPlayer == null) return;

        if (Player == null || !Player.IsAlive())
        {
            KillWaitPlayerSelect = null;
            KillWaitPlayer = null;
            return;
        }

        Vector2 GSpos = Player.transform.position;//GSの位置

        var target = KillWaitPlayer;
        float targetDistance = Vector2.Distance(GSpos, target.transform.position);

        var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
        if (targetDistance <= KillRange && Player.CanMove && target.CanMove)
        {
            ShotLimit--;
            Logger.Info($"{Player.GetNameWithRole()} : 残り{ShotLimit}発", "GrudgeSheriff");
            Player.RpcResetAbilityCooldown();

            if (!CanBeKilledBy(target))
            {
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Misfire;
                Player.RpcMurderPlayerEx(Player);
                Utils.MarkEveryoneDirtySettings();
                KillWaitPlayerSelect = null;
                KillWaitPlayer = null;

                if (!MisfireKillsTarget.GetBool())
                {
                    Utils.NotifyRoles(); return;
                }
            }
            target.SetRealKiller(Player);
            Player.RpcMurderPlayer(target);
            Utils.MarkEveryoneDirtySettings();
            KillWaitPlayerSelect = null;
            KillWaitPlayer = null;
            Utils.NotifyRoles();
        }
    }
    public static bool CanBeKilledBy(PlayerControl player)
    {
        var cRole = player.GetCustomRole();
        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => true,
            CustomRoleTypes.Madmate => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
            CustomRoleTypes.Neutral => CanKillNeutrals.GetValue() == 0 || !KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool(),
            _ => false,
        };
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || !CanUseKillButton() || isForMeeting) return string.Empty;

        var str = new StringBuilder();
        if (KillWaitPlayerSelect == null)
            str.Append(GetString(isForHud ? "SelectPlayerTagBefore" : "SelectPlayerTagMiniBefore"));
        else
        {
            str.Append(GetString(isForHud ? "SelectPlayerTag" : "SelectPlayerTagMini"));
            str.Append(KillWaitPlayerSelect.GetRealName());
        }
        return str.ToString();
    }

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(RoleInfo.RoleColor, $"({ShotLimit})");
    public override string GetAbilityButtonText() => GetString("ChangeButtonText");
}
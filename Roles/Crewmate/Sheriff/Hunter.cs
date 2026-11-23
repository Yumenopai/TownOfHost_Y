using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Roles.Neutral;
using UnityEngine;
using static TownOfHostY.Roles.Impostor.Witch;
using static TownOfHostY.Utils;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Hunter : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Hunter),
            player => new Hunter(player),
            CustomRoles.Hunter,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewSheriff + 100,
            SetupOptionItem,
            "ハンター",
            "#f8cd46",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Hunter(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
        KnowTargetRoleType = OptionKnowTargetRoleType.GetBool();
        KnowTargetMadmate = (ShowMarkTypeMadmate)OptionKnowTargetMadmate.GetValue();
        showTargetRoleType = null;
    }

    private static OptionItem KillCooldown;
    private static OptionItem ShotLimitOpt;
    private static OptionItem CanKillAllAlive;
    public static OptionItem IsInfoPoor;
    public static OptionItem IsClumsy;
    private static OptionItem OptionKnowTargetRoleType;
    private static OptionItem OptionKnowTargetMadmate;

    private static bool KnowTargetRoleType;
    private static ShowMarkTypeMadmate KnowTargetMadmate;

    enum OptionName
    {
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        SheriffIsInfoPoor,
        SheriffIsClumsy,
        HunterKnowTargetIsImpostor,
        HunterKnowTargetMadmate,
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    private CustomRoleTypes? showTargetRoleType = null;

    public enum ShowMarkTypeMadmate
    {
        ShowMarkTypeMadIsInpostor,
        ShowMarkTypeMadIsMadmate,
        ShowMarkTypeMadIsCrewmate,
    };


    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Crew;

    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 11, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SheriffCanKillAllAlive, true, false);
        IsInfoPoor = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffIsInfoPoor, false, false);
        IsClumsy = BooleanOptionItem.Create(RoleInfo, 16, OptionName.SheriffIsClumsy, false, false);
        OptionKnowTargetRoleType = BooleanOptionItem.Create(RoleInfo, 13, OptionName.HunterKnowTargetIsImpostor, true, false);
        OptionKnowTargetMadmate = StringOptionItem.Create(RoleInfo, 14, OptionName.HunterKnowTargetMadmate, EnumHelper.GetAllNames<ShowMarkTypeMadmate>(), 0, false, OptionKnowTargetRoleType);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();
        showTargetRoleType = null;
        ShotLimit = ShotLimitOpt.GetInt();
    }

    public float CalculateKillCooldown() => CanUseKillButton() ? CurrentKillCooldown : 0f;
    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide)
        {
            return;
        }

        (var killer, var target) = info.AttemptTuple;

        ShotLimit--;

        if (KnowTargetRoleType)
        {
            var targetRoleType = target.GetCustomRole().GetCustomRoleTypes();

            if (showTargetRoleType == CustomRoleTypes.Madmate)
            {
                switch (KnowTargetMadmate)
                {
                    case ShowMarkTypeMadmate.ShowMarkTypeMadIsInpostor:
                        showTargetRoleType = CustomRoleTypes.Impostor;
                        break;
                    case ShowMarkTypeMadmate.ShowMarkTypeMadIsMadmate:
                        showTargetRoleType = CustomRoleTypes.Madmate;
                        break;
                    case ShowMarkTypeMadmate.ShowMarkTypeMadIsCrewmate:
                        showTargetRoleType = CustomRoleTypes.Crewmate;
                        break;
                }
            }
            else
            {
                showTargetRoleType = targetRoleType;
            }

            Logger.Info($"ハンターキルによる陣営探知 {target.name} ({targetRoleType}, show:{showTargetRoleType})", "Hunter");

            NotifyRoles(SpecifySeer: killer);
        }
    }

    public override void OnStartMeeting()
    {
        showTargetRoleType = null;
    }

    public override string GetProgressText(bool comms = false) => ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"〈{ShotLimit}〉");

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seen != seer || !KnowTargetRoleType)
        {
            return string.Empty;
        }

        string mark = "";
        switch (showTargetRoleType)
        {
            case CustomRoleTypes.Impostor:
                mark = "<color=#ff1919>Ⓘ</color>";
                break;
            case CustomRoleTypes.Madmate:
                mark = "<color=#ff4500>Ⓜ</color>";
                break;
            case CustomRoleTypes.Crewmate:
                mark = "<color=#7ee6e6>Ⓒ</color>";
                break;
            case CustomRoleTypes.Neutral:
                mark = "<color=#ffa500>Ⓝ</color>";
                break;
        }
        return mark;
    }

    public void OnSchrodingerCatKill(SchrodingerCat schrodingerCat)
    {
        if (SchrodingerCat.ConsumeBullet)
        {
            ShotLimit--;
        }
    }
}
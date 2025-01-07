using System.Collections.Generic;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

using static TownOfHostY.Utils;
using TownOfHostY.Roles.Neutral;

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
        KnowTargetMadIsImpostor = OpKnowTargetMadIsImpostor.GetBool();
        isImpostor = 0;
    }

    private static OptionItem KillCooldown;
    private static OptionItem ShotLimitOpt;
    private static OptionItem CanKillAllAlive;
    public static OptionItem IsInfoPoor;
    public static OptionItem IsClumsy;
    private static OptionItem KnowTargetIsImpostor;
    private static OptionItem OpKnowTargetMadIsImpostor;
    private static bool KnowTargetMadIsImpostor;

    enum OptionName
    {
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        SheriffIsInfoPoor,
        SheriffIsClumsy,
        HunterKnowTargetIsImpostor,
        HunterKnowTargetMadIsImpostor,
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    int isImpostor = 0;
    public static readonly string[] KillOption =
    {
        "SheriffCanKillAll", "SheriffCanKillSeparately"
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
        KnowTargetIsImpostor = BooleanOptionItem.Create(RoleInfo, 13, OptionName.HunterKnowTargetIsImpostor, true, false);
        OpKnowTargetMadIsImpostor = BooleanOptionItem.Create(RoleInfo, 14, OptionName.HunterKnowTargetMadIsImpostor, true, false, KnowTargetIsImpostor);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();
        isImpostor = 0;
        ShotLimit = ShotLimitOpt.GetInt();
        Logger.Info($"{GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit}発", "Hunter");
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetHunterShotLimit);
        sender.Writer.Write(ShotLimit);
        sender.Writer.Write(isImpostor);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetHunterShotLimit) return;

        ShotLimit = reader.ReadInt32();
        isImpostor = reader.ReadInt32();
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
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            ShotLimit--;

            switch(target.GetCustomRole().GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    isImpostor = 1; break;
                case CustomRoleTypes.Madmate:
                    if(KnowTargetMadIsImpostor) isImpostor = 1;
                    else isImpostor = 0;
                    break;
                case CustomRoleTypes.Neutral:
                    isImpostor = 2; break;
                default:
                    isImpostor = 0; break;
            }

            SendRPC();
            NotifyRoles(SpecifySeer: killer);
            killer.ResetKillCooldown();
        }
    }
    public override string GetProgressText(bool comms = false) => ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"〈{ShotLimit}〉");

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seen == seer && KnowTargetIsImpostor.GetBool())
        {
            if (isImpostor == 1)
                return ColorString(RoleInfo.RoleColor, "◎");
            if (isImpostor == 2)
                return ColorString(RoleInfo.RoleColor, "▽");
        }
        return string.Empty;
    }
    public override void OnStartMeeting()
    {
        isImpostor = 0;
    }
    public void OnSchrodingerCatKill(SchrodingerCat schrodingerCat)
    {
        if (SchrodingerCat.ConsumeBullet)
        {
            ShotLimit--;
        }
    }
}
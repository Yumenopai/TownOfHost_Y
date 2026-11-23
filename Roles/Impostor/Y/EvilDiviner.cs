using AmongUs.GameOptions;
using System.Collections.Generic;
using Hazel;
using UnityEngine;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class EvilDiviner : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(EvilDiviner),
            player => new EvilDiviner(player),
            CustomRoles.EvilDiviner,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 600,
            SetupOptionItem,
            "イビルディバイナー"
        );
    public EvilDiviner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        DivinationMaxCount = OptionDivinationMaxCount.GetInt();
    }

    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionDivinationMaxCount;
    enum OptionName
    {
        EvilDivinerDivinationMaxCount,
    }
    private static float KillCooldown;
    private static int DivinationMaxCount;

    int divinationLimit;
    readonly HashSet<byte> divinationTargets = new();

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDivinationMaxCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.EvilDivinerDivinationMaxCount, new(1, 15, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        divinationTargets.Clear();
        divinationLimit = DivinationMaxCount;

        Player.AddDoubleTrigger();
    }

    public float CalculateKillCooldown() => KillCooldown;
    public override string GetProgressText(bool comms = false) => Utils.ColorString(divinationLimit > 0 ? Palette.ImpostorRed : Color.gray, $"[{divinationLimit}]");

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref string roleText)
    {
        if (divinationTargets.Contains(seen.PlayerId) && Player.IsAlive())
        {
            enabled = true;
        }
    }

    public bool OnCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        if (divinationLimit > 0)
        {
            return killer.CheckDoubleTrigger(target, () => { SetDivination(killer, target); });
        }
        else return true;
    }
    private void SetDivination(PlayerControl killer, PlayerControl target)
    {
        var targetId = target.PlayerId;
        if (divinationTargets.Add(targetId))
        {
            divinationLimit--;
            Logger.Info($"{killer.GetNameWithRole()}：占った 占い先→{target.GetNameWithRole()} || 残り{divinationLimit}回", "EvilDiviner");
            Utils.NotifyRoles(SpecifySeer: killer);

            //キルクールの適正化
            killer.SetKillCooldown();
        }
    }
}

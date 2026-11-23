using System.Linq;
using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral;
public sealed class Gang : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Gang),
            player => new Gang(player),
            CustomRoles.Gang,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuY + 1000,//使用しない
            null,
            "一味",
            "#cc4b33"
        );
    public Gang(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        //他視点用のMarkメソッド登録
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    private static HashSet<Gang> Gangs = new(15);

    bool canUseVent = false;
    bool canUseKill = false;
    bool grantAddon = false;
    bool isComplete = false;
    public override void Add()
    {
        Gangs.Add(this);

        canUseVent = false;
        canUseKill = false;
        grantAddon = false;
        isComplete = false;
    }

    public override bool OnCompleteTask()
    {
        var pirate = Pirate.PirateOfGang(Player);
        if (pirate == null) return true;

        int rate = MyTaskState.CompletedTasksCount * 100 / MyTaskState.AllTasksCount;

        if (!canUseVent && rate >= 25) canUseVent = true;
        if (!canUseKill && rate >= 50) canUseKill = true;
        if (!grantAddon && rate >= 75)
        {
            grantAddon = true;
            pirate.SetCustomRole(Pirate.grantAddonRole);
            Utils.NotifyRoles(SpecifySeer: pirate);
        }
        if (!isComplete && IsTaskFinished)
        {
            isComplete = true;
            foreach (var target in Main.AllPlayerControls.Where(pc=> pc.Is(CustomRoleTypes.Impostor)))
            {
                NameColorManager.Add(pirate.PlayerId, target.PlayerId);
            }
        }
        return true;
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        string mark = string.Empty;

        foreach (var gang in Gangs)
        {
            if (seer != seen) continue;
            if (seer != Pirate.PirateOfGang(gang.Player) && seer != gang.Player) continue;

            if (gang.canUseVent) mark += "Ｖ".Color(Color.cyan);
            if (gang.canUseKill) mark += "Ｋ".Color(Pirate.RoleInfo.RoleColor);
            if (gang.grantAddon) mark += "Ａ".Color(Utils.GetRoleColor(Pirate.grantAddonRole));
            if (gang.isComplete) mark += "Ｉ".Color(Color.red);
        }
        return mark;
    }

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        var pirate = Pirate.PirateOfGang(Player);
        if (pirate == null) return false;

        return CustomWinnerHolder.WinnerIds.Contains(pirate.PlayerId);
    }
}
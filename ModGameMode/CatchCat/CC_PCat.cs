using System.Linq;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Utils;
using static TownOfHostY.CatchCat.Option;
using static TownOfHostY.CatchCat.Common;
using AmongUs.GameOptions;

namespace TownOfHostY.CatchCat;

static class CatPlayer
{
    // Cat Vent
    public static bool CanUseVent(PlayerControl pc)
        => pc.IsAlive()
        && CanVent[pc.PlayerId];
    public static void ApplyGameOptions(PlayerControl pc, IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = CanUseVent(pc) ? T_VentCooldown.GetFloat() : 0f;
        AURoleOptions.EngineerInVentMaxTime = T_VentMaxTime.GetFloat();
    }

    // Cat Killed
    public static void OnCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        var targetRole = target.GetCustomRole();
        if (!targetRole.IsCCCatRoles()) return;

        if (targetRole.IsCCColorCatRoles() && !CanGuard[target.PlayerId])         // 所属済み猫
        {
            switch (NowColorCatKill)
            {
                case ColorCatKill.CCatJustKill:     // 無条件でキルされる
                    return;

                case ColorCatKill.COtherCatKill:    // 同陣営はキルできない
                    if (IsSameCamp_LederCat(killer.GetCustomRole(), targetRole)) info.DoKill = false;
                    return;

                case ColorCatKill.CCatOverride:     // 同陣営はキルできない
                    if (IsSameCamp_LederCat(killer.GetCustomRole(), targetRole))
                    {
                        info.DoKill = false; return;
                    }
                    break;  // 同陣営でない猫には上書き
            }
        }

        // 互いにパリン
        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        info.CanKill = false;

        if (targetRole.IsCCColorCatRoles()) //所属済みの猫
        {
            // ただのガードなのでここで返す
            if (CanGuard[target.PlayerId])
            {
                CanGuard[target.PlayerId] = false; return;
            }
            if (NowColorCatKill == ColorCatKill.CCatAlwaysGuard) return;

            // 元所属陣営の色を消す
            foreach (var pc in Main.AllPlayerControls)
            {
                if (IsSameCamp_LederCat(pc.GetCustomRole(), targetRole))
                {
                    NameColorManager.Remove(pc.PlayerId, target.PlayerId);
                }
                else if (ColorCatShowSameCamp.GetBool() && pc.GetCustomRole() == targetRole) //同陣営の猫
                {
                    NameColorManager.Remove(pc.PlayerId, target.PlayerId);
                    NameColorManager.Remove(target.PlayerId, pc.PlayerId);
                }
            }
        }

        // 役職変化
        switch (killer.GetCustomRole())
        {
            case CustomRoles.CCRedLeader:
                target.SetCustomRole(CustomRoles.CCRedCat);
                break;
            case CustomRoles.CCBlueLeader:
                target.SetCustomRole(CustomRoles.CCBlueCat);
                break;
            case CustomRoles.CCYellowLeader:
                target.SetCustomRole(CustomRoles.CCYellowCat);
                break;
        }
        NameColorManager.Add(killer.PlayerId, target.PlayerId);
        NameColorManager.Add(target.PlayerId, killer.PlayerId);

        targetRole = target.GetCustomRole();    // 陣営が変化しているので上書き
        // 同陣営の猫の色付け
        if (ColorCatShowSameCamp.GetBool())
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.GetCustomRole() == targetRole) //同陣営の猫
                {
                    NameColorManager.Add(pc.PlayerId, target.PlayerId);
                    NameColorManager.Add(target.PlayerId, pc.PlayerId);
                }
            }
        }

        NotifyRoles();
        MarkEveryoneDirtySettings();
    }

    // Task
    public static void OnCompleteTask(PlayerControl pc, TaskState ts)
    {
        int per = (int)(((float)ts.CompletedTasksCount / ts.AllTasksCount) * 100);
        Logger.Info($"{pc.GetRealName()} 完了タスク: {ts.CompletedTasksCount} / {ts.AllTasksCount} * 100 => {per}%", "OnCompleteTask");

        if (!IsSet[pc.PlayerId][0] && T_KnowAllLeader.GetInt() != 0 && per >= T_KnowAllLeader.GetInt())
        {
            foreach (var leader in Main.AllPlayerControls.Where(p => p.GetCustomRole().IsCCLeaderRoles()))
            {
                NameColorManager.Add(pc.PlayerId, leader.PlayerId);
                Logger.Info($"NameColorManager.Add({pc.GetRealName()}, {leader.GetRealName()})", "OnCompleteTask");
            }
            IsSet[pc.PlayerId][0] = true;
        }

        if (!IsSet[pc.PlayerId][1] && T_OneGuardOwn.GetInt() != 0 && per >= T_OneGuardOwn.GetInt())
        {
            CanGuard[pc.PlayerId] = true;
            Logger.Info($"CanGuard[{pc.GetRealName()}] : true", "OnCompleteTask");
            IsSet[pc.PlayerId][1] = true;
        }

        if (!IsSet[pc.PlayerId][2] && T_CanUseVent.GetInt() != 0 && per >= T_CanUseVent.GetInt())
        {
            CanVent[pc.PlayerId] = true;
            Logger.Info($"CanVent[{pc.GetRealName()}] : true", "OnCompleteTask");
            pc.MarkDirtySettings();
            pc.RpcResetAbilityCooldown();
            IsSet[pc.PlayerId][2] = true;
        }

        if (!IsSet[pc.PlayerId][3] && T_OwnLeaderKillcoolDecrease.GetInt() != 0 && per >= T_OwnLeaderKillcoolDecrease.GetInt())
        {
            Logger.Info($"OwnLeaderKillcoolDecrease[{pc.GetRealName()}] : true", "OnCompleteTask");
            IsSet[pc.PlayerId][3] = true;
        }
    }
}

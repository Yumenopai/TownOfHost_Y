using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownOfHostY.Roles.Core;
using static UnityEngine.RemoteConfigSettingsHelper;

namespace TownOfHostY;

static class GameModeUtils
{

    private static readonly int Id = 210000;
    public static OptionItem IgnoreVent;
    public static OptionItem LeaderNotKilled;
    public static OptionItem CatNotKilled;
    public static OptionItem CatOverrideKilled;

    public static void SetupCustomOption()
    {
        SetupLeaderRoleOptions(Id, CustomRoles.CCRedLeader);
        SetupLeaderRoleOptions(Id + 100, CustomRoles.CCBlueLeader);
        SetupAddLeaderRoleOptions(Id + 200, CustomRoles.CCYellowLeader);

        IgnoreVent = BooleanOptionItem.Create(Id + 1000, "IgnoreVent", false, TabGroup.MainSettings, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.CatchCat);
        LeaderNotKilled = BooleanOptionItem.Create(Id + 1001, "CCLeaderNotKilled", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.CatchCat);
        CatNotKilled = BooleanOptionItem.Create(Id + 1002, "CCCatNotKilled", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.CatchCat);
        CatOverrideKilled = BooleanOptionItem.Create(Id + 1003, "CCCatOverrideKilled", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.CatchCat);
    }
    public static void SetupLeaderRoleOptions(int id, CustomRoles role)
    {
        var spawnOption = IntegerOptionItem.Create(id, role.ToString() + "Fixed", new(100, 100, 1), 100, TabGroup.MainSettings, false).SetColor(Utils.GetRoleColor(role))
            .SetValueFormat(OptionFormat.Percent)
            .SetHeader(true)
            .SetFixValue(true)
            .SetGameMode(CustomGameMode.CatchCat) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, TabGroup.MainSettings, false).SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.CatchCat);

        Options.CustomRoleSpawnChances.Add(role, spawnOption);
        Options.CustomRoleCounts.Add(role, countOption);
    }
    public static void SetupAddLeaderRoleOptions(int id, CustomRoles role)
    {
        var spawnOption = IntegerOptionItem.Create(id, role.ToString(), new(0, 100, 100), 0, TabGroup.MainSettings, false).SetColor(Utils.GetRoleColor(role))
            .SetValueFormat(OptionFormat.Percent)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.CatchCat) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, TabGroup.MainSettings, false).SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(CustomGameMode.CatchCat);

        Options.CustomRoleSpawnChances.Add(role, spawnOption);
        Options.CustomRoleCounts.Add(role, countOption);
    }


    /// <summary>各条件に合ったプレイヤーの人数を取得し、配列に同順で格納します。</summary>
    public static int[] CountLivingPlayersByPredicates(params Predicate<PlayerControl>[] predicates)
    {
        int[] counts = new int[predicates.Length];
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            for (int i = 0; i < predicates.Length; i++)
            {
                if (predicates[i](pc)) counts[i]++;
            }
        }
        return counts;
    }


    public static bool OnCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (!Options.IsCCMode || (!target.Is(CustomRoles.CCNoCat) && !CatOverrideKilled.GetBool())) return true;

        // 互いにパリン
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);

        // 元所属陣営の色を消す
        if (!target.Is(CustomRoles.CCNoCat))
        {
            switch (target.GetCustomRole())
            {
                case CustomRoles.CCRedCat:
                    foreach (var leader in Main.AllPlayerControls.Where(pc => pc.GetCustomRole() == CustomRoles.CCRedLeader))
                        NameColorManager.Remove(leader.PlayerId, target.PlayerId);
                    break;

                case CustomRoles.CCBlueCat:
                    foreach (var leader in Main.AllPlayerControls.Where(pc => pc.GetCustomRole() == CustomRoles.CCBlueLeader))
                        NameColorManager.Remove(leader.PlayerId, target.PlayerId);
                    break;

                case CustomRoles.CCYellowCat:
                    foreach (var leader in Main.AllPlayerControls.Where(pc => pc.GetCustomRole() == CustomRoles.CCYellowLeader))
                        NameColorManager.Remove(leader.PlayerId, target.PlayerId);
                    break;
            }
        }

        // 役職変化
        switch (killer.GetCustomRole())
        {
            case CustomRoles.CCRedLeader:
                target.RpcSetCustomRole(CustomRoles.CCRedCat);
                break;

            case CustomRoles.CCBlueLeader:
                target.RpcSetCustomRole(CustomRoles.CCBlueCat);
                break;

            case CustomRoles.CCYellowLeader:
                target.RpcSetCustomRole(CustomRoles.CCYellowCat);
                break;
        }
        NameColorManager.Add(killer.PlayerId, target.PlayerId);

        Utils.NotifyRoles();
        Utils.MarkEveryoneDirtySettings();

        return false;
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Neutral;
using static TownOfHostY.Translator;

namespace TownOfHostY.Roles.Crewmate;

public sealed class NormalJudge : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(NormalJudge),
            player => new NormalJudge(player),
            CustomRoles.NormalJudge,
            () => RoleTypes.Judge,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewDefault + 500,
            SetupOptionItem,
            "ジャッジ",
            "#8cffff"
        );
    public NormalJudge(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        judgeTaskRequirement = OptionJudgeTaskRequirementPercentage.GetFloat();
        limitAbility = OptionAbilityCount.GetInt();
    }

    private static OptionItem OptionJudgeTaskRequirementPercentage;
    private static OptionItem OptionAbilityCount;
    private static OptionItem CanJudgeNeutrals;
    private static float judgeTaskRequirement;
    private bool isMisfire = false;
    private int limitAbility;

    public static Dictionary<CustomRoles, OptionItem> JudgeTargetOptions = new();
    public static Dictionary<SchrodingerCat.TeamType, OptionItem> SchrodingerCatJudgeTargetOptions = new();
    public static readonly string[] JudgeOption =
    {
        "SheriffCanKillAll", "SheriffCanKillSeparately"
    };

    enum OptionName
    {
        JudgeTaskRequirementPercentage,
        JudgeAbilityCount,
        JudgeCanJudgeNeutrals,
        JudgeCanJudge,
    }
    private static void SetupOptionItem()
    {
        OptionJudgeTaskRequirementPercentage = FloatOptionItem.Create(RoleInfo, 3, OptionName.JudgeTaskRequirementPercentage, new(0f, 100f, 5f), 50f, false)
            .SetValueFormat(OptionFormat.Percent);
        OptionAbilityCount = IntegerOptionItem.Create(RoleInfo, 4, OptionName.JudgeAbilityCount, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        SetUpJudgeTargetOption(CustomRoles.Madmate, 5);
        CanJudgeNeutrals = StringOptionItem.Create(RoleInfo, 6, OptionName.JudgeCanJudgeNeutrals, JudgeOption, 0, false);
        SetUpNeutralOptions(30);
    }
    public static void SetUpNeutralOptions(int idOffset)
    {
        foreach (var neutral in CustomRolesHelper.AllStandardRoles.Where(x => x.IsNeutral()).ToArray())
        {
            if (neutral is CustomRoles.SchrodingerCat) continue;
            SetUpJudgeTargetOption(neutral, idOffset, true, CanJudgeNeutrals);
            idOffset++;
        }
        foreach (var catType in EnumHelper.GetAllValues<SchrodingerCat.TeamType>())
        {
            if ((byte)catType < 50) continue;
            SetUpSchrodingerCatJudgeTargetOption(catType, idOffset, true, CanJudgeNeutrals);
            idOffset++;
        }
    }
    public static void SetUpJudgeTargetOption(CustomRoles role, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        parent ??= RoleInfo.RoleOption;
        var roleName = Utils.GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
        JudgeTargetOptions[role] = BooleanOptionItem.Create(id, OptionName.JudgeCanJudge + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
        JudgeTargetOptions[role].ReplacementDictionary = replacementDic;
    }
    public static void SetUpSchrodingerCatJudgeTargetOption(SchrodingerCat.TeamType catType, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        parent ??= RoleInfo.RoleOption;
        var inTeam = GetString("In%team%", new Dictionary<string, string>() { ["%team%"] = GetRoleString(catType.ToString()) });
        var catInTeam = Utils.ColorString(SchrodingerCat.GetCatColor(catType), Utils.GetRoleName(CustomRoles.SchrodingerCat) + inTeam);
        Dictionary<string, string> replacementDic = new() { ["%role%"] = catInTeam };
        SchrodingerCatJudgeTargetOptions[catType] = BooleanOptionItem.Create(id, OptionName.JudgeCanJudge + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
        SchrodingerCatJudgeTargetOptions[catType].ReplacementDictionary = replacementDic;
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.JudgeTaskRequirementPercentage = judgeTaskRequirement;
    }

    public override bool CallJudgeVote(PlayerControl voter, PlayerControl votefor, ref byte ExilePlayerid)
        => Vanilla.Judge.JudgeOverrule(voter, votefor, ref ExilePlayerid, ref isMisfire, ref limitAbility, CanBeJudgedBy);

    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
        => Vanilla.Judge.SetMisfireDeathReason(this, exiled, ref isMisfire);

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(limitAbility > 0 ? Color.yellow : Color.gray, $"({limitAbility})");

    public static bool CanBeJudgedBy(PlayerControl player)
    {
        var cRole = player.GetCustomRole();

        if (player.GetRoleClass() is SchrodingerCat schrodingerCat)
        {
            if (schrodingerCat.Team == SchrodingerCat.TeamType.None)
            {
                Logger.Warn($"ジャッジ({player.GetRealName()})に裁決されたシュレディンガーの猫のロールが変化していません", nameof(NormalJudge));
                return false;
            }
            return schrodingerCat.Team switch
            {
                SchrodingerCat.TeamType.Mad => JudgeTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
                SchrodingerCat.TeamType.Crew => false,
                _ => CanJudgeNeutrals.GetValue() == 0 || (SchrodingerCatJudgeTargetOptions.TryGetValue(schrodingerCat.Team, out var option) && option.GetBool()),
            };
        }

        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => true,
            CustomRoleTypes.Madmate => JudgeTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
            CustomRoleTypes.Neutral => CanJudgeNeutrals.GetValue() == 0 || !JudgeTargetOptions.TryGetValue(cRole, out var option) || option.GetBool(),
            _ => false,
        };
    }
}

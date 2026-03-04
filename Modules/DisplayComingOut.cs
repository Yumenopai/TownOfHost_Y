using System.Collections.Generic;
using System.Linq;
using TownOfHostY.Roles.Core;

namespace TownOfHostY;

class DisplayComingOut
{
    // CO可否表示
    private static OptionItem Enable;
    private static Dictionary<CustomRoleTypes, OptionItem> EachTypes = [];
    private static Dictionary<CustomRoles, OptionItem> EachRoles = [];

    private static readonly string[] displayComingOut =
    [
        "displayComingOut.None", "displayComingOut.OK", "displayComingOut.Limit", "displayComingOut.NG",
    ];
    public enum EnableComingOut
    {
        None,
        OK,
        Limit,
        NG,
    }
    public static EnableComingOut GetEnableComingOut(CustomRoles role) => (EnableComingOut)EachRoles[role].GetValue();

    public static void SetupCustomOption(int id)
    {
        Enable = BooleanOptionItem.Create(id + 0, "DisplayComingOutEnable", false, TabGroup.ModMainSettings, true).SetColor(Palette.CrewmateBlue);

        SetupTypeComingOut(id);
    }
    private static void SetupTypeComingOut(int baseId)
    {
        int idOffset = baseId + 1;
        foreach (var type in CustomRolesHelper.AllRoleTypes)
        {
            if (type is CustomRoleTypes.Unit) continue;

            Dictionary<string, string> replacementDic = new() { { "%type%", Translator.GetString($"CustomRoleTypes.{type}") } };
            EachTypes[type] = BooleanOptionItem.Create(idOffset, "displayComingOut%type%", false, TabGroup.ModMainSettings, true).SetParent(Enable);
            EachTypes[type].ReplacementDictionary = replacementDic;

            SetupRoleComingOut(type, baseId);

            idOffset++;
        }
    }
    private static void SetupRoleComingOut(CustomRoleTypes type, int baseId)
    {
        int idOffset = baseId + ((int)type + 1) * 100;
        foreach (var role in CustomRolesHelper.AllStandardRoles.Where(x => x.GetCustomRoleTypes() == type).ToArray())
        {
            if (IsDontShowRole(role)) continue;

            var roleName = Utils.GetRoleName(role);
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
            EachRoles[role] = StringOptionItem.Create(idOffset, "displayComingOut%role%", displayComingOut, 0, TabGroup.ModMainSettings, true).SetParent(EachTypes[type]);
            EachRoles[role].ReplacementDictionary = replacementDic;
            idOffset++;
        }
    }

    public static string GetString(CustomRoles role)
    {
        if (!Enable.GetBool()) return string.Empty;
        if (role == CustomRoles.GM) return string.Empty;
        if (!EachTypes[role.GetCustomRoleTypes()].GetBool()) return string.Empty;

        string coStr = string.Empty;
        switch (GetEnableComingOut(role))
        {
            case EnableComingOut.None:
                coStr = string.Empty;
                break;
            case EnableComingOut.OK:
                coStr = "<color=#c8e7fa>CO○</color><color=#ffffff>)</color>";
                break;
            case EnableComingOut.Limit:
                coStr = "<color=#ffd700>CO△</color><color=#ffffff>)</color>";
                break;
            case EnableComingOut.NG:
                coStr = "<color=#ff6347>CO×</color><color=#ffffff>)</color>";
                break;
        }
        return coStr;
    }

    private static bool IsDontShowRole(CustomRoles role)
    {
        return role is CustomRoles.Shapeshifter
            or CustomRoles.Phantom

            or CustomRoles.Engineer
            or CustomRoles.Scientist
            or CustomRoles.Tracker
            or CustomRoles.Noisemaker
            or CustomRoles.Potentialist

            or CustomRoles.EvilHacker // 一旦封印

            or CustomRoles.GM
            or CustomRoles.GuardianAngel;
    }
}
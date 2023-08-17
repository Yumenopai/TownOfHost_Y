using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Options;
using AmongUs.GameOptions;
using TownOfHostY.Attributes;

namespace TownOfHostY.Roles.AddOns.Common;

public static class AddLight
{
    private static readonly int Id = 80000;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.AddLight);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｌ");
    private static List<byte> playerIdList = new();

    private static OptionItem OptionAddCrewmateVision;
    private static OptionItem OptionAddImpostorVision;
    private static OptionItem OptionDisableLightOut;

    public static float AddCrewmateVision;
    public static float AddImpostorVision;
    public static bool DisableLightOut;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.AddLight);
        OptionAddCrewmateVision = FloatOptionItem.Create(79210, "AddLightAddCrewmateVision", new(0f, 5f, 0.1f), 0.3f, TabGroup.Addons, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionAddImpostorVision = FloatOptionItem.Create(79211, "AddLightAddImpostorVision", new(0f, 5f, 0.1f), 0.5f, TabGroup.Addons, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionDisableLightOut = BooleanOptionItem.Create(79212, "AddLighterDisableLightOut", true, TabGroup.Addons, false);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();

        AddCrewmateVision = OptionAddCrewmateVision.GetFloat();
        AddImpostorVision = OptionAddImpostorVision.GetFloat();
        DisableLightOut = OptionDisableLightOut.GetBool();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static void ApplyGameOptions(IGameOptions opt)
    {
        var crewLightMod = FloatOptionNames.CrewLightMod;
        var impostorLightMod = FloatOptionNames.ImpostorLightMod;

        opt.SetFloat(crewLightMod, opt.GetFloat(crewLightMod) + AddCrewmateVision);
        opt.SetFloat(impostorLightMod, opt.GetFloat(impostorLightMod) + AddImpostorVision);

        if (Utils.IsActive(SystemTypes.Electrical) && DisableLightOut)
            opt.SetFloat(crewLightMod, opt.GetFloat(crewLightMod) * 5);
    }

    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}
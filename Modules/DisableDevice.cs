using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Crewmate;

namespace TownOfHostY
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class DisableDevice
    {
        
        public static bool DoDisable
        {
            get
            {
                try
                {
                    var normal = Main.NormalOptions; 
                    if (normal == null) return false;

                    var map = (MapNames)normal.MapId;
                    bool byMap =
                        (map == MapNames.Skeld && (Options.DisableDevices_Skeld?.GetBool() ?? false)) ||
                        (map == MapNames.MiraHQ && (Options.DisableDevices_MiraHQ?.GetBool() ?? false)) ||
                        (map == MapNames.Polus && (Options.DisableDevices_Polus?.GetBool() ?? false)) ||
                        (map == MapNames.Airship && (Options.DisableDevices_Airship?.GetBool() ?? false)) ||
                        (map == MapNames.Fungle && (Options.DisableDevices_Fungle?.GetBool() ?? false));

                   
                    bool standardHas = Options.IsStandardHAS;

                    return byMap || standardHas || IsPoorEnableSafe();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"DoDisable getter failed: {ex.Message}", "DisableDevice");
                    return false;
                }
            }
        }

        private static List<byte> DesyncComms = new();
        private static int frame = 0;
        public static readonly Dictionary<string, Vector2> DevicePos = new()
        {
            ["SkeldAdmin"] = new(3.48f, -8.62f),
            ["SkeldCamera"] = new(-13.06f, -2.45f),
            ["MiraHQAdmin"] = new(21.02f, 19.09f),
            ["MiraHQDoorLog"] = new(16.22f, 5.82f),
            ["PolusLeftAdmin"] = new(22.80f, -21.52f),
            ["PolusRightAdmin"] = new(24.66f, -21.52f),
            ["PolusCamera"] = new(2.96f, -12.74f),
            ["PolusVital"] = new(26.70f, -15.94f),
            ["AirshipCockpitAdmin"] = new(-22.32f, 0.91f),
            ["AirshipRecordsAdmin"] = new(19.89f, 12.60f),
            ["AirshipCamera"] = new(8.10f, -9.63f),
            ["AirshipVital"] = new(25.24f, -7.94f),
            ["FungleVital"] = new(-2.765f, -9.819f),
            ["FungleTelescope"] = new(6.3f, 0.9f)
        };

        public static float UsableDistance()
        {
            var normal = Main.NormalOptions;
            if (normal == null) return 0f;

            var map = (MapNames)normal.MapId;
            return map switch
            {
                MapNames.Skeld => 1.8f,
                MapNames.MiraHQ => 2.4f,
                MapNames.Polus => 1.8f,
                MapNames.Airship => 1.8f,
                MapNames.Fungle => 1.8f,
                _ => 0.0f
            };
        }

        public static bool IsInfoPoor(PlayerControl pc)
        {
            try
            {
                if (pc == null) return false;
                return ((pc.Is(CustomRoles.Sheriff) && (Sheriff.IsInfoPoor?.GetBool() ?? false))
                        || (pc.Is(CustomRoles.SillySheriff) && (SillySheriff.IsInfoPoor?.GetBool() ?? false))
                        || (pc.Is(CustomRoles.Hunter) && (Hunter.IsInfoPoor?.GetBool() ?? false))
                        || pc.Is(CustomRoles.InfoPoor));
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPoorEnableSafe()
        {
            try
            {
                var list = Main.AllAlivePlayerControls; // 型名で直接参照
                if (list == null) return false;
                foreach (var pc in list)
                {
                    if (IsInfoPoor(pc)) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPoorEnable() => IsPoorEnableSafe();

        public static void FixedUpdate()
        {
            frame = frame == 3 ? 0 : ++frame;
            if (frame != 0) return;

            if (!DoDisable) return;

            var allPlayers = Main.AllPlayerControls; // 型名で直接参照
            if (allPlayers == null) return;

            foreach (var pc in allPlayers)
            {
                bool PcIsPoor = IsInfoPoor(pc);

                try
                {
                    if (pc == PlayerControl.LocalPlayer/*pc.IsModClient()*/) continue;

                    bool doComms = false;
                    Vector2 PlayerPos = pc.GetTruePosition();
                    bool ignore =
                        ((Options.DisableDevicesIgnoreImpostors?.GetBool() ?? false) && pc.Is(CustomRoleTypes.Impostor)) ||
                        ((Options.DisableDevicesIgnoreMadmates?.GetBool() ?? false) && pc.Is(CustomRoleTypes.Madmate)) ||
                        ((Options.DisableDevicesIgnoreNeutrals?.GetBool() ?? false) && pc.Is(CustomRoleTypes.Neutral)) ||
                        ((Options.DisableDevicesIgnoreCrewmates?.GetBool() ?? false) && pc.Is(CustomRoleTypes.Crewmate)) ||
                        ((Options.DisableDevicesIgnoreAfterAnyoneDied?.GetBool() ?? false) && GameStates.AlreadyDied);

                    if (pc.IsAlive() && !Utils.IsActive(SystemTypes.Comms))
                    {
                        switch ((MapNames)Main.NormalOptions.MapId)
                        {
                            case MapNames.Skeld:
                                if ((Options.DisableAdmin_Skeld?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= UsableDistance();
                                if ((Options.DisableCamera_Skeld?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldCamera"]) <= UsableDistance();
                                break;
                            case MapNames.MiraHQ:
                                if ((Options.DisableAdmin_MiraHQ?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= UsableDistance();
                                if ((Options.DisableDoorLog_MiraHQ?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQDoorLog"]) <= UsableDistance();
                                break;
                            case MapNames.Polus:
                                if ((Options.DisableAdmin_Polus?.GetBool() ?? false) || PcIsPoor)
                                {
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= UsableDistance();
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= UsableDistance();
                                }
                                if ((Options.DisableCamera_Polus?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusCamera"]) <= UsableDistance();
                                if ((Options.DisableVital_Polus?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusVital"]) <= UsableDistance();
                                break;
                            case MapNames.Airship:
                                if ((Options.DisableCockpitAdmin_Airship?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= UsableDistance();
                                if ((Options.DisableRecordsAdmin_Airship?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= UsableDistance();
                                if ((Options.DisableCamera_Airship?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCamera"]) <= UsableDistance();
                                if ((Options.DisableVital_Airship?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipVital"]) <= UsableDistance();
                                break;
                            case MapNames.Fungle:
                                if ((Options.DisableVital_Fungle?.GetBool() ?? false) || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["FungleVital"]) <= UsableDistance();
                                break;
                        }
                    }
                    doComms &= !ignore;
                    if (doComms && !pc.inVent && GameStates.IsInTask)
                    {
                        if (!DesyncComms.Contains(pc.PlayerId))
                            DesyncComms.Add(pc.PlayerId);

                        pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 128);
                    }
                    else if (!Utils.IsActive(SystemTypes.Comms) && DesyncComms.Contains(pc.PlayerId))
                    {
                        DesyncComms.Remove(pc.PlayerId);
                        pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 16);

                        if ((MapNames)Main.NormalOptions.MapId is MapNames.MiraHQ or MapNames.Fungle)
                            pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 17);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "DisableDevice");
                }
            }
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    public class RemoveDisableDevicesPatch
    {
        public static void Postfix()
        {
            if (!DisableDevice.DoDisable) return;
            UpdateDisableDevices();
        }

        public static void UpdateDisableDevices()
        {
            var player = PlayerControl.LocalPlayer;
            if (player == null) return;

            bool PcIsPoor = DisableDevice.IsInfoPoor(player);

            bool ignore = player.Is(CustomRoles.GM) ||
                !player.IsAlive() ||
                ((Options.DisableDevicesIgnoreImpostors?.GetBool() ?? false) && player.Is(CustomRoleTypes.Impostor)) ||
                ((Options.DisableDevicesIgnoreMadmates?.GetBool() ?? false) && player.Is(CustomRoleTypes.Madmate)) ||
                ((Options.DisableDevicesIgnoreNeutrals?.GetBool() ?? false) && player.Is(CustomRoleTypes.Neutral)) ||
                ((Options.DisableDevicesIgnoreCrewmates?.GetBool() ?? false) && player.Is(CustomRoleTypes.Crewmate)) ||
                ((Options.DisableDevicesIgnoreAfterAnyoneDied?.GetBool() ?? false) && GameStates.AlreadyDied);

            var admins = GameObject.FindObjectsOfType<MapConsole>(true);
            var consoles = GameObject.FindObjectsOfType<SystemConsole>(true);
            if (admins == null || consoles == null) return;

            switch ((MapNames)Main.NormalOptions.MapId)
            {
                case MapNames.Skeld:
                    if ((Options.DisableAdmin_Skeld?.GetBool() ?? false) || PcIsPoor)
                    {
                        if (admins.Length > 0)
                            admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = ignore;
                    }
                    if ((Options.DisableCamera_Skeld?.GetBool() ?? false) || PcIsPoor)
                        consoles.DoIf(x => x.name == "SurvConsole", x => x.gameObject.GetComponent<PolygonCollider2D>().enabled = ignore);
                    break;
                case MapNames.MiraHQ:
                    if ((Options.DisableAdmin_MiraHQ?.GetBool() ?? false) || PcIsPoor)
                    {
                        if (admins.Length > 0)
                            admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = ignore;
                    }
                    if ((Options.DisableDoorLog_MiraHQ?.GetBool() ?? false) || PcIsPoor)
                        consoles.DoIf(x => x.name == "SurvLogConsole", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    break;
                case MapNames.Polus:
                    if ((Options.DisableAdmin_Polus?.GetBool() ?? false) || PcIsPoor)
                        admins.Do(x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    if ((Options.DisableCamera_Polus?.GetBool() ?? false) || PcIsPoor)
                        consoles.DoIf(x => x.name == "Surv_Panel", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    if ((Options.DisableVital_Polus?.GetBool() ?? false) || PcIsPoor)
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    break;
                case MapNames.Airship:
                    admins.Do(x =>
                    {
                        if (((Options.DisableCockpitAdmin_Airship?.GetBool() ?? false) || PcIsPoor) && x.name == "panel_cockpit_map")
                            x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore;
                        if (((Options.DisableRecordsAdmin_Airship?.GetBool() ?? false) || PcIsPoor) && x.name == "records_admin_map")
                            x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore;
                    });
                    if ((Options.DisableCamera_Airship?.GetBool() ?? false) || PcIsPoor)
                        consoles.DoIf(x => x.name == "task_cams", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    if ((Options.DisableVital_Airship?.GetBool() ?? false) || PcIsPoor)
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<CircleCollider2D>().enabled = ignore);
                    break;
                case MapNames.Fungle:
                    if ((Options.DisableVital_Fungle?.GetBool() ?? false) || PcIsPoor)
                        consoles.DoIf(x => x.name == "VitalsConsole", x => x.GetComponent<Collider2D>().enabled = ignore);
                    break;
            }
        }
    }
}
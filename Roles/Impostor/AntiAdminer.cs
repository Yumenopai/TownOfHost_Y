using System;
using System.Collections.Generic;
using UnityEngine;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class AntiAdminer
    {
        static readonly int Id = 3100;
        static List<byte> playerIdList = new();

        private static OptionItem CanCheckCamera;
        public static bool IsAdminWatch;
        public static bool IsVitalWatch;
        public static bool IsDoorLogWatch;
        public static bool IsCameraWatch;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.AntiAdminer);
            CanCheckCamera = BooleanOptionItem.Create(Id + 10, "CanCheckCamera", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.AntiAdminer]);
        }
        public static void Init()
        {
            playerIdList = new();
            IsAdminWatch = false;
            IsVitalWatch = false;
            IsDoorLogWatch = false;
            IsCameraWatch = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable() => playerIdList.Count > 0;

        private static int Count = 0;
        public static void FixedUpdate()
        {
            Count--;
            if (Count > 0) return;
            Count = 3;

            if (CustomRoles.AntiAdminer.GetCount() < 1) return;

            bool Admin = false, Camera = false, DoorLog = false, Vital = false;
            foreach (PlayerControl pc in Main.AllAlivePlayerControls)
            {
                if (pc.inVent) continue;
                try
                {
                    Vector2 PlayerPos = pc.GetTruePosition();
                    switch (Main.NormalOptions.MapId)
                    {
                        case 0:
                            if (!DisableSkeldAdmin.GetBool())
                                Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldAdmin"]) <= DisableDevice.UsableDistance();
                            if (!DisableSkeldCamera.GetBool())
                                Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldCamera"]) <= DisableDevice.UsableDistance();
                            break;
                        case 1:
                            if (!DisableMiraHQAdmin.GetBool())
                                Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQAdmin"]) <= DisableDevice.UsableDistance();
                            if (!DisableMiraHQDoorLog.GetBool())
                                DoorLog |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQDoorLog"]) <= DisableDevice.UsableDistance();
                            break;
                        case 2:
                            if (!DisablePolusAdmin.GetBool())
                            {
                                Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusLeftAdmin"]) <= DisableDevice.UsableDistance();
                                Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusRightAdmin"]) <= DisableDevice.UsableDistance();
                            }
                            if (!DisablePolusCamera.GetBool())
                                Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusCamera"]) <= DisableDevice.UsableDistance();
                            if (!DisablePolusVital.GetBool())
                                Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusVital"]) <= DisableDevice.UsableDistance();
                            break;
                        case 4:
                            if (!DisableAirshipCockpitAdmin.GetBool())
                                Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCockpitAdmin"]) <= DisableDevice.UsableDistance();
                            if (!DisableAirshipRecordsAdmin.GetBool())
                                Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipRecordsAdmin"]) <= DisableDevice.UsableDistance();
                            if (!DisableAirshipCamera.GetBool())
                                Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCamera"]) <= DisableDevice.UsableDistance();
                            if (!DisableAirshipVital.GetBool())
                                Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipVital"]) <= DisableDevice.UsableDistance();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString(), "AntiAdmin");
                }
            }

            var isChange = false;

            isChange |= IsAdminWatch != Admin;
            IsAdminWatch = Admin;
            isChange |= IsVitalWatch != Vital;
            IsVitalWatch = Vital;
            isChange |= IsDoorLogWatch != DoorLog;
            IsDoorLogWatch = DoorLog;
            if (CanCheckCamera.GetBool())
            {
                isChange |= IsCameraWatch != Camera;
                IsCameraWatch = Camera;
            }

            if (isChange)
            {
                //アンチアドミナーのみ呼び出し
                foreach (var impostorId in playerIdList)
                {
                    var seer = Utils.GetPlayerById(impostorId);
                    Utils.NotifyRoles(SpecifySeer: seer);
                    //FixedUpdatePatch.Postfix(seer);
                }
            }
        }
    }
}
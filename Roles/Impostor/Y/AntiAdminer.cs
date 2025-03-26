using System;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Translator;
using static TownOfHostY.Options;

namespace TownOfHostY.Roles.Impostor;
public sealed class AntiAdminer : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(AntiAdminer),
            player => new AntiAdminer(player),
            CustomRoles.AntiAdminer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 100,
            SetupOptionItem,
            "アンチアドミナー"
        );
    public AntiAdminer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanCheckCamera = OptionCanCheckCamera.GetBool();
    }

    private static OptionItem OptionCanCheckCamera;
    enum OptionName
    {
        AntiAdminerCanCheckCamera
    }
    private static bool CanCheckCamera;

    public static bool IsAdminWatch;
    public static bool IsVitalWatch;
    public static bool IsDoorLogWatch;
    public static bool IsCameraWatch;
    int Count = 0;

    private static void SetupOptionItem()
    {
        OptionCanCheckCamera = BooleanOptionItem.Create(RoleInfo, 10, OptionName.AntiAdminerCanCheckCamera, false, false);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!Player.IsAlive()) return;

        Count--;
        if (Count > 0) return;
        Count = 3;

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
                        if (!DisableAdmin_Skeld.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldAdmin"]) <= DisableDevice.UsableDistance();
                        if (!DisableCamera_Skeld.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldCamera"]) <= DisableDevice.UsableDistance();
                        break;
                    case 1:
                        if (!DisableAdmin_MiraHQ.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQAdmin"]) <= DisableDevice.UsableDistance();
                        if (!DisableDoorLog_MiraHQ.GetBool())
                            DoorLog |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQDoorLog"]) <= DisableDevice.UsableDistance();
                        break;
                    case 2:
                        if (!DisableAdmin_Polus.GetBool())
                        {
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusLeftAdmin"]) <= DisableDevice.UsableDistance();
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusRightAdmin"]) <= DisableDevice.UsableDistance();
                        }
                        if (!DisableCamera_Polus.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusCamera"]) <= DisableDevice.UsableDistance();
                        if (!DisableVital_Polus.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusVital"]) <= DisableDevice.UsableDistance();
                        break;
                    case 4:
                        if (!DisableCockpitAdmin_Airship.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCockpitAdmin"]) <= DisableDevice.UsableDistance();
                        if (!DisableRecordsAdmin_Airship.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipRecordsAdmin"]) <= DisableDevice.UsableDistance();
                        if (!DisableCamera_Airship.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCamera"]) <= DisableDevice.UsableDistance();
                        if (!DisableVital_Airship.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipVital"]) <= DisableDevice.UsableDistance();
                        break;
                    case 5:
                        //if (!DisableFungleVital.GetBool())
                        //    Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["FungleVital"]) <= DisableDevice.UsableDistance();
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
        if (CanCheckCamera)
        {
            isChange |= IsCameraWatch != Camera;
            IsCameraWatch = Camera;
        }

        if (isChange)
        {
            Utils.NotifyRoles();
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        if (isForMeeting) return "";

        StringBuilder sb = new();
        if (IsAdminWatch) sb.Append('★').Append(GetString("AntiAdminerAD"));
        if (IsVitalWatch) sb.Append('★').Append(GetString("AntiAdminerVI"));
        if (IsDoorLogWatch) sb.Append('★').Append(GetString("AntiAdminerDL"));
        if (IsCameraWatch) sb.Append('★').Append(GetString("AntiAdminerCA"));

        return sb.ToString();
    }
}
using System.Collections.Generic;
using Hazel;
using Il2CppSystem.Text;
using UnityEngine;
using AmongUs.GameOptions;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor
{
    public static class Telepathisters
    {
        private static readonly int Id = 3700;
        private static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        private static OptionItem CanSeeKillFlash;
        public static OptionItem CanSeeLastRoomInMeeting;
        private static OptionItem VentMaxCount;

        private static Dictionary<byte, HashSet<byte>> ImpostorsId = new();

        public static int VentCountLimit;

        public static void SetupCustomOption()
        {
            SetupTelepathistersOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Telepathisters);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Telepathisters])
                .SetValueFormat(OptionFormat.Seconds);
            CanSeeKillFlash = BooleanOptionItem.Create(Id + 11, "TelepathistersCanSeeKillFlash", true, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnOnOff[CustomRoles.Telepathisters]);
            CanSeeLastRoomInMeeting = BooleanOptionItem.Create(Id + 12, "TelepathistersCanSeeLastRoomInMeeting", false, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnOnOff[CustomRoles.Telepathisters]);
            VentMaxCount = IntegerOptionItem.Create(Id + 13, "TelepathistersOptionVentCount", new(1, 20, 1), 2, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Telepathisters])
                .SetValueFormat(OptionFormat.Times);
        }

        private static void SetupTelepathistersOptions(int id, TabGroup tab, CustomRoles role)
        {
            var spawnOption = BooleanOptionItem.Create(id, role.ToString(), false, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard) as BooleanOptionItem;
            // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(2, 3, 1), 2, tab, false).SetParent(spawnOption)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(CustomGameMode.Standard);

            CustomRoleSpawnOnOff.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }

        public static void Init()
        {
            playerIdList = new();
            ImpostorsId = new();
            VentCountLimit = VentMaxCount.GetInt();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            //ImpostorsIdはEvilTracker内で共有
            ImpostorsId[playerId] = new();
            foreach (var target in Main.AllAlivePlayerControls)
            {
                var targetId = target.PlayerId;
                if (targetId != playerId && target.Is(CustomRoles.Telepathisters))
                {
                    ImpostorsId[playerId].Add(targetId);
                    TargetArrow.Add(playerId, targetId);
                }
            }
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsCanVent() => VentCountLimit > -1;
        public static void SetKillCooldown(byte id)
        {
            Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        }

        // 値取得の関数
        public static bool IsTrackTarget(PlayerControl seer, PlayerControl target)
            => seer.IsAlive() && playerIdList.Contains(seer.PlayerId)
            && target.Is(CustomRoles.Telepathisters)
            && seer != target && target.IsAlive();

        public static bool KillFlashCheck(PlayerControl killer, PlayerControl target)
        {
            if (!CanSeeKillFlash.GetBool()) return false;
            //シスターズによるキルかどうかの判別
            var realKiller = target.GetRealKiller() ?? killer;
            return playerIdList.Contains(realKiller.PlayerId) && realKiller != target;
        }

        // 表示系の関数
        public static string GetTargetArrow(PlayerControl seer, PlayerControl target)
        {
            if (!GameStates.IsInTask || !target.Is(CustomRoles.Telepathisters)) return "";

            var trackerId = target.PlayerId;
            if (seer.PlayerId != trackerId) return "";

            ImpostorsId[trackerId].RemoveWhere(id => Main.PlayerStates[id].IsDead);

            var sb = new StringBuilder(80);
            if (ImpostorsId[trackerId].Count > 0)
            {
                sb.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>");
                foreach (var impostorId in ImpostorsId[trackerId])
                {
                    sb.Append(TargetArrow.GetArrows(target, impostorId));
                }
                sb.Append($"</color>");
            }
            return sb.ToString();
        }
        public static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
        {
            string text = Utils.ColorString(Palette.ImpostorRed, TargetArrow.GetArrows(seer, target.PlayerId));
            var room = Main.PlayerStates[target.PlayerId].LastRoom;
            if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
            else text += Utils.ColorString(Palette.ImpostorRed, "@" + GetString(room.RoomId.ToString()));
            return text;
        }
        public static string GetVentCountLimit()
        {
            int count;
            if (VentCountLimit < 0) count = 0;
            else count = VentCountLimit;
            return Utils.ColorString(count > 0 ? Palette.ImpostorRed : Color.gray, $"({count})");
        }

        public static void SubNotifyRoles()
        {
            //テレパシスターズのみ呼び出し
            foreach (var impostorId in playerIdList)
            {
                Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(impostorId));
            }
        }
    }
}
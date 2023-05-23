using Hazel;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost
{
    public static class NameColorManager
    {
        public static string ApplyNameColorData(this string name, PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            if (!AmongUsClient.Instance.IsGameStarted) return name;
            if (Options.IsSyncColorMode) return name;

            if (isMeeting && seer.Is(CustomRoles.Snitch) && Snitch.SnitchCannotConfirmKillRoles.GetBool()
                && (target.Is(CustomRoleTypes.Impostor) || target.IsNeutralKiller())) return name;

            if (!TryGetData(seer, target, out var colorCode))
            {
                if (KnowTargetCampColor(seer, target, isMeeting, out bool onlyKiller))
                    colorCode = GetCampColorCode(target, onlyKiller);
                if (KnowTargetRoleColor(seer, target, isMeeting))
                    colorCode = target.GetRoleColorCode();
            }
            string openTag = "", closeTag = "";
            if (colorCode != "")
            {
                if (!colorCode.StartsWith('#'))
                    colorCode = "#" + colorCode;
                openTag = $"<color={colorCode}>";
                closeTag = "</color>";
            }
            return openTag + name + closeTag;
        }
        private static bool KnowTargetRoleColor(PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            return seer == target
                            || target.Is(CustomRoles.GM) || target.Is(CustomRoles.Rainbow)
                            || (target.Is(CustomRoles.Workaholic) && Options.WorkaholicSeen.GetBool())
                            || (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor))
                            || Mare.KnowTargetRoleColor(target, isMeeting)
                            || (FortuneTeller.IsShowTargetRole(seer, target) && isMeeting)
                            || (Psychic.IsShowTargetRole(seer, target) && isMeeting);
        }
        private static bool KnowTargetCampColor(PlayerControl seer, PlayerControl target, bool isMeeting, out bool onlyKiller)
        {
            return (FortuneTeller.IsShowTargetCamp(seer, target, out onlyKiller) && isMeeting)
                   || (Psychic.IsShowTargetCamp(seer, target, out onlyKiller) && isMeeting);
        }
        private static string GetCampColorCode(PlayerControl pc, bool onlyKiller)
        {
            if (pc.GetCustomRole().IsImpostor() ||
                (!onlyKiller && pc.GetCustomRole().IsMadmate()))
                return Utils.GetRoleColorCode(CustomRoles.Impostor);
            if (pc.GetCustomRole().IsNeutral() &&
                (!onlyKiller || Utils.IsNeutralKiller(pc)))
                return Utils.GetRoleColorCode(CustomRoles.Neutral);
            return Utils.GetRoleColorCode(CustomRoles.Crewmate);
        }
        public static bool TryGetData(PlayerControl seer, PlayerControl target, out string colorCode)
        {
            colorCode = "";
            var state = Main.PlayerStates[seer.PlayerId];
            if (!state.TargetColorData.TryGetValue(target.PlayerId, out var value)) return false;
            colorCode = value;
            return true;
        }

        public static void Add(byte seerId, byte targetId, string colorCode = "")
        {
            if (colorCode == "")
            {
                var target = Utils.GetPlayerById(targetId);
                if (target == null) return;
                colorCode = target.GetRoleColorCode();
            }
            var state = Main.PlayerStates[seerId];
            if (state.TargetColorData.TryGetValue(targetId, out var value) && colorCode == value) return;
            state.TargetColorData.Add(targetId, colorCode);
            SendRPC(seerId, targetId, colorCode);
        }
        public static void Remove(byte seerId, byte targetId)
        {
            var state = Main.PlayerStates[seerId];
            if (!state.TargetColorData.ContainsKey(targetId)) return;
            state.TargetColorData.Remove(targetId);
            SendRPC(seerId, targetId);
        }
        public static void RemoveAll(byte seerId)
        {
            Main.PlayerStates[seerId].TargetColorData.Clear();
            SendRPC(seerId);
        }
        private static void SendRPC(byte seerId, byte targetId = byte.MaxValue, string colorCode = "")
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetNameColorData, SendOption.Reliable, -1);
            writer.Write(seerId);
            writer.Write(targetId);
            writer.Write(colorCode);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte seerId = reader.ReadByte();
            byte targetId = reader.ReadByte();
            string colorCode = reader.ReadString();


            if (targetId == byte.MaxValue)
                RemoveAll(seerId);
            else if (colorCode == "")
                Remove(seerId, targetId);
            else
                Add(seerId, targetId, colorCode);
        }
    }
}
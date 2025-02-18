using Hazel;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Impostor;
using TownOfHostY.Roles.Neutral;

namespace TownOfHostY
{
    public static class NameColorManager
    {
        public static string ApplyNameColorData(this string name, PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            if (!AmongUsClient.Instance.IsGameStarted) return name;
            if (isMeeting && Snitch.IsCannotConfirmKillRoles(seer,target)) return name;
            if (Options.IsSyncColorMode && isMeeting) return name;

            if (!TryGetData(seer, target, out var colorCode))
            {
                if (KnowTargetRoleColor(seer, target, isMeeting))
                    colorCode = target.GetRoleColorCode();
            }
            // ジャニターターゲットの色上書き
            colorCode = Godfather.OverrideNameColorByJanitorTarget(target, colorCode);

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
                || target.Is(CustomRoles.GM)
                || (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor) && !seer.Is(CustomRoles.StrayWolf) && !target.Is(CustomRoles.StrayWolf))
                || Mare.KnowTargetRoleColor(target, isMeeting)
                || (target.Is(CustomRoles.Workaholic) && Workaholic.Seen)
                || target.Is(CustomRoles.Rainbow)
                || FortuneTeller.KnowTargetRoleColor(seer, target, isMeeting)
                || (seer.Is(CustomRoles.Psychic) && ((Psychic)seer.GetRoleClass()).KnowTargetRoleColor(target) && isMeeting);
        }
        public static bool TryGetData(PlayerControl seer, PlayerControl target, out string colorCode)
        {
            colorCode = "";
            var state = PlayerState.GetByPlayerId(seer.PlayerId);
            if (!state.TargetColorData.TryGetValue(target.PlayerId, out var value)) return false;
            colorCode = value;
            if (colorCode == "") colorCode = target.GetRoleColorCode();
            return true;
        }

        public static void Add(byte seerId, byte targetId, string colorCode = "")
        {
            var state = PlayerState.GetByPlayerId(seerId);
            if (state.TargetColorData.TryGetValue(targetId, out var value))
            {
                if (colorCode == value) return;
                Logger.Info($"SameKeyAdd seerId: {seerId}, targetId: {targetId}, colorCode: {colorCode},", "NameColorManager");
            }
            state.TargetColorData[targetId] = colorCode;

            SendRPC(seerId, targetId, colorCode);
        }
        public static void Remove(byte seerId, byte targetId)
        {
            var state = PlayerState.GetByPlayerId(seerId);
            if (!state.TargetColorData.ContainsKey(targetId)) return;
            state.TargetColorData.Remove(targetId);

            SendRPC(seerId, targetId);
        }
        public static void RemoveAll(byte seerId)
        {
            PlayerState.GetByPlayerId(seerId).TargetColorData.Clear();

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
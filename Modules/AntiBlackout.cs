using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Hazel;

namespace TownOfHost
{
    public static class AntiBlackout
    {
        ///<summary>
        ///追放処理を上書きするかどうか
        ///</summary>
        public static bool OverrideExiledPlayer => IsRequred && (IsSingleImpostor || Diff_CrewImp == 1);
        ///<summary>
        ///インポスターが一人しか存在しない設定かどうか
        ///</summary>
        public static bool IsSingleImpostor => Main.RealOptionsData != null ? Main.RealOptionsData.NumImpostors == 1 : PlayerControl.GameOptions.NumImpostors == 1;
        ///<summary>
        ///AntiBlackout内の処理が必要であるかどうか
        ///</summary>
        public static bool IsRequred => Options.NoGameEnd.GetBool() || CustomRoles.Jackal.IsEnable();
        ///<summary>
        ///インポスター以外の人数とインポスターの人数の差
        ///</summary>
        public static int Diff_CrewImp
        {
            get
            {
                int numImpostors = 0;
                int numCrewmates = 0;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.Role.IsImpostor) numImpostors++;
                    else numCrewmates++;
                }
                return numCrewmates - numImpostors;
            }
        }
        public static bool IsCached { get; private set; } = false;
        private static Dictionary<byte, bool> isDeadCache = new();

        public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            Logger.Info($"SetIsDead is called from {callerMethodName}", "AntiBlackout");
            if (IsCached)
            {
                Logger.Info("再度SetIsDeadを実行する前に、RestoreIsDeadを実行してください。", "AntiBlackout.Error");
                return;
            }
            isDeadCache.Clear();
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                isDeadCache[info.PlayerId] = info.IsDead;
                info.IsDead = false;
            }
            IsCached = true;
            if (doSend) SendGameData();
        }
        public static void RestoreIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            Logger.Info($"RestoreIsDead is called from {callerMethodName}", "AntiBlackout");
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                if (isDeadCache.TryGetValue(info.PlayerId, out bool val)) info.IsDead = val;
            }
            isDeadCache.Clear();
            IsCached = false;
            if (doSend) SendGameData();
        }

        public static void SendGameData([CallerMemberName] string callerMethodName = "")
        {
            Logger.Info($"SendGameData is called from {callerMethodName}", "AntiBlackout");
            MessageWriter writer = AmongUsClient.Instance.Streams[(int)SendOption.Reliable];
            writer.StartMessage(1);
            writer.WritePacked(GameData.Instance.NetId);
            GameData.Instance.Serialize(writer, true);
            writer.EndMessage();
        }
    }
}
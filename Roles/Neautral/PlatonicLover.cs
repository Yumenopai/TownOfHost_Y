using System.Collections.Generic;
using Hazel;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Neutral
{
    public static class PlatonicLover
    {
        private static readonly int Id = 60400;
        public static List<byte> playerIdList = new();
        public static Dictionary<byte, bool> isMadeLover = new();

        public static OptionItem PLoverAddWin;

        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PlatonicLover, 1);
            PLoverAddWin = BooleanOptionItem.Create(Id + 10, "LoversAddWin", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.PlatonicLover]);
        }
        public static void Init()
        {
            playerIdList = new();
            isMadeLover = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            isMadeLover.Add(playerId, false);

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPlatonicLoverMade, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(isMadeLover[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            isMadeLover[playerId] = reader.ReadBoolean();
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? 0.1f : 0f;
        public static bool CanUseKillButton(byte playerId)
            => !Main.PlayerStates[playerId].IsDead
            && !isMadeLover[playerId];

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            isMadeLover[killer.PlayerId] = true;
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);
            Logger.Info($"{killer.GetNameWithRole()} : 恋人を作った", "PlatonicLover");

            Main.LoversPlayers.Clear();
            Main.isLoversDead = false;
            killer.RpcSetCustomRole(CustomRoles.Lovers);
            target.RpcSetCustomRole(CustomRoles.Lovers);
            Main.LoversPlayers.Add(killer);
            Main.LoversPlayers.Add(target);
            RPC.SyncLoversPlayers();

            Utils.NotifyRoles();
            SendRPC(killer.PlayerId);
            return;
        }
    }
}
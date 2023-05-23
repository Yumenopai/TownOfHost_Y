using System.Collections.Generic;
using System.Linq;
using Hazel;
using HarmonyLib;

using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Neutral
{
    public static class Totocalcio
    {
        private static readonly int Id = 60700;
        public static List<byte> playerIdList = new();
        public static Dictionary<byte, PlayerControl> BetTarget = new();
        public static Dictionary<byte, int> BetTargetCount = new();

        private static OptionItem InitialCoolDown;
        private static OptionItem BetChangeCount;
        private static OptionItem FinalCoolDown;

        private static float InitCool;
        private static float FinalCool;
        private static float Coolrate;
        private static int AllPlayer;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Totocalcio);
            InitialCoolDown = FloatOptionItem.Create(Id + 10, "TotocalcioInitialCoolDown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Totocalcio])
                .SetValueFormat(OptionFormat.Seconds);
            BetChangeCount = IntegerOptionItem.Create(Id + 11, "TotocalcioBetChangeCount", new(0, 10, 1), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Totocalcio])
                .SetValueFormat(OptionFormat.Times);
            FinalCoolDown = FloatOptionItem.Create(Id + 12, "TotocalcioFinalCoolDown", new(0f, 180f, 2.5f), 60f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Totocalcio])
                .SetValueFormat(OptionFormat.Seconds);

        }
        public static void Init()
        {
            playerIdList = new();
            BetTarget = new();
            BetTargetCount = new();
            InitCool = InitialCoolDown.GetFloat();
            FinalCool = FinalCoolDown.GetFloat();
            AllPlayer = Main.AllPlayerControls.Count();
            if (AllPlayer > 3) Coolrate = (FinalCool - InitCool) / (AllPlayer - 3);
            else Coolrate = (FinalCool - InitCool) / AllPlayer;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            BetTarget.Add(playerId, null);
            BetTargetCount.Add(playerId, BetChangeCount.GetInt() + 1);

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTotocalcio, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(BetTarget[playerId].PlayerId);
            writer.Write(BetTargetCount[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            BetTarget[playerId] = Utils.GetPlayerById(reader.ReadByte());
            BetTargetCount[playerId] = reader.ReadInt32();
        }
        public static void SetKillCooldown(byte id)
        {
            float plusCool = Coolrate * (AllPlayer - Main.AllAlivePlayerControls.Count());
            //Vision = Mathf.Clamp(Vision, 0.01f, 5f);
            Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? InitCool + plusCool : 300f;
        }
        public static bool CanUseKillButton(byte playerId)
            => !Main.PlayerStates[playerId].IsDead
            && BetTargetCount[playerId] > 0;

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            BetTarget[killer.PlayerId] = target;
            BetTargetCount[killer.PlayerId]--;
            killer.RpcGuardAndKill(target);
            Logger.Info($"{killer.GetNameWithRole()} : {target.GetRealName()}に賭けた", "Totocalcio");

            Utils.NotifyRoles();
            SendRPC(killer.PlayerId);
            return;
        }

        public static void SetAbilityButtonText(HudManager __instance) => __instance.KillButton.OverrideText($"{GetString("TotocalcioButtonText")}");

        public static string GetBetMark(byte seer, PlayerControl target)
        {
            if (BetTarget.ContainsKey(seer) && BetTarget.TryGetValue(seer, out var tar) && tar == target)
            {
                return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Totocalcio), "▲");
            }
            return "";
        }

        public static void EndGameCheck()
        {
            BetTarget.Do(x =>
            {
                // 勝者にBetTargetが含まれている時
                if (x.Value != null && CustomWinnerHolder.WinnerIds.Contains(x.Value.PlayerId))
                {
                    CustomWinnerHolder.WinnerIds.Add(x.Key);
                    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Totocalcio);
                }
            });
        }
    }
}
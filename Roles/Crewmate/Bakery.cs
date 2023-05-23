using System.Collections.Generic;
using Hazel;

using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Crewmate
{
    public static class Bakery
    {
        private static readonly int Id = 35000;
        public static List<byte> playerIdList = new();
        public static List<byte> NplayerIdList = new();

        public static Dictionary<byte, PlayerControl> PoisonPlayer = new();
        public static OptionItem BakeryChangeChances;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Bakery);
            BakeryChangeChances = IntegerOptionItem.Create(Id + 10, "BakeryChangeChances", new(0, 20, 2), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Bakery])
                .SetValueFormat(OptionFormat.Percent);
        }
        public static void Init()
        {
            playerIdList = new();
            NplayerIdList = new();
            PoisonPlayer = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            PoisonPlayer.Add(playerId, null);
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static bool IsNEnable()
        {
            return NplayerIdList.Count > 0;
        }
        public static bool IsNAlive()
        {
            foreach (var bakeryId in NplayerIdList)
            {
                if (Utils.GetPlayerById(bakeryId).IsAlive())
                    return true;
            }
            return false;
        }
        private static void SendRPC(byte bakeryId, byte targetId = byte.MaxValue)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoPoison, SendOption.Reliable, -1);
            writer.Write(bakeryId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            var bakeryId = reader.ReadByte();
            var targetId = reader.ReadByte();
            if (targetId != byte.MaxValue)
            {
                PoisonPlayer[bakeryId].PlayerId = targetId;
            }
            else
            {
                PoisonPlayer[bakeryId] = null;
            }
        }

        public static bool HavePoisonedPlayer()
        {
            foreach (var bakeryId in NplayerIdList)
            {
                if (PoisonPlayer[bakeryId] != null)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsPoisoned(PlayerControl target)
        {
            foreach (var bakeryId in NplayerIdList)
            {
                if (PoisonPlayer[bakeryId] == target)
                {
                    return true;
                }
            }
            return false;
        }

        public static void OnCheckForEndVoting(byte exiled)
        {
            foreach (var bakeryId in NplayerIdList)
            {
                var bakeryPc = Utils.GetPlayerById(bakeryId);
                var target = PoisonPlayer[bakeryId];
                var targetId = target.PlayerId;
                if (bakeryId != exiled)
                {
                    if (!Main.PlayerStates[targetId].IsDead)
                    {
                        target.SetRealKiller(bakeryPc);
                        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Poisoning, targetId);
                    }
                }
                target = null;
                SendRPC(bakeryId);

                if (!bakeryPc.IsAlive()) NplayerIdList.Remove(bakeryId);
            }
        }
        public static void AfterMeetingTasks()
        {
            if (!IsNAlive()) return;

            //次のターゲットを決めておく
            List<PlayerControl> targetList = new();
            var rand = IRandom.Instance;
            foreach (var p in Main.AllAlivePlayerControls)
            {
                if (p.Is(CustomRoles.NBakery)) continue;
                targetList.Add(p);
            }
            foreach (var bakeryId in NplayerIdList)
            {
                var PoisonedPlayer = targetList[rand.Next(targetList.Count)];
                PoisonPlayer[bakeryId] = PoisonedPlayer;
                SendRPC(bakeryId, PoisonedPlayer.PlayerId);
                Logger.Info($"{Utils.GetPlayerById(bakeryId).GetNameWithRole()}の次ターン配布先：{PoisonedPlayer.GetNameWithRole()}", "NBakery");
            }
        }

        public static void NBakeryKilledTasks(byte bakeryId)
        {
            PoisonPlayer[bakeryId] = null;
            SendRPC(bakeryId);
            Logger.Info($"{Utils.GetPlayerById(bakeryId).GetNameWithRole()}の配布毒パン回収", "NBakery");
        }

        public static string GetPoisonMark(PlayerControl target, bool isMeeting)
        {
            if (isMeeting && IsNAlive() && IsPoisoned(target))
            {
                if(target.IsAlive())
                    return Utils.ColorString(Utils.GetRoleColor(CustomRoles.NBakery), "θ");
            }
            return "";
        }
        public static void SendAliveMessage(PlayerControl pc)
        {
            if (pc.Is(CustomRoles.NBakery) && !pc.Data.IsDead && !pc.Data.Disconnected)
            {
                if (PoisonPlayer[pc.PlayerId].IsAlive())
                {
                    Utils.SendMessage(GetString("BakeryChangeNow"), title: $"<color={Utils.GetRoleColorCode(CustomRoles.Bakery)}>{GetString("PanAliveMessageTitle")}</color>");
                }
                else
                {
                    PoisonPlayer[pc.PlayerId] = null;
                    SendRPC(pc.PlayerId);
                    Utils.SendMessage(GetString("BakeryChangeNONE"), title: $"<color={Utils.GetRoleColorCode(CustomRoles.Bakery)}>{GetString("PanAliveMessageTitle")}</color>");
                }
            }
            if (pc.Is(CustomRoles.Bakery) && !pc.Data.IsDead && !pc.Data.Disconnected)
            {
                string panMessage = "";
                int chance = UnityEngine.Random.Range(1, 101);
                if (chance <= Bakery.BakeryChangeChances.GetInt())
                {
                    panMessage = GetString("BakeryChange");
                    pc.RpcSetCustomRole(CustomRoles.NBakery);
                    playerIdList.Remove(pc.PlayerId);
                    NplayerIdList.Add(pc.PlayerId);
                }
                else if (chance <= 77) panMessage = GetString("PanAlive");
                else if (chance <= 79) panMessage = GetString("PanAlive1");
                else if (chance <= 81) panMessage = GetString("PanAlive2");
                else if (chance <= 82) panMessage = GetString("PanAlive3");
                else if (chance <= 84) panMessage = GetString("PanAlive4");
                else if (chance <= 86) panMessage = GetString("PanAlive5");
                else if (chance <= 87) panMessage = GetString("PanAlive6");
                else if (chance <= 88) panMessage = GetString("PanAlive7");
                else if (chance <= 90) panMessage = GetString("PanAlive8");
                else if (chance <= 92) panMessage = GetString("PanAlive9");
                else if (chance <= 94) panMessage = GetString("PanAlive10");
                else if (chance <= 96) panMessage = GetString("PanAlive11");
                else if (chance <= 98)
                {
                    List<PlayerControl> targetList = new();
                    var rand = IRandom.Instance;
                    foreach (var p in Main.AllAlivePlayerControls)
                    {
                        if (p.Is(CustomRoles.Bakery)) continue;
                        targetList.Add(p);
                    }
                    var TargetPlayer = targetList[rand.Next(targetList.Count)];
                    panMessage = string.Format(Translator.GetString("PanAlive12"), TargetPlayer.GetRealName());
                }
                else if (chance <= 100)
                {
                    List<PlayerControl> targetList = new();
                    var rand = IRandom.Instance;
                    foreach (var p in Main.AllAlivePlayerControls)
                    {
                        if (p.Is(CustomRoles.Bakery)) continue;
                        targetList.Add(p);
                    }
                    var TargetPlayer = targetList[rand.Next(targetList.Count)];
                    panMessage = string.Format(Translator.GetString("PanAlive13"), TargetPlayer.GetRealName());
                }

                Utils.SendMessage(panMessage, title: $"<color={Utils.GetRoleColorCode(CustomRoles.Bakery)}>{GetString("PanAliveMessageTitle")}</color>");
            }
        }
    }
}

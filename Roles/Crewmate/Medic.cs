using System.Collections.Generic;
using System.Text;
using Hazel;
using UnityEngine;

using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate
{
    public static class Medic
    {
        static readonly int Id = 37000;
        static List<byte> playerIdList = new();
        public static Dictionary<byte, PlayerControl> GuardPlayer = new();
        public static Dictionary<byte, int> NowSelectNumber = new();
        public static Dictionary<byte, bool> UseVent = new();

        //public static OptionItem IncreaseMeetingTime;
        //public static OptionItem MeetingTimeLimit;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medic);
            //IncreaseMeetingTime = IntegerOptionItem.Create(Id + 10, "TimeManagerIncreaseMeetingTime", new(5, 30, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeManager])
            //    .SetValueFormat(OptionFormat.Seconds);
            //MeetingTimeLimit = IntegerOptionItem.Create(Id + 11, "TimeManagerLimitMeetingTime", new(200, 900, 10), 300, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeManager])
            //    .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            playerIdList = new();
            GuardPlayer = new();
            NowSelectNumber = new();
            UseVent = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            GuardPlayer.Add(playerId, null);
            UseVent.Add(playerId, true);
            var pc = Utils.GetPlayerById(playerId);
            pc.AddVentSelect();
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void ApplyGameOptions(byte playerId)
        {
            AURoleOptions.EngineerCooldown = UseVent.TryGetValue(playerId, out var MedicCanUse) && MedicCanUse ? 0.0f : 300f;
            AURoleOptions.EngineerInVentMaxTime = 1.0f;
        }
        private static void SendRPC(bool Vent, byte playerId, byte targetId = byte.MaxValue)
        {
            if (Vent)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMedicVent, SendOption.Reliable, -1);
                writer.Write(playerId);
                writer.Write(UseVent[playerId]);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMedicGuardPlayer, SendOption.Reliable, -1);
                writer.Write(playerId);
                writer.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void ReceiveRPC(bool Vent, MessageReader reader)
        {
            byte MedicId = reader.ReadByte();
            if (Vent)
            {
                if (UseVent.ContainsKey(MedicId))
                    UseVent[MedicId] = reader.ReadBoolean();
                else
                    UseVent.Add(MedicId, true);
            }
            else
            {
                byte targetId = reader.ReadByte();

                if (GuardPlayer.ContainsKey(MedicId))
                    if (targetId != byte.MaxValue)
                    {
                        GuardPlayer[MedicId].PlayerId = targetId;
                    }
                    else
                    {
                        GuardPlayer[MedicId] = null;
                    }
                else
                    GuardPlayer.Add(MedicId, null);
            }
        }

        public static bool GuardPlayerCheckMurder(PlayerControl killer, PlayerControl target)
        {
            // メディックに守られていなければ返す
            if (!target.IsGuard()) return true;
            // 直接キル出来る役職チェック
            if (killer.Is(CustomRoles.Arsonist) || killer.Is(CustomRoles.PlatonicLover) || killer.Is(CustomRoles.Totocalcio) || killer.Is(CustomRoles.MadSheriff)) return true;

            killer.RpcGuardAndKill(target); //killer側のみ。斬られた側は見れない。

            foreach (var medic in playerIdList)
            {
                if (GuardPlayer[medic] == target)
                {
                    GuardPlayer[medic] = null;
                    SendRPC(false, medic);
                    break;
                }
            }
            return false;
        }

        public static void SetAbilityButtonText(HudManager __instance) => __instance.AbilityButton.OverrideText($"{GetString("ChangeButtonText")}");
        public static bool IsGuard(this PlayerControl target)
        {
            foreach (var medic in playerIdList)
            {
                if (GuardPlayer[medic] == target)
                {
                    return true;
                }
            }
            return false;
        }
        // ================================================以下、メディックがベントに入った時の処理

        public static void OnEnterVent(PlayerControl pc)
        {
            var playerId = pc.PlayerId;
            //if (!AmongUsClient.Instance.AmHost) return;
            if (playerIdList.Contains(playerId))
            {
                GuardPlayer[playerId] = pc.VentPlayerSelect(() =>
                {
                    UseVent[playerId] = false;
                });

                Utils.NotifyRoles(SpecifySeer: pc);
            }
        }
        // ================================================以下、メディック表示系処理
        public static string GetGuardMark(byte seer, PlayerControl target)
        {
            if (GuardPlayer.ContainsKey(seer) && GuardPlayer.TryGetValue(seer, out var tar) && tar == target)
            {
                return Utils.ColorString(Color.cyan, "Σ");
            }
            return "";
        }
        public static string GetGuardPlayerText(PlayerControl medic, bool hud, bool isMeeting = false)
        {
            var medicId = medic.PlayerId;
            if (medic == null || !UseVent[medicId] || isMeeting) return "";

            var str = new StringBuilder();
            if (hud)
            {
                if (GuardPlayer[medicId] == null)
                    str.Append(GetString("SelectPlayerTagBefore"));
                else
                {
                    str.Append(GetString("SelectPlayerTag"));
                    str.Append(GuardPlayer[medicId].GetRealName());
                }
            }
            else
            {
                if (GuardPlayer[medicId] == null)
                    str.Append(GetString("SelectPlayerTagMiniBefore"));
                else
                {
                    str.Append(GetString("SelectPlayerTagMini"));
                    str.Append(GuardPlayer[medicId].GetRealName());
                }
            }

            return str.ToString();
        }

    }
}
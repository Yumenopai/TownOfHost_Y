using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Hazel;
using Steamworks;
using UnityEngine;

using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate
{
    public static class Psychic
    {
        static readonly int Id = 36300;
        static List<byte> playerIdList = new();

        public static Dictionary<byte, int> DivinationCount = new();
        public static List<byte> IsCoolTimeOff = new();

        private static OptionItem Cooldown;
        public static OptionItem MaxCheckRole;
        public static OptionItem ConfirmCamp;
        public static OptionItem KillerOnly;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Psychic);
            Cooldown = FloatOptionItem.Create(Id + 10, "Cooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Psychic])
                .SetValueFormat(OptionFormat.Seconds);
            MaxCheckRole = IntegerOptionItem.Create(Id + 11, "PsychicMaxCheckRole", new(1, 10, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Psychic])
                .SetValueFormat(OptionFormat.Times);
            ConfirmCamp = BooleanOptionItem.Create(Id + 12, "PsychicConfirmCamp", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Psychic]);
            KillerOnly = BooleanOptionItem.Create(Id + 13, "PsychicKillerOnly", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.Psychic]);
        }
        public static void Init()
        {
            playerIdList = new();
            DivinationCount = new();
            IsCoolTimeOff = new();
            VentSelect.Init();
        }
        public static void ClearSelect()
        {
            IsCoolTimeOff.Clear();
            VentSelect.ClearSelect();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            DivinationCount.Add(playerId, MaxCheckRole.GetInt());
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void ApplyGameOptions(byte playerId)
        {
            AURoleOptions.EngineerCooldown = CanUseVent(playerId) ? (IsCoolTimeOff.Contains(playerId) ? 0f : Cooldown.GetFloat()) : 300f;
            AURoleOptions.EngineerInVentMaxTime = 1.0f;
        }
        public static bool CanUseVent(byte playerId) =>
            DivinationCount[playerId] > 0 && Main.AllDeadPlayerControls.Count() > 0;

        private static void SendRPC(byte playerId, byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPsychic, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(DivinationCount[playerId]);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            {
                if (DivinationCount.ContainsKey(playerId))
                    DivinationCount[playerId] = reader.ReadInt32();
                else
                    DivinationCount.Add(playerId, MaxCheckRole.GetInt());
            }
            {
                //if (DivinationCount.ContainsKey(playerId))
                //    CheckRolePlayer[playerId].Add(reader.ReadByte());
                //else
                //    CheckRolePlayer.Add(playerId, new());
            }
        }
        // ================================================以下、メディックがベントに入った時の処理
        public static void OnEnterVent(PlayerControl pc)
        {
            if (!pc.Is(CustomRoles.Psychic)) return;

            var playerId = pc.PlayerId;
            IsCoolTimeOff.Add(playerId);
            pc.MarkDirtySettings();

            VentSelect.PlayerSelect(pc);

            Utils.NotifyRoles(SpecifySeer: pc);
        }
        public static void OnFixedUpdate(PlayerControl player)
        {
            if (player == null) return;
            if (!player.Is(CustomRoles.Psychic)) return;
            if (!GameStates.IsInTask) return;

            if (VentSelect.OnFixedUpdate(player, out var selectedId))
            {
                if (AmongUsClient.Instance.AmHost)
                    SendRPC(player.PlayerId, (byte)selectedId);
                player.MarkDirtySettings();
                Utils.NotifyRoles(SpecifySeer: player);
            }
        }
        // ================================================以下表示系処理
        public static bool IsShowTargetRole(PlayerControl seer, PlayerControl target)
        {
            if (!VentSelect.IsShowTargetRole(seer, target)) return false;
            if (ConfirmCamp.GetBool()) return false;
            if (KillerOnly.GetBool() &&
                !(target.GetCustomRole().IsImpostor() || Utils.IsNeutralKiller(target))) return false;
            return true;
        }
        public static bool IsShowTargetCamp(PlayerControl seer, PlayerControl target, out bool onlyKiller)
        {
            onlyKiller = KillerOnly.GetBool();
            if (!VentSelect.IsShowTargetRole(seer, target)) return false;
            return !IsShowTargetRole(seer, target);
        }
        public static void SetAbilityButtonText(HudManager __instance) => __instance.AbilityButton.OverrideText($"{GetString("ChangeButtonText")}");

        public static string GetCheckPlayerText(PlayerControl psychic, bool hud, bool isMeeting = false)
        {
            var psychicId = psychic.PlayerId;
            if (psychic == null || !CanUseVent(psychicId) || isMeeting) return "";

            return VentSelect.GetCheckPlayerText(psychic, hud);
        }
    }


    public static class VentSelect
    {
        public static Dictionary<byte, PlayerControl> SelectPlayer = new();
        public static Dictionary<byte, bool> SelectFix = new();
        public static Dictionary<byte, float> TergetFixTimer = new();
        public static Dictionary<byte, List<byte>> CheckRolePlayer = new();
        public static void Init()
        {
            SelectPlayer = new();
            SelectFix = new();
            TergetFixTimer = new();
            CheckRolePlayer = new();
        }
        public static void ClearSelect()
        {
            SelectPlayer.Clear();
            SelectFix.Clear();
            TergetFixTimer.Clear();
        }
        public static IEnumerable<PlayerControl> SelectList(byte playerId)
                    => Main.AllDeadPlayerControls.Where(x => !CheckRolePlayer[playerId].Contains(x.PlayerId));
        public static void PlayerSelect(this PlayerControl player)
        {
            if (player == null) return;

            var playerId = player.PlayerId;
            if (TergetFixTimer.ContainsKey(playerId)) //タイマーリセット
                TergetFixTimer.Remove(playerId);

            if (SelectFix.TryGetValue(playerId, out var fix) && fix) return;

            PlayerControl first = null;
            SelectPlayer.TryGetValue(playerId, out var selectedPlayer);
            var preSelected = false;
            var selected = false;
            if (!CheckRolePlayer.ContainsKey(playerId)) CheckRolePlayer.Add(playerId, new());
            foreach (var target in SelectList(playerId))
            {
                if (target == player) continue;

                if (first == null) first = target;

                if (preSelected)
                {
                    SelectPlayer[playerId] = target;
                    selected = true;
                    Logger.Info($"{player.name} PlayerSelectNow:{target.name}, nextTarget", "player");
                    break;
                }

                if (target == selectedPlayer) preSelected = true;
            }
            if (first == null)
            {
                SelectPlayer[playerId] = null;
                Logger.Info($"{player.name} PlayerSelectNow:null, ターゲットなし", "player");
                return;
            }
            if (!selected)
            {
                SelectPlayer[playerId] = first;
                Logger.Info($"{player.name} PlayerSelectNow:{first?.name}, firstTarget", "player");
            }

            TergetFixTimer.Add(player.PlayerId, 3f);
        }
        public static bool OnFixedUpdate(PlayerControl player, out int selectedId)
        {
            selectedId = -1;

            if (player == null) return false;
            if (!GameStates.IsInTask) return false;

            var playerId = player.PlayerId;
            if (!TergetFixTimer.ContainsKey(playerId)) return false;

            TergetFixTimer[playerId] -= Time.fixedDeltaTime;
            if (TergetFixTimer[playerId] > 0) return false;

            //以下ターゲット確定
            SelectFix[playerId] = true;
            selectedId = SelectPlayer[playerId].PlayerId;

            if (!CheckRolePlayer.ContainsKey(playerId)) CheckRolePlayer.Add(playerId, new());
            CheckRolePlayer[playerId].Add(SelectPlayer[playerId].PlayerId);
            TergetFixTimer.Remove(playerId);

            Logger.Info($"{player.name} PlayerDecision:{SelectPlayer[playerId].name}", "player");

            player.RpcGuardAndKill();   //設定完了のパリン
            player.RpcResetAbilityCooldown();

            return true;
        }
        public static bool IsShowTargetRole(PlayerControl seer, PlayerControl target)
        {
            var IsWatch = false;
            CheckRolePlayer.Do(x =>
            {
                if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                    IsWatch = true;
            });
            return IsWatch;
        }
        public static string GetCheckPlayerText(PlayerControl psychic, bool hud)
        {
            if (psychic == null) return "";
            var psychicId = psychic.PlayerId;

            var str = new StringBuilder();
            SelectPlayer.TryGetValue(psychicId, out var target);
            if (hud)
            {
                if (target == null)
                    str.Append(GetString("SelectPlayerTagBefore"));
                else
                {
                    str.Append(GetString("SelectPlayerTag"));
                    str.Append(target.GetRealName());
                }
            }
            else
            {
                if (target == null)
                    str.Append(GetString("SelectPlayerTagMiniBefore"));
                else
                {
                    str.Append(GetString("SelectPlayerTagMini"));
                    str.Append(target.GetRealName());
                }
            }
            return str.ToString();
        }
    }
}
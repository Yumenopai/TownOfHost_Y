using System.Collections.Generic;
using UnityEngine;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Neutral;

namespace TownOfHostY.Roles.AddOns.Common;
public static class ChainShifterAddon
{
    //private static readonly int Id = (int)offsetId.AddonNeu + 100;
    //public static Color RoleColor = Utils.GetRoleColor(CustomRoles.ChainShifter);

    private static Dictionary<byte, float> ShiftInfo = new();

    private static byte prePlayerId = byte.MaxValue;
    private static PlayerControl Player = null;
    private static PlayerControl nextPlayer = null;
    private static PlayerControl nextPlayerByKill = null;

    private static float postMeetingTime = 0f;
    private static bool shiftActive = false;

    public static void Init()
    {
        ShiftInfo = new();

        prePlayerId = byte.MaxValue;
        Player = null;
        nextPlayer = null;
        nextPlayerByKill = null;

        postMeetingTime = 0f;
        shiftActive = false;

        //他視点用のメソッド登録
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);
    }
    public static void Add(byte playerId)
    {
        Logger.Info($"Add playerId: {playerId}", "ChainShifter.Add");

        Player = Utils.GetPlayerById(playerId);
    }
    private static void OnMurderPlayerOthers(MurderInfo info)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (info.IsMeeting) return;

        if (Player == null) return;

        if (info.AttemptTarget?.PlayerId == Player?.PlayerId)
        {
            SetShiftTarget(info.AttemptKiller, true);
        }
    }
    public static void OnFixedUpdateOthers(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask)
        {
            ShiftInfo.Clear();
            return;
        }

        if (player == null) return;
        if (Player == null || player.PlayerId != Player.PlayerId) return;
        if (!player.IsAlive()) return;

        if (!shiftActive) return; //初日は近接シフト禁止
        if (nextPlayer != null) return;
        if (postMeetingTime < ChainShifter.ShiftInactiveTime)
        {
            postMeetingTime += Time.deltaTime;
            if (postMeetingTime < ChainShifter.ShiftInactiveTime) return;
        }

        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (target.PlayerId == Player.PlayerId) continue;

            if (ShiftInfo.TryGetValue(target.PlayerId, out var time))
            {
                ShiftInfo.Remove(target.PlayerId);
            }
            else 
            {
                time = 0f;
            }

            if (target.PlayerId == prePlayerId) continue;   //前チェインシフターは除外
            if (target.inVent) continue;    //ベント内であれば除外

            var distance = Vector3.Distance(player.transform.position, target.transform.position);
            if (distance > ChainShifter.ShiftDistance)
            {
                //if (time > 0f) Logger.Info($"Chain targetOut time: {time:F2}, distance: {distance:F2}, {Player.name} => {target.name}", "ChainShifterAdd.OnFixedUpdateOthers");
                continue;
            }
            //if (time == 0f) Logger.Info($"Chain targetIn distance: {distance:F2}, {Player.name} => {target.name}", "ChainShifterAdd.OnFixedUpdateOthers");

            time += Time.fixedDeltaTime;
            if (time >= ChainShifter.ShiftTime)
            {
                SetShiftTarget(target);
                return;
            }
            ShiftInfo[target.PlayerId] = time;
        }
    }
    private static void SetShiftTarget(PlayerControl target, bool byKill = false)
    {
        if (Player == null) return;

        if (byKill)
        {
            nextPlayerByKill = target;
        }
        else
        {
            nextPlayer = target;
            ShiftInfo.Clear();
        }

        Logger.Info($"setChainTarget {Player?.name} => {nextPlayer?.name}, bykill: {byKill}", "ChainShifterAdd.SetShiftTarget");

        if (Player.CanUseKillButton())
        {
            Player.SetKillCooldown();
            Logger.Info($"SetKillCooldown", "ChainShifterAdd.SetShiftTarget");
        }
        else
        {
            Player.RpcProtectedMurderPlayer();
            Logger.Info($"RpcProtectedMurderPlayer", "ChainShifterAdd.SetShiftTarget");
        }
    }
    public static void OnStartMeeting()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        shiftActive = false;
        postMeetingTime = 0f;
    }
    public static void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        shiftActive = true;
        postMeetingTime = 0f;
        ChainShift();
    }
    private static void ChainShift()
    {
        postMeetingTime = 0f;
        ShiftInfo.Clear();

        if (Player == null) return;
        if (nextPlayer == null && nextPlayerByKill == null) return;

        var nowPlayer = Player;
        var next = nextPlayerByKill;
        if (nextPlayer != null && (next == null || !next.IsAlive())) next = nextPlayer;

        Logger.Info($"target player: {nowPlayer?.name}, next: {nextPlayer?.name}(alive: {nextPlayer?.IsAlive()}), nextByKill: {nextPlayerByKill?.name}(alive: {nextPlayerByKill?.IsAlive()})", "ChainShifterAdd.ChainShift");
        Logger.Info($"targetFix target: {next?.name}", "ChainShifterAdd.ChainShift");

        nextPlayerByKill = null;
        nextPlayer = null;

        if (!next.IsAlive())
        {
            Logger.Info($"ChainFail {nowPlayer.name} => {next?.name}", "ChainShifterAdd.ChainShift");
            return;
        }

        //シフト確定
        PlayerState.GetByPlayerId(nowPlayer.PlayerId).RemoveSubRole(CustomRoles.ChainShifterAddon);
        next.RpcSetCustomRole(CustomRoles.ChainShifterAddon);
        Logger.Info($"shift {nowPlayer.name} => {next?.name}", "ChainShifterAdd.ChainShift");

        if (nowPlayer.Is(CustomRoles.ChainShifter))
        {
            nowPlayer.RpcSetCustomRole(ChainShifter.ShiftedRole);
            Logger.Info($"roleChange {nowPlayer.name} => {ChainShifter.ShiftedRole}", "ChainShifterAdd.ChainShift");
        }

        prePlayerId = nowPlayer.PlayerId;
        nowPlayer = next;
        Logger.Info($"preShifter player: {prePlayerId}", "ChainShifterAdd.ChainShift");

        nowPlayer.SyncSettings();
        Utils.NotifyRoles();
    }
}
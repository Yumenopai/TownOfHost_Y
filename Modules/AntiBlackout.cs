using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using AmongUs.GameOptions;
using TownOfHostY.Attributes;
using TownOfHostY.Modules;
using TownOfHostY.Roles.Impostor;
using TownOfHostY.Roles.Neutral;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Unit;

namespace TownOfHostY;

public static class AntiBlackout
{
    ///<summary>
    ///追放処理を上書きするかどうか
    ///</summary>
    public static bool OverrideExiledPlayer
        => Options.NoGameEnd.GetBool()
        || Jackal.RoleInfo.IsEnable || StrayWolf.RoleInfo.IsEnable
        || Pirate.RoleInfo.IsEnable || JackOLantern.RoleInfo.IsEnable
        || Options.IsCCMode;

    public static bool IsCached { get; private set; } = false;
    private static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = new();
    private readonly static LogHandler logger = Logger.Handler("AntiBlackout");

    private static RecognizeType recognizeType = RecognizeType.Impostor;
    public static int ExiledPlayerId = -1;

    public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
    {
        SetRoleChange();

        logger.Info($"SetIsDead is called from {callerMethodName}");
        if (IsCached)
        {
            logger.Info("再度SetIsDeadを実行する前に、RestoreIsDeadを実行してください。");
            return;
        }
        isDeadCache.Clear();
        foreach (var info in GameData.Instance.AllPlayers)
        {
            if (info == null) continue;
            isDeadCache[info.PlayerId] = (info.IsDead, info.Disconnected);
            info.IsDead = false;
            info.Disconnected = false;
        }
        IsCached = true;
        if (doSend) SendGameData();
    }
    private enum RecognizeType
    {
        Impostor,
        StrayWolf,
        Jackal,
        Pirate,
    }
    private static bool IsRecognizeType(PlayerControl pc, RecognizeType type)
    {
        if (pc == null) return false;
        var customRole = pc.GetCustomRole();
        var countType = PlayerState.GetByPlayerId(pc.PlayerId).CountType;
        //var countType = customRole.GetRoleInfo().CountType;
        Logger.Info($"RecognizeType {pc?.name}, {countType}, {customRole}({customRole.GetRoleInfo().CountType})", "AntiBlackout");
        return type switch
        {
            RecognizeType.Impostor => countType == CountTypes.Impostor && customRole != CustomRoles.StrayWolf,
            RecognizeType.StrayWolf => countType == CountTypes.Impostor && customRole == CustomRoles.StrayWolf,
            RecognizeType.Jackal => countType == CountTypes.Jackal,
            RecognizeType.Pirate => countType == CountTypes.Pirate,
            _ => false,
        };
    }
    private static void SetRoleChange()
    {
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;

        RecognizeType remaining = RecognizeType.Impostor;
        List<PlayerControl> list = new();
        foreach (RecognizeType type in Enum.GetValues(typeof(RecognizeType)))
        {
            remaining = type;
            list = Main.AllAlivePlayerControls.Where(pc => pc.PlayerId != ExiledPlayerId && IsRecognizeType(pc, type)).ToList();
            Logger.Info($"CheckRoleCount type: {remaining}, count: {list.Count}, exiled: {ExiledPlayerId}", "AntiBlackout");
            if (list.Count > 0) break;
        }

        recognizeType = remaining;

        Logger.Info($"SetDummyImpostor type: {recognizeType}, count:{list.Count}", "AntiBlackout");
        foreach (var pc in Main.AllPlayerControls.Where(x => !x.Data.Disconnected))
        {
            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
            if (pc.IsAlive() && pc.GetCustomRole().GetRoleInfo().IsDesyncImpostor) continue;
            foreach (var dummy in list)
            {
                dummy.RpcSetRoleDesync(RoleTypes.Impostor, pc.GetClientId());
            }
            foreach (var dead in Main.AllDeadPlayerControls.Where(x => !x.Data.Disconnected))
            {
                dead.RpcSetRoleDesync(RoleTypes.CrewmateGhost, pc.GetClientId());
            }
            Logger.Info($"SetDummyImpostor player: {pc?.name}", "AntiBlackout");
        }
        ExiledPlayerId = -1;
    }
    public static void RestoreIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
    {
        logger.Info($"RestoreIsDead is called from {callerMethodName}");
        foreach (var info in GameData.Instance.AllPlayers)
        {
            if (info == null) continue;
            if (isDeadCache.TryGetValue(info.PlayerId, out var val))
            {
                info.IsDead = val.isDead;
                info.Disconnected = val.Disconnected;
            }
        }
        isDeadCache.Clear();
        IsCached = false;
        if (doSend) SendGameData();
    }

    public static void SendGameData([CallerMemberName] string callerMethodName = "")
    {
        logger.Info($"SendGameData is called from {callerMethodName}");
        foreach (var innerNetObject in GameData.Instance.AllPlayers)
        {
            innerNetObject.SetDirtyBit(uint.MaxValue);
        }
    }
    public static void OnDisconnect(NetworkedPlayerInfo player)
    {
        // 実行条件: クライアントがホストである, IsDeadが上書きされている, playerが切断済み
        if (!AmongUsClient.Instance.AmHost || !IsCached || !player.Disconnected) return;
        isDeadCache[player.PlayerId] = (true, true);
        player.IsDead = player.Disconnected = false;
        SendGameData();
    }

    /// <summary>
    /// 会議終了後、各プレイヤーの視点に正しいRoleTypesをDesync送信して暗転を防ぐ
    /// </summary>
    public static void ResetSetRole(PlayerControl player)
    {
        if (player == null) return;
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;
        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) return;

        var targetClientId = player.GetClientId();
        if (targetClientId == -1) return;

        var sender = CustomRpcSender.Create("ResetSetRole", Hazel.SendOption.Reliable);
        sender.StartMessage(targetClientId);

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc == null) continue;

            var customRole = pc.GetCustomRole();
            var roleInfo = customRole.GetRoleInfo();
            bool isAlive = pc.IsAlive();

            RoleTypes sendRole;

            if (!isAlive)
            {
               
                sendRole = (customRole.IsImpostor() || customRole.IsMadmate()
                    || roleInfo?.BaseRoleType?.Invoke() == RoleTypes.Impostor)
                    ? RoleTypes.ImpostorGhost
                    : RoleTypes.CrewmateGhost;
            }
            else
            {
               
                sendRole = roleInfo?.BaseRoleType?.Invoke() ?? RoleTypes.Crewmate;

               
                if (player.PlayerId != pc.PlayerId && (roleInfo?.IsDesyncImpostor == true))
                    sendRole = RoleTypes.Crewmate;
            }

            pc.RpcSetRoleDesync(sendRole, targetClientId);
        }

        
    }

    ///<summary>
    ///一時的にIsDeadを本来のものに戻した状態でコードを実行します
    ///<param name="action">実行内容</param>
    ///</summary>
    public static void TempRestore(Action action)
    {
        logger.Info("==Temp Restore==");
        //IsDeadが上書きされた状態でTempRestoreが実行されたかどうか
        bool before_IsCached = IsCached;
        try
        {
            if (before_IsCached) RestoreIsDead(doSend: false);
            action();
        }
        catch (Exception ex)
        {
            logger.Warn("AntiBlackout.TempRestore内で例外が発生しました");
            logger.Exception(ex);
        }
        finally
        {
            if (before_IsCached) SetIsDead(doSend: false);
            logger.Info("==/Temp Restore==");
        }
    }

    [GameModuleInitializer]
    public static void Reset()
    {
        logger.Info("==Reset==");
        if (isDeadCache == null) isDeadCache = new();
        isDeadCache.Clear();
        IsCached = false;
        recognizeType = RecognizeType.Impostor;
    }
}
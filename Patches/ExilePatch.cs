using AmongUs.Data;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using System.Collections;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Neutral;
using AmongUs.GameOptions;
namespace TownOfHostY
{
    class ExileControllerWrapUpPatch
    {
        public static NetworkedPlayerInfo AntiBlackout_LastExiled;
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.initData.networkedPlayer);
                }
                finally
                {
                    WrapUpFinalizer(__instance.initData.networkedPlayer);
                }
            }
        }

        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.Animate))]
        class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance, ref Il2CppSystem.Collections.IEnumerator __result)
            {
                __result = WrapAnimateCoroutine(__instance, __result).WrapToIl2Cpp();
            }
            static System.Collections.IEnumerator WrapAnimateCoroutine(AirshipExileController instance, Il2CppSystem.Collections.IEnumerator original)
            {
                while (original.MoveNext())
                {
                    yield return original.Current;
                }
                try
                {
                    WrapUpPostfix(instance.initData.networkedPlayer);
                }
                finally
                {
                    WrapUpFinalizer(instance.initData.networkedPlayer);
                }
            }
        }
        static void WrapUpPostfix(NetworkedPlayerInfo exiled)
        {
            if (AntiBlackout.OverrideExiledPlayer)
            {
                exiled = AntiBlackout_LastExiled;
            }

            bool DecidedWinner = false;
            if (AmongUsClient.Instance.AmHost)
            {
                AntiBlackout.RestoreIsDead(doSend: false);
                if (exiled != null)
                {
                    var role = exiled.GetCustomRole();
                    var info = role.GetRoleInfo();
                    //霊界用暗転バグ対処
                    if (!AntiBlackout.OverrideExiledPlayer && info?.IsDesyncImpostor == true)
                        exiled.Object?.ResetPlayerCam(1f);

                    exiled.IsDead = true;
                    if (role != CustomRoles.AntiComplete || PlayerState.GetByPlayerId(exiled.PlayerId).DeathReason == CustomDeathReason.etc)
                        PlayerState.GetByPlayerId(exiled.PlayerId).DeathReason = CustomDeathReason.Vote;

                    foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
                    {
                        roleClass.OnExileWrapUp(exiled, ref DecidedWinner);
                    }
                    Sending.OnExileWrapUp(exiled.Object);

                    if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) PlayerState.GetByPlayerId(exiled.PlayerId).SetDead();
                }                
                AntiBlackout.SendGameData();
            }
            AfterMeetingTasks();
        }
        static void AfterMeetingTasks()
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
                // ランダムスポーン
                switch ((MapNames)Main.NormalOptions.MapId)
                {
                    case MapNames.Skeld:
                        if (Options.RandomSpawn_Skeld.GetBool())
                        {
                            Main.AllPlayerControls.Do(new RandomSpawn.SkeldSpawnMap().RandomTeleport);
                        }
                        break;
                    case MapNames.MiraHQ:
                        if (Options.RandomSpawn_MiraHQ.GetBool())
                        {
                            Main.AllPlayerControls.Do(new RandomSpawn.MiraHQSpawnMap().RandomTeleport);
                        }
                        break;
                    case MapNames.Polus:
                        if (Options.RandomSpawn_Polus.GetBool())
                        {
                            Main.AllPlayerControls.Do(new RandomSpawn.PolusSpawnMap().RandomTeleport);
                        }
                        break;
                    case MapNames.Fungle:
                        if (Options.RandomSpawn_Fungle.GetBool())
                        {
                            Main.AllPlayerControls.Do(new RandomSpawn.FungleSpawnMap().RandomTeleport);
                        }
                        break;
                }
            }
            FallFromLadder.Reset();
            Utils.CountAlivePlayers(true);
            Utils.AfterMeetingTasks();
            Utils.NotifyRoles();
            Utils.SyncAllSettings();
        }

        static void WrapUpFinalizer(NetworkedPlayerInfo exiled)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    exiled = AntiBlackout_LastExiled;
                    if (AntiBlackout.OverrideExiledPlayer &&
                        exiled != null &&
                        exiled.Object != null)
                    {
                        exiled.Object.RpcExileV3();
                    }
                }, 0.5f, "Restore IsDead Task");

                _ = new LateTask(() =>
                {
                    Main.AfterMeetingDeathPlayers.Do(x =>
                    {
                        (byte playerId, CustomDeathReason reason) = (x.Key, x.Value);

                        var player = Utils.GetPlayerById(playerId);
                        var roleClass = CustomRoleManager.GetByPlayerId(playerId);
                        var requireResetCam = player?.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true;
                        var state = PlayerState.GetByPlayerId(playerId);
                        Logger.Info($"{player.GetNameWithRole()}を{reason}で死亡させました", "AfterMeetingDeath");
                        state.DeathReason = reason;
                        player?.RpcExileV3();
                        state.SetDead();
                        if (reason == CustomDeathReason.Suicide)
                            player?.SetRealKiller(player, true);
                        if (requireResetCam)
                            player?.ResetPlayerCam(1f);
                        Executioner.ChangeRoleByTarget(playerId);
                        Lawyer.ChangeRoleByTarget(player);
                        if (roleClass is Jackal jackal)
                            Jackal.CheckPromoted();
                        Elder.DeadByRevenge(playerId);
                    });
                    Main.AfterMeetingDeathPlayers.Clear();
                }, 0.6f, "AfterMeetingDeathPlayers Task");


                _ = new LateTask(() =>
                {
                    Main.AllPlayerControls.Do(pc => AntiBlackout.ResetSetRole(pc));
                }, 0.65f, "AfterMeeting_ResetSetRole");

                _ = new LateTask(() =>
                {
                    if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        var state = PlayerState.GetByPlayerId(pc.PlayerId);
                        state.IsBlackOut = false;
                        pc.ResetKillCooldown(); // AllPlayerKillCooldown に正しい値をセットしてから同期
                        pc.MarkDirtySettings();
                    }
                    Utils.SyncAllSettings();
                }, 1.0f, "AfterMeeting_SetKillCooldown");
            }

            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            RemoveDisableDevicesPatch.UpdateDisableDevices();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

            Logger.Info("タスクフェイズ開始", "Phase");
        }
    }


    [HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
    class PolusExileHatFixPatch
    {
        public static void Prefix(PbExileController __instance)
        {
            __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
        }
    }
}
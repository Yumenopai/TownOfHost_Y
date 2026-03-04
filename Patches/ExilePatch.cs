using AmongUs.Data;
using HarmonyLib;
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

        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
        class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance)
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
        static void WrapUpPostfix(NetworkedPlayerInfo exiled)
        {
            if (AntiBlackout.OverrideExiledPlayer)
            {
                exiled = AntiBlackout_LastExiled;
            }

            bool DecidedWinner = false;
            if (!AmongUsClient.Instance.AmHost) return;
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

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.ResetKillCooldown();
            }
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
            FallFromLadder.Reset();
            Utils.CountAlivePlayers(true);
            Utils.AfterMeetingTasks();
            Utils.SyncAllSettings();
            Utils.NotifyRoles();
        }

        static void WrapUpFinalizer(NetworkedPlayerInfo exiled)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    exiled = AntiBlackout_LastExiled;
                    AntiBlackout.SendGameData();
                    if (AntiBlackout.OverrideExiledPlayer &&
                        exiled != null &&
                        exiled.Object != null)
                    {
                        exiled.Object.RpcExileV2();
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
                        state.SetDead();
                        player?.RpcExileV2();
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
                }, 0.5f, "AfterMeetingDeathPlayers Task");

                
                _ = new LateTask(() =>
                {
                    Main.AllPlayerControls.Do(pc => AntiBlackout.ResetSetRole(pc));                  
                }, 0.52f, "AfterMeeting_ResetSetRole");
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
using System.Linq;
using AmongUs.Data;
using HarmonyLib;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;

namespace TownOfHost
{
    class ExileControllerWrapUpPatch
    {
        public static GameData.PlayerInfo AntiBlackout_LastExiled;
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.exiled);
                }
                finally
                {
                    WrapUpFinalizer(__instance.exiled);
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
                    WrapUpPostfix(__instance.exiled);
                }
                finally
                {
                    WrapUpFinalizer(__instance.exiled);
                }
            }
        }
        static void WrapUpPostfix(GameData.PlayerInfo exiled)
        {
            if (AntiBlackout.OverrideExiledPlayer)
            {
                exiled = AntiBlackout_LastExiled;
            }

            bool DecidedWinner = false;
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            AntiBlackout.RestoreIsDead(doSend: false);

            if (Options.CurrentGameMode.IsOneNightMode())
            {
                int WinNumber = 0;
                byte IsWinONHangedManId = 255;

                if (exiled != null)
                {
                    //霊界用暗転バグ対処
                    if (!AntiBlackout.OverrideExiledPlayer && Main.ResetCamPlayerList.Contains(exiled.PlayerId))
                        exiled.Object?.ResetPlayerCam(1f);

                    exiled.IsDead = true;
                    if (Main.PlayerStates[exiled.PlayerId].deathReason != PlayerState.DeathReason.Execution)
                    {
                        Main.PlayerStates[exiled.PlayerId].deathReason = PlayerState.DeathReason.Vote;
                        Main.PlayerStates[exiled.PlayerId].SetDead();
                    }
                    var role = exiled.GetCustomRole();

                    if (role == CustomRoles.ONHangedMan && AmongUsClient.Instance.AmHost)
                    {
                        WinNumber = 2;
                        IsWinONHangedManId = exiled.PlayerId;
                    }
                    else if (role.IsONImpostor() && AmongUsClient.Instance.AmHost)
                    {
                        WinNumber = 1;
                    }
                }
                //ONMeetingExiledPlayersの処理
                {
                    byte HunterPlayerId = 255;
                    foreach(var exiledPlayer in Main.ONMeetingExiledPlayers)
                    {
                        var role = Utils.GetPlayerById(exiledPlayer).GetCustomRole();

                        if (role == CustomRoles.ONHunter && AmongUsClient.Instance.AmHost)
                        {
                            WinNumber = -1;
                            HunterPlayerId = exiledPlayer;
                        }
                    }
                    if (WinNumber == -1)
                    {
                        Main.ONMeetingExiledPlayers.Remove(HunterPlayerId);
                        HunterPlayerId = 255;
                    }
                    else if (WinNumber != 1)
                    {
                        foreach (var exiledPlayerId in Main.ONMeetingExiledPlayers)
                        {
                            var role = Utils.GetPlayerById(exiledPlayerId).GetCustomRole();

                            if (role == CustomRoles.ONHangedMan && AmongUsClient.Instance.AmHost)
                            {
                                WinNumber = 2;
                                IsWinONHangedManId = exiledPlayerId;
                                break;
                            }
                            else if (role.IsONImpostor() && AmongUsClient.Instance.AmHost)
                            {
                                WinNumber = 1;
                            }
                        }
                    }
                }
                if (WinNumber == -1)
                {
                    WinNumber = 0;
                }
                else if (WinNumber == 2)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.HangedMan);
                    CustomWinnerHolder.WinnerIds.Add(IsWinONHangedManId);
                }
                else if (WinNumber == 1)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
            }
            else
            {
                if (exiled != null)
                {
                    //霊界用暗転バグ対処
                    if (!AntiBlackout.OverrideExiledPlayer && Main.ResetCamPlayerList.Contains(exiled.PlayerId))
                        exiled.Object?.ResetPlayerCam(1f);

                    exiled.IsDead = true;
                    var role = exiled.GetCustomRole();

                    if (Main.PlayerStates[exiled.PlayerId].deathReason == PlayerState.DeathReason.win)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.AntiComplete);
                        CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                        DecidedWinner = true;
                    }
                    if (role != CustomRoles.AntiComplete)
                        Main.PlayerStates[exiled.PlayerId].deathReason = PlayerState.DeathReason.Vote;

                    if (role == CustomRoles.Jester && AmongUsClient.Instance.AmHost)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jester);
                        CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                        //吊られたJesterをターゲットにしているExecutionerも追加勝利
                        foreach (var executioner in Executioner.playerIdList)
                        {
                            var GetValue = Executioner.Target.TryGetValue(executioner, out var targetId);
                            if (GetValue && exiled.PlayerId == targetId)
                            {
                                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Executioner);
                                CustomWinnerHolder.WinnerIds.Add(executioner);
                            }
                        }
                        DecidedWinner = true;
                    }
                    if (role == CustomRoles.Terrorist && AmongUsClient.Instance.AmHost)
                    {
                        Utils.CheckTerroristWin(exiled);
                        DecidedWinner = true;
                    }
                    Executioner.CheckExileTarget(exiled, DecidedWinner);
                    Lawyer.CheckExileTarget(exiled.PlayerId);
                    SchrodingerCat.ChangeTeam(exiled.Object);

                    Main.ExiledPlayer = exiled.PlayerId;

                    if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) Main.PlayerStates[exiled.PlayerId].SetDead();
                }
                if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
                    Main.RefixCooldownDelay = Options.DefaultKillCooldown - 3f;

                Witch.RemoveSpelledPlayer();

                foreach (var pc in Main.AllPlayerControls)
                {
                    pc.ResetKillCooldown();
                    if ((Options.MayorHasPortableButton.GetBool() && pc.Is(CustomRoles.Mayor)) || pc.Is(CustomRoles.Chairman) || pc.Is(CustomRoles.GrudgeSheriff) || pc.Is(CustomRoles.Psychic))
                        pc.RpcResetAbilityCooldown();
                    if (pc.Is(CustomRoles.Warlock))
                    {
                        Main.CursedPlayers[pc.PlayerId] = null;
                        Main.isCurseAndKill[pc.PlayerId] = false;
                    }
                }
                if (Options.RandomSpawn.GetBool())
                {
                    RandomSpawn.SpawnMap map;
                    switch (Main.NormalOptions.MapId)
                    {
                        case 0:
                            map = new RandomSpawn.SkeldSpawnMap();
                            Main.AllPlayerControls.Do(map.RandomTeleport); break;
                        case 1:
                            map = new RandomSpawn.MiraHQSpawnMap();
                            Main.AllPlayerControls.Do(map.RandomTeleport); break;
                        case 2:
                            map = new RandomSpawn.PolusSpawnMap();
                            Main.AllPlayerControls.Do(map.RandomTeleport); break;
                    }
                }
                FallFromLadder.Reset();
            }

            Utils.CountAlivePlayers(true);
            Utils.AfterMeetingTasks();
            Utils.SyncAllSettings();
            Utils.NotifyRoles();
        }

        static void WrapUpFinalizer(GameData.PlayerInfo exiled)
        {
            //WrapUpPostfixで例外が発生しても、この部分だけは確実に実行されます。
            if (AmongUsClient.Instance.AmHost)
            {
                new LateTask(() =>
                {
                    exiled = AntiBlackout_LastExiled;
                    AntiBlackout.SendGameData();
                    if (AntiBlackout.OverrideExiledPlayer && // 追放対象が上書きされる状態 (上書きされない状態なら実行不要)
                        exiled != null && //exiledがnullでない
                        exiled.Object != null) //exiled.Objectがnullでない
                    {
                        exiled.Object.RpcExileV2();
                    }
                }, 0.5f, "Restore IsDead Task");
                new LateTask(() =>
                {
                    Main.AfterMeetingDeathPlayers.Do(x =>
                    {
                        var player = Utils.GetPlayerById(x.Key);
                        Logger.Info($"{player.GetNameWithRole()}を{x.Value}で死亡させました", "AfterMeetingDeath");
                        Main.PlayerStates[x.Key].deathReason = x.Value;
                        Main.PlayerStates[x.Key].SetDead();
                        player?.RpcExileV2();
                        if (x.Value == PlayerState.DeathReason.Suicide)
                            player?.SetRealKiller(player, true);
                        if (Main.ResetCamPlayerList.Contains(x.Key))
                            player?.ResetPlayerCam(1f);
                        if (Executioner.Target.ContainsValue(x.Key))
                            Executioner.ChangeRoleByTarget(player);
                        if (Lawyer.Target.ContainsValue(x.Key))
                            Lawyer.ChangeRoleByTarget(player);
                    });
                    Main.AfterMeetingDeathPlayers.Clear();
                }, 0.5f, "AfterMeetingDeathPlayers Task");
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
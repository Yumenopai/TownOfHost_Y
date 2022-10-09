using System;
using System.Linq;
using HarmonyLib;
using Hazel;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
    class GameEndChecker
    {
        private static GameEndPredicate predicate;
        public static bool Prefix(ShipStatus __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam != CustomWinner.Draw) return false;

            GameOverReason reason = GameOverReason.ImpostorByKill;

            if (predicate != null && predicate.CheckForEndGame(out var r)) reason = r;

            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                switch (CustomWinnerHolder.WinnerTeam)
                {
                    case CustomWinner.Crewmate:
                        PlayerControl.AllPlayerControls.ToArray()
                            .Where(pc => pc.Is(RoleType.Crewmate) && !pc.Is(CustomRoles.Lovers))
                            .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                    case CustomWinner.Impostor:
                        PlayerControl.AllPlayerControls.ToArray()
                                .Where(pc => pc.Is(RoleType.Impostor) || pc.Is(RoleType.Madmate))
                                .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                }
                __instance.enabled = false;
                StartEndGame(
                    reason,
                    CustomWinnerHolder.WinnerTeam is not CustomWinner.Crewmate or CustomWinner.Impostor
                );
                predicate = null;
            }
            return false;
        }
        public static void StartEndGame(GameOverReason reason, bool SetImpostorsToGA)
        {
            var sender = new CustomRpcSender("EndGameSender", SendOption.Reliable, true);
            sender.StartMessage(-1); // 5: GameData

            //守護天使化
            var canEgoistWin = Main.AliveImpostorCount == 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if ((SetImpostorsToGA && pc.Data.Role.IsImpostor) || //インポスター: 引数による
                    pc.Is(CustomRoles.Sheriff) || //シェリフ: 無条件
                    (pc.Is(CustomRoles.Arsonist) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) || //アーソニスト: 敗北時
                    (pc.Is(CustomRoles.Jackal) && !CustomWinnerHolder.WinnerRoles.Contains(CustomRoles.Jackal)) || //ジャッカル: 敗北時
                    (canEgoistWin && pc.Is(RoleType.Impostor)) || //インポスター: エゴイスト勝利
                    (!canEgoistWin && pc.Is(CustomRoles.Egoist)) //エゴイスト: インポスター勝利
                )
                {
                    Logger.Info($"{pc.GetNameWithRole()}: GuardianAngelに変更", "ResetRoleAndEndGame");
                    sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                        .Write((ushort)RoleTypes.GuardianAngel)
                        .EndRpc();
                    pc.SetRole(RoleTypes.GuardianAngel);
                }
            }

            // CustomWinnerHolderの情報の同期
            sender.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame);
            CustomWinnerHolder.WriteTo(sender.stream);
            sender.EndRpc()
                .EndMessage();

            // バニラ側のゲーム終了RPC
            MessageWriter writer = sender.stream;
            writer.StartMessage(8); //8: EndGame
            {
                writer.Write(AmongUsClient.Instance.GameId); //GameId
                writer.Write((byte)reason); //GameoverReason
                writer.Write(false); //showAd
            }
            writer.EndMessage();

            sender.SendMessage();
        }

        public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
        public static void SetPredicateToHideAndSeek() => predicate = new HideAndSeekGameEndPredicate();

        // ===== ゲーム終了条件 =====
        // 通常ゲーム用
        class NormalGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;
                if (CheckGameEndBySabotage(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int[] counts = CountLivingPlayersByPredicates(
                    pc => pc.Is(RoleType.Impostor) || pc.Is(CustomRoles.Egoist), //インポスター
                    pc => pc.Is(CustomRoles.Jackal), //ジャッカル
                    pc => !pc.Is(RoleType.Impostor) && !pc.Is(CustomRoles.Egoist) && !pc.Is(CustomRoles.Jackal) //その他
                );
                int Imp = counts[0], Jackal = counts[1], Crew = counts[2];


                if (Imp == 0 && Crew == 0 && Jackal == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Jackal == 0 && Crew <= Imp) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0 && Crew <= Jackal) //ジャッカル勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JSchrodingerCat);
                }
                else if (Jackal == 0 && Imp == 0) //クルー勝利
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
            public bool CheckGameEndByTask(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (Options.DisableTaskWin.GetBool()) return false;

                if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
                {
                    reason = GameOverReason.HumansByTask;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                    return true;
                }
                return false;
            }
            public bool CheckGameEndBySabotage(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (ShipStatus.Instance.Systems == null) return false;

                // TryGetValueは使用不可
                var systems = ShipStatus.Instance.Systems;
                LifeSuppSystemType LifeSupp;
                if (systems.ContainsKey(SystemTypes.LifeSupp) && // サボタージュ存在確認
                    (LifeSupp = systems[SystemTypes.LifeSupp].TryCast<LifeSuppSystemType>()) != null && // キャスト可能確認
                    LifeSupp.Countdown < 0f) // タイムアップ確認
                {
                    // 酸素サボタージュ
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                    reason = GameOverReason.ImpostorBySabotage;
                    LifeSupp.Countdown = 10000f;
                    return true;
                }

                ISystemType sys = null;
                if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
                else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];

                ICriticalSabotage critical;
                if (sys != null && // サボタージュ存在確認
                    (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
                    critical.Countdown < 0f) // タイムアップ確認
                {
                    // リアクターサボタージュ
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                    reason = GameOverReason.ImpostorBySabotage;
                    critical.ClearSabotage();
                    return true;
                }

                return false;
            }
        }

        // HideAndSeek用
        class HideAndSeekGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int[] counts = CountLivingPlayersByPredicates(
                    pc => pc.Is(RoleType.Impostor), //インポスター
                    pc => pc.Is(RoleType.Crewmate) //クルー(Troll,Fox除く)
                );
                int Imp = counts[0], Crew = counts[1];


                if (Imp == 0 && Crew == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Crew <= 0) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0) //クルー勝利(インポスター切断など)
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
            public bool CheckGameEndByTask(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (Options.DisableTaskWin.GetBool()) return false;

                if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
                {
                    reason = GameOverReason.HumansByTask;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                    return true;
                }
                return false;
            }
        }
    }

    public abstract class GameEndPredicate
    {
        /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
        /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
        /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
        public abstract bool CheckForEndGame(out GameOverReason reason);

        /// <summary>各条件に合ったプレイヤーの人数を取得し、配列に同順で格納します。</summary>
        public int[] CountLivingPlayersByPredicates(params Predicate<PlayerControl>[] predicates)
        {
            int[] counts = new int[predicates.Length];
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                for (int i = 0; i < predicates.Length; i++)
                {
                    if (pc.IsAlive() && predicates[i](pc)) counts[i]++;
                }
            }
            return counts;
        }
    }
    // =============================
    //勝利判定処理
    //[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
    /*class CheckGameEndPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            if (!GameData.Instance) return false;
            if (DestroyableSingleton<TutorialManager>.InstanceExists) return true;
            var statistics = new PlayerStatistics(__instance);

            if (CheckAndEndGameForTerminate(__instance)) return false;

            if (Options.NoGameEnd.GetBool()) return false;

            if (CheckAndEndGameForSoloWin(__instance)) return false;
            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Default)
            {
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (CheckAndEndGameForHideAndSeek(__instance, statistics)) return false;
                    if (CheckAndEndGameForTroll(__instance)) return false;
                    if (CheckAndEndGameForTaskWin(__instance)) return false;
                }
                else
                {
                    if (CheckAndEndGameForTaskWin(__instance)) return false;
                    if (CheckAndEndGameForEveryoneDied(__instance, statistics)) return false;
                    if (CheckAndEndGameForSabotageWin(__instance) ||
                        CheckAndEndGameForImpostorWin(__instance, statistics))
                    {
                        CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Impostor);
                        PlayerControl.AllPlayerControls.ToArray().Do(pc =>
                        {
                            if ((pc.Is(RoleType.Impostor) || pc.Is(RoleType.Madmate)) && !pc.Is(CustomRoles.Lovers))
                                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        });
                        return false;
                    }
                    if (CheckAndEndGameForJackalWin(__instance, statistics)) return false;
                    if (CheckAndEndGameForCrewmateWin(__instance, statistics)) return false;
                }
            }
            return false;
        }

        private static bool CheckAndEndGameForSabotageWin(ShipStatus __instance)
        {
            if (__instance.Systems == null) return false;
            ISystemType systemType = __instance.Systems.ContainsKey(SystemTypes.LifeSupp) ? __instance.Systems[SystemTypes.LifeSupp] : null;
            if (systemType != null)
            {
                LifeSuppSystemType lifeSuppSystemType = systemType.TryCast<LifeSuppSystemType>();
                if (lifeSuppSystemType != null && lifeSuppSystemType.Countdown < 0f)
                {
                    EndGameForSabotage(__instance);
                    lifeSuppSystemType.Countdown = 10000f;
                    return true;
                }
            }
            ISystemType systemType2 = __instance.Systems.ContainsKey(SystemTypes.Reactor) ? __instance.Systems[SystemTypes.Reactor] : null;
            if (systemType2 == null)
            {
                systemType2 = __instance.Systems.ContainsKey(SystemTypes.Laboratory) ? __instance.Systems[SystemTypes.Laboratory] : null;
            }
            if (systemType2 != null)
            {
                ICriticalSabotage criticalSystem = systemType2.TryCast<ICriticalSabotage>();
                if (criticalSystem != null && criticalSystem.Countdown < 0f)
                {
                    EndGameForSabotage(__instance);
                    criticalSystem.ClearSabotage();
                    return true;
                }
            }
            return false;
        }

        private static bool CheckAndEndGameForTaskWin(ShipStatus __instance)
        {
            if (Options.DisableTaskWin.GetBool()) return false;
            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Crewmate);
                PlayerControl.AllPlayerControls.ToArray().Do(pc =>
                {
                    if (pc.Is(RoleType.Crewmate) && !pc.Is(CustomRoles.Lovers))
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                });
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.HumansByTask, false);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForEveryoneDied(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TotalAlive <= 0)
            {
                __instance.enabled = false;
                CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                ResetRoleAndEndGame(GameOverReason.ImpostorByKill, true);
                return true;
            }
            return false;
        }
        private static bool CheckAndEndGameForImpostorWin(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TeamImpostorsAlive >= statistics.TotalAlive - statistics.TeamImpostorsAlive &&
                statistics.TeamJackalAlive <= 0)
            {
                if (Options.IsStandardHAS && statistics.TotalAlive - statistics.TeamImpostorsAlive != 0) return false;
                __instance.enabled = false;
                var endReason = TempData.LastDeathReason switch
                {
                    DeathReason.Exile => GameOverReason.ImpostorByVote,
                    DeathReason.Kill => GameOverReason.ImpostorByKill,
                    _ => GameOverReason.ImpostorByVote,
                };
                ResetRoleAndEndGame(endReason, false);
                return true;
            }
            return false;
        }
        private static bool CheckAndEndGameForJackalWin(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TeamJackalAlive >= statistics.TotalAlive - statistics.TeamJackalAlive &&
                statistics.TeamImpostorsAlive <= 0)
            {
                if (Options.IsStandardHAS && statistics.TotalAlive - statistics.TeamJackalAlive != 0) return false;
                __instance.enabled = false;
                var endReason = TempData.LastDeathReason switch
                {
                    DeathReason.Exile => GameOverReason.ImpostorByVote,
                    DeathReason.Kill => GameOverReason.ImpostorByKill,
                    _ => GameOverReason.ImpostorByVote,
                };

                CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JSchrodingerCat);
                ResetRoleAndEndGame(endReason, true);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForCrewmateWin(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TeamImpostorsAlive == 0 && statistics.TeamJackalAlive == 0)
            {
                CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Crewmate);
                PlayerControl.AllPlayerControls.ToArray().Do(pc =>
                {
                    if (pc.Is(RoleType.Crewmate) && !pc.Is(CustomRoles.Lovers))
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                });
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.HumansByVote, false);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForHideAndSeek(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TotalAlive - statistics.TeamImpostorsAlive == 0)
            {
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.ImpostorByKill, false);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForTroll(ShipStatus __instance)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var hasRole = Main.AllPlayerCustomRoles.TryGetValue(pc.PlayerId, out var role);
                if (!hasRole) return false;
                if (role == CustomRoles.HASTroll && pc.Data.IsDead)
                {
                    CustomWinnerHolder.WinnerTeam = CustomWinner.HASTroll;
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    __instance.enabled = false;
                    ResetRoleAndEndGame(GameOverReason.ImpostorByKill, true);
                    return true;
                }
            }
            return false;
        }

        private static bool CheckAndEndGameForTerminate(ShipStatus __instance)
        {
            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Draw)
            {
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.ImpostorByKill, false);
                return true;
            }
            return false;
        }
        private static bool CheckAndEndGameForSoloWin(ShipStatus __instance)
        {
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.ImpostorByKill, true);
                return true;
            }
            return false;
        }


        private static void EndGameForSabotage(ShipStatus __instance)
        {
            __instance.enabled = false;
            ResetRoleAndEndGame(GameOverReason.ImpostorBySabotage, false);
            return;
        }
        private static void ResetRoleAndEndGame(GameOverReason reason, bool SetImpostorsToGA, bool showAd = false)
        {
            var sender = new CustomRpcSender("EndGameSender", SendOption.Reliable, true);
            sender.StartMessage(-1); // 5:GameData

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var LoseImpostorRole = Main.AliveImpostorCount == 0 ? pc.Is(RoleType.Impostor) : pc.Is(CustomRoles.Egoist);
                if ((SetImpostorsToGA && pc.Data.Role.IsImpostor) || //インポスター:引数による
                    pc.Is(CustomRoles.Sheriff) || //シェリフ:無条件
                    (!(CustomWinnerHolder.WinnerTeam == CustomWinner.Arsonist) && pc.Is(CustomRoles.Arsonist)) || //アーソニスト:敗北
                    (CustomWinnerHolder.WinnerTeam != CustomWinner.Jackal && pc.Is(CustomRoles.Jackal)) || //ジャッカル:敗北
                    LoseImpostorRole
                )
                {
                    try
                    {
                        Logger.Info($"{pc.GetNameWithRole()}: GuardianAngelに変更", "ResetRoleAndEndGame");
                        sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                                .Write((ushort)RoleTypes.GuardianAngel)
                                .EndRpc();
                        pc.SetRole(RoleTypes.GuardianAngel); //ホスト用
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"GuardianAngelへ変更中にエラーが発生しました。\n{ex}", "ResetRoleAndEndGame");
                    }
                }
            }

            // CustomWinnerHolderの情報送信
            sender.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame);
            CustomWinnerHolder.WriteTo(sender.stream);
            sender.EndRpc();
            sender.EndMessage();

            // AmongUs側のゲーム終了RPC
            MessageWriter writer = sender.stream;
            writer.StartMessage(8);
            {
                writer.Write(AmongUsClient.Instance.GameId); //ここまでStartEndGameの内容
                writer.Write((byte)reason);
                writer.Write(showAd);
            }
            writer.EndMessage();

            sender.SendMessage();
        }
        //プレイヤー統計
        internal class PlayerStatistics
        {
            public int TeamImpostorsAlive { get; set; }
            public int TotalAlive { get; set; }
            public int TeamJackalAlive { get; set; }

            public PlayerStatistics(ShipStatus __instance)
            {
                GetPlayerCounts();
            }

            private void GetPlayerCounts()
            {
                int numImpostorsAlive = 0;
                int numTotalAlive = 0;
                int numJackalsAlive = 0;

                for (int i = 0; i < GameData.Instance.PlayerCount; i++)
                {
                    GameData.PlayerInfo playerInfo = GameData.Instance.AllPlayers[i];
                    var hasHideAndSeekRole = Main.AllPlayerCustomRoles.TryGetValue((byte)i, out var role);
                    if (!playerInfo.Disconnected)
                    {
                        if (!playerInfo.IsDead)
                        {
                            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek || !hasHideAndSeekRole)
                            {
                                numTotalAlive++;//HideAndSeek以外
                            }
                            else
                            {
                                //HideAndSeek中
                                if (role is not CustomRoles.HASFox and not CustomRoles.HASTroll) numTotalAlive++;
                            }

                            if (playerInfo.Role.TeamType == RoleTeamTypes.Impostor &&
                            (playerInfo.GetCustomRole() != CustomRoles.Sheriff || playerInfo.GetCustomRole() != CustomRoles.Arsonist))
                            {
                                numImpostorsAlive++;
                            }
                            else if (playerInfo.GetCustomRole() == CustomRoles.Jackal) numJackalsAlive++;
                        }
                    }
                }

                TeamImpostorsAlive = numImpostorsAlive;
                TotalAlive = numTotalAlive;
                TeamJackalAlive = numJackalsAlive;
            }
        }
    }//*/
}
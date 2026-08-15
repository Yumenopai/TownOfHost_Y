using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using UnityEngine;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Neutral;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    class GameEndChecker
    {
        private static GameEndPredicate predicate;
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            //ゲーム終了判定済みなら中断
            if (predicate == null) return false;

            //ゲーム終了しないモードで廃村以外の場合は中断
            if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam != CustomWinner.Draw) return false;

            //廃村用に初期値を設定
            var reason = GameOverReason.ImpostorsByKill;

            //ゲーム終了判定
            predicate.CheckForEndGame(out reason);

            //ゲーム終了時
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                Logger.Info($"GameEnd winner: {CustomWinnerHolder.WinnerTeam}, reason: {reason}", "GameEndChecker");
                //カモフラージュ強制解除
                Main.AllPlayerControls.Do(pc =>
                {
                    Camouflage.RpcSetSkin(false, pc, ForceRevert: true, RevertToDefault: true);
                    SkinChangeMode.RpcSetSkin(pc, pc);
                });

                var originalWinnerTeam = CustomWinnerHolder.WinnerTeam;
                var baseWinnerOverridden = false;

                switch (originalWinnerTeam)
                {
                    case CustomWinner.Crewmate:
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc.Is(CustomRoleTypes.Crewmate) && !pc.Is(CustomRoles.Lovers)
                                && !(pc.Is(CustomRoles.Bakery) && Bakery.IsNeutral(pc)) && !pc.Is(CustomRoles.Archenemy))
                            {
                                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            }
                        }
                        break;
                    case CustomWinner.Impostor:
                        if (Egoist.CheckWin()) break;

                        Main.AllPlayerControls
                            .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Archenemy) && !pc.Is(CustomRoles.MadJester))
                            .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                }
                if (originalWinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
                {
                    if (FoxSpirit.CheckWin() && !reason.Equals(GameOverReason.ImpostorsBySabotage))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.FoxSpirit);
                        baseWinnerOverridden = true;
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.FoxSpirit) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    if (God.CheckWin())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
                        baseWinnerOverridden = true;
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.God) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    if (Lovers.playersList.Count > 0 && Lovers.playersList.ToArray().All(p => p.IsAlive())
                        && !reason.Equals(GameOverReason.CrewmatesByTask) && !(Lovers.LoversAddWin.GetBool() || PlatonicLover.AddWin))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                        baseWinnerOverridden = true;
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.Lovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    var darkHideOrNBakeryTriggered = false;
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.DarkHide) && !pc.Data.IsDead
                            && ((originalWinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorsBySabotage)) || originalWinnerTeam == CustomWinner.DarkHide
                            || (originalWinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.CrewmatesByTask) && ((DarkHide)pc.GetRoleClass()).IsWinKill == true)))
                        {                          
                            if (!darkHideOrNBakeryTriggered)
                            {
                                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DarkHide);
                                baseWinnerOverridden = true;
                                darkHideOrNBakeryTriggered = true;
                            }
                            else
                            {
                                CustomWinnerHolder.WinnerTeam = CustomWinner.DarkHide;
                            }
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        else if (pc.Is(CustomRoles.Bakery) && Bakery.IsNeutral(pc) && pc.IsAlive()
                            && ((originalWinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorsBySabotage)) || originalWinnerTeam == CustomWinner.NBakery
                            || (originalWinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.CrewmatesByTask))))
                        {
                            if (!darkHideOrNBakeryTriggered)
                            {
                                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NBakery);
                                baseWinnerOverridden = true;
                                darkHideOrNBakeryTriggered = true;
                            }
                            else
                            {
                                CustomWinnerHolder.WinnerTeam = CustomWinner.NBakery;
                            }
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                    }
                    if (!baseWinnerOverridden && CustomWinnerHolder.WinnerTeam == originalWinnerTeam && CustomWinnerHolder.WinnerIds.Count == 0)
                    {
                        switch (originalWinnerTeam)
                        {
                            case CustomWinner.Crewmate:
                                foreach (var pc in PlayerControl.AllPlayerControls)
                                {
                                    if (pc.Is(CustomRoleTypes.Crewmate) && !pc.Is(CustomRoles.Lovers)
                                        && !(pc.Is(CustomRoles.Bakery) && Bakery.IsNeutral(pc)) && !pc.Is(CustomRoles.Archenemy))
                                    {
                                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                    }
                                }
                                break;
                            case CustomWinner.Impostor:
                                if (!Egoist.CheckWin())
                                {
                                    Main.AllPlayerControls
                                        .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Archenemy) && !pc.Is(CustomRoles.MadJester))
                                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                                }
                                break;
                        }
                    }

                    //追加勝利陣営
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        //Lover追加勝利
                        if (pc.Is(CustomRoles.Lovers) && pc.IsAlive()
                            && (Lovers.LoversAddWin.GetBool() || PlatonicLover.AddWin))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Lovers);
                        }

                        if (pc.GetRoleClass() is IAdditionalWinner additionalWinner)
                        {
                            var winnerRole = pc.GetCustomRole();
                            if (additionalWinner.CheckWin(ref winnerRole))
                            {
                                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                CustomWinnerHolder.AdditionalWinnerRoles.Add(winnerRole);
                            }
                        }
                        if (Duelist.ArchenemyCheckWin(pc))
                        {
                            if (!CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Archenemy);
                        }
                    }
                    //弁護士且つ追跡者
                    Lawyer.EndGameCheck();

                    //確定敗北陣営
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.ChainShifterAddon))
                        {
                            CustomWinnerHolder.WinnerIds.Remove(pc.PlayerId);
                            CustomWinnerHolder.CantWinPlayerIds.Add(pc.PlayerId);
                        }
                    }
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        pc.GetRoleClass()?.CheckWinner(reason);
                    }
                }
                ShipStatus.Instance.enabled = false;
                
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Crewmate
                    && (reason.Equals(GameOverReason.CrewmatesByTask) || reason.Equals(GameOverReason.CrewmatesByVote)))
                    reason = GameOverReason.ImpostorsByVote;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }
        public static void StartEndGame(GameOverReason reason)
        {
            AmongUsClient.Instance.StartCoroutine(CoEndGame(AmongUsClient.Instance, reason).WrapToIl2Cpp());
        }
        private static IEnumerator CoEndGame(AmongUsClient self, GameOverReason reason)
        {
            // サーバー側のパケットサイズ制限によりCustomRpcSenderが利用できないため，遅延を挟むことで順番の整合性を保つ．

            // バニラ画面でのアウトロを正しくするためのゴーストロール化
            List<byte> ReviveRequiredPlayerIds = new();
            var winner = CustomWinnerHolder.WinnerTeam;
            foreach (var pc in Main.AllPlayerControls)
            {
                if (winner == CustomWinner.Draw)
                {
                    SetGhostRole(ToGhostImpostor: true);
                    continue;
                }
                //if (Options.IsONMode && winner == CustomWinner.Crewmate)
                //    reason = GameOverReason.CrewmatesByVote;

                bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                    CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
                canWin &= !CustomWinnerHolder.CantWinPlayerIds.Contains(pc.PlayerId);
                bool isCrewmateWin = reason.Equals(GameOverReason.CrewmatesByVote) || reason.Equals(GameOverReason.CrewmatesByTask);
                SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);

                void SetGhostRole(bool ToGhostImpostor)
                {
                    var isDead = pc.Data.IsDead;
                    if (!isDead) ReviveRequiredPlayerIds.Add(pc.PlayerId);
                    if (ToGhostImpostor)
                    {
                        Logger.Info($"{pc.GetNameWithRole()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRoleNormal(RoleTypes.ImpostorGhost);
                    }
                    else
                    {
                        Logger.Info($"{pc.GetNameWithRole()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRoleNormal(RoleTypes.CrewmateGhost);
                    }
                    // 蘇生までの遅延の間にオートミュートをかけられないように元に戻しておく
                    pc.Data.IsDead = isDead;
                }
            }

            // CustomWinnerHolderの情報の同期
            var winnerWriter = self.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, SendOption.Reliable);
            CustomWinnerHolder.WriteTo(winnerWriter);
            self.FinishRpcImmediately(winnerWriter);

            // 蘇生を確実にゴーストロール設定の後に届けるための遅延
            yield return new WaitForSeconds(EndGameDelay);

            if (ReviveRequiredPlayerIds.Count > 0)
            {
                // 蘇生 パケットが膨れ上がって死ぬのを防ぐため，1送信につき1人ずつ蘇生する
                for (int i = 0; i < ReviveRequiredPlayerIds.Count; i++)
                {
                    var playerId = ReviveRequiredPlayerIds[i];
                    var playerInfo = GameData.Instance.GetPlayerById(playerId);
                    // 蘇生
                    playerInfo.IsDead = false;
                    // 送信
                    playerInfo.SetDirtyBit(0b_1u << playerId);
                    AmongUsClient.Instance.SendAllStreamedObjects();
                }
                // ゲーム終了を確実に最後に届けるための遅延
                yield return new WaitForSeconds(EndGameDelay);
            }

            try
            {
                SetRoleSummaryText();
            }
            catch (System.Exception ex)
            {
                Logger.Exception(ex, "SetRoleSummaryText");
            }
            yield return new WaitForSeconds(EndGameDelay);

            // ゲーム終了
            GameManager.Instance.RpcEndGame(reason, false);
          
            float delay = EndGameDelay;
            for (int i = 0; i < PostEndGameResendCount; i++)
            {
                yield return new WaitForSeconds(delay);
                try
                {
                    SetRoleSummaryText();
                }
                catch (System.Exception ex)
                {
                    Logger.Exception(ex, $"SetRoleSummaryText(post RpcEndGame #{i + 1})");
                }
                delay += EndGameDelay; 
            }
        }
        private static void SetRoleSummaryText(CustomRpcSender sender = null)
        {
            var winners = new List<PlayerControl>();
            foreach (var pc in Main.AllPlayerControls)
            {
                if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) winners.Add(pc);
            }
            foreach (var team in CustomWinnerHolder.WinnerRoles)
            {
                winners.AddRange(Main.AllPlayerControls.Where(p => p.Is(team) && !winners.Contains(p)));
            }
            foreach (var id in CustomWinnerHolder.CantWinPlayerIds)
            {
                var pc = Main.AllPlayerControls.FirstOrDefault(p => p.PlayerId == id);
                if (pc == null) continue;
                winners.Remove(pc);
            }

            List<byte> winnerList = new();
            if (winners.Count != 0)
                foreach (var pc in winners)
                {
                    if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;
                    if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) && winnerList.Contains(pc.PlayerId)) continue;
                    if (CustomWinnerHolder.CantWinPlayerIds.Contains(pc.PlayerId)) continue;

                    winnerList.Add(pc.PlayerId);
                }
            string CustomWinnerColor;
            string CustomWinnerText;
            switch (CustomWinnerHolder.WinnerTeam)
            {
                case CustomWinner.Draw:
                    CustomWinnerText = Translator.GetString("ForceEndText");
                    CustomWinnerColor = "#AAAAAAFF"; // gray
                    break;
                case CustomWinner.None:
                    CustomWinnerText = Translator.GetString("EveryoneDied");
                    CustomWinnerColor = "#AAAAAAFF"; // gray
                    break;
                default:
                    var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
                    CustomWinnerText = $"{Utils.GetRoleName(winnerRole)}{Translator.GetString("Win")}";
                    CustomWinnerColor = (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate
                        ? Utils.GetRoleColorCode(CustomRoles.Engineer)
                        : Utils.GetRoleColorCode(winnerRole)) ?? "#FFFFFFFF";
                    break;
            }
            var winnerSize = GetScale(CustomWinnerText.RemoveHtmlTags().Length, 1.6, 2.5);
            CustomWinnerText = $"<size={winnerSize}><color={CustomWinnerColor}>{CustomWinnerText}</color></size>";
            static double GetScale(int input, double min, double max)
                => min + (max - min) * (1 - (double)(input - 1) / 13);

            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (pc == null) continue;
                try
                {
                    var target = (winnerList.Contains(pc.PlayerId) ? pc : (winnerList.Count == 0 ? pc : Main.AllPlayerControls.FirstOrDefault(p => p.PlayerId == winnerList.OrderBy(x => x).FirstOrDefault()) ?? pc)) ?? pc;
                    var targetname = Main.AllPlayerNames[target.PlayerId];

                    var text = $"<voffset=50>{CustomWinnerText}\n<voffset=0>{targetname}\n\n<voffset=24><size=40%></size>";
                    if (text.Length > 320)
                    {
                        text = text.Replace("</color>", "");
                        if (text.Length > 320)
                        {
                            text = text.RemoveColorTags();
                        }
                    }

                    if (sender == null)
                    {
                        target.RpcSetNamePrivate(text, true, pc, true);
                    }
                    else
                    {
                        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetName, pc.GetClientId())
                            .Write(pc.Data.NetId)
                            .Write(text)
                            .Write(true)
                            .EndRpc();
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.Exception(ex, "SetRoleSummaryText(per player)");
                }
            }
        }
        private const float EndGameDelay = 0.2f;
        private const int PostEndGameResendCount = 8;

        public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
        public static void SetPredicateToHideAndSeek() => predicate = new HideAndSeekGameEndPredicate();
        public static void SetPredicateToCatchCat() => predicate = new CatchCat.GameEndPredicate();

        // ===== ゲーム終了条件 =====
        // 通常ゲーム用
        class NormalGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorsByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;
                if (CheckGameEndBySabotage(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorsByKill;

                int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
                int Jackal = Utils.AlivePlayersCount(CountTypes.Jackal);
                int Pirate = Utils.AlivePlayersCount(CountTypes.Pirate);
                int Crew = Utils.AlivePlayersCount(CountTypes.Crew);

                if (Imp == 0 && Crew == 0 && Jackal == 0 && Pirate == 0) //全滅
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers))) //ラバーズ勝利
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                }
                else if (Jackal == 0 && Pirate == 0 && Crew <= Imp) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0 && Pirate == 0 && Crew <= Jackal) //ジャッカル勝利
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JClient);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JSidekick);
                }
                else if (Imp == 0 && Jackal == 0 && Crew <= Pirate) //海賊勝利
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Pirate);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Pirate);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Gang);
                    CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Gang);
                }
                else if (Jackal == 0 && Pirate == 0 && Imp == 0) //クルー勝利
                {
                    reason = GameOverReason.CrewmatesByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }

        // HideAndSeek用
        class HideAndSeekGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorsByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorsByKill;

                int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
                int Crew = Utils.AlivePlayersCount(CountTypes.Crew);

                if (Imp == 0 && Crew == 0) //全滅
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Crew <= 0) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0) //クルー勝利(インポスター切断など)
                {
                    reason = GameOverReason.CrewmatesByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }

        //// OneNight
        //class OneNightGameEndPredicate : GameEndPredicate
        //{
        //    public override bool CheckForEndGame(out GameOverReason reason)
        //    {
        //        reason = GameOverReason.ImpostorsByKill;
        //        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
        //        if (CheckGameEndByLivingPlayers(out reason)) return true;
        //        if (CheckGameEndBySabotage(out reason)) return true;

        //        return false;
        //    }

        //    public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        //    {
        //        reason = GameOverReason.ImpostorsByKill;

        //        return false; //勝利条件未達成
        //    }
        //}
    }

    public abstract class GameEndPredicate
    {
        /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
        /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
        /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
        public abstract bool CheckForEndGame(out GameOverReason reason);

        /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndByTask(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
            if (Options.DisableTaskWin.GetBool() || TaskState.InitialTotalTasks == 0) return false;

            (int vtComp, int vtTotal) = VentEnterTask.TaskWinCountData();
            if (GameData.Instance.TotalTasks + vtTotal <= GameData.Instance.CompletedTasks + vtComp)
            {
                reason = GameOverReason.CrewmatesByTask;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                Logger.Info($"GemeEndByTask task: {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "CheckGameEndByTask");
                return true;
            }
            return false;
        }
        /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
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
                reason = GameOverReason.ImpostorsBySabotage;
                LifeSupp.Countdown = 10000f;
                return true;
            }

            ISystemType sys = null;
            if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
            else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];
            else if (systems.ContainsKey(SystemTypes.HeliSabotage)) sys = systems[SystemTypes.HeliSabotage];

            ICriticalSabotage critical;
            if (sys != null && // サボタージュ存在確認
                (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
                critical.Countdown < 0f) // タイムアップ確認
            {
                // リアクターサボタージュ
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                reason = GameOverReason.ImpostorsBySabotage;
                critical.ClearSabotage();
                return true;
            }

            return false;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHost.Roles.Neutral;

namespace TownOfHost
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
            var reason = GameOverReason.ImpostorByKill;

            //ゲーム終了判定
            predicate.CheckForEndGame(out reason);

            //ゲーム終了時
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                //カモフラージュ強制解除
                Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true));
                Main.AllPlayerControls.Do(pc => SkinChangeMode.RpcSetSkin(pc, pc));

                switch (CustomWinnerHolder.WinnerTeam)
                {
                    case CustomWinner.Crewmate:
                        Main.AllPlayerControls
                            .Where(pc => pc.Is(CustomRoleTypes.Crewmate) && !pc.Is(CustomRoles.Lovers))
                            .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                    case CustomWinner.Impostor:
                        if (Main.AllAlivePlayerControls.Count(p => p.Is(CustomRoleTypes.Impostor)) == 0 && Main.AllAlivePlayerControls.Count(p => p.Is(CustomRoles.Egoist)) > 0) //インポスター全滅でエゴイストが生存
                            goto case CustomWinner.Egoist;
                        Main.AllPlayerControls
                            .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)) && !pc.Is(CustomRoles.Lovers))
                            .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                    case CustomWinner.Egoist:
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.EgoSchrodingerCat);
                        break;
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
                {
                    if (Main.LoversPlayers.Count > 0 && Main.LoversPlayers.ToArray().All(p => p.IsAlive()) && !reason.Equals(GameOverReason.HumansByTask) && !(Options.LoversAddWin.GetBool() || PlatonicLover.PLoverAddWin.GetBool()))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.Lovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }

                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.DarkHide) && !pc.Data.IsDead
                            && ((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorBySabotage)) || CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide
                            || (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.HumansByTask) && DarkHide.IsWinKill[pc.PlayerId] == true)))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DarkHide);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        else if (pc.Is(CustomRoles.NBakery) && !pc.Data.IsDead
                            && ((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorBySabotage)) || CustomWinnerHolder.WinnerTeam == CustomWinner.NBakery
                            || (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.HumansByTask))))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NBakery);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                    }

                    //追加勝利陣営
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        //Opportunist
                        if (pc.Is(CustomRoles.Opportunist) && pc.IsAlive())
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Opportunist);
                        }
                        //Oppo猫
                        if (pc.Is(CustomRoles.OSchrodingerCat) && pc.IsAlive())
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        //SchrodingerCat
                        SchrodingerCat.CheckAdditionalWin(pc);
                        //Lover追加勝利
                        if (pc.Is(CustomRoles.Lovers) && pc.IsAlive()
                            && (Options.LoversAddWin.GetBool() || PlatonicLover.PLoverAddWin.GetBool()))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lovers);
                        }
                        //Dark猫
                        if (pc.Is(CustomRoles.DSchrodingerCat) && (CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                    }
                    //弁護士且つ追跡者
                    Lawyer.EndGameCheck();
                    Totocalcio.EndGameCheck();
                }
                ShipStatus.Instance.enabled = false;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }
        public static void StartEndGame(GameOverReason reason)
        {
            var sender = new CustomRpcSender("EndGameSender", SendOption.Reliable, true);
            sender.StartMessage(-1); // 5: GameData
            MessageWriter writer = sender.stream;

            //ゴーストロール化
            List<byte> ReviveRequiredPlayerIds = new();
            var winner = CustomWinnerHolder.WinnerTeam;
            foreach (var pc in Main.AllPlayerControls)
            {
                if (winner == CustomWinner.Draw)
                {
                    SetGhostRole(ToGhostImpostor: true);
                    continue;
                }
                if (Options.CurrentGameMode.IsCatMode())
                {
                    if (winner == CustomWinner.RedL)
                    {
                        if (pc.Is(CustomRoles.CatRedLeader) || pc.Is(CustomRoles.CatRedCat))
                        {
                            SetGhostRole(ToGhostImpostor: true);
                        }
                        else
                        {
                            SetGhostRole(ToGhostImpostor: false);
                        }
                    }
                    else if (winner == CustomWinner.BlueL)
                    {
                        if (pc.Is(CustomRoles.CatBlueLeader) || pc.Is(CustomRoles.CatBlueCat))
                        {
                            SetGhostRole(ToGhostImpostor: true);
                        }
                        else
                        {
                            SetGhostRole(ToGhostImpostor: false);
                        }
                    }
                    else if (winner == CustomWinner.YellowL)
                    {
                        if (pc.Is(CustomRoles.CatYellowLeader) || pc.Is(CustomRoles.CatYellowCat))
                        {
                            SetGhostRole(ToGhostImpostor: true);
                        }
                        else
                        {
                            SetGhostRole(ToGhostImpostor: false);
                        }
                    }
                }
                else
                {
                    if (Options.CurrentGameMode.IsOneNightMode())
                    {
                        if (winner == CustomWinner.Crewmate) reason = GameOverReason.HumansByVote;
                    }

                    bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                        CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
                    bool isCrewmateWin = reason.Equals(GameOverReason.HumansByVote) || reason.Equals(GameOverReason.HumansByTask);
                    SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);
                }
                void SetGhostRole(bool ToGhostImpostor)
                {
                    if (!pc.Data.IsDead) ReviveRequiredPlayerIds.Add(pc.PlayerId);
                    if (ToGhostImpostor)
                    {
                        //Logger.Info($"{pc.GetNameWithRole()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                        sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                            .Write((ushort)RoleTypes.ImpostorGhost)
                            .EndRpc();
                        pc.SetRole(RoleTypes.ImpostorGhost);
                    }
                    else
                    {
                        //Logger.Info($"{pc.GetNameWithRole()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                        sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                            .Write((ushort)RoleTypes.CrewmateGhost)
                            .EndRpc();
                        pc.SetRole(RoleTypes.Crewmate);
                    }
                }
            }

            // CustomWinnerHolderの情報の同期
            sender.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame);
            CustomWinnerHolder.WriteTo(sender.stream);
            sender.EndRpc();

            // GameDataによる蘇生処理
            writer.StartMessage(1); // Data
            {
                writer.WritePacked(GameData.Instance.NetId); // NetId
                foreach (var info in GameData.Instance.AllPlayers)
                {
                    if (ReviveRequiredPlayerIds.Contains(info.PlayerId))
                    {
                        // 蘇生&メッセージ書き込み
                        info.IsDead = false;
                        writer.StartMessage(info.PlayerId);
                        info.Serialize(writer);
                        writer.EndMessage();
                    }
                }
                writer.EndMessage();
            }

            sender.EndMessage();

            // バニラ側のゲーム終了RPC
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
        public static void SetPredicateToOneNight() => predicate = new OneNightGameEndPredicate();

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

                int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
                int Jackal = Utils.AlivePlayersCount(CountTypes.Jackal);
                int Crew = Utils.AlivePlayersCount(CountTypes.Crew);

                if (Imp == 0 && Crew == 0 && Jackal == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers))) //ラバーズ勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
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
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JClient);
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
        }

        // HideAndSeek改め猫取用
        class HideAndSeekGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

                if (CheckGameEndByLivingPlayers(out reason)) return true;
                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int[] counts = CountLivingPlayersByPredicates(
                    pc => pc.Is(CustomRoles.CatRedLeader),//0
                    pc => pc.Is(CustomRoles.CatBlueLeader),//1
                    pc => pc.Is(CustomRoles.CatYellowLeader),//2
                    pc => pc.Is(CustomRoles.Crewmate),//3
                    pc => pc.Is(CustomRoles.CatRedCat),//4
                    pc => pc.Is(CustomRoles.CatBlueCat),//5
                    pc => pc.Is(CustomRoles.CatYellowCat)//6
                );
                int Leader = counts[0] + counts[1] + counts[2];
                int NoCat = counts[3];
                int RedTeam = counts[0] + counts[4];
                int BlueTeam = counts[1] + counts[5];
                int YellowTeam = counts[2] + counts[6];

                if (Leader == 0 && NoCat == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Leader == 1) //リーダーが残り1名になった
                {
                    reason = GameOverReason.ImpostorByKill;
                    if (counts[0] == 1)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.RedL);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.RedC);
                    }
                    else if (counts[1] == 1)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BlueL);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.BlueC);
                    }
                    else if (counts[2] == 1)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.YellowL);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.YellowC);
                    }
                }
                else if (NoCat <= 0) //無陣営の猫がいなくなった
                {
                    reason = GameOverReason.ImpostorByKill;

                    if (RedTeam > BlueTeam && RedTeam > YellowTeam)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.RedL);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.RedC);
                    }
                    else if (RedTeam < BlueTeam && BlueTeam > YellowTeam)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BlueL);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.BlueC);
                    }
                    else if (RedTeam < YellowTeam && BlueTeam < YellowTeam)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.YellowL);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.YellowC);
                    }
                }
                else if (Leader == 0) //クルー勝利(インポスター切断など)
                {
                    reason = GameOverReason.ImpostorDisconnect;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }

        // OneNight
        class OneNightGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndBySabotage(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                return false; //勝利条件未達成
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
        public static int[] CountLivingPlayersByPredicates(params Predicate<PlayerControl>[] predicates)
        {
            int[] counts = new int[predicates.Length];
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                for (int i = 0; i < predicates.Length; i++)
                {
                    if (predicates[i](pc)) counts[i]++;
                }
            }
            return counts;
        }
        /// <summary>各条件に合ったプレイヤーの人数を取得し、配列に同順で格納します。</summary>
        public static int[] CountPlayersByPredicates(params Predicate<PlayerControl>[] predicates)
        {
            int[] counts = new int[predicates.Length];
            foreach (var pc in Main.AllPlayerControls)
            {
                for (int i = 0; i < predicates.Length; i++)
                {
                    if (predicates[i](pc)) counts[i]++;
                }
            }
            return counts;
        }


        /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndByTask(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (Options.DisableTaskWin.GetBool() || (TaskState.InitialTotalTasks == 0)) return false;

            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                reason = GameOverReason.HumansByTask;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                return true;
            }
            return false;
        }
        /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
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
}
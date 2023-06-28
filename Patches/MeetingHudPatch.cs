using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            var voteLog = Logger.Handler("Vote");
            //try
            {
                List<MeetingHud.VoterState> statesList = new();
                MeetingHud.VoterState[] states;
                foreach (var pva in __instance.playerStates)
                {
                    if (pva == null) continue;
                    PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
                    if (pc == null) continue;
                    //死んでいないディクテーターが投票済み
                    if ((pc.Is(CustomRoles.Dictator) || pc.Is(CustomRoles.MadDictator)) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                    {
                        var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);
                        statesList.Add(new()
                        {
                            VoterId = pva.TargetPlayerId,
                            VotedForId = pva.VotedFor
                        });
                        states = statesList.ToArray();
                        if (AntiBlackout.OverrideExiledPlayer)
                        {
                            __instance.RpcVotingComplete(states, null, true);
                            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = voteTarget.Data;
                        }
                        else __instance.RpcVotingComplete(states, voteTarget.Data, false); //通常処理

                        //if (Bakery.IsNEnable())
                        //{
                        //    Bakery.OnCheckForEndVoting(pva.VotedFor);
                        //}
                        Logger.Info($"{voteTarget.GetNameWithRole()}を追放", "Dictator");
                        CheckForDeathOnExile(PlayerState.DeathReason.Vote, pva.VotedFor);
                        Logger.Info("ディクテーターによる強制会議終了", "Special Phase");
                        voteTarget.SetRealKiller(pc);
                        return true;
                    }
                    //死んでいないチェアマンが投票済み
                    if (pc.Is(CustomRoles.Chairman) && !Options.ChairmanIgnoreSkip.GetBool() && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                    {
                        __instance.RpcVotingComplete(new MeetingHud.VoterState[]{ new ()
                        {
                            VoterId = pva.TargetPlayerId,
                            VotedForId = 253
                        }}, null, false); //RPC
                        Logger.Info("チェアマンによる強制会議終了", "Special Phase");
                        return true;
                    }
                    //アンチコンプリート
                    if (pc.Is(CustomRoles.AntiComplete) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                    {
                        var taskState = Main.PlayerStates[pva.VotedFor].GetTaskState();
                        __instance.RpcVotingComplete(new MeetingHud.VoterState[]{ new ()
                        {
                            VoterId = pva.TargetPlayerId,
                            VotedForId = pva.VotedFor,
                        }}, pc.Data, false); //RPC
                        Logger.Info("アンチコンプによる強制会議終了", "Special Phase");

                        if ((0 < taskState.CompletedTasksCount) && (taskState.AllTasksCount <= taskState.CompletedTasksCount))
                        {
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.win;
                        }
                        else
                        {
                            TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);
                        }
                        return true;
                    }
                    //ONHunter道連れ処刑
                    if (!MeetingStates.FirstMeeting && pc.Is(CustomRoles.ONHunter) && pva.DidVote && pva.VotedFor < 253 && !pc.Data.IsDead)
                    {
                        var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                        __instance.RpcVotingComplete(new MeetingHud.VoterState[]{ new ()
                        {
                            VoterId = pva.TargetPlayerId,
                            VotedForId = pva.VotedFor,
                        }}, Utils.GetPlayerInfoById(pva.VotedFor), false); //RPC
                        Logger.Info("ON狩人による道連れ処刑", "Special Phase");
                        Main.PlayerStates[pva.VotedFor].deathReason = PlayerState.DeathReason.Execution;
                        Main.PlayerStates[pva.VotedFor].SetDead();
                        voteTarget.SetRealKiller(pc);
                        return true;
                    }
                }
                foreach (var ps in __instance.playerStates)
                {
                    //死んでいないプレイヤーが投票していない
                    if (!(Main.PlayerStates[ps.TargetPlayerId].IsDead || ps.DidVote)) return false;
                }

                GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
                bool tie = false;

                Dictionary<byte, byte> TieBreakerVote = new();

                for (var i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea ps = __instance.playerStates[i];
                    if (ps == null) continue;
                    voteLog.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"));
                    var voter = Utils.GetPlayerById(ps.TargetPlayerId);
                    if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                    if (Options.VoteMode.GetBool())
                    {
                        if (ps.VotedFor == 253 && !voter.Data.IsDead && //スキップ
                            !(Options.WhenSkipVoteIgnoreFirstMeeting.GetBool() && MeetingStates.FirstMeeting) && //初手会議を除く
                            !(Options.WhenSkipVoteIgnoreNoDeadBody.GetBool() && !MeetingStates.IsExistDeadBody) && //死体がない時を除く
                            !(Options.WhenSkipVoteIgnoreEmergency.GetBool() && MeetingStates.IsEmergencyMeeting) //緊急ボタンを除く
                            )
                        {
                            switch (Options.GetWhenSkipVote())
                            {
                                case VoteMode.Suicide:
                                    TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                    voteLog.Info($"スキップしたため{voter.GetNameWithRole()}を自殺させました");
                                    break;
                                case VoteMode.SelfVote:
                                    ps.VotedFor = ps.TargetPlayerId;
                                    voteLog.Info($"スキップしたため{voter.GetNameWithRole()}に自投票させました");
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (ps.VotedFor == 254 && !voter.Data.IsDead)//無投票
                        {
                            switch (Options.GetWhenNonVote())
                            {
                                case VoteMode.Suicide:
                                    TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                    voteLog.Info($"無投票のため{voter.GetNameWithRole()}を自殺させました");
                                    break;
                                case VoteMode.SelfVote:
                                    ps.VotedFor = ps.TargetPlayerId;
                                    voteLog.Info($"無投票のため{voter.GetNameWithRole()}に自投票させました");
                                    break;
                                case VoteMode.Skip:
                                    ps.VotedFor = 253;
                                    voteLog.Info($"無投票のため{voter.GetNameWithRole()}にスキップさせました");
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    if(Options.CurrentGameMode.IsCatMode())
                    {
                        if (ps.VotedFor != 253 && !voter.Data.IsDead)
                        {
                            ps.VotedFor = 253;
                            Logger.Info($"スキップ以外のため{voter.GetNameWithRole()}にスキップさせました", "Vote");
                        }
                    }
                    else if(!MeetingStates.FirstMeeting && Options.CurrentGameMode.IsOneNightMode())
                    {
                        if (ps.VotedFor != 253 && !voter.Data.IsDead)
                        {
                            ps.VotedFor = 253;
                            Logger.Info($"スキップ以外のため{voter.GetNameWithRole()}にスキップさせました", "Vote");
                        }
                    }
                    statesList.Add(new MeetingHud.VoterState()
                    {
                        VoterId = ps.TargetPlayerId,
                        VotedForId = ps.VotedFor
                    });
                    if (IsMayor(ps.TargetPlayerId))//Mayorの投票数
                    {
                        for (var i2 = 0; i2 < Options.MayorAdditionalVote.GetFloat(); i2++)
                        {
                            statesList.Add(new MeetingHud.VoterState()
                            {
                                VoterId = ps.TargetPlayerId,
                                VotedForId = ps.VotedFor
                            });
                        }
                    }
                    if (IsONMayor(ps.TargetPlayerId))//Mayorの投票数
                    {
                        statesList.Add(new MeetingHud.VoterState()
                        {
                            VoterId = ps.TargetPlayerId,
                            VotedForId = ps.VotedFor
                        });
                    }
                    if (IsPlusVote(ps.TargetPlayerId))//Mayorの投票数
                    {
                        statesList.Add(new MeetingHud.VoterState()
                        {
                            VoterId = ps.TargetPlayerId,
                            VotedForId = ps.VotedFor
                        });
                    }
                    if (IsTieBreaker(ps.TargetPlayerId))
                    {
                        voteLog.Info(ps.TargetPlayerId + "がタイブレーカー投票(" + ps.VotedFor + ")");
                        TieBreakerVote.Add(ps.TargetPlayerId, ps.VotedFor);
                    }
                    if (voter.Is(CustomRoles.FortuneTeller)) //占い師の占い投票
                    {
                        Logger.Info($"MeetingPrefix voter: {voter.name}, vote: {ps.DidVote} target: {ps.name}, notSelf: {voter.PlayerId != ps.VotedFor}, pcIsDead: {voter.Data.IsDead}, voteFor: {ps.VotedFor}", "test★");
                        if (ps.DidVote && ps.VotedFor != voter.PlayerId && ps.VotedFor < 253 && !voter.Data.IsDead) //自分以外に投票
                            voter.VoteForecastTarget(ps.VotedFor);

                    }
                }
                states = statesList.ToArray();

                var VotingData = __instance.CustomCalculateVotes();
                byte exileId = byte.MaxValue;
                int max = 0;
                voteLog.Info("===追放者確認処理開始===");
                foreach (var data in VotingData)
                {
                    voteLog.Info($"{data.Key}({Utils.GetVoteName(data.Key)}):{data.Value}票");

                    if (data.Value > max)
                    {
                        voteLog.Info(data.Key + "番が最高値を更新(" + data.Value + ")");
                        if (Options.CurrentGameMode.IsOneNightMode() && data.Key == 253)
                        {
                            voteLog.Info("スキップは処理をスキップします。");
                        }
                        else
                        {
                            exileId = data.Key;
                            max = data.Value;
                            tie = false;
                        }
                    }
                    else if (data.Value == max)
                    {
                        voteLog.Info(data.Key + "番が" + exileId + "番と同数(" + data.Value + ")");
                        exileId = byte.MaxValue;
                        tie = true;
                    }
                    voteLog.Info($"exileId: {exileId}, max: {max}票");
                }
                if (tie)
                {
                    var tiebreakerUse = false;
                    var tiebreakerCollision = false;
                    foreach (var data in VotingData.Where(x => x.Value == max))
                    {
                        if (TieBreakerVote.ContainsValue(data.Key))
                        {
                            if (tiebreakerUse) tiebreakerCollision = true;
                            exileId = data.Key;
                            tiebreakerUse = true;
                            voteLog.Info(exileId + "番がTieBreakerで優先");
                        }
                    }
                    if (tiebreakerCollision)
                    {
                        voteLog.Info("TieBreakerの衝突");
                        exileId = byte.MaxValue;
                    }
                    else
                        tie = false;
                }

                voteLog.Info($"追放者決定: {exileId}({Utils.GetVoteName(exileId)})");

                if (IsRefusing(exileId) && !Main.IsAdd1NextExiled[exileId])
                {
                    __instance.RpcVotingComplete(states, null, false);
                    Main.IsAdd1NextExiled[exileId] = true;
                    exiledPlayer = null;
                }
                if (ONHunterExiled(exileId))
                {
                    __instance.RpcVotingComplete(states, null, true);
                    Main.PlayerStates[exileId].deathReason = PlayerState.DeathReason.Vote;
                    Main.PlayerStates[exileId].SetDead();
                    exiledPlayer = null;
                }

                if (Options.CurrentGameMode.IsOneNightMode() && tie)
                {
                    var exileIds = VotingData.Where(x => x.Key < 15 && x.Value == max).Select(kvp => kvp.Key).ToArray();
                    foreach (var playerId in exileIds)
                    {
                        Main.PlayerStates[playerId].deathReason = PlayerState.DeathReason.Vote;
                        Main.PlayerStates[playerId].SetDead();
                        if (ONHunterExiled(playerId))
                        {

                        }
                        else
                        {
                            Main.ONMeetingExiledPlayers.Add(playerId);
                            Utils.GetPlayerById(playerId).SetRealKiller(null);
                            TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Vote, playerId);
                        }
                    }
                    exiledPlayer = null;
                }
                else if (Options.VoteMode.GetBool() && Options.WhenTie.GetBool() && tie)
                {
                    switch ((TieMode)Options.WhenTie.GetValue())
                    {
                        case TieMode.Default:
                            exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == exileId);
                            break;
                        case TieMode.All:
                            var exileIds = VotingData.Where(x => x.Key < 15 && x.Value == max).Select(kvp => kvp.Key).ToArray();
                            foreach (var playerId in exileIds)
                                Utils.GetPlayerById(playerId).SetRealKiller(null);
                            TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Vote, exileIds);
                            exiledPlayer = null;
                            break;
                        case TieMode.Random:
                            exiledPlayer = GameData.Instance.AllPlayers.ToArray().OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => VotingData.TryGetValue(x.PlayerId, out int vote) && vote == max);
                            tie = false;
                            break;
                    }
                }
                else
                    exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);

                if (exiledPlayer != null)
                    exiledPlayer.Object.SetRealKiller(null);

                //RPC
                if (AntiBlackout.OverrideExiledPlayer)
                {
                    __instance.RpcVotingComplete(states, null, true);
                    ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiledPlayer;
                }
                else __instance.RpcVotingComplete(states, exiledPlayer, tie); //通常処理

                if (exiledPlayer != null)
                    CheckForDeathOnExile(PlayerState.DeathReason.Vote, exiledPlayer.PlayerId);
                return false;
            }
            //catch (Exception ex)
            //{
            //    //Logger.SendInGame(string.Format(GetString("Error.MeetingException"), ex.Message), true);
            //    throw;
            //}
        }
        private static bool ONHunterExiled(byte exileId)
        {
            if (exileId == byte.MaxValue) return false;

            if (Utils.GetPlayerById(exileId).Is(CustomRoles.ONHunter))
            {
                Main.ONMeetingExiledPlayers.Add(exileId);
                Utils.GetPlayerById(exileId).SetRealKiller(null);
                new LateTask(() =>
                {
                    Utils.GetPlayerById(exileId).NoCheckStartMeeting(Utils.GetPlayerInfoById(exileId));
                }, 15f, "ONHunterExiled");
                return true;
            }
            return false;
        }

        public static bool IsMayor(byte id)
        {
            var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == id).FirstOrDefault();
            return player != null && player.Is(CustomRoles.Mayor);
        }
        public static bool IsONMayor(byte id)
        {
            var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == id).FirstOrDefault();
            return player != null && player.Is(CustomRoles.ONMayor);
        }
        public static bool IsTieBreaker(byte id)
        {
            var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == id).FirstOrDefault();
            return player != null && player.Is(CustomRoles.TieBreaker);
        }
        public static bool IsRefusing(byte id)
        {
            var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == id).FirstOrDefault();
            return player != null && player.Is(CustomRoles.Refusing);
        }
        public static bool IsPlusVote(byte id)
        {
            var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == id).FirstOrDefault();
            return player != null && player.Is(CustomRoles.PlusVote);
        }
        public static void TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason deathReason, params byte[] playerIds)
        {
            var AddedIdList = new List<byte>();
            foreach (var playerId in playerIds)
                if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
                    AddedIdList.Add(playerId);
            CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
        }
        public static void CheckForDeathOnExile(PlayerState.DeathReason deathReason, params byte[] playerIds)
        {
            Witch.OnCheckForEndVoting(deathReason, playerIds);

            foreach (var playerId in playerIds)
            {
                if (Bakery.IsNEnable())
                {
                    Bakery.OnCheckForEndVoting(playerId);
                }

                //ジャッカル死亡時のJクライアント後追い
                if (CustomRoles.JClient.IsEnable() && Options.JClientAfterJackalDead.GetString() == GetString(Options.AfterJackalDeadMode.Following.ToString()))
                {
                    var jackal = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.Is(CustomRoles.Jackal)).FirstOrDefault();
                    if (jackal == null || jackal.Data.IsDead || jackal.PlayerId == playerId ||
                        Main.AfterMeetingDeathPlayers.ContainsKey(jackal.PlayerId))
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc != null && pc.Is(CustomRoles.JClient) && !pc.Data.IsDead && pc.GetPlayerTaskState().IsTaskFinished)
                                Main.AfterMeetingDeathPlayers.TryAdd(pc.PlayerId, PlayerState.DeathReason.FollowingSuicide);
                        }
                    }
                }

                //Loversの後追い
                if (CustomRoles.Lovers.IsEnable() && !Main.isLoversDead && Main.LoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                    FixedUpdatePatch.LoversSuicide(playerId, true);
                //道連れチェック
                RevengeOnExile(playerId, deathReason);
            }
        }
        //道連れ
        private static void RevengeOnExile(byte playerId, PlayerState.DeathReason deathReason)
        {
            var player = Utils.GetPlayerById(playerId);
            if (player == null) return;

            if ((player.Is(CustomRoles.SKMadmate) && Options.MadmateRevengeCrewmate.GetBool()) || player.Is(CustomRoles.Evilneko) || player.Is(CustomRoles.Nekomata)|| player.Is(CustomRoles.Revenger))
            {
                var target = PickRevengeTarget(player, deathReason);
                if (target == null) return;
                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Revenge, target.PlayerId);
                target.SetRealKiller(player);
                Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
            }
        }
        private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, PlayerState.DeathReason deathReason)//道連れ先選定
        {
            List<PlayerControl> TargetList = new();
            foreach (var candidate in Main.AllAlivePlayerControls)
            {
                if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;

                //対象とならない人を判定
                if (exiledplayer.Is(CustomRoleTypes.Madmate) || exiledplayer.Is(CustomRoleTypes.Impostor)) //インポスター陣営の場合
                {
                    if (candidate.Is(CustomRoleTypes.Impostor)) continue; //インポスター
                    if (candidate.Is(CustomRoleTypes.Madmate) && !Options.TakeCompanionMad.GetBool()) continue; //マッドメイト（設定）
                }
                if (candidate.Is(CustomRoleTypes.Neutral) && !Options.TakeCompanionNeutral.GetBool()) continue; //第三陣営（設定）

                TargetList.Add(candidate);

                //switch (exiledplayer.GetCustomRole())
                //{
                //    //ここに道連れ役職を追加
                //    case CustomRoles.Evilneko:
                //        if (Options.TakeCompanionMad.GetBool()&&!candidate.Is(RoleType.Madmate))
                //            TargetList.Add(candidate);
                //            break;
                //    default:
                //        if (exiledplayer.Is(RoleType.Madmate) && deathReason == PlayerState.DeathReason.Vote && Options.MadmateRevengeCrewmate.GetBool() //黒猫オプション
                //        && !candidate.Is(RoleType.Impostor))
                //            TargetList.Add(candidate);
                //        break;
                //}
            }
            if (TargetList == null || TargetList.Count == 0) return null;
            var rand = IRandom.Instance;
            var target = TargetList[rand.Next(TargetList.Count)];
            Main.RevengeTargetPlayer.Add((exiledplayer,target));
            return target;
        }
    }

    static class ExtendedMeetingHud
    {
        public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance)
        {
            Logger.Info("CustomCalculateVotes開始", "Vote");
            Dictionary<byte, int> dic = new();
            //| 投票された人 | 投票された回数 |
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea ps = __instance.playerStates[i];
                if (ps == null) continue;
                if (ps.VotedFor is not ((byte)252) and not byte.MaxValue and not ((byte)254))
                {
                    int VoteNum = 1;
                    if (CheckForEndVotingPatch.IsMayor(ps.TargetPlayerId)) VoteNum += Options.MayorAdditionalVote.GetInt();
                    if (CheckForEndVotingPatch.IsPlusVote(ps.TargetPlayerId)) VoteNum = 2;
                    if (CheckForEndVotingPatch.IsONMayor(ps.TargetPlayerId)) VoteNum = 2;
                    //投票を1追加 キーが定義されていない場合は1で上書きして定義
                    dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out int num) ? VoteNum : num + VoteNum;
                }
            }
            return dic;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class MeetingHudStartPatch
    {
        public static void Prefix(MeetingHud __instance)
        {
            Logger.Info("------------会議開始------------", "Phase");
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            Main.ExiledPlayer = 253;
            Hunter.ResetIsImp();
            Psychic.ClearSelect();
            ONDiviner.SetNotkillTarget();
            ONBigWerewolf.SetNotkillTarget();
            ONPhantomThief.SetChangeRoles();
            //Utils.NotifyRoles(isMeeting: true, NoCache: true);

            MeetingStates.MeetingCalled = true;
        }
        public static void Postfix(MeetingHud __instance)
        {
            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;

            if (AntiBlackout.OverrideExiledPlayer && !Options.CurrentGameMode.IsCatMode())
            {
                Utils.SendMessage(Translator.GetString("Warning.OverrideExiledPlayer"));
            }
            if (Options.IsReportShow.GetBool() && ReportDeadBodyPatch.Target == null)
            {
                    Utils.SendMessage(Translator.GetString("isButton"));
            }
            else if (Options.IsReportShow.GetBool() /*&& ReportDeadBodyPatch.Target?.Object != null*/)
            {
                if (!(!MeetingStates.FirstMeeting && Options.CurrentGameMode.IsOneNightMode()))
                    Utils.SendMessage(string.Format(Translator.GetString("isReport"), ReportDeadBodyPatch.Target.PlayerName));
            }
            if (Options.SyncButtonMode.GetBool())
            {
                Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }
            if (Options.ShowRevengeTarget.GetBool())
            {
                foreach (var Exiled_Target in Main.RevengeTargetPlayer)
                {
                    Utils.SendMessage(string.Format(GetString("RevengeText"), Exiled_Target.Item1.name, Exiled_Target.Item2.name));
                }
                Main.RevengeTargetPlayer.Clear();
            }
            if (!MeetingStates.FirstMeeting && Options.CurrentGameMode.IsOneNightMode())
            {
                Utils.SendMessage(GetString("IsHunterMeetingText"), title: $"<color={Utils.GetRoleColorCode(CustomRoles.ONHunter)}>{GetString("IsHunterMeeting")}</color>");
            }

            if (Options.CurrentGameMode.IsCatMode())
            {
                int[] counts = GameEndPredicate.CountLivingPlayersByPredicates(
                    pc => pc.Is(CustomRoles.CatRedLeader),
                    pc => pc.Is(CustomRoles.CatBlueLeader),
                    pc => pc.Is(CustomRoles.CatYellowLeader),
                    pc => pc.Is(CustomRoles.Crewmate),
                    pc => pc.Is(CustomRoles.CatRedCat),
                    pc => pc.Is(CustomRoles.CatBlueCat),
                    pc => pc.Is(CustomRoles.CatYellowCat)
                );
                int Leader = counts[0] + counts[1] + counts[2];
                int NoCat = counts[3];
                int RedTeam = counts[0] + counts[4];
                int BlueTeam = counts[1] + counts[5];
                int YellowTeam = counts[2] + counts[6];

                String TITLE = $"<color=#f8cd46>{GetString("CatMidwayResultsTitle")}</color>";

                if (CustomRoles.CatYellowLeader.IsEnable() && NoCat == 0)
                    Utils.SendMessage(string.Format(GetString("Message.CatMidwayResultsSudden3"), RedTeam, BlueTeam, YellowTeam), title: TITLE);
                else if (!CustomRoles.CatYellowLeader.IsEnable() && NoCat == 0)
                    Utils.SendMessage(string.Format(GetString("Message.CatMidwayResultsSudden"), RedTeam, BlueTeam), title: TITLE);
                else
                    Utils.SendMessage(string.Format(GetString("Message.CatMidwayResults"), Leader,NoCat), title: TITLE);

                Logger.Info("リーダー"+ Leader + "人生存中。無陣営猫残り" + NoCat + "人", "MidwayResults");
            }
            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                var RoleTextData = Utils.GetRoleText(pc.PlayerId);
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                roleTextMeeting.text = RoleTextData.Item1;
                roleTextMeeting.color = RoleTextData.Item2;

                var SubRoleText = pc.GetDisplaySubRoleName();
                var beforeRoleText = pc.GetDisplayBeforeRoleName();

                if (Options.CurrentGameMode.IsOneNightMode() && PlayerControl.LocalPlayer.IsAlive())
                {
                    if (pc != PlayerControl.LocalPlayer || Main.DefaultRole[pva.TargetPlayerId] != CustomRoles.ONPhantomThief)
                    {
                        roleTextMeeting.text = Utils.GetRoleName(Main.DefaultRole[pva.TargetPlayerId]);
                        roleTextMeeting.color = Utils.GetRoleColor(Main.DefaultRole[pva.TargetPlayerId]);

                        beforeRoleText = "";
                    }
                }

                if (PlayerControl.LocalPlayer == pc || !PlayerControl.LocalPlayer.IsAlive()) roleTextMeeting.text = beforeRoleText + SubRoleText + roleTextMeeting.text;

                if (Main.VisibleTasksCount && !Options.CurrentGameMode.IsOneNightMode())
                {//TOH_Y追加
                    if ((PlayerControl.LocalPlayer.Is(CustomRoles.TaskManager) || PlayerControl.LocalPlayer.Is(CustomRoles.Management)) && PlayerControl.LocalPlayer == pc)
                        roleTextMeeting.text += $" {Utils.GetTMtaskCountText(pc)}";

                    if (PlayerControl.LocalPlayer.Is(CustomRoles.Lawyer) || PlayerControl.LocalPlayer.Is(CustomRoles.EvilDiviner))
                    {
                        if (PlayerControl.LocalPlayer == pc
                            || !PlayerControl.LocalPlayer.IsAlive()
                            || (pc.Is(CustomRoles.Workaholic) && Options.WorkaholicTaskSeen.GetBool())
                            )
                            roleTextMeeting.text += Utils.GetProgressText(pc);
                    }
                    else if (!((pc.Is(CustomRoles.Workaholic) && !Options.WorkaholicTaskSeen.GetBool())
                        || (pc.Is(CustomRoles.Rainbow) && PlayerControl.LocalPlayer.IsAlive())))
                        roleTextMeeting.text += Utils.GetProgressText(pc);
                }
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;
                roleTextMeeting.enabled = pc.AmOwner || //対象がLocalPlayer
                    (Main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead && !Options.GhostCanSeeOtherRoles.GetBool()) || //LocalPlayerが死亡していて幽霊が他人の役職を見れるとき
                    pc.Is(CustomRoles.GM) || pc.Is(CustomRoles.Rainbow) || (pc.Is(CustomRoles.Workaholic) && Options.WorkaholicSeen.GetBool());

                if (EvilTracker.IsTrackTarget(PlayerControl.LocalPlayer, pc) && EvilTracker.CanSeeLastRoomInMeeting)
                {
                    roleTextMeeting.text = EvilTracker.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                    roleTextMeeting.enabled = true;
                }
                else if (Telepathisters.IsTrackTarget(PlayerControl.LocalPlayer, pc) && Telepathisters.CanSeeLastRoomInMeeting.GetBool())
                {
                    roleTextMeeting.text = Telepathisters.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                    roleTextMeeting.enabled = true;
                }
                else if (Psychic.IsShowTargetCamp(PlayerControl.LocalPlayer, pc, out bool onlyKiller))
                {
                    roleTextMeeting.text = Utils.GetCampText(pc, onlyKiller);
                    roleTextMeeting.enabled = true;
                }
                else if (Psychic.IsShowTargetRole(PlayerControl.LocalPlayer, pc))
                {
                    roleTextMeeting.text = Utils.GetRoleName(pc.GetCustomRole());
                    roleTextMeeting.enabled = true;
                }
                else if (FortuneTeller.IsShowTargetCamp(PlayerControl.LocalPlayer, pc, out bool onlyKillerF))
                {
                    roleTextMeeting.text = Utils.GetCampText(pc, onlyKillerF);
                    roleTextMeeting.enabled = true;
                }
                else if (FortuneTeller.IsShowTargetRole(PlayerControl.LocalPlayer, pc))
                {
                    roleTextMeeting.text = Utils.GetRoleName(pc.GetCustomRole());
                    roleTextMeeting.enabled = true;
                }
                else if (Lawyer.IsWatchTargetRole(PlayerControl.LocalPlayer, pc)) roleTextMeeting.enabled = true;
                else if (EvilDiviner.IsShowTargetRole(PlayerControl.LocalPlayer, pc)) roleTextMeeting.enabled = true;
                else if (ONDiviner.IsShowTargetRole(PlayerControl.LocalPlayer, pc)) roleTextMeeting.enabled = true;
                else if (ONBigWerewolf.IsShowTargetRole(PlayerControl.LocalPlayer, pc)) roleTextMeeting.enabled = true;
                if (Options.IsSyncColorMode && !(PlayerControl.LocalPlayer.Is(CustomRoles.TaskManager) || PlayerControl.LocalPlayer.Is(CustomRoles.Management)) && PlayerControl.LocalPlayer.IsAlive()) roleTextMeeting.enabled = false;

                Bakery.SendAliveMessage(pc);
                if (MeetingStates.FirstMeeting && pc.Is(CustomRoles.ONBakery) && !pc.Data.IsDead && !pc.Data.Disconnected)
                {
                    List<PlayerControl> targetList = new();
                    var rand = IRandom.Instance;
                    foreach (var p in Main.AllAlivePlayerControls)
                    {
                        if (p == pc) continue;
                        targetList.Add(p);
                    }
                    var GetPlayerId = targetList[rand.Next(targetList.Count)].PlayerId;
                    Logger.Info($"{Utils.GetPlayerById(pc.PlayerId).GetNameWithRole()}のパン配布先：{Utils.GetPlayerById(GetPlayerId).GetNameWithRole()}", "ONBakery");

                    Utils.SendMessage(GetString("ONPanAlive"), GetPlayerId, $"<color={Utils.GetRoleColorCode(CustomRoles.ONBakery)}>{GetString("ONPanAliveMessageTitle")}</color>");
                }

                if (Options.ShowRoleInfoAtFirstMeeting.GetBool() && MeetingStates.FirstMeeting)
                {
                    String RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                    String RoleInfoTitle = $"{Utils.ColorString(RoleTextData.Item2, RoleInfoTitleString)}";
                    Utils.SendMessage(Utils.SendRoleInfo(pc), sendTo: pva.TargetPlayerId, title: RoleInfoTitle);
                }
            }
            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            TemplateManager.SendTemplate("OnMeeting", noErr: true);

            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    foreach (var seer in Main.AllPlayerControls)
                    {
                        foreach (var target in Main.AllPlayerControls)
                        {
                            var seerName = seer.GetRealName(isMeeting: true);
                            var coloredName = Utils.ColorString(seer.GetRoleColor(), seerName);
                            seer.RpcSetNamePrivate(
                                seer == target ? coloredName : seerName,
                                true);
                        }
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                }, 3f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl seer = PlayerControl.LocalPlayer;
                PlayerControl target = Utils.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                var sb = new StringBuilder();
                //会議画面での名前変更
                //自分自身の名前の色を変更
                //pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

                if (target.AmOwner && AmongUsClient.Instance.IsGameStarted) //変更先が自分自身
                {
                    if (Options.CurrentGameMode.IsCatMode() && target.Is(CustomRoles.Crewmate))
                        pva.NameText.color = Utils.GetRoleColor(CustomRoles.CatNoCat);
                    else if (Options.CurrentGameMode.IsOneNightMode() && (Main.DefaultRole[pva.TargetPlayerId] != CustomRoles.ONPhantomThief))
                        pva.NameText.color = Utils.GetRoleColor(Main.DefaultRole[pva.TargetPlayerId]);
                    else if (Options.CurrentGameMode.IsOneNightMode() && (Main.DefaultRole[pva.TargetPlayerId] == CustomRoles.ONPhantomThief))
                        pva.NameText.color = Utils.GetRoleColor(seer.GetCustomRole());
                    else
                        pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);
                }
                else
                {
                    if (Options.CurrentGameMode.IsOneNightMode() && Main.DefaultRole[seer.PlayerId].IsONImpostor() && Main.DefaultRole[target.PlayerId].IsONImpostor())
                        pva.NameText.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);
                    else if (Options.CurrentGameMode.IsOneNightMode() && Main.DefaultRole[seer.PlayerId] == CustomRoles.ONPhantomThief && Main.DefaultRole[target.PlayerId].IsONImpostor())
                    { }
                    else if (Options.CurrentGameMode.IsOneNightMode() && (Main.DefaultRole[target.PlayerId] == CustomRoles.ONPhantomThief))
                    { }
                    else
                        pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);
                }

                //if (seer.Is(CustomRoles.Snitch) && Snitch.SnitchCannotConfirmKillRoles.GetBool())
                //    pva.NameText.color = Color.white;

                if (seer.KnowDeathReason(target))
                    sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");

                //インポスター表示
                switch (seer.GetCustomRole().GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Impostor:
                        if (target.Is(CustomRoles.MadSnitch) && target.GetPlayerTaskState().IsTaskFinished && Options.MadSnitchCanAlsoBeExposedToImpostor.GetBool())
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.MadSnitch), "★")); //変更対象にSnitchマークをつける
                        sb.Append(Snitch.GetWarningMark(seer, target));
                        break;
                }
                switch (seer.GetCustomRole())
                {
                    case CustomRoles.Arsonist:
                        if (seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), "▲"));
                        break;
                    case CustomRoles.Executioner:
                        sb.Append(Executioner.TargetMark(seer, target));
                        break;
                    case CustomRoles.Egoist:
                    case CustomRoles.DarkHide:
                    case CustomRoles.Opportunist:
                        if (seer.Is(CustomRoles.Opportunist) && !Options.OpportunistCanKill.GetBool()) break;
                        sb.Append(Snitch.GetWarningMark(seer, target));
                        break;
                    case CustomRoles.Jackal:
                        sb.Append(Snitch.GetWarningMark(seer, target)); //スニッチのマーク
                        if (target.Is(CustomRoles.JClient) && target.GetPlayerTaskState().IsTaskFinished && Options.JClientCanAlsoBeExposedToJackal.GetBool())
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.JClient), "★")); //クライアントを視認できる時のマーク
                        sb.Append(Snitch.GetWarningMark(seer, target));
                        break;
                    case CustomRoles.EvilTracker:
                        sb.Append(EvilTracker.GetTargetMark(seer, target));
                        break;

                    case CustomRoles.Sympathizer:
                        if (target.Is(CustomRoles.Sympathizer))
                            if (seer.GetPlayerTaskState().SympaTask && target.GetPlayerTaskState().SympaTask)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Sympathizer), "◎"));
                        break;
                    case CustomRoles.FortuneTeller:
                        if (seer.HasForecastResult(target.PlayerId))
                            sb.Append(FortuneTeller.TargetMark(seer, target));
                        break;
                }

                // 弁護士orそのターゲットにマーク付く
                sb.Append(Lawyer.TargetMark(seer, target));
                sb.Append(Utils.AntiCompMark(seer, target, true));

                foreach (var subRole in target.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Lovers:
                            if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♥"));
                            break;
                    }
                }

                //呪われている場合
                sb.Append(Witch.GetSpelledMark(target.PlayerId, true));
                if (Bakery.IsPoisoned(target))
                    sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NBakery), "θ"));

                sb.Append(Medic.GetGuardMark(seer.PlayerId, target));
                sb.Append(Totocalcio.GetBetMark(seer.PlayerId, target));
                //会議画面ではインポスター自身の名前にSnitchマークはつけません。
                pva.NameText.text += sb.ToString();
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class MeetingHudUpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var player = Utils.GetPlayerById(x.TargetPlayerId);
                    player.RpcExileV2();
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Execution;
                    Main.PlayerStates[player.PlayerId].SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
                });
            }
        }
    }
    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
    class SetHighlightedPatch
    {
        public static bool Prefix(PlayerVoteArea __instance, bool value)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (!__instance.HighlightedFX) return false;
            __instance.HighlightedFX.enabled = value;
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class MeetingHudOnDestroyPatch
    {
        public static void Postfix()
        {
            MeetingStates.FirstMeeting = false;
            Logger.Info("------------会議終了------------", "Phase");
            if (AmongUsClient.Instance.AmHost)
            {
                AntiBlackout.SetIsDead();
                Main.AllPlayerControls.Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);
            }
        }
    }
}
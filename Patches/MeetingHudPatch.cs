using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using TownOfHostY.Modules;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Impostor;
using TownOfHostY.Roles.Neutral;
using UnityEngine;
using static TownOfHostY.Translator;
namespace TownOfHostY;

[HarmonyPatch]
public static class MeetingHudPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            MeetingVoteManager.Instance?.CheckAndEndMeeting();
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    class VotingCompletePatch
    {
        public static void Postfix(MeetingHud __instance, [HarmonyArgument(1)] NetworkedPlayerInfo exiled, [HarmonyArgument(2)] bool tie)
        {
            if (exiled == null) return;

            var exiledPc = exiled.Object;
            // リヴェラー属性
            Revealer.ChangeName(exiledPc);
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    public static class CastVotePatch
    {
        public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] PlayerId srcPlayerId /* 投票した人 */ , [HarmonyArgument(1)] PlayerId suspectPlayerId /* 投票された人 */ )
        {
            var voter = Utils.GetPlayerById(srcPlayerId);
            var voted = Utils.GetPlayerById(suspectPlayerId);
            if (voter.GetRoleClass()?.CheckVoteAsVoter(voted) == false)
            {
                __instance.RpcClearVote(voter.PlayerId);
                Logger.Info($"{voter.GetNameWithRole()} は投票しない", nameof(CastVotePatch));
                return false;
            }

            MeetingVoteManager.Instance?.SetVote(srcPlayerId, suspectPlayerId);
            return true;
        }
    }
    
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.SetJudgeOverrule))]
    public static class SetJudgeOverrulePatch
    {        
        public static void ShowOverruleLocally(ushort overruleNonce)
        {
            var meetingHud = MeetingHud.Instance;
            if (meetingHud == null) return;

            bool anotherJudgeBeatYouToIt = false;            
            var judgeRole = PlayerControl.LocalPlayer?.Data?.Role?.TryCast<JudgeRole>();
            if (judgeRole != null && judgeRole.HasAlreadyOverruledThisMeeting)
            {
                if (judgeRole.OverruleNonce == overruleNonce) judgeRole.ConsumeOverruleVotesUsage();
                else anotherJudgeBeatYouToIt = true;
            }
            meetingHud.ShowJudgeOverrule(anotherJudgeBeatYouToIt);
        }

        public static bool Prefix(MeetingHud __instance,
            [HarmonyArgument(0)] PlayerId judgePlayerId /* 裁決した人 */,
            [HarmonyArgument(1)] PlayerId targetPlayerId /* 裁決された人 */,
            [HarmonyArgument(2)] ushort overruleNonce)
        {           
            if (!AmongUsClient.Instance.AmHost) return false;

            var voter = Utils.GetPlayerById(judgePlayerId);
            var votefor = Utils.GetPlayerById(targetPlayerId);
            if (voter == null || votefor == null)
            {
                Logger.Warn($"裁決者({judgePlayerId})か対象({targetPlayerId})が見つかりません", nameof(SetJudgeOverrulePatch));
                return false;
            }
            Logger.Info($"{voter.GetNameWithRole()} => {votefor.GetNameWithRole()} (nonce: {overruleNonce})", nameof(SetJudgeOverrulePatch));

            byte exilePlayerId = byte.MaxValue;
            if (voter.GetRoleClass()?.CallJudgeVote(voter, votefor, ref exilePlayerId) != true
                || exilePlayerId == byte.MaxValue)
            {                
                __instance.RpcClearVote(voter.PlayerId);
                Logger.Info($"{voter.GetNameWithRole()} の裁決は無効化されました", nameof(SetJudgeOverrulePatch));
                return false;
            }

            MeetingVoteManager.Instance?.SetVote(
                judgePlayerId, targetPlayerId,
                judgeExiledId: exilePlayerId, judgeNonce: overruleNonce);
            MeetingVoteManager.Instance?.EndMeeting();
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartPatch
    {
        public static void Prefix()
        {
            Logger.Info("------------会議開始------------", "Phase");
            VentilationSystemPatch.ClearVent();
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            ChainShifterAddon.OnStartMeeting();
            foreach (var tm in Main.AllAlivePlayerControls.Where(p => p.Is(CustomRoles.TaskManager) || p.Is(CustomRoles.Management)))
                Utils.NotifyRoles(true, tm);
            TargetDeadArrow.OnStartMeeting();

            Sending.OnStartMeeting();

            // 前の会議で追放されたリヴェラーの役職を次の会議(今)から公開
            Revealer.OnStartMeeting();
        }
        public static void Postfix(MeetingHud __instance)
        {
            MeetingVoteManager.Start();

            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;
            var myRole = PlayerControl.LocalPlayer.GetRoleClass();
            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.PlayerId);
                if (pc == null) continue;                
                foreach (var name in new[] { "RoleTextMeeting", "SuffixTextMeeting" })
                {
                    var old = pva.PlayerIcon.transform.Find(name);
                    if (old != null) Object.Destroy(old.gameObject);
                }

                var roleTextMeeting = Object.Instantiate(pva.NameText);
                var suffixTextMeeting = Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.PlayerIcon.transform);
                suffixTextMeeting.transform.SetParent(pva.PlayerIcon.transform);

                roleTextMeeting.transform.localPosition = new Vector3(3.25f, 1.02f, -5f);
                roleTextMeeting.fontSize = 1.5f;
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;
                (roleTextMeeting.enabled, roleTextMeeting.text)
                    = Utils.GetRoleNameAndProgressTextData(true, PlayerControl.LocalPlayer, pc);

                
                suffixTextMeeting.transform.localPosition = new Vector3(3.25f, 0.02f, 0f);
                suffixTextMeeting.fontSize = 1.5f;
                suffixTextMeeting.gameObject.name = "SuffixTextMeeting";
                suffixTextMeeting.enableWordWrapping = false;
                suffixTextMeeting.enabled = false;
                suffixTextMeeting.text = "";

                // シンクロカラーモード
                if (Options.IsSyncColorMode && Options.SCM_NothingMeetingNameColor.GetBool()
                    && PlayerControl.LocalPlayer.IsAlive())
                {
                    roleTextMeeting.enabled = false;
                    continue;
                }

                var suffixBuilder = new StringBuilder(32);
                if (myRole != null)
                {
                    suffixBuilder.Append(myRole.GetSuffix(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                }
                suffixBuilder.Append(CustomRoleManager.GetSuffixOthers(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                // Management
                if (pc.Is(CustomRoles.Management))
                {
                    suffixBuilder.Append(Management.GetSuffix(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                }

                if (suffixBuilder.Length > 0)
                {
                    suffixTextMeeting.text = suffixBuilder.ToString();
                    suffixTextMeeting.enabled = true;
                }
                // 役職/属性テキストは NameText の外(PlayerIcon側)に置いているため、
                // 以前のように NameText 自体をずらす必要はない
            }
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.OnStartMeeting());

            List<string> messageList = new();

            if (Options.SyncButtonMode.GetBool())
            {
                messageList.Add(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }
            if (Options.ShowReportReason.GetBool())
            {
                if (ReportDeadBodyPatch.ReportTarget == null)
                    messageList.Add(GetString("Message.isButton"));
                else if (!ReportDeadBodyPatch.SpecialMeeting)
                    messageList.Add(string.Format(GetString("Message.isReport"),
                        $"{ReportDeadBodyPatch.ReportTarget.PlayerName}{ReportDeadBodyPatch.ReportTarget.ColorName.Color(ReportDeadBodyPatch.ReportTarget.Color)}"));
            }
            if (Options.ShowRevengeTarget.GetBool())
            {
                foreach (var Exiled_Target in RevengeTargetPlayer)
                {
                    messageList.Add(string.Format(GetString("Message.RevengeText"),
                        $"{Exiled_Target.exiled.PlayerName}{Exiled_Target.exiled.ColorName.Color(Exiled_Target.exiled.Color)}", $"{Exiled_Target.revengeTarget.PlayerName}{Exiled_Target.revengeTarget.ColorName.Color(Exiled_Target.revengeTarget.Color)}"));
                }
            }

            if (AntiBlackout.OverrideExiledPlayer && !Options.IsCCMode)
            {
                messageList.Add(GetString("Warning.OverrideExiledPlayer"));
            }
            if (Options.IsCCMode)
            {
                CatchCat.Infomation.ShowMeeting();
            }

            if (messageList.Count > 0)
            {
                var message = string.Join("\n", messageList);
                Utils.SendMessage(message, true);
            }

            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            TemplateManager.SendTemplate("OnMeeting", noErr: true);

            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                    {
                        ChatUpdatePatch.DoBlockChat = false;
                        return;
                    }
                    foreach (var seen in Main.AllPlayerControls)
                    {
                        var seenName = seen.GetRealName(isMeeting: true);
                        var coloredName = Utils.ColorString(seen.GetRoleColor(true), seenName);
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            seen.RpcSetNamePrivate(
                                seer == seen ? coloredName : seenName,
                                true,
                                seer);
                        }
                    }

                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (!Main.ShowRoleInfoAtMeeting.Contains(pc.PlayerId)) continue;
                        var targetRole = pc.GetCustomRole();
                        if (targetRole == CustomRoles.Potentialist)
                            targetRole = CustomRoles.Crewmate;
                        string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                        string RoleInfoTitle = $"{Utils.ColorString(Utils.GetRoleColor(targetRole), RoleInfoTitleString)}";
                        Utils.SendMessage(Utils.GetMyRoleInfo(pc),true, RoleInfoTitle, pc.PlayerId);
                        Main.ShowRoleInfoAtMeeting.Remove(pc.PlayerId);
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                }, 3f, "SetName To Chat");
                if (ReportDeadBodyPatch.SpecialMeeting)
                {
                    Utils.SendMessage("強制会議の場合は一部役職名が表示されず名前が赤く見えることがありますが、会議終了後回復します。", true);
                }
            }

            // MeetingDisplayText
            int Showi = 0;

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();

                var target = Utils.GetPlayerById(pva.PlayerId);
                if (target == null) continue;

               
                pva.NameText.text = target.GetRealName(isMeeting: true);

                // 役職説明表示
                if (Main.ShowRoleInfoAtMeeting.Contains(target.PlayerId))
                {

                }

                var sb = new StringBuilder();

                //会議画面での名前変更
                //自分自身の名前の色を変更
                //NameColorManager準拠の処理
                if (target.AmOwner && AmongUsClient.Instance.IsGameStarted) //変更先が自分自身
                {
                    //if (Options.IsONMode && (Main.DefaultRole[pva.PlayerId] != CustomRoles.ONPhantomThief))
                    //    pva.NameText.color = Utils.GetRoleColor(Main.DefaultRole[pva.PlayerId]);
                    //else if (Options.IsONMode && (Main.DefaultRole[pva.PlayerId] == CustomRoles.ONPhantomThief))
                    //    pva.NameText.color = Utils.GetRoleColor(seer.GetCustomRole());
                    //else
                    pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

                    (Color c, string t) = (pva.NameText.color, "");
                    //trueRoleNameでColor上書きあればそれになる
                    target.GetRoleClass()?.OverrideShowMainRoleText(ref c, ref t);//colorのみ
                    pva.NameText.color = c;
                }
                else
                {
                    //if (Options.IsONMode && Main.DefaultRole[seer.PlayerId].IsONImpostor() && Main.DefaultRole[target.PlayerId].IsONImpostor())
                    //    pva.NameText.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);
                    //else if (Options.IsONMode && Main.DefaultRole[seer.PlayerId] == CustomRoles.ONPhantomThief && Main.DefaultRole[target.PlayerId].IsONImpostor())
                    //{ }
                    //else if (Options.IsONMode && (Main.DefaultRole[target.PlayerId] == CustomRoles.ONPhantomThief))
                    //{ }
                    //else
                    pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);
                }

                if (seer.KnowDeathReason(target))
                    sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");

                sb.Append(seerRole?.GetMark(seer, target, true));
                sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));
                //Lovers
                sb.Append(Lovers.GetMark(seer, target));

                //会議画面ではインポスター自身の名前にSnitchマークはつけません。

                pva.NameText.text += sb.ToString();

                if (!pva.AmDead && Showi < 3)
                {
                    pva.NameText.text = MeetingDisplayText.AddTextForClient(pva.NameText.text, Showi);
                    Showi++;
                }
            }
            // 道連れ記載情報破棄
            RevengeTargetPlayer.Clear();
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class UpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var player = Utils.GetPlayerById(x.PlayerId);
                    // ゲッサーと完全同等の方式でキル（バン・hacking判定回避）
                    player.Data.IsDead = true;
                    player.RpcExileV3();
                    var state = PlayerState.GetByPlayerId(player.PlayerId);
                    state.DeathReason = CustomDeathReason.Execution;
                    CustomRoleManager.CheckMurderInfos[player.PlayerId] = new MurderInfo(PlayerControl.LocalPlayer, player, player, player);
                    CustomRoleManager.OnMurderPlayer(player, player);
                    Main.AllPlayerControls.Do(pc => pc.KillFlash());
                    foreach (var va in __instance.playerStates)
                    {
                        if (va.VotedForId != player.PlayerId) continue;
                        var voter = Utils.GetPlayerById(va.PlayerId);
                        if (voter == null) continue;
                        __instance.RpcClearVote(voter.PlayerId);
                    }
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName), true);
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                });
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class OnDestroyPatch
    {
        public static void Postfix()
        {
            MeetingStates.FirstMeeting = false;
            MeetingStates.MeetingCalled = false;
            Logger.Info("------------会議終了------------", "Phase");
            ChatUpdatePatch.DoBlockChat = false;
            if (AmongUsClient.Instance.AmHost)
            {
                if (!AntiBlackout.IsCached) AntiBlackout.SetIsDead();
                Utils.AfterMeetingTasks();

                Main.AllPlayerControls.Where(pc => !pc.Is(CustomRoles.GM)).Do(pc => RandomSpawn.CustomNetworkTransformPatch.FirstTP[pc.PlayerId] = true);

                _ = new LateTask(() =>
                {
                    if (!GameStates.IsInGame) return;


                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.GetClientId() == -1) continue;

                        var role = pc.GetCustomRole();
                        var roleInfo = role.GetRoleInfo();


                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId
                            && Options.EnableGM.GetBool()) continue;


                        // 霊界（死亡済み）プレイヤーにはSetRoleしない
                        if (PlayerState.GetByPlayerId(pc.PlayerId).IsDead) continue;

                        var baseRole = roleInfo?.BaseRoleType?.Invoke() ?? RoleTypes.Crewmate;
                        if (roleInfo?.IsDesyncImpostor == true)
                        {
                            // Desync role: send Impostor to self, Crewmate to all other clients
                            pc.RpcSetRoleDesync(baseRole, pc.GetClientId());
                            foreach (var viewer in Main.AllPlayerControls)
                            {
                                if (viewer.PlayerId == pc.PlayerId) continue;
                                int viewerClientId = viewer.GetClientId();
                                if (viewerClientId == -1) continue;
                                if (viewer.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                                pc.RpcSetRoleDesync(RoleTypes.Crewmate, viewerClientId);
                            }
                            continue;
                        }

                        pc.RpcSetRoleDesync(baseRole, pc.GetClientId());
                        Logger.Info($"AfterMeeting baseRole: {pc.GetNameWithRole()} -> {baseRole}", "AfterMeeting_RoleSync");
                    }

                    Utils.NotifyRoles();
                }, 0.5f, "AfterMeeting_RoleSync");
            }
            MeetingVoteManager.Instance?.Destroy();
        }
    }

    public static void TryAddAfterMeetingDeathPlayers(CustomDeathReason deathReason, params byte[] playerIds)
    {
        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
                AddedIdList.Add(playerId);
        CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(CustomDeathReason deathReason, params byte[] playerIds)
    {
        foreach (var playerId in playerIds)
        {
            // 役職による道連れ
            Lovers.VoteSuicide(playerId);
            Janitor.VoteSuicide(playerId);
            //道連れチェック
            RevengeOnExile(playerId, deathReason);
        }
    }
    //道連れ(する側,される側)
    public static List<(NetworkedPlayerInfo exiled, NetworkedPlayerInfo revengeTarget)> RevengeTargetPlayer;
    private static void RevengeOnExile(byte playerId, CustomDeathReason deathReason)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        //道連れ能力持たない時は下を通さない
        if (!((player.Is(CustomRoles.SKMadmate) && Options.MadmateRevengeCrewmate.GetBool())
            || (player.Is(CustomRoles.NekoKabocha) && NekoKabocha.revengeOnExile)
            || (player.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.RevengeOnExile)
            || player.Is(CustomRoles.EvilNekomata) || player.Is(CustomRoles.Nekomata)
            || player.Is(CustomRoles.Immoralist) || player.Is(CustomRoles.Revenger))) return;

        var target = PickRevengeTarget(player, deathReason);
        if (target == null) return;
        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);
        Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, CustomDeathReason deathReason)//道連れ先選定
    {
        List<PlayerControl> TargetList = new();
        foreach (var candidate in Main.AllAlivePlayerControls)
        {
            if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;

            ///対象とならない人を判定
            // インポスター陣営の場合
            if (exiledplayer.Is(CustomRoleTypes.Madmate) || exiledplayer.Is(CustomRoleTypes.Impostor))
            {
                if (candidate.Is(CustomRoleTypes.Impostor) && !Options.RevengeImpostorByImpostor.GetBool()) continue; //インポスター
                if (candidate.Is(CustomRoleTypes.Madmate) && !Options.RevengeMadByImpostor.GetBool()) continue; //マッドメイト
            }
            // 背徳者は妖狐を道連れしない
            if (exiledplayer.Is(CustomRoles.Immoralist) && candidate.Is(CustomRoles.FoxSpirit)) continue;

            // 第三陣営を道連れするか（設定）
            if (candidate.Is(CustomRoleTypes.Neutral) && !Options.RevengeNeutral.GetBool()) continue;

            // チェインシフターは道連れされない（涙）
            if (candidate.Is(CustomRoles.ChainShifterAddon)) continue;

            TargetList.Add(candidate);
        }
        if (TargetList == null || TargetList.Count == 0) return null;
        var rand = IRandom.Instance;
        var target = TargetList[rand.Next(TargetList.Count)];
        // 道連れする側とされる側をセットでリストに追加
        NetworkedPlayerInfo exiledInfo = exiledplayer.Data;
        NetworkedPlayerInfo targetInfo = target.Data;
        RevengeTargetPlayer.Add((exiledInfo, targetInfo));
        return target;
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
using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using TownOfHostY.Modules;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Impostor;
using UnityEngine;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static StringBuilder Mark = new(20);
        private static StringBuilder Suffix = new(120);
        public static void Postfix(PlayerControl __instance)
        {
            try
            {
                if (__instance == null) return; // 安全ガード

                var player = __instance;

                if (!GameStates.IsModHost) return;

                TargetArrow.OnFixedUpdate(player);
                TargetDeadArrow.OnFixedUpdate(player);
                VentSelect.OnFixedUpdate(player);
                CustomRoleManager.OnFixedUpdate(player);

                if (AmongUsClient.Instance.AmHost)
                {
                    // 実行クライアントがホストの場合のみ実行
                    if (GameStates.IsLobby && (!Main.AllowPublicRoom || ModUpdater.hasUpdate || !VersionChecker.IsSupported || !Main.IsPublicAvailableOnThisVersion) && AmongUsClient.Instance.IsGamePublic)
                        AmongUsClient.Instance.ChangeGamePublic(false);

                    if (GameStates.IsInTask && !ReportDeadBodyPatch.CannotReportList.Contains(__instance.PlayerId) && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
                    {
                        var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                        ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                        Logger.Info($"{__instance.GetNameWithRole()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                        __instance.ReportDeadBody(info);
                    }

                    DoubleTrigger.OnFixedUpdate(player);

                    // ターゲットのリセット
                    if (GameStates.IsInTask && player.IsAlive() && Options.LadderDeath.GetBool())
                    {
                        FallFromLadder.FixedUpdate(player);
                    }

                    if (GameStates.IsInGame && player.AmOwner)
                        DisableDevice.FixedUpdate();

                    if (__instance.AmOwner)
                    {
                        Utils.ApplySuffix();
                    }
                }

                // LocalPlayer専用
                if (__instance.AmOwner)
                {
                    // キルターゲットの上書き処理
                    if (GameStates.IsInTask && !((__instance.Is(CustomRoleTypes.Impostor) && !__instance.Is(CustomRoles.StrayWolf)) || __instance.Is(CustomRoles.Egoist)) && __instance.CanUseKillButton() && !__instance.Data.IsDead)
                    {
                        var players = __instance.GetPlayersInAbilityRangeSorted(false);
                        PlayerControl closest = players.Count <= 0 ? null : players[0];
                        var hud = HudManager.Instance;
                        if (hud != null && hud.KillButton != null)
                        {
                            hud.KillButton.SetTarget(closest);
                        }
                    }
                }

                // 役職テキストの表示
                // safety: cosmetics/nameText may be null in some states; check before use
                var nameTextObj = __instance?.cosmetics?.nameText;
                if (nameTextObj == null) return;

                var RoleTextTransform = nameTextObj.transform.Find("RoleText");
                TMPro.TextMeshPro RoleText = null;
                if (RoleTextTransform != null)
                {
                    // GetComponent が null 参照を投げないようチェック
                    try
                    {
                        RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"RoleText GetComponent で例外: {ex.Message}", "FixedUpdatePatch");
                        RoleText = null;
                    }
                }

                if (RoleText == null)
                {
                    // RoleText が null の場合は以降の表示処理を行わない
                    return;
                }

                // GameStates が null などの可能性を考慮して以降を保護
                if (GameStates.IsLobby)
                {
                    if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                    {
                        try
                        {
                            if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                                __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                            else if (Main.version.CompareTo(ver.version) != 0)
                                __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                            else
                                __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Lobby nameText 書き換えで例外: {ex.Message}", "FixedUpdatePatch");
                        }
                    }
                    else
                    {
                        try { __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName; } catch { }
                    }
                }

                if (GameStates.IsInGame)
                {
                    // まず seer (表示する側) を取得。LocalPlayer が null かもしれないので代替を用意
                    var seer = PlayerControl.LocalPlayer ?? __instance;
                    // seer が null であればロール表示を諦める
                    if (seer == null)
                    {
                        RoleText.enabled = false;
                        RoleText.text = "";
                        return;
                    }

                    // Utils.GetRoleNameAndProgressTextData は内部で ShipStatus 等を参照するので例外を吸収して安全に扱う
                    bool enabled = false;
                    string roleTextStr = "";
                    try
                    {
                        (enabled, roleTextStr) = Utils.GetRoleNameAndProgressTextData(false, seer, __instance);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"GetRoleNameAndProgressTextData で例外: {ex.Message}", "FixedUpdatePatch");
                        enabled = false;
                        roleTextStr = "";
                    }

                    RoleText.enabled = enabled;
                    RoleText.text = roleTextStr;

                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                    {
                        RoleText.enabled = false; // ゲームが始まっておらずフリープレイでなければロールを非表示
                        if (!__instance.AmOwner)
                        {
                            try { __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName; } catch { }
                        }
                    }

                    // 以下は name display の組み立て（多くの参照に nullガードを入れる）
                    var target = __instance;
                    string RealName = target.GetRealName();
                    Mark.Clear();
                    Suffix.Clear();

                    // 名前色等の処理（seer・target の null をチェックしつつ）
                    try
                    {
                        if (target.AmOwner && AmongUsClient.Instance.IsGameStarted)
                        {
                            if (target.Is(CustomRoles.SeeingOff) || target.Is(CustomRoles.Sending) || target.Is(CustomRoles.MadDilemma))
                            {
                                string str = Sending.RealNameChange();
                                if (!string.IsNullOrEmpty(str))
                                {
                                    RealName = str;
                                }
                            }
                            else if (Options.IsCCMode)
                            {
                                RealName = Utils.ColorString(seer.GetRoleColor(), seer.GetRoleInfo());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"名前変更処理で例外: {ex.Message}", "FixedUpdatePatch");
                    }

                    // NameColorManager 準拠の処理（安全第一でラップ）
                    try
                    {
                        RealName = RealName.ApplyNameColorData(seer, target, false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"ApplyNameColorData で例外: {ex.Message}", "FixedUpdatePatch");
                    }

                    // trueRoleName の color 上書き（null 安全に）
                    try
                    {
                        (Color c, string t) = (Color.clear, "");
                        var targetRoleClass = target.GetRoleClass();
                        targetRoleClass?.OverrideShowMainRoleText(ref c, ref t);
                        if (c != Color.clear)
                        {
                            RealName = RealName.Color(c); // 既存の Color 拡張を利用
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"OverrideShowMainRoleText で例外: {ex.Message}", "FixedUpdatePatch");
                    }

                    // Mark / Suffix の組み立て（null 安全に）
                    try
                    {
                        var seerRole = seer.GetRoleClass();
                        Mark.Append(seerRole?.GetMark(seer, target, false));
                        Mark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));
                        Mark.Append(Lovers.GetMark(seer, target));

                        if (seer == target && ReportDeadBodyPatch.DontReportMarkList.Contains(seer.PlayerId))
                            Mark.Append(Utils.ColorString(Palette.Orange, "◀×"));

                        Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target));
                        Suffix.Append(seerRole?.GetSuffix(seer, target));
                        Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target));
                        if (seer.Is(CustomRoles.Management))
                        {
                            Suffix.Append(Management.GetSuffix(seer, target));
                        }
                        Suffix.Append(TargetDeadArrow.GetDeadBodiesArrow(seer, target));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Mark/Suffix 組み立てで例外: {ex.Message}", "FixedUpdatePatch");
                    }

                    try
                    {
                        if (Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() && !Options.IsSyncColorMode)
                            RealName = $"<size=0>{RealName}</size> ";
                    }
                    catch (Exception ex)
                    {
                        // Utils.IsActive で例外が出るケースがあるので吸収して続行
                        Logger.Warn($"IsActive チェックで例外: {ex.Message}", "FixedUpdatePatch");
                    }

                    if (EvilDyer.IsColorCamouflage)
                        RealName = $"<size=0>{RealName}</size> ";

                    string DeathReason = "";
                    try
                    {
                        if (seer?.Data != null && seer.Data.IsDead && seer.KnowDeathReason(target))
                            DeathReason = $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})";
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"DeathReason 取得で例外: {ex.Message}", "FixedUpdatePatch");
                    }

                    // 画面反映
                    try
                    {
                        target.cosmetics.nameText.text = $"{RealName}{DeathReason}{Mark}";

                        if (Suffix.Length != 0)
                        {
                            RoleText.transform.SetLocalY(0.45f);
                            target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();
                        }
                        else
                        {
                            RoleText.transform.SetLocalY(0.3f);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"名前表示反映で例外: {ex.Message}", "FixedUpdatePatch");
                    }
                }
                else
                {
                    // ゲーム中でない場合は RoleText 座標を初期に戻す
                    try { RoleText.transform.SetLocalY(0.3f); } catch { }
                }
            }
            catch (Exception ex)
            {
                // Postfix 全体での保険: ここで例外が出てもゲームループを止めない
                Logger.Error($"FixedUpdatePatch.Postfix で例外: {ex.Message}", "FixedUpdatePatch");
                Logger.Exception(ex, "FixedUpdatePatch");
            }
        }
    }
}
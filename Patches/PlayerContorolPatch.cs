using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHostY.Modules;
using TownOfHostY.Roles;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Roles.Neutral;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.AddOns.Crewmate;
using static TownOfHostY.Translator;
using TownOfHostY.Roles.Impostor;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace TownOfHostY;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
class CheckProtectPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole() + "=>" + target.GetNameWithRole(), "CheckProtect");
        if (__instance.Is(CustomRoles.Sheriff)
            ||__instance.Is(CustomRoles.SillySheriff)
            ||__instance.Is(CustomRoles.Hunter)
            ||__instance.Is(CustomRoles.MadSheriff))
        {
            if (__instance.Data.IsDead)
            {
                Logger.Info("守護をブロックしました。", "CheckProtect");
                return false;
            }
        }
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
class CheckMurderPatch
{
    public static Dictionary<byte, float> TimeSinceLastKill = new();
    public static void Update()
    {
        for (byte i = 0; i < 15; i++)
        {
            if (TimeSinceLastKill.ContainsKey(i))
            {
                TimeSinceLastKill[i] += Time.deltaTime;
                if (15f < TimeSinceLastKill[i]) TimeSinceLastKill.Remove(i);
            }
        }
    }
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        // 処理は全てCustomRoleManager側で行う
        if (!CustomRoleManager.OnCheckMurder(__instance, target))
        {
            // キル失敗
            __instance.RpcMurderPlayer(target, false);
        }

        return false;
    }

    // 不正キル防止チェック
    public static bool CheckForInvalidMurdering(MurderInfo info, bool killerMyselfInvoke)
    {
        (var killer, var target) = info.AttemptTuple;

        // Killerが既に死んでいないかどうか
        if (!killer.IsAlive())
        {
            Logger.Info($"{killer.GetNameWithRole()}は死亡しているためキャンセルされました。", "CheckMurder");
            return false;
        }
        // targetがキル可能な状態か
        if (
            // PlayerDataがnullじゃないか確認
            target.Data == null ||
            // targetの状態をチェック
            target.inVent ||
            target.MyPhysics.Animations.IsPlayingEnterVentAnimation() ||
            target.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
            target.inMovingPlat)
        {
            Logger.Info("targetは現在キルできない状態です。", "CheckMurder");
            return false;
        }
        // targetが既に死んでいないか
        if (!target.IsAlive())
        {
            Logger.Info("targetは既に死んでいたため、キルをキャンセルしました。", "CheckMurder");
            return false;
        }
        // 会議中のキルでないか
        if (MeetingHud.Instance != null)
        {
            Logger.Info("会議が始まっていたため、キルをキャンセルしました。", "CheckMurder");
            return false;
        }

        // 連打キルでないか
        if (!killerMyselfInvoke)
        {
            float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / 1000f * 6f); //※AmongUsClient.Instance.Pingの値はミリ秒(ms)なので÷1000
            //TimeSinceLastKillに値が保存されていない || 保存されている時間がminTime以上 => キルを許可
            //↓許可されない場合
            if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
            {
                Logger.Info("前回のキルからの時間が早すぎるため、キルをブロックしました。", "CheckMurder");
                return false;
            }
        }
        TimeSinceLastKill[killer.PlayerId] = 0f;

        // HideAndSeek_キルボタンが使用可能か
        if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
        {
            Logger.Info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。", "CheckMurder");
            return false;
        }
        // キルが可能なプレイヤーか(遠隔は除く)
        if (!info.IsFakeSuicide && !killer.CanUseKillButton())
        {
            Logger.Info(killer.GetNameWithRole() + "はKillできないので、キルはキャンセルされました。", "CheckMurder");
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
class MurderPlayerPatch
{
    private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.MurderPlayer));
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags, ref bool __state /* 成功したキルかどうか */ )
    {
        logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}({resultFlags})");
        var isProtectedByClient = resultFlags.HasFlag(MurderResultFlags.DecisionByHost) && target.IsProtected();
        var isProtectedByHost = resultFlags.HasFlag(MurderResultFlags.FailedProtected);
        var isFailed = resultFlags.HasFlag(MurderResultFlags.FailedError);
        var isSucceeded = __state = !isProtectedByClient && !isProtectedByHost && !isFailed;
        if (isProtectedByClient)
        {
            logger.Info("守護されているため，キルは失敗します");
        }
        if (isProtectedByHost)
        {
            logger.Info("守護されているため，キルはホストによってキャンセルされました");
        }
        if (isFailed)
        {
            logger.Info("キルはホストによってキャンセルされました");
        }

        if (isSucceeded)
        {
            if (target.shapeshifting)
            {
                //シェイプシフトアニメーション中
                //アニメーション時間を考慮して1s、加えてクライアントとのラグを考慮して+0.5s遅延する
                _ = new LateTask(
                    () =>
                    {
                        if (GameStates.IsInTask)
                        {
                            target.RpcShapeshift(target, false);
                        }
                    },
                    1.5f, "RevertShapeshift");
            }
            else
            {
                if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out var shapeshifting) && shapeshifting)
                {
                    //シェイプシフト強制解除
                    target.RpcShapeshift(target, false);
                }
            }
            Camouflage.RpcSetSkin(Camouflage.IsCamouflage, target, ForceRevert: true, RevertToDefault: true);
            if (Options.IsSyncColorMode && Options.SCM_RestoredDeadPlayer.GetBool())
                {
                    SkinChangeMode.RpcSetSkin(target, target);
                }
        }
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, bool __state)
    {
        if (!__state) return; // キルが成功していない場合，何もしない

        if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
        if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;
        // 以降ホストしか処理しない
        // 処理は全てCustomRoleManager側で行う
        CustomRoleManager.OnMurderPlayer(__instance, target);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckShapeshift))]
public static class PlayerControlCheckShapeshiftPatch
{
    private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.CheckShapeshift));

    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate)
    {
        if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost)
        {
            return false;
        }

        // 無効な変身を弾く．これより前に役職等の処理をしてはいけない
        if (!CheckInvalidShapeshifting(__instance, target, shouldAnimate))
        {
            __instance.RpcRejectShapeshift();
            return false;
        }
        var shapeshifter = __instance;
        var shapeshifting = shapeshifter.PlayerId != target.PlayerId;
        // 変身したとき一番近い人をマッドメイトにする処理
        if (shapeshifter.CanMakeMadmate() && shapeshifting)
        {
            var sidekickable = shapeshifter.GetRoleClass() as ISidekickable;
            var targetRole = sidekickable?.SidekickTargetRole ?? CustomRoles.SKMadmate;

            Vector2 shapeshifterPosition = shapeshifter.transform.position;//変身者の位置
            Dictionary<PlayerControl, float> mpdistance = new();
            float dis;
            foreach (var p in Main.AllAlivePlayerControls)
            {
                if (p.Data.Role.Role != RoleTypes.Shapeshifter && !p.Is(CustomRoleTypes.Impostor) && !p.Is(targetRole))
                {
                    dis = Vector2.Distance(shapeshifterPosition, p.transform.position);
                    mpdistance.Add(p, dis);
                }
            }
            if (mpdistance.Count != 0)
            {
                var min = mpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                PlayerControl targetm = min.Key;
                targetm.RpcSetCustomRole(targetRole);
                Logger.Info($"Make SKMadmate:{targetm.name}", "Shapeshift");
                Main.SKMadmateNowCount++;
                Utils.MarkEveryoneDirtySettings();
                Utils.NotifyRoles();
            }
        }
        // 役職の処理
        var role = shapeshifter.GetRoleClass();
        if (role?.OnCheckShapeshift(target, ref shouldAnimate) == false)
        {
            if (role.CanDesyncShapeshift)
            {
                shapeshifter.RpcSpecificRejectShapeshift(target, shouldAnimate);
            }
            else
            {
                shapeshifter.RpcRejectShapeshift();
            }
            return false;
        }

        shapeshifter.RpcShapeshift(target, shouldAnimate);
        return false;
    }
    private static bool CheckInvalidShapeshifting(PlayerControl instance, PlayerControl target, bool animate)
    {
        logger.Info($"Checking shapeshift {instance.GetNameWithRole()} -> {(target == null || target.Data == null ? "(null)" : target.GetNameWithRole())}");

        if (!target || target.Data == null)
        {
            logger.Info("targetがnullのため変身をキャンセルします");
            return false;
        }
        if (!instance.IsAlive())
        {
            logger.Info("変身者が死亡しているため変身をキャンセルします");
            return false;
        }
        // RoleInfoによるdesyncシェイプシフター用の判定を追加
        if (instance.Data.Role.Role != RoleTypes.Shapeshifter && instance.GetCustomRole().GetRoleInfo()?.BaseRoleType?.Invoke() != RoleTypes.Shapeshifter)
        {
            logger.Info("変身者がシェイプシフターではないため変身をキャンセルします");
            return false;
        }
        if (instance.Data.Disconnected)
        {
            logger.Info("変身者が切断済のため変身をキャンセルします");
            return false;
        }
        if (target.IsMushroomMixupActive() && animate)
        {
            logger.Info("キノコカオス中のため変身をキャンセルします");
            return false;
        }
        if (MeetingHud.Instance && animate)
        {
            logger.Info("会議中のため変身をキャンセルします");
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
class ShapeshiftPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        Logger.Info($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");

        var shapeshifter = __instance;
        var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

        if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
        {
            Logger.Info($"{__instance?.GetNameWithRole()}:Cancel Shapeshift.Prefix", "Shapeshift");
            return;
        }

        Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
        Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

        shapeshifter.GetRoleClass()?.OnShapeshift(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (!shapeshifting) Camouflage.RpcSetSkin(Camouflage.IsCamouflage, __instance);

        //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
        if (!shapeshifting)
        {
            _ = new LateTask(() =>
            {
                Utils.NotifyRoles(NoCache: true);
            },
            1.2f, "ShapeShiftNotify");
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckVanish))]
public static class PlayerControlCheckVanishPatch
{
    private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.CheckVanish));

    public static bool Prefix(PlayerControl __instance)
    {
        if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost)
        {
            return false;
        }

        var phantom = __instance;
        Logger.Info($"{phantom.GetNameWithRole()} : CheckVanishStart", "CheckVanish");

        // 役職の処理
        var role = phantom.GetRoleClass();

        /***************************** 
         * ◆下記OnCheckVanishがfalseの場合は設定できる
         * ・killCooldown : キルクールが10秒に設定されてしまうため任意に設定する
         * 　　ホストは仕様上キルクールは強制されないが、バニラの仕様に合わせセットする
         * ・canResetAbilityCooldown : アビリティクールダウンをリセットするか設定する
         *****************************/
        float killCooldown = 10.0f;
        bool canResetAbilityCooldown = false;
        if (role?.OnCheckVanish(ref killCooldown, ref canResetAbilityCooldown) == false)
        {
            Logger.Info($"{phantom.GetNameWithRole()} : OnCheckVanish() == false", "CheckVanish");

            if (phantom.PlayerId != PlayerControl.LocalPlayer.PlayerId &&
                phantom.IsAlive())
            {
                SendDummyClearCharge(phantom);
            }

            // キルクールリセット
            phantom.SetKillCooldown(killCooldown);
            // アビリティクールリセット
            if (canResetAbilityCooldown)
            {
                phantom.RpcResetAbilityCooldown();
            }
            return false;
        }

        return true;
    }
    private static void SendDummyClearCharge(PlayerControl phantom)
    {
        int clientId = phantom.GetClientId();
        var stream = MessageWriter.Get(SendOption.Reliable);
        stream.StartMessage(6);
        stream.Write(AmongUsClient.Instance.GameId);
        stream.WritePacked(clientId);
        {
            stream.StartMessage(2);
            stream.WritePacked(phantom.NetId);
            stream.Write((byte)RpcCalls.SetRole);
            stream.Write((ushort)RoleTypes.Impostor);
            stream.Write(true);     //canOverrideRole
            stream.EndMessage();

            stream.StartMessage(2);
            stream.WritePacked(phantom.NetId);
            stream.Write((byte)RpcCalls.SetRole);
            stream.Write((ushort)RoleTypes.Phantom);
            stream.Write(true);     //canOverrideRole
            stream.EndMessage();
        }
        stream.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(stream);
        stream.Recycle();
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckVanish))]
public static class PlayerControlCmdCheckVanishPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        __instance?.CheckVanish();
        return false;
    }
}
[HarmonyPatch(typeof(PhantomRole), nameof(PhantomRole.UseAbility))]
public static class PhantomRoleUseAbilityPatch
{
    public static bool Prefix(PhantomRole __instance)
    {
        // プレイヤーがアクティブで使用可能か確認
        if (__instance.Player.AmOwner &&
            !__instance.Player.Data.IsDead &&
            __instance.Player.moveable &&
            !Minigame.Instance &&
            !__instance.IsCoolingDown &&
            !__instance.fading)
        {
            // 現在の役職アニメーションがVanishingチャージでないことを確認
            bool hasVanishChargeAnimation = false;
            foreach (var anim in __instance.Player.currentRoleAnimations)
            {
                if (anim.effectType == RoleEffectAnimation.EffectType.Vanish_Charge)
                {
                    hasVanishChargeAnimation = true;
                    break;
                }
            }

            if (!hasVanishChargeAnimation && !__instance.Player.walkingToVent && !__instance.Player.inMovingPlat)
            {
                if (__instance.isInvisible)
                {
                    __instance.MakePlayerVisible(true, true);
                    return false;
                }

                var hud = DestroyableSingleton<HudManager>.Instance;
                hud.AbilityButton.SetSecondImage(__instance.Ability);
                hud.AbilityButton.OverrideText(DestroyableSingleton<TranslationController>
                    .Instance.GetString(StringNames.PhantomAbilityUndo, new Il2CppReferenceArray<Il2CppSystem.Object>(0)));

                // PhantomDuration を直接取得
                float phantomDuration = GetPhantomDuration();
                __instance.Player.CmdCheckVanish(phantomDuration);

                return false; // 元のメソッドは実行しない
            }
        }

        return false; // 条件に関わらず元メソッドは実行しない
    }

    // LogicOptions から PhantomDuration を取得する自前メソッド
    private static float GetPhantomDuration()
    {
        var options = GameManager.Instance.LogicOptions;

        // LogicOptions に直接フィールドがある場合
        // 例: options._phantomDuration
        // もし存在しない場合は固定値を返す
        float duration = 5f; // デフォルト5秒
        try
        {
            var field = options.GetType().GetField("_phantomDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                duration = (float)field.GetValue(options);
        }
        catch
        {
            // 例外無視してデフォルト値を使用
        }

        return duration;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
class ReportDeadBodyPatch
{
    public static NetworkedPlayerInfo  reporter;
    public static NetworkedPlayerInfo ReportTarget;
    public static bool SpecialMeeting = reporter?.PlayerId == ReportTarget?.PlayerId;

    public static List<byte> CannotReportList;
    public static List<byte> CannotReportByDeadBodyList;
    public static List<byte> DontReportMarkList;
    public static Dictionary<byte, List<NetworkedPlayerInfo>> WaitReport = new();
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
    {
        reporter = __instance.Data;
        ReportTarget = target;
        SpecialMeeting = reporter?.PlayerId == ReportTarget?.PlayerId;
        if (GameStates.IsMeeting) return false;
        Logger.Info($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
        if (SpecialMeeting) return true;
        if (Options.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
        if (Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) return false;
        if (Options.IsCCMode && CatchCat.Option.IgnoreReport.GetBool() && target != null) return false;
        if (target == null && Options.DisableButtonInMushroomMixup.GetBool() && MushroomMixupUpdateSystemPatch.InSabotage)
        {
            Logger.Info("DisableButton InMushroomMixup", "ReportDeadBody");
            return false;
        }
        if ((reporter.Object.Is(CustomRoles.NonReport) || reporter.Object.Is(CustomRoles.FoxSpirit)) &&
            target != null && !target.Object.Is(CustomRoles.Bait) && !target.Object.Is(CustomRoles.AddBait))
        {
            DontReportMarkList.Add(reporter.PlayerId);
            Utils.NotifyRoles(SpecifySeer: reporter.Object);
            _ = new LateTask(() =>
            {
                DontReportMarkList.Remove(reporter.PlayerId);
                Utils.NotifyRoles(SpecifySeer: reporter.Object);
            }, 5f, "Don't Report Mark Remove");
            return false;
        }
        // Scavenger
        if (target != null && CannotReportByDeadBodyList.Contains(target.PlayerId))
        {
            DontReportMarkList.Add(reporter.PlayerId);
            Utils.NotifyRoles(SpecifySeer: reporter.Object);
            _ = new LateTask(() =>
            {
                DontReportMarkList.Remove(reporter.PlayerId);
                Utils.NotifyRoles(SpecifySeer: reporter.Object);
            }, 5f, "Scavenger DontReportMark Remove");
            return false;
        }

        if (CannotReportList.Contains(__instance.PlayerId))
        {
            WaitReport[__instance.PlayerId].Add(target);
            Logger.Warn($"{__instance.GetNameWithRole()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");
            return false;
        }

        //会議ボタンの場合、サボタージュ中に呼び出しを受けない
        if (target == null && Utils.IsActiveDontOpenMeetingSabotage(out var sabotage))
        {
            Logger.Warn($"{sabotage}サボタージュ中です", "ReportDeadBody");
            return false;
        }

        if (!AmongUsClient.Instance.AmHost) return true;

        //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
        if (__instance.Data.IsDead) return false;

        if (Options.SyncButtonMode.GetBool() && target == null)
        {
            Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
            if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
            {
                Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                return false;
            }
            else Options.UsedButtonCount++;
            if (Options.SyncedButtonCount.GetFloat() == Options.UsedButtonCount)
            {
                Logger.Info("使用可能ボタン回数が最大数に達しました。", "ReportDeadBody");
            }
        }

        //=============================================
        //以下、ボタンが押されることが確定したものとする。
        //=============================================

        foreach (var pc in Main.AllPlayerControls)
        {
            var state = PlayerState.GetByPlayerId(pc.PlayerId);

            // ブラックアウト
            if (pc.IsAlive())
            {
                state.IsBlackOut = true;
                pc.MarkDirtySettings();
            }

            // 最終位置の部屋の取得・記憶
            state.LastRoom = pc.GetPlainShipRoom();

            // 変身解除処理
            if (Main.CheckShapeshift.ContainsKey(pc.PlayerId))
            {
                Camouflage.RpcSetSkin(Camouflage.IsCamouflage, pc, RevertToDefault: true);
            }
        }
          
        MushroomMixupDeterioratePatch.RestorName();

        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            role.OnReportDeadBody(__instance, target);
        }

        // 役職変更表示のリセット
        Main.ShowChangeMainRole.Clear();

        MeetingTimeManager.OnReportDeadBody();

        Utils.NotifyRoles(isForMeeting: true, NoCache: true);

        Utils.SyncAllSettings();

        if (ShapeKiller.DummyReportDeadBody(__instance, target)) return false;

        if (!__instance.IsAlive())
        {
            //通報者が通報処理中に死亡した場合の強制会議実行
            RPC.ReportDeadBodyForced(__instance, target);
            return false;
        }

        return true;
    }
    public static async void ChangeLocalNameAndRevert(string name, int time)
    {
        //async Taskじゃ警告出るから仕方ないよね。
        var revertName = PlayerControl.LocalPlayer.name;
        PlayerControl.LocalPlayer.RpcSetNameEx(name);
        await Task.Delay(time);
        PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
public static class PlayerControlStartMeetingPatch
{
    public static void Prefix()
    {
        //foreach (var kvp in PlayerState.AllPlayerStates)
        //{
        //    var pc = Utils.GetPlayerById(kvp.Key);
        //    kvp.Value.LastRoom = pc.GetPlainShipRoom();
        //}
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
class PlayerStartPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
        roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
        roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        roleText.transform.localScale = new(1f, 1f, 1f);
        roleText.fontSize = Main.RoleTextSize;
        roleText.text = "RoleText";
        roleText.gameObject.name = "RoleText";
        roleText.enabled = false;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
class SetColorPatch
{
    public static bool IsAntiGlitchDisabled = false;
    public static bool Prefix(PlayerControl __instance, int bodyColor)
    {
        //色変更バグ対策
        if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
        if ((AmongUsClient.Instance.IsGameStarted && Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            || (AmongUsClient.Instance.IsGameStarted && Options.IsCCMode))
        {
            //ゲーム中に色を変えた場合
            __instance.RpcMurderPlayer(__instance);
        }
        return true;
    }
}
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
class CoEnterVentPatch
{
    public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            var user = __instance.myPlayer;
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.IgnoreVent.GetBool())
                __instance.RpcBootFromVent(id);

            if ((!user.GetRoleClass()?.OnEnterVent(__instance, id) ?? false) ||
                !VentEnterTask.OnEnterVent(__instance, id) ||
                (user.GetCustomRole().IsCCCatRoles() && !CatchCat.CatPlayer.CanUseVent(user)) ||
                (user.Data.Role.Role != RoleTypes.Engineer && //エンジニアでなく
                !user.CanUseImpostorVentButton()) //インポスターベントも使えない
            )
            {
                _ = new LateTask(() =>
                {
                    __instance.RpcBootFromVent(id);
                }, 0.5f, "Cancel Vent");
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
class SetNamePatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] string name)
    {
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
class PlayerControlCompleteTaskPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        var pc = __instance;

        Logger.Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
        var taskState = pc.GetPlayerTaskState();
        taskState.Update(pc);

        var roleClass = pc.GetRoleClass();
        var ret = true;
        if (roleClass != null)
        {
            ret = roleClass.OnCompleteTask();
        }
        //属性クラスの扱いを決定するまで仮置き
        ret &= Workhorse.OnCompleteTask(pc);
        CompreteCrew.OnCompleteTask(pc);

        if (Options.IsCCMode) CatchCat.CatPlayer.OnCompleteTask(pc, taskState);

        Utils.NotifyRoles();
        return ret;
    }
    public static void Postfix()
    {
        //人外のタスクを排除して再計算
        GameData.Instance.RecomputeTaskCounts();
        Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "TaskState.Update");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
class PlayerControlProtectPlayerPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "ProtectPlayer");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
class PlayerControlRemoveProtectionPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        Logger.Info($"{__instance.GetNameWithRole()}", "RemoveProtection");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
class PlayerControlSetRolePatch
{
    public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType, bool canOverrideRole)
    {
        if (RpcSetRoleReplacer.DoReplace()) return true;

        var target = __instance;
        var targetName = __instance.GetNameWithRole();
        Logger.Info($"{targetName} =>{roleType}", "PlayerControl.RpcSetRole");
        if (!ShipStatus.Instance.enabled) return true;
        if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
        {
            var targetIsKiller = target.GetRoleClass() is IKiller;
            var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();
            foreach (var seer in Main.AllPlayerControls)
            {
                var self = seer.PlayerId == target.PlayerId;
                var seerIsKiller = seer.GetRoleClass() is IKiller;

                if ((self && targetIsKiller) || (!seerIsKiller && target.Is(CustomRoleTypes.Impostor)))
                {
                    ghostRoles[seer] = RoleTypes.ImpostorGhost;
                }
                else
                {
                    ghostRoles[seer] = RoleTypes.CrewmateGhost;
                }
                if (Pirate.TargetSetGhostAndTask(target)) ghostRoles[seer] = RoleTypes.CrewmateGhost;
            }
            if (ghostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
            {
                roleType = RoleTypes.CrewmateGhost;
            }
            else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
            {
                roleType = RoleTypes.ImpostorGhost;
            }
            else
            {
                foreach ((var seer, var role) in ghostRoles)
                {
                    Logger.Info($"Desync {targetName} =>{role} for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                    target.RpcSetRoleDesync(role, seer.GetClientId());
                }
                return false;
            }
        }

        target.RpcSetRoleNormal(roleType, canOverrideRole);

        return false;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class PlayerControlDiePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            // 死者の最終位置にペットが残るバグ対応
            __instance.RpcSetPet("");
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckUseZipline))]
public static class PlayerControlCheckUseZiplinePatch
{
    public static bool Prefix(PlayerControl target, ZiplineBehaviour ziplineBehaviour, bool fromTop)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            if (Options.FungleCanUseZiplineFromTop.GetBool() && fromTop) return false;
            if (Options.FungleCanUseZiplineFromUnder.GetBool() && !fromTop) return false;
        }

        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckSporeTrigger))]
public static class PlayerControlCheckSporeTriggerPatch
{
    public static bool Prefix()
    {
        return !Options.FungleCanSporeTrigger.GetBool();
    }
}

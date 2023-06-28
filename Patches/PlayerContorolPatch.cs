using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Madmate;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
    class CheckProtectPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole() + "=>" + target.GetNameWithRole(), "CheckProtect");
            if (__instance.IsCrewKiller())
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

            var killer = __instance; //読み替え変数

            Logger.Info($"{killer.GetNameWithRole()} => {target.GetNameWithRole()}", "CheckMurder");

            //死人はキルできない
            if (killer.Data.IsDead)
            {
                Logger.Info($"{killer.GetNameWithRole()}は死亡しているためキャンセルされました。", "CheckMurder");
                return false;
            }

            //不正キル防止処理
            if (target.Data == null || //PlayerDataがnullじゃないか確認
                target.inVent || target.inMovingPlat //targetの状態をチェック
            )
            {
                Logger.Info("targetは現在キルできない状態です。", "CheckMurder");
                return false;
            }
            if (target.Data.IsDead) //同じtargetへの同時キルをブロック
            {
                Logger.Info("targetは既に死んでいたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            if (MeetingHud.Instance != null) //会議中でないかの判定
            {
                Logger.Info("会議が始まっていたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }

            float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / 1000f * 6f); //※AmongUsClient.Instance.Pingの値はミリ秒(ms)なので÷1000
            //TimeSinceLastKillに値が保存されていない || 保存されている時間がminTime以上 => キルを許可
            //↓許可されない場合
            if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
            {
                Logger.Info("前回のキルからの時間が早すぎるため、キルをブロックしました。", "CheckMurder");
                return false;
            }
            TimeSinceLastKill[killer.PlayerId] = 0f;

            killer.ResetKillCooldown();

            //キルボタンを使えない場合の判定
            if (Options.IsStandardHAS && Options.HideAndSeekKillDelayTimer > 0)
            {
                Logger.Info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }

            //キル可能判定
            if (killer.PlayerId != target.PlayerId && !killer.CanUseKillButton())
            {
                Logger.Info(killer.GetNameWithRole() + "はKillできないので、キルはキャンセルされました。", "CheckMurder");
                return false;
            }

            //実際のキラーとkillerが違う場合の入れ替え処理
            if (Sniper.IsEnable)
            {
                Sniper.TryGetSniper(target.PlayerId, ref killer);
            }
            if (killer != __instance)
            {
                Logger.Info($"Real Killer={killer.GetNameWithRole()}", "CheckMurder");

            }

            if (killer.Is(CustomRoles.EvilDiviner))//イビルディバイナーのみ占いのためここで先に処理
            {
                if (!EvilDiviner.OnCheckMurder(killer, target))
                    return false;
            }

            if (target.Is(CustomRoles.Guarding))
            {
                if (!killer.Is(CustomRoles.Arsonist) &&
                    !killer.Is(CustomRoles.PlatonicLover) &&
                    !killer.Is(CustomRoles.Totocalcio) &&
                    !killer.Is(CustomRoles.MadSheriff))
                {
                    if (GuardingGuard(killer, target)) return false;
                }
            }

            //Todo::キルされた時の特殊判定
            switch (target.GetCustomRole())
            {
                case CustomRoles.SchrodingerCat:
                    if (!SchrodingerCat.OnCheckMurder(killer, target)) return false;
                    break;

                case CustomRoles.CursedWolf:
                    if (killer.Is(CustomRoles.Arsonist) || killer.Is(CustomRoles.PlatonicLover)|| killer.Is(CustomRoles.Totocalcio) || killer.Is(CustomRoles.MadSheriff)) break;
                    if (Main.CursedWolfSpellCount[target.PlayerId] <= 0) break;
                    if (killer.Is(CustomRoles.SillySheriff)) break;

                    CurseWolfGuard(killer, target);
                    return false;

                //==========マッドメイト系役職==========//
                case CustomRoles.MadGuardian:
                    //killerがキルできないインポスター判定役職の場合はスキップ
                    if (killer.Is(CustomRoles.Arsonist) || killer.Is(CustomRoles.PlatonicLover)|| killer.Is(CustomRoles.Totocalcio) || killer.Is(CustomRoles.MadSheriff)) break;

                    //MadGuardianを切れるかの判定処理
                    var taskState = target.GetPlayerTaskState();
                    if (taskState.IsTaskFinished)
                    {
                        var colorCode = Utils.GetRoleColorCode(CustomRoles.MadGuardian);
                        if (!NameColorManager.TryGetData(killer, target, out var value) || value != colorCode)
                        {
                            NameColorManager.Add(killer.PlayerId, target.PlayerId);
                            if (Options.MadGuardianCanSeeWhoTriedToKill.GetBool())
                                NameColorManager.Add(target.PlayerId, killer.PlayerId, colorCode);
                            Utils.NotifyRoles();
                        }
                        return false;
                    }
                    break;

                //Crewmate
                case CustomRoles.Blinder:
                    Main.isBlindVision[killer.PlayerId] = true;
                    killer.RpcSetBlinderVisionPlayer(true);
                    killer.MarkDirtySettings();
                    break;

                case CustomRoles.Pursuer:
                    if (!Lawyer.OnCheckMurder(killer, target)) return false;
                    break;

                //Neutral
                case CustomRoles.LoveCutter:
                    if (killer.Is(CustomRoles.Arsonist) || killer.Is(CustomRoles.PlatonicLover) || killer.Is(CustomRoles.Totocalcio) || killer.Is(CustomRoles.MadSheriff)) break;
                    if (killer.Is(CustomRoles.SillySheriff)) break;
                    LoveCutterGuard(killer, target);
                    Utils.NotifyRoles();
                    return false;
                case CustomRoles.AntiComplete:
                    if (killer.Is(CustomRoles.Arsonist) || killer.Is(CustomRoles.PlatonicLover) || killer.Is(CustomRoles.Totocalcio) || killer.Is(CustomRoles.MadSheriff)) break;
                    if (AntiCompGuard(killer, target)) return false;
                    break;
                case CustomRoles.NBakery:
                    Bakery.NBakeryKilledTasks(target.PlayerId);
                    break;

                //絶対に切られない設定
                case CustomRoles.CatRedLeader:
                case CustomRoles.CatBlueLeader:
                case CustomRoles.CatYellowLeader:
                    if (Options.LeaderNotKilled.GetBool())
                    {
                        killer.RpcGuardAndKill(target);
                        return false;
                    }
                    break;
                case CustomRoles.CatRedCat:
                case CustomRoles.CatBlueCat:
                case CustomRoles.CatYellowCat:
                    if (Options.CatNotKilled.GetBool())
                    {
                        killer.RpcGuardAndKill(target);
                        return false;
                    }
                    break;

                default:
                    if (Options.CurrentGameMode.IsCatMode() && target.Is(CustomRoles.Crewmate))
                    {
                        CatNoCat.OnCheckMurder(killer, target);
                        return false;
                    }
                    else if(Options.CurrentGameMode.IsOneNightMode() && target.Is(CustomRoles.ONPhantomThief))
                    {
                        ONPhantomThief.OnCheckMurderTarget(killer, target);
                    }
                    else if (!Medic.GuardPlayerCheckMurder(killer, target)) return false;
                    break;
            }

            //キル時の特殊判定
            if (killer.PlayerId != target.PlayerId)
            {
                //自殺でない場合のみ役職チェック
                switch (killer.GetCustomRole())
                {
                    //==========インポスター役職==========//
                    case CustomRoles.BountyHunter: //キルが発生する前にここの処理をしないとバグる
                        BountyHunter.OnCheckMurder(killer, target);
                        break;
                    case CustomRoles.SerialKiller:
                        SerialKiller.OnCheckMurder(killer);
                        break;
                    case CustomRoles.Vampire:
                        if (!Vampire.OnCheckMurder(killer, target)) return false;
                        break;
                    case CustomRoles.Warlock:
                        if (!Main.CheckShapeshift[killer.PlayerId] && !Main.isCurseAndKill[killer.PlayerId])
                        { //Warlockが変身時以外にキルしたら、呪われる処理
                            Main.isCursed = true;
                            killer.SetKillCooldown();
                            Main.CursedPlayers[killer.PlayerId] = target;
                            Main.WarlockTimer.Add(killer.PlayerId, 0f);
                            Main.isCurseAndKill[killer.PlayerId] = true;
                            return false;
                        }
                        if (Main.CheckShapeshift[killer.PlayerId])
                        {//呪われてる人がいないくて変身してるときに通常キルになる
                            killer.RpcMurderPlayer(target);
                            killer.RpcGuardAndKill(target);
                            return false;
                        }
                        if (Main.isCurseAndKill[killer.PlayerId]) killer.RpcGuardAndKill(target);
                        return false;
                    case CustomRoles.Witch:
                        if (!Witch.OnCheckMurder(killer, target))
                        {
                            //Spellモードの場合は終了
                            return false;
                        }
                        break;
                    case CustomRoles.Puppeteer:
                        Main.PuppeteerList[target.PlayerId] = killer.PlayerId;
                        killer.SetKillCooldown();
                        Utils.NotifyRoles(SpecifySeer: killer);
                        return false;

                    case CustomRoles.Greedier:
                        Greedier.OnCheckMurder(killer);
                        break;
                    case CustomRoles.Ambitioner:
                        Ambitioner.OnCheckMurder(killer);
                        break;
                    case CustomRoles.Scavenger:
                        if (!Options.ScavengerIgnoreBait.GetBool() && target.Is(CustomRoles.Bait))
                        {
                            Logger.Info($"{target.GetNameWithRole()}：ベイトキルなので通報される", "Scavenger");
                        }
                        else //ベイトじゃない又はベイト無効など
                        {
                            if (target.Is(CustomRoles.Bait)) Main.BaitKillPlayer = byte.MaxValue; //ベイトマーク取り消し
                            ReportDeadBodyPatch.CanReportByDeadBody[target.PlayerId] = false;
                            Logger.Info($"{target.GetNameWithRole()}：通報できない死体", "Scavenger");
                        }
                        break;
                    //case CustomRoles.EvilDivinerのみ上で
                    //    if (!EvilDiviner.OnCheckMurder(killer, target))
                    //        return false;
                    //    break;

                    //==========マッドメイト系役職==========//
                    case CustomRoles.MadSheriff:
                        MadSheriff.OnCheckMurder(killer, target);
                        return false;

                    //==========ニュートラル役職==========//
                    case CustomRoles.Arsonist:
                        killer.SetKillCooldown(Options.ArsonistDouseTime.GetFloat());
                        if (!Main.isDoused[(killer.PlayerId, target.PlayerId)] && !Main.ArsonistTimer.ContainsKey(killer.PlayerId))
                        {
                            Main.ArsonistTimer.Add(killer.PlayerId, (target, 0f));
                            Utils.NotifyRoles(SpecifySeer: __instance);
                            RPC.SetCurrentDousingTarget(killer.PlayerId, target.PlayerId);
                        }
                        return false;

                    case CustomRoles.DarkHide:
                        DarkHide.OnCheckMurder(killer, target);
                        break;

                    case CustomRoles.PlatonicLover:
                        PlatonicLover.OnCheckMurder(killer, target);
                        return false;

                    case CustomRoles.Totocalcio:
                        Totocalcio.OnCheckMurder(killer, target);
                        return false;

                    case CustomRoles.Opportunist:
                        Main.OppoKillerShotLimit[killer.PlayerId]--;
                        Logger.Info($"{killer.GetNameWithRole()} : 残り{Main.OppoKillerShotLimit[killer.PlayerId]}発", "OppoKiller");
                        RPC.SendRPCOppoKillerShot(killer.PlayerId);
                        break;

                    //==========クルー役職==========//
                    case CustomRoles.Sheriff:
                        if (!Sheriff.OnCheckMurder(killer, target))
                            return false;
                        break;

                    case CustomRoles.SillySheriff:
                        SillySheriff.OnCheckMurder(killer, target, Process: "RemoveShotLimit");
                        if (!SillySheriff.OnCheckMurder(killer, target, Process: "Suicide"))
                            return false;

                        if (target.Is(CustomRoles.CursedWolf))
                        {
                            CurseWolfGuard(killer, target);
                            return false;
                        }
                        else if (target.Is(CustomRoles.LoveCutter))
                        {
                            LoveCutterGuard(killer, target);
                            return false;
                        }
                        else if (target.Is(CustomRoles.AntiComplete))
                        {
                            AntiCompGuard(killer, target);
                            return false;
                        }
                        else if (target.Is(CustomRoles.Guarding))
                        {
                            GuardingGuard(killer, target);
                            return false;
                        }
                        break;
                    case CustomRoles.Hunter:
                        Hunter.OnCheckMurder(killer, target);
                        break;

                    //ON
                    case CustomRoles.ONWerewolf:
                        ONWerewolf.OnCheckMurder(killer, target);
                        break;
                    case CustomRoles.ONBigWerewolf:
                        ONBigWerewolf.OnCheckMurder(killer, target);
                        break;
                    case CustomRoles.ONDiviner:
                        ONDiviner.OnCheckMurder(killer, target);
                        return false;
                    case CustomRoles.ONPhantomThief:
                        ONPhantomThief.OnCheckMurder(killer, target);
                        return false;
                }
            }

            //==キル処理==
            __instance.RpcMurderPlayer(target);
            //============

            return false;
        }

        static void CurseWolfGuard(PlayerControl killer, PlayerControl target)
        {
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);
            Main.CursedWolfSpellCount[target.PlayerId] -= 1;
            RPC.SendRPCCursedWolfSpellCount(target.PlayerId);
            Logger.Info($"{target.GetNameWithRole()} : {Main.CursedWolfSpellCount[target.PlayerId]}回目", "CursedWolf");
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Spell;
            killer.RpcMurderPlayer(killer);
        }

        static void LoveCutterGuard(PlayerControl killer, PlayerControl target)
        {
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);
            Main.LoveCutterKilledCount[target.PlayerId] += 1;
            RPC.SendRPCLoveCutterGuard(target.PlayerId);
            Utils.NotifyRoles(SpecifySeer: target);
            Logger.Info($"{target.GetNameWithRole()} : {Main.LoveCutterKilledCount[target.PlayerId]}回目", "LoveCutter");
            NameColorManager.Add(killer.PlayerId, target.PlayerId, Utils.GetRoleColorCode(CustomRoles.LoveCutter));
            Utils.CheckLoveCutterWin(target.Data);
        }
        static bool AntiCompGuard(PlayerControl killer, PlayerControl target)
        {
            if (Main.AntiCompGuardCount[target.PlayerId].Item1 <= 0)  return false;
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);
            Main.AntiCompGuardCount[target.PlayerId] = (Main.AntiCompGuardCount[target.PlayerId].Item1-1 , Main.AntiCompGuardCount[target.PlayerId].Item2);
            RPC.SendRPCAntiCompGuard(target.PlayerId);
            Logger.Info($"{target.GetNameWithRole()} : {Main.AntiCompGuardCount[target.PlayerId].Item1}回目", "AntiComp");
            NameColorManager.Add(killer.PlayerId, target.PlayerId, Utils.GetRoleColorCode(CustomRoles.AntiComplete));
            Utils.NotifyRoles(SpecifySeer: target);
            return true;
        }
        static bool GuardingGuard(PlayerControl killer, PlayerControl target)
        {
            if (Main.GuardingGuardCount[target.PlayerId])  return false;
            killer.RpcGuardAndKill(target);
            Main.GuardingGuardCount[target.PlayerId] = true;
            RPC.SendRPCGuardingGuard(target.PlayerId);
            Logger.Info($"{killer.GetNameWithRole()}->{target.GetNameWithRole()}:ガード", "Guarding");
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPlayerPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}{(target.protectedByGuardian ? "(Protected)" : "")}", "MurderPlayer");

            if (RandomSpawn.CustomNetworkTransformPatch.NumOfTP.TryGetValue(__instance.PlayerId, out var num) && num > 2) RandomSpawn.CustomNetworkTransformPatch.NumOfTP[__instance.PlayerId] = 3;
            if (!target.protectedByGuardian)
                Camouflage.RpcSetSkin(target, ForceRevert: true);
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;

            PlayerControl killer = __instance; //読み替え変数
            //実際のキラーとkillerが違う場合の入れ替え処理
            if (Sniper.IsEnable)
            {
                if (Sniper.TryGetSniper(target.PlayerId, ref killer))
                {
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Sniped;
                }
            }
            if (killer != __instance)
            {
                Logger.Info($"Real Killer={killer.GetNameWithRole()}", "MurderPlayer");
            }
            if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.etc)
            {
                //死因が設定されていない場合は死亡判定
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Kill;
            }

            //When Bait is killed
            if (target.GetCustomRole() == CustomRoles.Bait && killer.PlayerId != target.PlayerId)
            {
                Main.BaitKillPlayer = killer.PlayerId;
                Logger.Info(target?.Data?.PlayerName + "はBaitだった", "MurderPlayer");
                new LateTask(() =>
                {
                    if(GameStates.IsInTask)
                        killer.CmdReportDeadBody(target.Data);
                    Main.BaitKillPlayer = 253;
                }, 0.15f + Options.BaitWaitTime.GetFloat(), "Bait Self Report");
            }
            else if (target.Is(CustomRoles.AddBait) && killer.PlayerId != target.PlayerId)
            {
                Logger.Info(target?.Data?.PlayerName + "はAddBaitだった", "MurderPlayer");
                new LateTask(() => killer.CmdReportDeadBody(target.Data), 0.15f, "AddBait Self Report");
            }
            else
            //Terrorist
            if (target.Is(CustomRoles.Terrorist))
            {
                Logger.Info(target?.Data?.PlayerName + "はTerroristだった", "MurderPlayer");
                Utils.CheckTerroristWin(target.Data);
            }
            if (target.Is(CustomRoles.Trapper) && !killer.Is(CustomRoles.Trapper))
                killer.TrapperKilled(target);
            if (target.Is(CustomRoles.ONTrapper) && !killer.Is(CustomRoles.ONTrapper))
                killer.TrapperKilled(target);
            if (Executioner.Target.ContainsValue(target.PlayerId))
                Executioner.ChangeRoleByTarget(target);
            if (target.Is(CustomRoles.Executioner) && Executioner.Target.ContainsKey(target.PlayerId))
            {
                Executioner.Target.Remove(target.PlayerId);
                Executioner.SendRPC(target.PlayerId);
            }
            if (Lawyer.Target.ContainsValue(target.PlayerId))
                Lawyer.ChangeRoleByTarget(target);

            FixedUpdatePatch.LoversSuicide(target.PlayerId);

            ONDeadTargetArrow.UpdateDeadBody();

            Main.PlayerStates[target.PlayerId].SetDead();
            target.SetRealKiller(killer, true); //既に追加されてたらスキップ
            Utils.CountAlivePlayers(true);

            Utils.TargetDies(__instance, target);

            Utils.SyncAllSettings();
            Utils.NotifyRoles();
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

            Sniper.OnShapeshift(shapeshifter, shapeshifting);

            if (!AmongUsClient.Instance.AmHost) return;
            if (!shapeshifting) Camouflage.RpcSetSkin(__instance);

            switch (shapeshifter.GetCustomRole())
            {
                case CustomRoles.EvilTracker:
                    EvilTracker.OnShapeshift(shapeshifter, target, shapeshifting);
                    break;
                case CustomRoles.FireWorks:
                    FireWorks.ShapeShiftState(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.Warlock:
                    if (Main.CursedPlayers[shapeshifter.PlayerId] != null)//呪われた人がいるか確認
                    {
                        if (shapeshifting && !Main.CursedPlayers[shapeshifter.PlayerId].Data.IsDead)//変身解除の時に反応しない
                        {
                            var cp = Main.CursedPlayers[shapeshifter.PlayerId];
                            Vector2 cppos = cp.transform.position;//呪われた人の位置
                            Dictionary<PlayerControl, float> cpdistance = new();
                            float dis;
                            foreach (PlayerControl p in Main.AllAlivePlayerControls)
                            {
                                if (p != cp)
                                {
                                    dis = Vector2.Distance(cppos, p.transform.position);
                                    cpdistance.Add(p, dis);
                                    Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Warlock");
                                }
                            }
                            var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                            PlayerControl targetw = min.Key;
                            targetw.SetRealKiller(shapeshifter);
                            Logger.Info($"{targetw.GetNameWithRole()}was killed", "Warlock");
                            cp.RpcMurderPlayerV2(targetw);//殺す
                            shapeshifter.RpcGuardAndKill(shapeshifter);
                            Main.isCurseAndKill[shapeshifter.PlayerId] = false;
                        }
                        Main.CursedPlayers[shapeshifter.PlayerId] = null;
                    }
                    break;
            }
            if (shapeshifter.Is(CustomRoles.ShapeKiller)) ShapeKiller.Shapeshift(shapeshifter, target, shapeshifting);

            if (shapeshifter.CanMakeMadmate() && shapeshifting)
            {//変身したとき一番近い人をマッドメイトにする処理
                Vector2 shapeshifterPosition = shapeshifter.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new();
                float dis;
                foreach (var p in Main.AllAlivePlayerControls)
                {
                    if (p.Data.Role.Role != RoleTypes.Shapeshifter && !p.Is(CustomRoleTypes.Impostor) && !p.Is(CustomRoles.SKMadmate))
                    {
                        dis = Vector2.Distance(shapeshifterPosition, p.transform.position);
                        mpdistance.Add(p, dis);
                    }
                }
                if (mpdistance.Count() != 0)
                {
                    var min = mpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                    PlayerControl targetm = min.Key;
                    targetm.RpcSetCustomRole(CustomRoles.SKMadmate);
                    Logger.Info($"Make SKMadmate:{targetm.name}", "Shapeshift");
                    Main.SKMadmateNowCount++;
                    Utils.MarkEveryoneDirtySettings();
                    Utils.NotifyRoles();
                }
            }

            //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
            if (!shapeshifting)
            {
                new LateTask(() =>
                {
                    Utils.NotifyRoles(NoCache: true);
                },
                1.2f, "ShapeShiftNotify");
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static GameData.PlayerInfo reporter;
        public static GameData.PlayerInfo Target;

        public static Dictionary<byte, bool> CanReport;
        public static Dictionary<byte, bool> CanReportByDeadBody;
        public static Dictionary<byte, List<GameData.PlayerInfo>> WaitReport = new();
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            reporter = __instance.Data;
            Target = target;
            if (GameStates.IsMeeting) return false;
            Logger.Info($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
            if (Options.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
            if (Options.IsStandardHAS) return false;
            if (reporter.Object.Is(CustomRoles.NonReport) && 
                target != null && target.GetCustomRole() != CustomRoles.Bait && !target.Object.Is(CustomRoles.AddBait)) return false;
            if (target != null && !CanReportByDeadBody[target.PlayerId]) return false;
            if (!CanReport[__instance.PlayerId])
            {
                WaitReport[__instance.PlayerId].Add(target);
                Logger.Warn($"{__instance.GetNameWithRole()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");
                return false;
            }
            foreach (var kvp in Main.PlayerStates)
            {
                var pc = Utils.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc.GetPlainShipRoom();
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

            if (target == null) //ボタン
            {
                if (__instance.Is(CustomRoles.Mayor))
                {
                    Main.MayorUsedButtonCount[__instance.PlayerId] += 1;
                }
                else if (__instance.Is(CustomRoles.Chairman))
                {
                    Main.ChairmanUsedButtonCount[__instance.PlayerId] += 1;
                }
            }
            Main.ArsonistTimer.Clear();
            Main.PuppeteerList.Clear();

            BountyHunter.OnReportDeadBody();
            SerialKiller.OnReportDeadBody();
            Sniper.OnReportDeadBody();
            GrudgeSheriff.OnReportDeadBody();
            Vampire.OnStartMeeting();

            Main.AllPlayerControls.Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
                            .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));
            MeetingTimeManager.OnReportDeadBody();

            FortuneTeller.ConfirmForecastResult(); //占い結果確定

            Utils.NotifyRoles(isForMeeting: true, NoCache: true);
            Utils.SyncAllSettings();
            //シェイプキラーレポート
            if (__instance.Is(CustomRoles.ShapeKiller) && target != null && __instance.PlayerId != target.PlayerId)
            {
                var shapeTarget = ShapeKiller.GetTarget(__instance);
                if (shapeTarget != null &&
                    (ShapeKiller.CanDeadReport.GetBool() ||
                     (!shapeTarget.Data.IsDead && !shapeTarget.Data.Disconnected)))
                {
                    RPC.ReportDeadBodyForced(shapeTarget, target);
                    Logger.Info($"ShapeKillerの偽装通報 player: {shapeTarget?.name}, target: {target?.PlayerName}", "ReportDeadBody");
                    return false;
                }
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
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static StringBuilder Mark = new(20);
        private static StringBuilder Suffix = new(120);
        public static void Postfix(PlayerControl __instance)
        {
            var player = __instance;
            if (!GameStates.IsModHost) return;

            TargetArrow.OnFixedUpdate(player);
            ONDeadTargetArrow.OnFixedUpdate(player);

            Sniper.OnFixedUpdate(player);
            VentSelect.OnFixedUpdate(player);

            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                if (GameStates.IsLobby && !Main.AllowPublicRoom && AmongUsClient.Instance.IsGamePublic)
                    AmongUsClient.Instance.ChangeGamePublic(false);

                if (GameStates.IsInTask && ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
                {
                    var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                    ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                    Logger.Info($"{__instance.GetNameWithRole()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                    __instance.ReportDeadBody(info);
                }

                DoubleTrigger.OnFixedUpdate(player);
                Vampire.OnFixedUpdate(player);
                CandleLighter.FixedUpdate(player);
                Psychic.OnFixedUpdate(player);

                if (GameStates.IsInTask && CustomRoles.SerialKiller.IsEnable()) SerialKiller.FixedUpdate(player);
                if (GameStates.IsInTask && Main.WarlockTimer.ContainsKey(player.PlayerId))//処理を1秒遅らせる
                {
                    if (player.IsAlive())
                    {
                        if (Main.WarlockTimer[player.PlayerId] >= 1f)
                        {
                            player.RpcResetAbilityCooldown();
                            Main.isCursed = false;//変身クールを１秒に変更
                            player.SyncSettings();
                            Main.WarlockTimer.Remove(player.PlayerId);
                        }
                        else Main.WarlockTimer[player.PlayerId] = Main.WarlockTimer[player.PlayerId] + Time.fixedDeltaTime;//時間をカウント
                    }
                    else
                    {
                        Main.WarlockTimer.Remove(player.PlayerId);
                    }
                }

                if (GameStates.IsInTask && Main.colorchange.ContainsKey(player.PlayerId))
                {
                    if (player.IsAlive())
                    {
                        {
                            Main.colorchange[player.PlayerId] %= 18;
                            if (Main.colorchange[player.PlayerId] is >= 0 and < 1) player.RpcSetColor(8);
                            else if (Main.colorchange[player.PlayerId] is >= 1 and < 2) player.RpcSetColor(1);
                            else if (Main.colorchange[player.PlayerId] is >= 2 and < 3) player.RpcSetColor(10);
                            else if (Main.colorchange[player.PlayerId] is >= 3 and < 4) player.RpcSetColor(2);
                            else if (Main.colorchange[player.PlayerId] is >= 4 and < 5) player.RpcSetColor(11);
                            else if (Main.colorchange[player.PlayerId] is >= 5 and < 6) player.RpcSetColor(14);
                            else if (Main.colorchange[player.PlayerId] is >= 6 and < 7) player.RpcSetColor(5);
                            else if (Main.colorchange[player.PlayerId] is >= 7 and < 8) player.RpcSetColor(4);
                            else if (Main.colorchange[player.PlayerId] is >= 8 and < 9) player.RpcSetColor(17);
                            else if (Main.colorchange[player.PlayerId] is >= 9 and < 10) player.RpcSetColor(0);
                            else if (Main.colorchange[player.PlayerId] is >= 10 and < 11) player.RpcSetColor(3);
                            else if (Main.colorchange[player.PlayerId] is >= 11 and < 12) player.RpcSetColor(13);
                            else if (Main.colorchange[player.PlayerId] is >= 12 and < 13) player.RpcSetColor(7);
                            else if (Main.colorchange[player.PlayerId] is >= 13 and < 14) player.RpcSetColor(15);
                            else if (Main.colorchange[player.PlayerId] is >= 14 and < 15) player.RpcSetColor(6);
                            else if (Main.colorchange[player.PlayerId] is >= 15 and < 16) player.RpcSetColor(12);
                            else if (Main.colorchange[player.PlayerId] is >= 16 and < 17) player.RpcSetColor(9);
                            else if (Main.colorchange[player.PlayerId] is >= 17 and < 18) player.RpcSetColor(16);
                            Main.colorchange[player.PlayerId] += Time.fixedDeltaTime;
                        }
                    }
                    else
                    {
                        Main.colorchange.Remove(player.PlayerId);
                    }
                }

                //ターゲットのリセット
                BountyHunter.FixedUpdate(player);
                //グラージシェリフキル
                GrudgeSheriff.FixedUpdate(player);

                if (GameStates.IsInTask && player.IsAlive() && Options.LadderDeath.GetBool())
                {
                    FallFromLadder.FixedUpdate(player);
                }
                /*if (GameStates.isInGame && main.AirshipMeetingTimer.ContainsKey(__instance.PlayerId)) //会議後すぐにここの処理をするため不要になったコードです。今後#465で変更した仕様がバグって、ここの処理が必要になった時のために残してコメントアウトしています
                {
                    if (main.AirshipMeetingTimer[__instance.PlayerId] >= 9f && !main.AirshipMeetingCheck)
                    {
                        main.AirshipMeetingCheck = true;
                        Utils.CustomSyncAllSettings();
                    }
                    if (main.AirshipMeetingTimer[__instance.PlayerId] >= 10f)
                    {
                        Utils.AfterMeetingTasks();
                        main.AirshipMeetingTimer.Remove(__instance.PlayerId);
                    }
                    else
                        main.AirshipMeetingTimer[__instance.PlayerId] = (main.AirshipMeetingTimer[__instance.PlayerId] + Time.fixedDeltaTime);
                    }
                }*/

                if (GameStates.IsInGame) LoversSuicide();

                if (GameStates.IsInTask && Main.ArsonistTimer.ContainsKey(player.PlayerId))//アーソニストが誰かを塗っているとき
                {
                    if (!player.IsAlive())
                    {
                        Main.ArsonistTimer.Remove(player.PlayerId);
                        Utils.NotifyRoles(SpecifySeer: __instance);
                        RPC.ResetCurrentDousingTarget(player.PlayerId);
                    }
                    else
                    {
                        var ar_target = Main.ArsonistTimer[player.PlayerId].Item1;//塗られる人
                        var ar_time = Main.ArsonistTimer[player.PlayerId].Item2;//塗った時間
                        if (!ar_target.IsAlive())
                        {
                            Main.ArsonistTimer.Remove(player.PlayerId);
                        }
                        else if (ar_time >= Options.ArsonistDouseTime.GetFloat())//時間以上一緒にいて塗れた時
                        {
                            player.SetKillCooldown();
                            Main.ArsonistTimer.Remove(player.PlayerId);//塗が完了したのでDictionaryから削除
                            Main.isDoused[(player.PlayerId, ar_target.PlayerId)] = true;//塗り完了
                            player.RpcSetDousedPlayer(ar_target, true);
                            Utils.NotifyRoles();//名前変更
                            RPC.ResetCurrentDousingTarget(player.PlayerId);
                        }
                        else
                        {
                            float dis;
                            dis = Vector2.Distance(player.transform.position, ar_target.transform.position);//距離を出す
                            if (dis <= 1.75f)//一定の距離にターゲットがいるならば時間をカウント
                            {
                                Main.ArsonistTimer[player.PlayerId] = (ar_target, ar_time + Time.fixedDeltaTime);
                            }
                            else//それ以外は削除
                            {
                                Main.ArsonistTimer.Remove(player.PlayerId);
                                Utils.NotifyRoles(SpecifySeer: __instance);
                                RPC.ResetCurrentDousingTarget(player.PlayerId);

                                Logger.Info($"Canceled: {__instance.GetNameWithRole()}", "Arsonist");
                            }
                        }

                    }
                }
                if (GameStates.IsInTask && Main.PuppeteerList.ContainsKey(player.PlayerId))
                {
                    if (!player.IsAlive())
                    {
                        Main.PuppeteerList.Remove(player.PlayerId);
                    }
                    else
                    {
                        Vector2 puppeteerPos = player.transform.position;//PuppeteerListのKeyの位置
                        Dictionary<byte, float> targetDistance = new();
                        float dis;
                        foreach (var target in Main.AllAlivePlayerControls)
                        {
                            if (target.PlayerId != player.PlayerId && !target.Is(CountTypes.Impostor))
                            {
                                dis = Vector2.Distance(puppeteerPos, target.transform.position);
                                targetDistance.Add(target.PlayerId, dis);
                            }
                        }
                        if (targetDistance.Count() != 0)
                        {
                            var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                            PlayerControl target = Utils.GetPlayerById(min.Key);
                            var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                            if (min.Value <= KillRange && player.CanMove && target.CanMove)
                            {
                                var puppeteerId = Main.PuppeteerList[player.PlayerId];
                                RPC.PlaySoundRPC(puppeteerId, Sounds.KillSound);
                                target.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                                player.RpcMurderPlayer(target);
                                Utils.MarkEveryoneDirtySettings();
                                Main.PuppeteerList.Remove(player.PlayerId);
                                Utils.NotifyRoles();
                            }
                        }
                    }
                }
                if (GameStates.IsInTask && player == PlayerControl.LocalPlayer)
                    DisableDevice.FixedUpdate();
                //TOH_Y
                if (GameStates.IsInTask && player == PlayerControl.LocalPlayer)
                    AntiAdminer.FixedUpdate();

                if (GameStates.IsInGame && Main.RefixCooldownDelay <= 0)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.Vampire) || pc.Is(CustomRoles.Warlock))
                            Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown * 2;
                    }

                if (__instance.AmOwner) Utils.ApplySuffix();
            }
            //LocalPlayer専用
            if (__instance.AmOwner)
            {
                //キルターゲットの上書き処理
                if (GameStates.IsInTask && !(__instance.Is(CustomRoleTypes.Impostor) || __instance.Is(CustomRoles.Egoist)) && __instance.CanUseKillButton() && !__instance.Data.IsDead)
                {
                    var players = __instance.GetPlayersInAbilityRangeSorted(false);
                    PlayerControl closest = players.Count <= 0 ? null : players[0];
                    HudManager.Instance.KillButton.SetTarget(closest);
                }
            }

            //役職テキストの表示
            var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (RoleText != null && __instance != null)
            {
                if (GameStates.IsLobby)
                {
                    if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                    {
                        if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                            __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                        else if (Main.version.CompareTo(ver.version) == 0)
                            __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{__instance.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
                        else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                    }
                    else __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                }
                if (GameStates.IsInGame)
                {
                    //変数定義
                    var seer = PlayerControl.LocalPlayer;
                    var target = __instance;

                    var RoleTextData = Utils.GetRoleText(target.PlayerId);
                    var SubRoleText = target.GetDisplaySubRoleName();
                    //if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                    //{
                    //    var hasRole = main.AllPlayerCustomRoles.TryGetValue(__instance.PlayerId, out var role);
                    //    if (hasRole) RoleTextData = Utils.GetRoleTextHideAndSeek(__instance.Data.Role.Role, role);
                    //}
                    RoleText.text = RoleTextData.Item1;
                    RoleText.color = RoleTextData.Item2;
                    if (target.Is(CustomRoles.Rainbow)) RoleText.text = GetString("RainbowResize");

                    if (Options.CurrentGameMode.IsOneNightMode())
                    {
                        if (Main.DefaultRole[target.PlayerId] != CustomRoles.ONPhantomThief)
                        {
                            RoleText.text = Utils.GetRoleName(Main.DefaultRole[target.PlayerId]);
                            RoleText.color = Utils.GetRoleColor(Main.DefaultRole[target.PlayerId]);
                        }
                    }
                    RoleText.text += ONPhantomThief.GetChangeMark(seer, target);


                    if (Options.CurrentGameMode.IsOneNightMode() && !seer.IsAlive()) RoleText.text = SubRoleText + RoleText.text;
                    else if (seer == target || !seer.IsAlive()) RoleText.text = SubRoleText + RoleText.text;

                    if (target.AmOwner) RoleText.enabled = true; //自分ならロールを表示
                    else if (Main.VisibleTasksCount && seer.Data.IsDead && !Options.GhostCanSeeOtherRoles.GetBool())
                        RoleText.enabled = true; //他プレイヤーでVisibleTasksCountが有効なおかつ自分が死んでいるならロールを表示
                    else RoleText.enabled = false; //そうでなければロールを非表示
                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                    {
                        RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                        if (!target.AmOwner) target.cosmetics.nameText.text = target?.Data?.PlayerName;
                    }
                    //TOH_Y
                    if (((target.Is(CustomRoles.Rainbow) && !Options.RainbowDontSeeTaskTurn.GetBool()) || (target.Is(CustomRoles.Workaholic) && Options.WorkaholicSeen.GetBool())
                        ) && !seer.Data.IsDead)
                    {
                        RoleText.enabled = true;
                    }

                    if (Lawyer.IsWatchTargetRole(seer, target)) RoleText.enabled = true;
                    if (ONDiviner.IsShowTargetRole(seer, target)) RoleText.enabled = true;
                    if (EvilDiviner.IsShowTargetRole(seer, target)) RoleText.enabled = true;

                    if (Main.VisibleTasksCount)//他プレイヤーでVisibleTasksCountは有効なら
                    {
                        if ((seer.Is(CustomRoles.TaskManager) || seer.Is(CustomRoles.Management)) && seer == target)
                            RoleText.text += Utils.GetTMtaskCountText(__instance);

                        if ((target.Is(CustomRoles.Workaholic) && Options.WorkaholicTaskSeen.GetBool())
                            || seer == target
                            || !seer.IsAlive())
                            RoleText.text += Utils.GetProgressText(__instance); //ロールの横にタスクなど進行状況表示
                    }

                    //名前変更
                    string RealName = target.GetRealName();


                    //名前色変更処理
                    //NameColorManager準拠の処理
                    //RealName = RealName.ApplyNameColorData(seer, target, false);

                    //自分自身の名前を変更(色付き)
                    if (target.AmOwner && AmongUsClient.Instance.IsGameStarted) //targetが自分自身
                    {
                        if (target.Is(CustomRoles.Arsonist) && target.IsDouseDone())
                            RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), GetString("EnterVentToWin"));
                        else if (target.Is(CustomRoles.SeeingOff) || target.Is(CustomRoles.Sending))
                            RealName = SeeingOff.RealNameChange(RealName);
                        else if (Options.CurrentGameMode.IsCatMode())
                        {
                            if (seer.Is(CustomRoles.Crewmate))
                                RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.CatNoCat), GetString("CatNoCatInfo"));
                            else
                                RealName = Utils.ColorString(seer.GetRoleColor(), seer.GetRoleInfo());
                        }
                        else
                        {
                            //NameColorManager準拠の処理
                            RealName = RealName.ApplyNameColorData(seer, target, false);
                        }
                    }
                    else
                    {
                        //NameColorManager準拠の処理
                        RealName = RealName.ApplyNameColorData(seer, target, false);
                    }

                    Mark.Clear();
                    Suffix.Clear();

                    if (seer.GetCustomRole().IsImpostor()) //seerがインポスター
                    {
                        if (target.Is(CustomRoles.MadSnitch) && target.GetPlayerTaskState().IsTaskFinished && Options.MadSnitchCanAlsoBeExposedToImpostor.GetBool()) //targetがタスクを終わらせたマッドスニッチ
                            Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.MadSnitch), "★")); //targetにマーク付与
                    }

                    //インポスター/キル可能なニュートラルがタスクが終わりそうなSnitchを確認できる
                    Mark.Append(Snitch.GetWarningMark(seer, target));

                    Mark.Append(Hunter.TargetMark(seer, target));
                    Mark.Append(Executioner.TargetMark(seer, target));
                    Mark.Append(Lawyer.TargetMark(seer, target));
                    Mark.Append(Utils.AntiCompMark(seer, target));
                    Mark.Append(Medic.GetGuardMark(seer.PlayerId, target));
                    Mark.Append(Totocalcio.GetBetMark(seer.PlayerId, target));

                    if (Main.BaitKillPlayer != 253 && seer == target && seer == Utils.GetPlayerById(Main.BaitKillPlayer))
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Bait)}>！</color>");
                    }

                    if (seer.Is(CustomRoles.Arsonist))
                    {
                        if (seer.IsDousedPlayer(target))
                        {
                            Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>");
                        }
                        else if (
                            Main.currentDousingTarget != 255 &&
                            Main.currentDousingTarget == target.PlayerId
                        )
                        {
                            Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Arsonist)}>△</color>");
                        }
                    }
                    if (seer.Is(CustomRoles.Puppeteer))
                    {
                        if (seer.Is(CustomRoles.Puppeteer) &&
                        Main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                        Main.PuppeteerList.ContainsKey(target.PlayerId))
                            Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>◆</color>");
                    }
                    if (Sniper.IsEnable && target.AmOwner)
                    {
                        //銃声が聞こえるかチェック
                        Mark.Append(Sniper.GetShotNotify(target.PlayerId));

                    }
                    if (seer.Is(CustomRoles.EvilTracker)) Mark.Append(EvilTracker.GetTargetMark(seer, target));
                    //タスクが終わりそうなSnitchがいるとき、インポスター/キル可能なニュートラルに警告が表示される
                    Mark.Append(Snitch.GetWarningArrow(seer, target));

                    //ハートマークを付ける(会議中MOD視点)
                    if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Lovers))
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                    }
                    else if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                    }

                    if (seer.Is(CustomRoles.Sympathizer) && target.Is(CustomRoles.Sympathizer)
                        && seer.GetPlayerTaskState().SympaTask && target.GetPlayerTaskState().SympaTask)
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Sympathizer)}>◎</color>");
                    }

                    //矢印オプションありならタスクが終わったスニッチはインポスター/キル可能なニュートラルの方角がわかる
                    Suffix.Append(Snitch.GetSnitchArrow(seer, target));
                    Suffix.Append(ONDeadTargetArrow.GetDeadBodiesArrow(seer, target));
                    Suffix.Append(BountyHunter.GetTargetArrow(seer, target));
                    Suffix.Append(EvilTracker.GetTargetArrow(seer, target));
                    Suffix.Append(Telepathisters.GetTargetArrow(seer, target));

                    if (seer.Is(CustomRoles.AntiAdminer))
                    {
                        AntiAdminer.FixedUpdate();
                        if (target.AmOwner)
                        {
                            //MODなら矢印表示
                            if (AntiAdminer.IsAdminWatch) Suffix.Append("★").Append(GetString("AntiAdminerAD"));
                            if (AntiAdminer.IsVitalWatch) Suffix.Append("★").Append(GetString("AntiAdminerVI"));
                            if (AntiAdminer.IsDoorLogWatch) Suffix.Append("★").Append(GetString("AntiAdminerDL"));
                            if (AntiAdminer.IsCameraWatch) Suffix.Append("★").Append(GetString("AntiAdminerCA"));
                        }
                    }

                    /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                        Mark = isBlocked ? "(true)" : "(false)";
                    }*/
                    if (Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                        RealName = $"<size=0>{RealName}</size> ";

                    string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})" : "";
                    //Mark・Suffixの適用
                    target.cosmetics.nameText.text = $"{RealName}{DeathReason}{Mark}";

                    if (Suffix.ToString() != "" || Options.CurrentGameMode.IsOneNightMode())
                    {
                        //名前が2行になると役職テキストを上にずらす必要がある
                        RoleText.transform.SetLocalY(0.35f);
                        target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();

                    }
                    else
                    {
                        //役職テキストの座標を初期値に戻す
                        RoleText.transform.SetLocalY(0.2f);
                    }
                }
                else
                {
                    //役職テキストの座標を初期値に戻す
                    RoleText.transform.SetLocalY(0.2f);
                }
            }
        }
        //FIXME: 役職クラス化のタイミングで、このメソッドは移動予定
        public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if ((CustomRoles.Lovers.IsEnable()||CustomRoles.PlatonicLover.IsEnable()) && Main.isLoversDead == false)
            {
                foreach (var loversPlayer in Main.LoversPlayers)
                {
                    //生きていて死ぬ予定でなければスキップ
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    Main.isLoversDead = true;
                    foreach (var partnerPlayer in Main.LoversPlayers)
                    {
                        //本人ならスキップ
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                        //残った恋人を全て殺す(2人以上可)
                        //生きていて死ぬ予定もない場合は心中
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
                            if (isExiled)
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer);
                        }
                    }
                }
            }
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
            roleText.fontSize -= 1.2f;
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
            if (AmongUsClient.Instance.IsGameStarted && Options.CurrentGameMode.IsCatMode())
            {
                //ゲーム中に色を変えた場合
                __instance.RpcMurderPlayer(__instance);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    class EnterVentPatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
        {

            Witch.OnEnterVent(pc);
            Medic.OnEnterVent(pc);
            GrudgeSheriff.OnEnterVent(pc);
            Psychic.OnEnterVent(pc);
            if (pc.Is(CustomRoles.Mayor))
            {
                if (Main.MayorUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt())
                {
                    pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                    pc?.ReportDeadBody(null);
                }
            }
            if (pc.Is(CustomRoles.Chairman))
            {
                if (Main.ChairmanUsedButtonCount.TryGetValue(pc.PlayerId, out var Ccount) && Ccount < Options.ChairmanNumOfUseButton.GetInt())
                {
                    pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                    pc?.ReportDeadBody(null);
                }
            }
            if (pc.Is(CustomRoles.Telepathisters) && Telepathisters.VentCountLimit > 0)
            {
                Telepathisters.VentCountLimit--;
                Telepathisters.SubNotifyRoles();
            }
            if (pc.Is(CustomRoles.MadNatureCalls))
            {
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 79);
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 80);
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 81);
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 82);
            }
            if (pc.Is(CustomRoles.MadBrackOuter))
            {
                if (!AmongUsClient.Instance.AmHost) return;

                MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, pc.GetClientId());
                SabotageFixWriter.Write((byte)SystemTypes.Electrical);
                SabotageFixWriter.WriteNetObject(pc);
                AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);

                foreach (var target in Main.AllPlayerControls)
                {
                    if (target == pc || target.Data.Disconnected) continue;
                    SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, target.GetClientId());
                    SabotageFixWriter.Write((byte)SystemTypes.Electrical);
                    SabotageFixWriter.WriteNetObject(target);
                    AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
                }
            }
        }

    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                //if (Options.CurrentGameMode == CustomGameMode.CatchCat && Options.IgnoreVent.GetBool())
                //    __instance.RpcBootFromVent(id);下に移動
                PlayerControl usePlayer = __instance.myPlayer;
                byte usePlayerId = usePlayer.PlayerId;

                if (AmongUsClient.Instance.IsGameStarted &&
                    usePlayer.IsDouseDone())
                {
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (pc != usePlayer)
                        {
                            //生存者は焼殺
                            pc.SetRealKiller(usePlayer);
                            pc.RpcMurderPlayer(pc);
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                            Main.PlayerStates[pc.PlayerId].SetDead();
                        }
                        else
                            RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
                    }
                    CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
                    CustomWinnerHolder.WinnerIds.Add(usePlayerId);
                    return true;
                }

                if (usePlayer.Is(CustomRoles.Telepathisters) && Telepathisters.VentCountLimit == 0)
                {
                    Telepathisters.VentCountLimit--;
                }

                if ((usePlayer.Data.Role.Role != RoleTypes.Engineer  //エンジニアでなく
                    && !usePlayer.CanUseImpostorVentButton()) //インポスターベントも使えない
                    || (usePlayer.Is(CustomRoles.Mayor) && Main.MayorUsedButtonCount
                        .TryGetValue(usePlayerId, out var count) && count >= Options.MayorNumOfUseButton.GetInt())
                    || (usePlayer.Is(CustomRoles.Chairman) && Main.ChairmanUsedButtonCount
                        .TryGetValue(usePlayerId, out var Ccount) && Ccount >= Options.ChairmanNumOfUseButton.GetInt())
                    || (usePlayer.Is(CustomRoles.Medic) && Medic.UseVent.TryGetValue(usePlayerId, out var MedicCanUse) && !MedicCanUse)
                    || (usePlayer.Is(CustomRoles.Psychic) && !Psychic.CanUseVent(usePlayerId))
                    || (usePlayer.Is(CustomRoles.GrudgeSheriff) && (!GrudgeSheriff.CanUseKillButton(usePlayerId) || GrudgeSheriff.KillWaitPlayer.ContainsKey(usePlayerId)))
                    || (Options.CurrentGameMode.IsCatMode() && Options.IgnoreVent.GetBool())
                )
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                    writer.WritePacked(127);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    new LateTask(() =>
                    {
                        int clientId = usePlayer.GetClientId();
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.5f, "Fix DesyncImpostor Stuck");
                    return false;
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
    [HarmonyPatch(typeof(GameData), nameof(GameData.CompleteTask))]
    class GameDataCompleteTaskPatch
    {
        public static void Postfix(PlayerControl pc)
        {
            Logger.Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
            Main.PlayerStates[pc.PlayerId].UpdateTask(pc);
            Utils.NotifyRoles();
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class PlayerControlCompleteTaskPatch
    {
        public static bool Prefix(PlayerControl __instance)
        {
            if (Workhorse.OnCompleteTask(__instance)) //タスク勝利をキャンセル
                return false;
            return true;
        }
        public static void Postfix(PlayerControl __instance)
        {
            var pc = __instance;
            Snitch.OnCompleteTask(pc);
            CompreteCrew.OnCompleteTask(pc);
            ONDeadTargetArrow.CheckTask(pc);

            var isTaskFinish = pc.GetPlayerTaskState().IsTaskFinished;
            if (isTaskFinish && pc.Is(CustomRoles.MadSnitch))
            {
                foreach (var impostor in Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
                {
                    NameColorManager.Add(pc.PlayerId, impostor.PlayerId);
                }
                Utils.NotifyRoles(SpecifySeer: pc);
            }
            if (isTaskFinish && pc.Is(CustomRoles.ONMadFanatic))
            {
                foreach (var impostor in Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
                {
                    NameColorManager.Add(pc.PlayerId, impostor.PlayerId);
                }
                Utils.NotifyRoles(SpecifySeer: pc);
            }
            if (isTaskFinish && pc.Is(CustomRoles.JClient))
            {
                foreach (var jackal in Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoles.Jackal)))
                {
                    NameColorManager.Add(pc.PlayerId, jackal.PlayerId);
                }
                Utils.NotifyRoles(SpecifySeer: pc);
            }
            if (isTaskFinish && pc.Is(CustomRoles.LoveCutter) && Options.LoveCutterKnow.GetBool())
            {
                foreach (var killer in Main.AllAlivePlayerControls
                    .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.IsCrewKiller() || pc.IsNeutralKiller())
                    && !(pc.Is(CustomRoles.Arsonist) || pc.Is(CustomRoles.PlatonicLover) || pc.Is(CustomRoles.Totocalcio) || pc.Is(CustomRoles.MadSheriff))))
                {
                    NameColorManager.Add(pc.PlayerId, killer.PlayerId);
                }
                Utils.NotifyRoles(SpecifySeer: pc);
            }

            if (pc.Is(CustomRoles.AntiComplete) && isTaskFinish && !Main.AntiCompGuardCount[pc.PlayerId].Item2)
                Main.AntiCompGuardCount[pc.PlayerId] = (Main.AntiCompGuardCount[pc.PlayerId].Item1 + Options.AntiCompAddGuardCount.GetInt(), true);

            if ((isTaskFinish &&
                pc.GetCustomRole() is CustomRoles.Doctor) ||
                pc.GetCustomRole() is CustomRoles.SpeedBooster or CustomRoles.Lighter)
            {
                //ライターもしくはスピードブースターもしくはドクターがいる試合のみタスク終了時にCustomSyncAllSettingsを実行する
                Utils.MarkEveryoneDirtySettings();
            }
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
        public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType)
        {
            var target = __instance;
            var targetName = __instance.GetNameWithRole();
            Logger.Info($"{targetName} =>{roleType}", "PlayerControl.RpcSetRole");
            if (!ShipStatus.Instance.enabled) return true;
            if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
            {
                var targetIsKiller = target.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(target.PlayerId);
                var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();
                foreach (var seer in Main.AllPlayerControls)
                {
                    var self = seer.PlayerId == target.PlayerId;
                    var seerIsKiller = seer.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(seer.PlayerId);
                    if ((self && targetIsKiller) || (!seerIsKiller && target.Is(CustomRoleTypes.Impostor)))
                    {
                        ghostRoles[seer] = RoleTypes.ImpostorGhost;
                    }
                    else
                    {
                        ghostRoles[seer] = RoleTypes.CrewmateGhost;
                    }
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
            return true;
        }
    }

}
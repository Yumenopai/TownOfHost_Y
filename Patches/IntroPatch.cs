using System;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

using TownOfHostY.Roles.Core;
using static TownOfHostY.Translator;

namespace TownOfHostY;
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]

class SetUpRoleTextPatch
{
    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (!GameStates.IsModHost) return;
        _ = new LateTask(() =>
        {
            CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();
            if (!role.IsVanilla() && role != CustomRoles.Potentialist)
            {
                __instance.YouAreText.color = Utils.GetRoleColor(role);
                __instance.RoleText.text = Utils.GetRoleName(role);
                __instance.RoleText.color = Utils.GetRoleColor(role);
                __instance.RoleBlurbText.color = Utils.GetRoleColor(role);

                __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();
            }

            foreach (var subRole in PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SubRoles)
            {
                if (subRole == CustomRoles.ChainShifterAddon) continue;
                __instance.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(subRole), GetString($"{subRole}Info"));
            }
            __instance.RoleText.text = RoleText.GetRoleNameText(PlayerControl.LocalPlayer.PlayerId, showSubRole: false);

        }, 0.01f, "Override Role Text");

    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
class CoBeginPatch
{
    public static void Prefix()
    {
        try
        {
            var logger = Logger.Handler("Info");
            logger.Info("------------名前表示------------");
            foreach (var pc in Main.AllPlayerControls)
            {
                logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc.name.PadRightV2(20)}:{pc.cosmetics.nameText.text}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", "")})");
                pc.cosmetics.nameText.text = pc.name;
            }
            logger.Info("----------役職割り当て----------");
            foreach (var pc in Main.AllPlayerControls)
            {
                logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc?.Data?.PlayerName?.PadRightV2(20)}:{pc.GetAllRoleName().RemoveHtmlTags()}");
            }
            logger.Info("--------------環境--------------");
            foreach (var pc in Main.AllPlayerControls)
            {
                try
                {
                    var text = pc.AmOwner ? "[*]" : "   ";
                    text += $"{pc.PlayerId,-2}:{pc.Data?.PlayerName?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", ""),-11}";
                    if (Main.playerVersion.TryGetValue(pc.PlayerId, out PlayerVersion pv))
                        text += $":Mod({pv.forkId}/{pv.version}:{pv.tag})";
                    else text += ":Vanilla";
                    logger.Info(text);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "Platform");
                }
            }
            logger.Info("------------基本設定------------");
            var tmp = GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1);
            foreach (var t in tmp) logger.Info(t);
            logger.Info("------------詳細設定------------");
            foreach (var o in OptionItem.AllOptions)
                if (!o.IsHiddenOn(Options.CurrentGameMode) && (o.Parent == null ? !o.GetString().Equals("0%") : o.Parent.GetBool()))
                    logger.Info($"{(o.Parent == null ? o.Name.PadRightV2(40) : $"┗ {o.Name}".PadRightV2(41))}:{o.GetString().RemoveHtmlTags()}");
            logger.Info("-------------その他-------------");
            logger.Info($"プレイヤー数: {Main.AllPlayerControls.Count()}人");

            // タスク初期化
            Main.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));

            if (GameData.Instance != null)
            {
                GameData.Instance.RecomputeTaskCounts();
                TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
            }

            // NotifyRoles 呼び出し（try-catch で保護）
            try
            {
                Utils.NotifyRoles();
            }
            catch (Exception ex)
            {
                Logger.Error($"NotifyRoles で例外が発生しました: {ex.Message}", "CoBeginPatch");
                Logger.Exception(ex, "CoBeginPatch");
            }

            GameStates.InGame = true;
            Logger.Info("CoBegin completed: GameStates.InGame set to true", "CoBeginPatch");
        }
        catch (Exception ex)
        {
            Logger.Error($"CoBeginPatch.Prefix で予期しない例外が発生しました: {ex.Message}", "CoBeginPatch");
            Logger.Exception(ex, "CoBeginPatch");
            GameStates.InGame = true; // 最低限ゲーム開始状態にする
        }
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
class BeginCrewmatePatch
{
    public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral) || PlayerControl.LocalPlayer.Is(CustomRoles.StrayWolf)
            || PlayerControl.LocalPlayer.GetCustomRole().IsCCLeaderRoles())
        {
            //ぼっち役職
            var soloTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            soloTeam.Add(PlayerControl.LocalPlayer);
            teamToDisplay = soloTeam;
        }
    }
    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        //チーム表示変更
        CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();

        if (role.GetRoleInfo()?.IntroSound is AudioClip introSound)
        {
            PlayerControl.LocalPlayer.Data.Role.IntroSound = introSound;
        }
        int numImpostors = Main.NormalOptions.NumImpostors;

        if (!Options.ChangeIntro.GetBool())
        {
            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Neutral:
                    __instance.TeamTitle.text = GetString("Neutral");
                    __instance.TeamTitle.color = Color.gray;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("NeutralInfo");
                    __instance.BackgroundBar.material.color = Color.gray;
                    StartFadeIntro(__instance, Color.gray, Utils.GetRoleColor(role));
                    break;

                case CustomRoleTypes.Madmate:
                    StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);

                    // Impostor の RoleBehaviour を探す
                    RoleBehaviour impostorRole = null;
                    foreach (var r in RoleManager.Instance.AllRoles)
                    {
                        if (r.Role == RoleTypes.Impostor)
                        {
                            impostorRole = r;
                            break;
                        }
                    }
                    if (impostorRole != null)
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = impostorRole.IntroSound;
                    break;
            }

            switch (role)
            {
                case CustomRoles.Jackal:
                case CustomRoles.JClient:
                    __instance.TeamTitle.text = Utils.GetRoleName(CustomRoles.Jackal);
                    __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.Jackal);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("TeamJackal");
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Jackal);
                    break;

                case CustomRoles.FoxSpirit:
                case CustomRoles.Immoralist:
                    __instance.TeamTitle.text = Utils.GetRoleName(CustomRoles.FoxSpirit);
                    __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.FoxSpirit);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("TeamFoxSpirit");
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.FoxSpirit);
                    break;

                case CustomRoles.MadSheriff:
                case CustomRoles.MadConnecter:
                case CustomRoles.jO:
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = numImpostors == 1
                        ? DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsS)
                        : string.Format(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsP), numImpostors);
                    __instance.ImpostorText.text = __instance.ImpostorText.text.Replace("[FF1919FF]", "<color=#FF1919FF>").Replace("[]", "</color>");
                    break;
            }
        }
        else
        {
            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Neutral:
                    __instance.TeamTitle.text = Utils.GetRoleName(role);
                    __instance.TeamTitle.color = Utils.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = role switch
                    {
                        CustomRoles.Egoist => GetString("TeamEgoist"),
                        CustomRoles.Jackal => GetString("TeamJackal"),
                        CustomRoles.JSidekick => GetString("TeamJackal"),
                        CustomRoles.JClient => GetString("TeamJackal"),
                        _ => GetString("NeutralInfo"),
                    };
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                    break;

                case CustomRoleTypes.Madmate:
                    __instance.TeamTitle.text = GetString("Madmate");
                    __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.Madmate);
                    __instance.ImpostorText.text = GetString("TeamImpostor");
                    StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);
                    break;
            }
        }

        switch (role)
        {
            case CustomRoles.StrayWolf:
                __instance.TeamTitle.text = GetString("Impostor");
                __instance.TeamTitle.color = Palette.ImpostorRed;
                __instance.ImpostorText.gameObject.SetActive(false);
                __instance.BackgroundBar.material.color = Palette.ImpostorRed;
                break;

            case CustomRoles.Sheriff:
            case CustomRoles.Hunter:
            case CustomRoles.SillySheriff:
                __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = numImpostors == 1
                    ? GetString(StringNames.NumImpostorsS)
                    : string.Format(GetString(StringNames.NumImpostorsP), numImpostors);
                __instance.ImpostorText.text = __instance.ImpostorText.text.Replace("[FF1919FF]", "<color=#FF1919FF>").Replace("[]", "</color>");
                break;

            case CustomRoles.GM:
                __instance.TeamTitle.text = Utils.GetRoleName(role);
                __instance.TeamTitle.color = Utils.GetRoleColor(role);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                __instance.ImpostorText.gameObject.SetActive(false);
                break;
        }

        if (Options.IsCCMode)
        {
            if (role.IsCCLeaderRoles())
            {
                __instance.TeamTitle.text = GetString("CCLeaderIntro");
                __instance.TeamTitle.color = Color.red;
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("CCLeaderIntro2");
                __instance.BackgroundBar.material.color = Color.red;
                __instance.RoleBlurbText.text = GetString("CCLeaderIntro2");

                RoleBehaviour impostorRole = null;
                foreach (var r in RoleManager.Instance.AllRoles)
                {
                    if (r.Role == RoleTypes.Impostor)
                    {
                        impostorRole = r;
                        break;
                    }
                }
                if (impostorRole != null)
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = impostorRole.IntroSound;
            }
            else
            {
                __instance.TeamTitle.text = GetString("CCNoCatIntro");
                __instance.TeamTitle.color = Utils.GetRoleColor(role);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("CCNoCatIntro2");
                __instance.BackgroundBar.material.color = Color.white;
                __instance.RoleBlurbText.text = GetString("CCNoCatIntro2");

                RoleBehaviour crewmateRole = null;
                foreach (var r in RoleManager.Instance.AllRoles)
                {
                    if (r.Role == RoleTypes.Crewmate)
                    {
                        crewmateRole = r;
                        break;
                    }
                }
                if (crewmateRole != null)
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = crewmateRole.IntroSound;
            }
        }


        //else if (Options.IsONMode)
        //{
        //    if (role.IsONImpostor())
        //    {
        //        __instance.TeamTitle.text = GetString("Wteam");
        //        __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);
        //        __instance.ImpostorText.gameObject.SetActive(true);
        //        __instance.ImpostorText.text = GetString("WteamInfo");
        //        __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);

        //        __instance.RoleBlurbText.text = GetString("CatLeaderIntro2");
        //        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
        //    }
        //    else if (role.IsONMadmate())
        //    {
        //        __instance.TeamTitle.text = GetString("Wteam");
        //        __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);
        //        __instance.ImpostorText.gameObject.SetActive(true);
        //        __instance.ImpostorText.text = GetString("ONMadmanInfo");
        //        __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);
        //    }
        //    else if (role.IsONCrewmate())
        //    {
        //        __instance.TeamTitle.text = GetString("Vteam");
        //        __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.ONVillager);
        //        __instance.ImpostorText.gameObject.SetActive(true);
        //        __instance.ImpostorText.text = GetString("VteamInfo");
        //        __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.ONVillager);
        //    }
        //    else if (role.IsONNeutral())
        //    {
        //        __instance.TeamTitle.text = Utils.GetRoleName(role);
        //        __instance.TeamTitle.color = Utils.GetRoleColor(role);
        //        __instance.ImpostorText.gameObject.SetActive(true);
        //        __instance.ImpostorText.text = PlayerControl.LocalPlayer.GetRoleInfo();
        //        __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
        //    }
        //}

        if (Input.GetKey(KeyCode.RightShift))
        {
            __instance.TeamTitle.text = Main.ModName;
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "https://github.com/tukasa0001/TownOfHost" +
                "\r\nOut Now on Github";
            __instance.TeamTitle.color = Color.cyan;
            StartFadeIntro(__instance, Color.cyan, Color.yellow);
        }
        if (Input.GetKey(KeyCode.RightControl))
        {
            __instance.TeamTitle.text = "Discord Server";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "https://discord.gg/v8SFfdebpz";
            __instance.TeamTitle.color = Color.magenta;
            StartFadeIntro(__instance, Color.magenta, Color.magenta);
        }
    }
    private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end)
    {
        await Task.Delay(2000);
        int milliseconds = 0;
        while (true)
        {
            await Task.Delay(20);
            milliseconds += 20;
            float time = (float)milliseconds / (float)500;
            Color LerpingColor = Color.Lerp(start, end, time);
            if (__instance == null || milliseconds > 500)
            {
                Logger.Info("ループを終了します", "StartFadeIntro");
                break;
            }
            __instance.BackgroundBar.material.color = LerpingColor;
        }
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
class BeginImpostorPatch
{
    public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        if (PlayerControl.LocalPlayer.Is(CustomRoles.Sheriff)
            ||PlayerControl.LocalPlayer.Is(CustomRoles.Hunter)
            ||PlayerControl.LocalPlayer.Is(CustomRoles.SillySheriff)
            ||PlayerControl.LocalPlayer.Is(CustomRoles.jO))
        {
            //シェリフ等の場合はキャンセルしてBeginCrewmateに繋ぐ
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls)
            {
                if (!pc.AmOwner) yourTeam.Add(pc);
            }
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = Palette.CrewmateBlue;
            return false;
        }
        BeginCrewmatePatch.Prefix(__instance, ref yourTeam);
        return true;
    }
    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
class IntroCutsceneDestroyPatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        if (!GameStates.IsInGame) return;
        Main.introDestroyed = true;
        Logger.Info("IntroCutscene.OnDestroy: introDestroyed set to true", "IntroCutscene");

        // Wrap entire host-specific block to prevent exceptions from bubbling into native trampolines
        try
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                Logger.Info("Not host - skipping host-specific OnDestroy actions", "IntroCutscene");
                return;
            }

            Logger.Info("Host-specific OnDestroy actions starting", "IntroCutscene");

            // Reset ability cooldowns for all players (safe iteration)
            if (Main.NormalOptions == null)
            {
                Logger.Warn("Main.NormalOptions is null during IntroCutscene.OnDestroy", "IntroCutscene");
            }

            if (Main.NormalOptions == null || Main.NormalOptions.MapId != 4)
            {
                Logger.Info("Resetting ability cooldowns for all players (if available)", "IntroCutscene");
                try
                {
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc == null) continue;
                        try { pc.RpcResetAbilityCooldown(); } catch (Exception ex) { Logger.Warn($"RpcResetAbilityCooldown failed for {pc.PlayerId}: {ex.Message}", "IntroCutscene"); }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed iterating AllPlayerControls: {ex.Message}", "IntroCutscene");
                }

                if (Options.FixFirstKillCooldown.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        try
                        {
                            Logger.Info("Applying first kill cooldown fix (delayed)", "IntroCutscene");
                            if (Main.AllPlayerKillCooldown != null)
                            {
                                foreach (var pc in Main.AllPlayerControls)
                                {
                                    if (pc == null) continue;
                                    try
                                    {
                                        if (Main.AllPlayerKillCooldown.TryGetValue(pc.PlayerId, out var val))
                                            pc.SetKillCooldown(val - 2f);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Warn($"SetKillCooldown failed for {pc.PlayerId}: {ex.Message}", "IntroCutscene");
                                    }
                                }
                            }
                            else
                            {
                                Logger.Warn("Main.AllPlayerKillCooldown is null; skipping FixFirstKillCooldown", "IntroCutscene");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception(ex, "IntroCutscene");
                        }
                    }, 2f, "FixKillCooldownTask");
                }
            }

            // GM exile: guard LocalPlayer
            var local = PlayerControl.LocalPlayer;
            if (local != null)
            {
                try
                {
                    if (local.Is(CustomRoles.GM))
                    {
                        Logger.Info("Local player is GM: Exiling host", "IntroCutscene");
                        try { local.RpcExile(); } catch (Exception ex) { Logger.Warn($"RpcExile failed: {ex.Message}", "IntroCutscene"); }
                        try { PlayerState.GetByPlayerId(local.PlayerId)?.SetDead(); } catch (Exception ex) { Logger.Warn($"SetDead failed for GM: {ex.Message}", "IntroCutscene"); }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Checking GM failed: {ex.Message}", "IntroCutscene");
                }
            }
            else
            {
                Logger.Warn("PlayerControl.LocalPlayer is null in IntroCutscene.OnDestroy", "IntroCutscene");
            }

            // 初手のランダムスポーン (safe checks)
            try
            {
                if (Main.NormalOptions != null)
                {
                    switch ((MapNames)Main.NormalOptions.MapId)
                    {
                        case MapNames.Skeld:
                            if (Options.RandomSpawn_Skeld.GetBool() && !Options.FirstFixedSpawn_Skeld.GetBool())
                            {
                                Logger.Info("Applying random spawn for Skeld", "IntroCutscene");
                                Main.AllPlayerControls.Do(new RandomSpawn.SkeldSpawnMap().RandomTeleport);
                            }
                            break;
                        case MapNames.MiraHQ:
                            if (Options.RandomSpawn_MiraHQ.GetBool() && !Options.FirstFixedSpawn_MiraHQ.GetBool())
                            {
                                Logger.Info("Applying random spawn for MiraHQ", "IntroCutscene");
                                Main.AllPlayerControls.Do(new RandomSpawn.MiraHQSpawnMap().RandomTeleport);
                            }
                            break;
                        case MapNames.Polus:
                            if (Options.RandomSpawn_Polus.GetBool() && !Options.FirstFixedSpawn_Polus.GetBool())
                            {
                                Logger.Info("Applying random spawn for Polus", "IntroCutscene");
                                Main.AllPlayerControls.Do(new RandomSpawn.PolusSpawnMap().RandomTeleport);
                            }
                            break;
                        case MapNames.Fungle:
                            if (Options.RandomSpawn_Fungle.GetBool() && !Options.FirstFixedSpawn_Fungle.GetBool())
                            {
                                Logger.Info("Applying random spawn for Fungle", "IntroCutscene");
                                Main.AllPlayerControls.Do(new RandomSpawn.FungleSpawnMap().RandomTeleport);
                            }
                            break;
                    }
                }
                else
                {
                    Logger.Warn("Main.NormalOptions is null; skipping random spawn", "IntroCutscene");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Random spawn handling failed: {ex.Message}", "IntroCutscene");
            }

            // Desync impostor handling (guard local)
            try
            {
                if (local != null)
                {
                    var roleInfo = local.GetCustomRole().GetRoleInfo();
                    var amDesyncImpostor = roleInfo?.IsDesyncImpostor == true;
                    if (amDesyncImpostor)
                    {
                        try
                        {
                            local.Data.Role.AffectedByLightAffectors = false;
                            Logger.Info("Host is desync impostor: AffectedByLightAffectors set to false", "IntroCutscene");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Failed to set AffectedByLightAffectors: {ex.Message}", "IntroCutscene");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Desync impostor handling failed: {ex.Message}", "IntroCutscene");
            }
        }
        catch (Exception ex)
        {
            // Log but do not rethrow to avoid breaking native trampoline
            Logger.Error($"IntroCutscene.OnDestroy encountered exception: {ex.Message}", "IntroCutscene");
            Logger.Exception(ex, "IntroCutscene");
        }

        Logger.Info("IntroCutscene.OnDestroy completed", "IntroCutscene");
    }
}
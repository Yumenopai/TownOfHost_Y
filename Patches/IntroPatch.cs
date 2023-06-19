using System;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
    class SetUpRoleTextPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            if (!GameStates.IsModHost) return;
            new LateTask(() =>
            {
                CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();
                if (!role.IsVanilla() && !(role.IsAddOnOnlyRole() && !role.IsAddOnOnlyMadmate()) && role != CustomRoles.Potentialist)
                {
                    __instance.YouAreText.color = Utils.GetRoleColor(role);
                    __instance.RoleText.text = Utils.GetRoleName(role);
                    __instance.RoleText.color = Utils.GetRoleColor(role);
                    __instance.RoleBlurbText.color = Utils.GetRoleColor(role);
                    __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();
                }
                else if (Options.CurrentGameMode.IsCatMode() && (role == CustomRoles.Crewmate))
                {
                    role = CustomRoles.CatNoCat;
                    __instance.YouAreText.color = Utils.GetRoleColor(role);
                    __instance.RoleText.text = Utils.GetRoleName(role);
                    __instance.RoleText.color = Utils.GetRoleColor(role);
                    __instance.RoleBlurbText.color = Utils.GetRoleColor(role);
                    __instance.RoleBlurbText.text = GetString("CatNoCatIntro2");
                }
                foreach (var subRole in Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SubRoles)
                    __instance.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(subRole), GetString($"{subRole}Info"));
                __instance.RoleText.text += Utils.GetSubRolesText(PlayerControl.LocalPlayer.PlayerId);

            }, 0.01f, "Override Role Text");

        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    class CoBeginPatch
    {
        public static void Prefix()
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
            Main.AllPlayerControls.Do(x => Main.PlayerStates[x.PlayerId].InitTask(x));
            GameData.Instance.RecomputeTaskCounts();
            TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;

            Utils.NotifyRoles();
            GameStates.InGame = true;
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    class BeginCrewmatePatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral))
            {
                //ぼっち役職
                var soloTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                soloTeam.Add(PlayerControl.LocalPlayer);
                teamToDisplay = soloTeam;
            }if (PlayerControl.LocalPlayer.IsLeaderKiller())
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

            if (!Options.ChangeIntro.GetBool())
            {
                switch (role.GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Neutral:
                        __instance.TeamTitle.text = GetString("Neutral");
                        __instance.TeamTitle.color = Color.gray;
                        //__instance.TeamTitle.text = Utils.GetRoleName(role);
                        //__instance.TeamTitle.color = Utils.GetRoleColor(role);
                        __instance.ImpostorText.gameObject.SetActive(true);
                        __instance.ImpostorText.text = GetString("NeutralInfo");
                        __instance.BackgroundBar.material.color = Color.gray;
                        if (!Options.CurrentGameMode.IsOneNightMode())
                            StartFadeIntro(__instance, Color.gray, Utils.GetRoleColor(role));
                        break;
                
                    case CustomRoleTypes.Madmate:
                        if (!Options.CurrentGameMode.IsOneNightMode())
                            StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
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

                    case CustomRoles.MadSheriff:
                        __instance.ImpostorText.gameObject.SetActive(true);
                        var numImpostors = Main.NormalOptions.NumImpostors;
                        __instance.ImpostorText.text = numImpostors == 1
                            ? DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsS)
                            : __instance.ImpostorText.text = string.Format(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsP), numImpostors);
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
                            CustomRoles.JClient => GetString("TeamJackal"),
                            _ => GetString("NeutralInfo"),
                        };
                        __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                        break;
                    case CustomRoleTypes.Madmate:
                        __instance.TeamTitle.text = GetString("Madmate");
                        __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.Madmate);
                        __instance.ImpostorText.text = GetString("TeamImpostor");
                        if(!Options.CurrentGameMode.IsOneNightMode())
                            StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                        break;
                }
            }
            switch (role)
            {
                case CustomRoles.Terrorist:
                    var sound = ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault()
                    .MinigamePrefab.OpenSound;
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = sound;
                    break;

                case CustomRoles.Executioner:
                case CustomRoles.Vampire:
                case CustomRoles.Opportunist:
                case CustomRoles.DarkHide:
                case CustomRoles.ONHangedMan:
                    if (role == CustomRoles.Opportunist && !Options.OpportunistCanKill.GetBool()) break;
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                    break;

                case CustomRoles.SabotageMaster:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                    break;

                case CustomRoles.Doctor:
                case CustomRoles.TaskManager:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Scientist);
                    break;

                case CustomRoles.Sheriff:
                case CustomRoles.Hunter:
                case CustomRoles.SillySheriff:
                case CustomRoles.ONDiviner:
                case CustomRoles.ONPhantomThief:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                    __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    var numImpostors = Main.NormalOptions.NumImpostors;
                    __instance.ImpostorText.text = numImpostors == 1
                        ? DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsS)
                        : __instance.ImpostorText.text = string.Format(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsP), numImpostors);
                    __instance.ImpostorText.text = __instance.ImpostorText.text.Replace("[FF1919FF]", "<color=#FF1919FF>").Replace("[]", "</color>");
                    break;

                case CustomRoles.Arsonist:
                case CustomRoles.Mayor:
                case CustomRoles.PlatonicLover:
                case CustomRoles.Totocalcio:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                    break;

                case CustomRoles.SchrodingerCat:
                case CustomRoles.JClient:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                    break;

                case CustomRoles.GM:
                    __instance.TeamTitle.text = Utils.GetRoleName(role);
                    __instance.TeamTitle.color = Utils.GetRoleColor(role);
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(false);
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                    break;

                /****************TOH_Y*******************/
                case CustomRoles.Sympathizer:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskUpdateSound;
                    break;

                case CustomRoles.Chairman:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.VentEnterSound;
                    break;

                //case CustomRoles.Medic:
                //    PlayerControl.LocalPlayer.Data.Role.IntroSound = PlayerControl.LocalPlayer.KillSfx;
                //    break;
            }

            if (Options.CurrentGameMode.IsCatMode())
            {
                if (role.IsCatLeaderRoles())
                {
                    __instance.TeamTitle.text = GetString("CatLeaderIntro");
                    __instance.TeamTitle.color = Color.red;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("CatLeaderIntro2");
                    __instance.BackgroundBar.material.color = Color.red;

                    __instance.RoleBlurbText.text = GetString("CatLeaderIntro2");
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                }
                else if (role == CustomRoles.Crewmate)
                {
                    __instance.TeamTitle.text = GetString("CatNoCatIntro");
                    __instance.TeamTitle.color = Utils.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("CatNoCatIntro2");
                    __instance.BackgroundBar.material.color = Color.white;
                }
            }
            else if (Options.CurrentGameMode.IsOneNightMode())
            {
                if (role.IsONImpostor())
                {
                    __instance.TeamTitle.text = GetString("Wteam");
                    __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("WteamInfo");
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);

                    __instance.RoleBlurbText.text = GetString("CatLeaderIntro2");
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                }
                else if (role.IsONMadmate())
                {
                    __instance.TeamTitle.text = GetString("Wteam");
                    __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("ONMadmanInfo");
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.ONWerewolf);
                }
                else if (role.IsONCrewmate())
                {
                    __instance.TeamTitle.text = GetString("Vteam");
                    __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.ONVillager);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("VteamInfo");
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.ONVillager);
                }
                else if (role.IsONNeutral())
                {
                    __instance.TeamTitle.text = Utils.GetRoleName(role);
                    __instance.TeamTitle.color = Utils.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = PlayerControl.LocalPlayer.GetRoleInfo();
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                }
            }

            //if (Input.GetKey(KeyCode.RightShift))
            //{
            //    __instance.TeamTitle.text = Main.ModName;
            //    __instance.ImpostorText.gameObject.SetActive(true);
            //    __instance.ImpostorText.text = "https://github.com/tukasa0001/TownOfHost" +
            //        "\r\nOut Now on Github";
            //    __instance.TeamTitle.color = Color.cyan;
            //    StartFadeIntro(__instance, Color.cyan, Color.yellow);
            //}
            //if (Input.GetKey(KeyCode.RightControl))
            //{
            //    __instance.TeamTitle.text = "Discord Server";
            //    __instance.ImpostorText.gameObject.SetActive(true);
            //    __instance.ImpostorText.text = "https://discord.gg/v8SFfdebpz";
            //    __instance.TeamTitle.color = Color.magenta;
            //    StartFadeIntro(__instance, Color.magenta, Color.magenta);
            //}
        }
        private static AudioClip GetIntroSound(RoleTypes roleType)
        {
            return RoleManager.Instance.AllRoles.Where((role) => role.Role == roleType).FirstOrDefault().IntroSound;
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
            if (PlayerControl.LocalPlayer.IsCrewKiller() && !PlayerControl.LocalPlayer.Is(CustomRoles.MadSheriff))
            {
                //シェリフの場合はキャンセルしてBeginCrewmateに繋ぐ
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
            if (AmongUsClient.Instance.AmHost)
            {
                if (Main.NormalOptions.MapId != 4)
                {
                    Main.AllPlayerControls.Do(pc => pc.RpcResetAbilityCooldown());
                    if (Options.FixFirstKillCooldown.GetBool() || Options.CurrentGameMode.IsOneNightMode())
                        new LateTask(() =>
                        {
                            PlayerControl.AllPlayerControls.ToArray().Do(pc => pc.SetKillCooldown(Main.AllPlayerKillCooldown[pc.PlayerId] - 2f));
                        }, 2f, "FixKillCooldownTask");
                }
                new LateTask(() => Main.AllPlayerControls.Do(pc => pc.RpcSetRoleDesync(RoleTypes.Shapeshifter, -3)), 2f, "SetImpostorForServer");
                if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
                {
                    PlayerControl.LocalPlayer.RpcExile();
                    Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
                }
                if (Options.RandomSpawn.GetBool())
                {
                    RandomSpawn.SpawnMap map;
                    switch (Main.NormalOptions.MapId)
                    {
                        case 0:
                            map = new RandomSpawn.SkeldSpawnMap();
                            Main.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                        case 1:
                            map = new RandomSpawn.MiraHQSpawnMap();
                            Main.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                    }
                }
            }
            Logger.Info("OnDestroy", "IntroCutscene");
        }
    }
}
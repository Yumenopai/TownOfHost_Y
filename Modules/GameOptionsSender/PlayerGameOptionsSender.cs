using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Linq;
using InnerNet;
using Mathf = UnityEngine.Mathf;
using UnhollowerBaseLib;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;

namespace TownOfHost.Modules
{
    public class PlayerGameOptionsSender : GameOptionsSender
    {
        public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
        public static void SetDirty(byte playerId) =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .Where(sender => sender.player.PlayerId == playerId)
            .ToList().ForEach(sender => sender.SetDirty());
        public static void SetDirtyToAll() =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .ToList().ForEach(sender => sender.SetDirty());

        public override IGameOptions BasedGameOptions =>
            Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
        public override bool IsDirty { get; protected set; }

        public PlayerControl player;

        public PlayerGameOptionsSender(PlayerControl player)
        {
            this.player = player;
        }
        public void SetDirty() => IsDirty = true;

        public override void SendGameOptions()
        {
            if (player.AmOwner)
            {
                var opt = BuildGameOptions();
                foreach (var com in GameManager.Instance.LogicComponents)
                {
                    if (com.TryCast<LogicOptions>(out var lo))
                        lo.SetGameOptions(opt);
                }
                GameOptionsManager.Instance.CurrentGameOptions = opt;
            }
            else base.SendGameOptions();
        }

        public override void SendOptionsArray(Il2CppStructArray<byte> optionArray)
        {
            for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
                {
                    SendOptionsArray(optionArray, i, player.GetClientId());
                }
            }
        }

        public static void RemoveSender(PlayerControl player)
        {
            var sender = AllSenders.OfType<PlayerGameOptionsSender>()
            .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
            if (sender == null) return;
            sender.player = null;
            AllSenders.Remove(sender);
        }
        public override IGameOptions BuildGameOptions()
        {
            if (Main.RealOptionsData == null)
            {
                Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
            }

            var opt = BasedGameOptions;
            AURoleOptions.SetOpt(opt);
            var state = Main.PlayerStates[player.PlayerId];
            opt.BlackOut(state.IsBlackOut);

            CustomRoles role = player.GetCustomRole();
            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    break;
                case CustomRoleTypes.Madmate:
                    AURoleOptions.EngineerCooldown = Options.MadmateVentCooldown.GetFloat();
                    AURoleOptions.EngineerInVentMaxTime = Options.MadmateVentMaxTime.GetFloat();
                    //if (Options.MadmateHasImpostorVision.GetBool())
                    //    opt.SetVision(true);
                    //if (Options.MadmateCanSeeOtherVotes.GetBool())
                    //    opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
            }

            switch (role)
            {
                case CustomRoles.SKMadmate:
                    if (Options.MadmateHasImpostorVision.GetBool())
                        opt.SetVision(true);
                    if (Options.MadmateCanSeeOtherVotes.GetBool())
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
                case CustomRoles.Terrorist:
                    AURoleOptions.EngineerCooldown = 0;
                    AURoleOptions.EngineerInVentMaxTime = 0;
                    break;
                case CustomRoles.ShapeMaster:
                    AURoleOptions.ShapeshifterCooldown = 0f;
                    AURoleOptions.ShapeshifterLeaveSkin = false;
                    AURoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                    break;
                case CustomRoles.Warlock:
                    AURoleOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.ApplyGameOptions(player);
                    break;
                case CustomRoles.BountyHunter:
                    BountyHunter.ApplyGameOptions();
                    break;
                case CustomRoles.EvilWatcher:
                case CustomRoles.NiceWatcher:
                    opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
                case CustomRoles.Sheriff:
                case CustomRoles.SillySheriff://TOH_Y
                case CustomRoles.Hunter:
                case CustomRoles.Arsonist:
                case CustomRoles.PlatonicLover:
                case CustomRoles.Totocalcio:
                    opt.SetVision(false);
                    break;
                case CustomRoles.Lighter:
                    if (player.GetPlayerTaskState().IsTaskFinished
                        || (player.GetPlayerTaskState().CompletedTasksCount >= Options.LighterTaskTrigger.GetInt()))
                    {
                        opt.SetFloat(
                            FloatOptionNames.CrewLightMod,
                            Options.LighterTaskCompletedVision.GetFloat());
                        if (Utils.IsActive(SystemTypes.Electrical) && Options.LighterTaskCompletedDisableLightOut.GetBool())
                        {
                            opt.SetFloat(
                            FloatOptionNames.CrewLightMod,
                            opt.GetFloat(FloatOptionNames.CrewLightMod) * 5);
                        }
                    }
                    break;
                case CustomRoles.EgoSchrodingerCat:
                    opt.SetVision(true);
                    break;
                case CustomRoles.Doctor:
                    AURoleOptions.ScientistCooldown = 0f;
                    AURoleOptions.ScientistBatteryCharge = Options.DoctorTaskCompletedBatteryCharge.GetFloat();
                    break;
                case CustomRoles.Mayor:
                    AURoleOptions.EngineerCooldown =
                        Main.MayorUsedButtonCount.TryGetValue(player.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt()
                        ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                        : 300f;
                    AURoleOptions.EngineerInVentMaxTime = 1;
                    break;
                case CustomRoles.Mare:
                    Mare.ApplyGameOptions(player.PlayerId);
                    break;
                case CustomRoles.EvilTracker:
                    EvilTracker.ApplyGameOptions(player.PlayerId);
                    break;
                case CustomRoles.Jackal:
                case CustomRoles.JSchrodingerCat:
                    Jackal.ApplyGameOptions(opt);
                    break;

                /****************TOH_Y****************/
                case CustomRoles.MadSheriff:
                    opt.SetVision(Options.AddOnRoleOptions[(CustomRoles.MadSheriff,CustomRoles.AddLight)].GetBool());
                    break;
                case CustomRoles.Opportunist:
                case CustomRoles.OSchrodingerCat:
                    opt.SetVision(Options.KOpportunistHasImpostorVision.GetBool());
                    break;
                case CustomRoles.Chairman:
                    AURoleOptions.EngineerCooldown =
                        Main.ChairmanUsedButtonCount.TryGetValue(player.PlayerId, out var Ccount) && Ccount < Options.ChairmanNumOfUseButton.GetInt()
                        ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                        : 300f;
                    AURoleOptions.EngineerInVentMaxTime = 1.0f;
                    break;
                case CustomRoles.Medic:
                    Medic.ApplyGameOptions(player.PlayerId);
                    break;
                case CustomRoles.GrudgeSheriff:
                    GrudgeSheriff.ApplyGameOptions(player.PlayerId);
                    break;
                case CustomRoles.Psychic:
                    Psychic.ApplyGameOptions(player.PlayerId);
                    break;
                case CustomRoles.Workaholic:
                    AURoleOptions.EngineerCooldown = Options.WorkaholicVentCooldown.GetFloat();
                    AURoleOptions.EngineerInVentMaxTime = 0.0f;
                    break;
                case CustomRoles.Express:
                    Main.AllPlayerSpeed[player.PlayerId] = Options.ExpressSpeed.GetFloat();
                    break;
                case CustomRoles.DarkHide:
                case CustomRoles.DSchrodingerCat:
                    DarkHide.ApplyGameOptions(opt);
                    break;
                case CustomRoles.Blinder:
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Options.BlinderVision.GetFloat());
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (Main.isBlindVision[pc.PlayerId])
                        {
                            opt.SetFloat(FloatOptionNames.CrewLightMod, Options.BlinderVision.GetFloat());
                            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.BlinderVision.GetFloat());
                            opt.SetVision(false);
                        }
                    }
                    break;
                case CustomRoles.Lawyer:
                case CustomRoles.Pursuer:
                    opt.SetVision(Lawyer.HasImpostorVision.GetBool());
                    break;
                case CustomRoles.JClient:
                    AURoleOptions.EngineerCooldown = Options.JClientVentCooldown.GetFloat();
                    AURoleOptions.EngineerInVentMaxTime = Options.JClientVentMaxTime.GetFloat();
                    break;
                case CustomRoles.CandleLighter:
                    CandleLighter.ApplyGameOptions(opt, player);
                    break;
            }

            foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles)
            {
                switch (subRole)
                {
                    case CustomRoles.AddWatch:
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        break;

                    case CustomRoles.AddLight:
                        opt.SetFloat(FloatOptionNames.CrewLightMod, opt.GetFloat(FloatOptionNames.CrewLightMod) + Options.AddLightAddCrewmateVision.GetFloat());
                        opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod) + Options.AddLightAddImpostorVision.GetFloat());

                        if (Utils.IsActive(SystemTypes.Electrical) && Options.AddLighterDisableLightOut.GetBool())
                            opt.SetFloat(FloatOptionNames.CrewLightMod,opt.GetFloat(FloatOptionNames.CrewLightMod) * 5);
                        break;

                    case CustomRoles.Sunglasses:
                        opt.SetFloat(FloatOptionNames.CrewLightMod, opt.GetFloat(FloatOptionNames.CrewLightMod) - Options.SunglassesSubCrewmateVision.GetFloat());
                        opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod) - Options.SunglassesSubImpostorVision.GetFloat());
                        break;
                }
            }

            if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
            {
                AURoleOptions.KillCooldown = Mathf.Max(0f, killCooldown);
            }
            if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
            {
                AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 3f);
            }

            state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
            if (!Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead)
                opt.SetBool(BoolOptionNames.AnonymousVotes, false);
            if (Options.AdditionalEmergencyCooldown.GetBool() &&
                Options.AdditionalEmergencyCooldownThreshold.GetInt() <= Utils.AllAlivePlayersCount)
            {
                opt.SetInt(Int32OptionNames.EmergencyCooldown,
                    Options.AdditionalEmergencyCooldownTime.GetInt());
            }
            if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
            {
                opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
            }
            if (Options.IsStandardHAS && Options.HideAndSeekKillDelayTimer > 0)
            {
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0f);
                if (player.Is(CountTypes.Impostor))
                {
                    AURoleOptions.PlayerSpeedMod = Main.MinSpeed;
                }
            }
            MeetingTimeManager.ApplyGameOptions(opt);

            AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
            AURoleOptions.ProtectionDurationSeconds = 0f;

            return opt;
        }
        public override bool AmValid()
        {
            return base.AmValid() && player != null && !player.Data.Disconnected && Main.RealOptionsData != null;
        }
    }
}
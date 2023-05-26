using AmongUs.GameOptions;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;

namespace TownOfHost
{
    static class CustomRolesHelper
    {
        public static bool IsImpostor(this CustomRoles role)
        {
            return
                role is CustomRoles.Impostor or
                CustomRoles.Shapeshifter or
                CustomRoles.BountyHunter or
                CustomRoles.Vampire or
                CustomRoles.Witch or
                CustomRoles.ShapeMaster or
                CustomRoles.Warlock or
                CustomRoles.SerialKiller or
                CustomRoles.Mare or
                CustomRoles.Puppeteer or
                CustomRoles.EvilWatcher or
                CustomRoles.TimeThief or
                CustomRoles.Mafia or
                CustomRoles.FireWorks or
                CustomRoles.Sniper or
                CustomRoles.EvilTracker or
                CustomRoles.ShapeKiller or
                //TOH_Y
                CustomRoles.Evilneko or
                CustomRoles.AntiAdminer or
                CustomRoles.CursedWolf or
                CustomRoles.Greedier or
                CustomRoles.Ambitioner or
                CustomRoles.Scavenger or
                CustomRoles.EvilDiviner or
                CustomRoles.Telepathisters or
                CustomRoles.NormalImpostor or

                //ON
                CustomRoles.ONWerewolf or
                CustomRoles.ONBigWerewolf or

                CustomRoles.CatRedLeader
                || role.IsAddOnOnlyImpostor()
                || role.IsAddOnOnlyShapeshifter();
        }
        public static bool IsMadmate(this CustomRoles role)
        {
            return
                role is CustomRoles.Madmate or
                CustomRoles.SKMadmate or
                CustomRoles.MadGuardian or
                CustomRoles.MadSnitch or

                //TOH_Y
                CustomRoles.MadDictator or
                CustomRoles.MadNatureCalls or
                CustomRoles.MadBrackOuter or
                CustomRoles.MadSheriff or

                CustomRoles.MSchrodingerCat
                || role.IsAddOnOnlyMadmate();
        }
        public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
        public static bool IsNeutral(this CustomRoles role)
        {
            return
                role is CustomRoles.Jester or
                CustomRoles.Opportunist or
                CustomRoles.SchrodingerCat or
                CustomRoles.Terrorist or
                CustomRoles.Executioner or
                CustomRoles.Arsonist or
                CustomRoles.Egoist or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.Jackal or
                CustomRoles.JSchrodingerCat or
                CustomRoles.JClient or
                //TOH_Y
                CustomRoles.OSchrodingerCat or
                CustomRoles.AntiComplete or
                CustomRoles.Workaholic or
                CustomRoles.DarkHide or
                CustomRoles.DSchrodingerCat or
                CustomRoles.LoveCutter or
                CustomRoles.PlatonicLover or
                CustomRoles.Lawyer or
                CustomRoles.Pursuer or
                CustomRoles.NBakery or
                CustomRoles.Totocalcio or

                CustomRoles.HASTroll or
                CustomRoles.HASFox;
        }
        public static bool IsCrewmate(this CustomRoles role) => !role.IsImpostorTeam() && !role.IsNeutral();
        public static bool IsVanilla(this CustomRoles role)
        {
            return
                role is CustomRoles.Crewmate or
                CustomRoles.Engineer or
                CustomRoles.Scientist or
                CustomRoles.GuardianAngel or
                CustomRoles.Impostor or
                CustomRoles.Shapeshifter;
        }
        public static bool IsTOHHASRole(this CustomRoles role) => role is CustomRoles.HASTroll or CustomRoles.HASFox;

        public static bool IsStanderdRole(this CustomRoles role)
        {
            return !role.IsTOHHASRole() && !role.IsCatCatchRole() && !role.IsOneNightRoles();
        }

        public static bool IsAddAddOn(this CustomRoles role)
        {
            return role.IsMadmate() || role.IsAddOnOnlyRole() ||
                role is CustomRoles.JClient;
        }
        public static bool IsAddOnOnlyRole(this CustomRoles role)
        {
            return role.IsAddOnOnlyImpostor()
                || role.IsAddOnOnlyShapeshifter()
                || role.IsAddOnOnlyMadmate()
                || role.IsAddOnOnlyCrewmate()
                || role.IsAddOnOnlyEngineer()
                || role.IsAddOnOnlyScientist();
        }
        public static bool IsAddOnOnlyImpostor(this CustomRoles role)
        {
            return role is
                CustomRoles.Impostor1 or
                CustomRoles.Impostor2 or
                CustomRoles.Impostor3;
        }
        public static bool IsAddOnOnlyShapeshifter(this CustomRoles role)
        {
            return role is
                CustomRoles.Shapeshifter1 or
                CustomRoles.Shapeshifter2 or
                CustomRoles.Shapeshifter3;
        }
        public static bool IsAddOnOnlyMadmate(this CustomRoles role)
        {
            return role is
                CustomRoles.Madmate1 or
                CustomRoles.Madmate2 or
                CustomRoles.Madmate3;
        }
        public static bool IsAddOnOnlyCrewmate(this CustomRoles role)
        {
            return role is
                CustomRoles.Crewmate1 or
                CustomRoles.Crewmate2 or
                CustomRoles.Crewmate3;
        }
        public static bool IsAddOnOnlyEngineer(this CustomRoles role)
        {
            return role is
                CustomRoles.Engineer1 or
                CustomRoles.Engineer2 or
                CustomRoles.Engineer3;
        }
        public static bool IsAddOnOnlyScientist(this CustomRoles role)
        {
            return role is
                CustomRoles.Scientist1 or
                CustomRoles.Scientist2 or
                CustomRoles.Scientist3;
        }

        public static bool IsAddOn(this CustomRoles role) => role.IsBuffAddOn() || role.IsDebuffAddOn();
        public static bool IsBuffAddOn(this CustomRoles role)
        {
            return
                role is CustomRoles.AddWatch or
                CustomRoles.AddLight or
                CustomRoles.AddSeer or
                CustomRoles.Autopsy or
                CustomRoles.VIP or
                CustomRoles.Revenger or
                CustomRoles.Management or
                CustomRoles.Sending or
                CustomRoles.TieBreaker or
                CustomRoles.Loyalty or
                CustomRoles.PlusVote or
                CustomRoles.Guarding or
                CustomRoles.AddBait or
                CustomRoles.Refusing;
        }
        public static bool IsDebuffAddOn(this CustomRoles role)
        {
            return
                role is 
                CustomRoles.Sunglasses or
                CustomRoles.Clumsy or
                CustomRoles.InfoPoor or
                CustomRoles.NonReport;
        }
        public static bool IsKilledSchrodingerCat(this CustomRoles role)
        {
            return role is
                CustomRoles.SchrodingerCat or
                CustomRoles.MSchrodingerCat or
                CustomRoles.CSchrodingerCat or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.JSchrodingerCat or
                CustomRoles.DSchrodingerCat or
                CustomRoles.OSchrodingerCat;
        }

        public static bool IsCatCatchRole(this CustomRoles role) => role.IsCatLeaderRoles() || role.IsCatRoles(); 
        public static bool IsCatLeaderRoles(this CustomRoles role)
        {
            return
                role is
                CustomRoles.CatRedLeader or
                CustomRoles.CatBlueLeader or
                CustomRoles.CatYellowLeader;
        }
        public static bool IsCatRoles(this CustomRoles role)
        {
            return
                role is
                CustomRoles.CatNoCat or
                CustomRoles.CatRedCat or
                CustomRoles.CatYellowCat or
                CustomRoles.CatBlueCat;
        }

        public static bool IsOneNightRoles(this CustomRoles role)
            => role.IsONImpostor() || role.IsONMadmate() || role.IsONCrewmate() || role.IsONNeutral();
        public static bool IsONImpostor(this CustomRoles role)
        {
            return
                role is
                CustomRoles.ONWerewolf or
                CustomRoles.ONBigWerewolf;
        }
        public static bool IsONMadmate(this CustomRoles role)
        {
            return
                role is
                CustomRoles.ONMadman or
                CustomRoles.ONMadFanatic;
        }
        public static bool IsONCrewmate(this CustomRoles role)
        {
            return
                role is
                CustomRoles.ONVillager or
                CustomRoles.ONDiviner or
                CustomRoles.ONPhantomThief or
                CustomRoles.ONHunter or
                CustomRoles.ONMayor or
                CustomRoles.ONBakery or
                CustomRoles.ONTrapper;
        }
        public static bool IsONNeutral(this CustomRoles role)
        {
            return
                role is
                CustomRoles.ONHangedMan;
        }

        public static bool IsCatMode(this CustomGameMode mode)
        {
            return
                mode is CustomGameMode.CatchCat;
        }
        public static bool IsOneNightMode(this CustomGameMode mode)
        {
            return
                mode is CustomGameMode.OneNight;
        }

        public static CustomRoleTypes GetCustomRoleTypes(this CustomRoles role)
        {
            CustomRoleTypes type = CustomRoleTypes.Crewmate;
            if (role.IsImpostor()) type = CustomRoleTypes.Impostor;
            if (role.IsNeutral()) type = CustomRoleTypes.Neutral;
            if (role.IsMadmate()) type = CustomRoleTypes.Madmate;
            if (role.IsONImpostor()) type = CustomRoleTypes.Impostor;
            if (role.IsONNeutral()) type = CustomRoleTypes.Neutral;
            if (role.IsONMadmate()) type = CustomRoleTypes.Madmate;
            return type;
        }
        public static int GetCount(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                var roleOpt = Main.NormalOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.Engineer => roleOpt.GetNumPerGame(RoleTypes.Engineer),
                    CustomRoles.Scientist => roleOpt.GetNumPerGame(RoleTypes.Scientist),
                    CustomRoles.Shapeshifter => roleOpt.GetNumPerGame(RoleTypes.Shapeshifter),
                    CustomRoles.GuardianAngel => roleOpt.GetNumPerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetNumPerGame(RoleTypes.Crewmate),
                    _ => 0
                };
            }
            else
            {
                return Options.GetRoleCount(role);
            }
        }
        public static float GetChance(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                var roleOpt = Main.NormalOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.Engineer => roleOpt.GetChancePerGame(RoleTypes.Engineer),
                    CustomRoles.Scientist => roleOpt.GetChancePerGame(RoleTypes.Scientist),
                    CustomRoles.Shapeshifter => roleOpt.GetChancePerGame(RoleTypes.Shapeshifter),
                    CustomRoles.GuardianAngel => roleOpt.GetChancePerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetChancePerGame(RoleTypes.Crewmate),
                    _ => 0
                } / 100f;
            }
            else
            {
                return Options.GetRoleChance(role);
            }
        }
        public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
        public static bool CanMakeMadmate(this CustomRoles role)
            => role switch
            {
                CustomRoles.Shapeshifter => true,
                CustomRoles.ShapeKiller => true,
                CustomRoles.EvilTracker => EvilTracker.CanCreateMadmate,
                CustomRoles.Egoist => Egoist.CanCreateMadmate,
                _ => false,
            };
        public static RoleTypes GetRoleTypes(this CustomRoles role)
        {
            if (role.IsAddOnOnlyEngineer()) return RoleTypes.Engineer;
            else if (role.IsAddOnOnlyScientist()) return RoleTypes.Scientist;
            else if (role.IsAddOnOnlyShapeshifter()) return RoleTypes.Shapeshifter;

            return role switch
            {
                CustomRoles.Sheriff or
                CustomRoles.Arsonist or
                CustomRoles.Hunter or
                CustomRoles.SillySheriff or
                CustomRoles.MadSheriff or
                CustomRoles.DarkHide or
                CustomRoles.PlatonicLover or
                CustomRoles.Totocalcio or
                CustomRoles.Jackal => RoleTypes.Impostor,

                CustomRoles.Scientist => RoleTypes.Scientist,

                CustomRoles.Engineer or
                CustomRoles.Madmate or
                CustomRoles.MadNatureCalls or
                CustomRoles.MadBrackOuter or
                CustomRoles.Chairman or
                CustomRoles.Workaholic or
                CustomRoles.Medic or
                CustomRoles.GrudgeSheriff or
                CustomRoles.Psychic or
                CustomRoles.Terrorist => RoleTypes.Engineer,

                CustomRoles.GuardianAngel or
                CustomRoles.GM => RoleTypes.GuardianAngel,

                CustomRoles.Doctor => Options.DoctorHasVital.GetBool() ? RoleTypes.Scientist : RoleTypes.Crewmate,
                CustomRoles.MadSnitch => Options.MadSnitchCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
                CustomRoles.MadDictator => Options.MadDictatorCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
                CustomRoles.JClient => Options.JClientCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
                CustomRoles.Mayor => Options.MayorHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
                CustomRoles.Opportunist => Options.OpportunistCanKill.GetBool() ? RoleTypes.Impostor : RoleTypes.Crewmate,

                CustomRoles.Shapeshifter or
                CustomRoles.BountyHunter or
                CustomRoles.SerialKiller or
                CustomRoles.FireWorks or
                CustomRoles.Sniper or
                CustomRoles.ShapeMaster or
                CustomRoles.Warlock or
                CustomRoles.Egoist or
                CustomRoles.ShapeKiller => RoleTypes.Shapeshifter,

                CustomRoles.EvilTracker => EvilTracker.RoleTypes,

                _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
            };
        }
        public static CountTypes GetCountTypes(this CustomRoles role)
            => role switch
            {
                CustomRoles.GM => CountTypes.OutOfGame,
                CustomRoles.Egoist => CountTypes.Impostor,
                CustomRoles.Jackal => CountTypes.Jackal,
                CustomRoles.HASFox or
                CustomRoles.HASTroll => CountTypes.None,
                _ => role.IsImpostor() ? CountTypes.Impostor : CountTypes.Crew,
            };
    }
    public enum CustomRoleTypes
    {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
    public enum CountTypes
    {
        OutOfGame,
        None,
        Crew,
        Impostor,
        Jackal,
    }
}
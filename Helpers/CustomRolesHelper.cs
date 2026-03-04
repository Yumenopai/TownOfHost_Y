using System.Linq;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY;

static class CustomRolesHelper
{
    public static readonly CustomRoles[] AllRoles = EnumHelper.GetAllValues<CustomRoles>().Where(role => !role.IsNotAssignedRoles()).ToArray();
    public static readonly CustomRoleTypes[] AllRoleTypes = EnumHelper.GetAllValues<CustomRoleTypes>();

    /// <summary>すべてのメイン役職(ゲームモードも含む、属性は含まない)</summary>
    public static readonly CustomRoles[] AllMainRoles = EnumHelper.GetAllValues<CustomRoles>().Where(role => role < CustomRoles.StartAddon && !role.IsNotAssignedRoles()).ToArray();
    /// <summary>すべての属性</summary>
    public static readonly CustomRoles[] AllAddOnRoles = EnumHelper.GetAllValues<CustomRoles>().Where(role => role > CustomRoles.StartAddon && !role.IsNotAssignedRoles()).ToArray();
    /// <summary>スタンダードモードのメイン役職</summary>
    public static readonly CustomRoles[] AllStandardRoles = EnumHelper.GetAllValues<CustomRoles>().Where(role => role < CustomRoles.MaxMain && !role.IsNotAssignedRoles()).ToArray();
    /// <summary>HASモードのメイン役職</summary>
    public static readonly CustomRoles[] AllHASRoles = { CustomRoles.HASFox, CustomRoles.HASTroll };
    /// <summary>CCモードのメイン役職</summary>
    public static readonly CustomRoles[] AllCCRoles = EnumHelper.GetAllValues<CustomRoles>().Where(role => role.IsCCRole()).ToArray();

    public static bool IsImpostor(this CustomRoles role)
    {
        var roleInfo = role.GetRoleInfo();
        if (roleInfo != null)
            return roleInfo.CustomRoleType == CustomRoleTypes.Impostor;
        
        return role == CustomRoles.CCRedLeader;
    }
    public static bool IsMadmate(this CustomRoles role)
    {
        var roleInfo = role.GetRoleInfo();
        if (roleInfo != null)
            return roleInfo.CustomRoleType == CustomRoleTypes.Madmate;
        return role == CustomRoles.SKMadmate;
    }
    public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
    public static bool IsNeutral(this CustomRoles role)
    {
        var roleInfo = role.GetRoleInfo();
        if (roleInfo != null)
            return roleInfo.CustomRoleType == CustomRoleTypes.Neutral;
        return role is CustomRoles.HASTroll or CustomRoles.HASFox;
    }
    public static bool IsCrewmate(this CustomRoles role) => role.GetRoleInfo()?.CustomRoleType == CustomRoleTypes.Crewmate || (!role.IsImpostorTeam() && !role.IsNeutral());
    public static bool IsVanilla(this CustomRoles role)
    {
        return
            role is CustomRoles.Crewmate or
            CustomRoles.Engineer or
            CustomRoles.Scientist or
            CustomRoles.Tracker or
            CustomRoles.Noisemaker or
            CustomRoles.Detective or
            CustomRoles.GuardianAngel or
            CustomRoles.Impostor or
            CustomRoles.Shapeshifter or
            CustomRoles.Phantom or
            CustomRoles.Viper;
    }
    public static CustomRoles VanillaRoleConversion(this CustomRoles role)
    {
        return role switch
        {
            CustomRoles.NormalImpostor => CustomRoles.Impostor,
            CustomRoles.NormalShapeshifter => CustomRoles.Shapeshifter,
             CustomRoles.NormalPhantom => CustomRoles.Phantom,
           CustomRoles.NormalEngineer => CustomRoles.Engineer,
            CustomRoles.NormalScientist => CustomRoles.Scientist,
            CustomRoles.NormalTracker => CustomRoles.Tracker,
            CustomRoles.NormalNoisemaker => CustomRoles.Noisemaker,
            CustomRoles.NormalViper => CustomRoles.Viper,
            CustomRoles.NormalDetective => CustomRoles.Detective,
            _ => role
        };
    }
    public static bool IsNormalVanillaRole(this CustomRoles role)
    {
        return role is
            CustomRoles.NormalImpostor or
            CustomRoles.NormalShapeshifter or
            CustomRoles.NormalPhantom or
            CustomRoles.NormalEngineer or
            CustomRoles.NormalScientist or
            CustomRoles.NormalTracker or
            CustomRoles.NormalNoisemaker;
    }

    // 新役職等(トップ掲載の常設役職)
    public static bool IsNewRole(this CustomRoles role)
    {
        if (Main.IsInitialRelease && role == CustomRoles.Bakery)
            return true;

        //return role is CustomRoles.Administer
        //    or CustomRoles.Charger
        //    or CustomRoles.GrudgeCharger;

        return false;
    }

    public static bool IsPairRole(this CustomRoles role)
    {
        return role is CustomRoles.Lovers
            or CustomRoles.Sympathizer
            or CustomRoles.CounselorAndMadDilemma
            or CustomRoles.GodfatherAndJanitor
            or CustomRoles.JackOLantern;
    }
    public static bool IsFixedCountRole(this CustomRoles role)
    {
        if (IsPairRole(role)) return true;

        return role is CustomRoles.Jackal
            or CustomRoles.StrayWolf
            or CustomRoles.DarkHide
            or CustomRoles.Pirate
            or CustomRoles.ChainShifter
            or CustomRoles.PlatonicLover
            or CustomRoles.FoxSpirit;
    }

    public static bool IsDontShowOptionRole(this CustomRoles role)
    {
        return role is CustomRoles.Counselor or CustomRoles.MadDilemma
            or CustomRoles.Godfather or CustomRoles.Janitor
            // 期間限定で
            or CustomRoles.JackOLantern
            or CustomRoles.Jack or CustomRoles.jO or CustomRoles.Lantern
            or CustomRoles.Gang
            or CustomRoles.SKMadmate
          
            or CustomRoles.EvilHacker //一旦封印
            or CustomRoles.Potentialist
            or CustomRoles.Impostor or CustomRoles.Crewmate;
    }
    public static bool IsDontSelectLoyaltyRole(this CustomRoles role)
    {
        return role is CustomRoles.CustomImpostor or CustomRoles.CustomCrewmate
            or CustomRoles.MadSnitch
            or CustomRoles.MadDilemma
            or CustomRoles.MadConnecter
            or CustomRoles.jO

            or CustomRoles.Jackal or CustomRoles.JClient or
            CustomRoles.LastImpostor or CustomRoles.CompleteCrew;
    }

    public static bool IsAddAddOn(this CustomRoles role)
    {
        return role.IsMadmate() || 
            role is CustomRoles.CustomImpostor or CustomRoles.CustomCrewmate or CustomRoles.Jackal or CustomRoles.JClient;
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
            CustomRoles.Refusing or
            CustomRoles.Revealer;
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
    public static bool IsOtherAddOn(this CustomRoles role)
    {
        return
            role is
            CustomRoles.LastImpostor or
            CustomRoles.Workhorse or
            CustomRoles.CompleteCrew or
            CustomRoles.Lovers;
    }

    public static bool IsFirstWriteAddOn(this CustomRoles role)
    {
        return
            role is
            CustomRoles.LastImpostor or
            CustomRoles.Workhorse or
            CustomRoles.CompleteCrew or
            CustomRoles.Archenemy or
            CustomRoles.ChainShifterAddon or
            CustomRoles.Lovers;
    }

    public static bool IsDirectKillRole(this CustomRoles role)
    {
        return role is
            CustomRoles.Arsonist or
            CustomRoles.PlatonicLover or
            CustomRoles.Totocalcio or
            CustomRoles.MadSheriff or
            CustomRoles.MadConnecter or
            CustomRoles.Pirate or
            CustomRoles.jO;
    }

    //CC
    public static bool IsCCRole(this CustomRoles role) => role.IsCCLeaderRoles() || role.IsCCCatRoles();
    public static bool IsCCLeaderRoles(this CustomRoles role)
    {
        return
            role is
            CustomRoles.CCRedLeader or
            CustomRoles.CCBlueLeader or
            CustomRoles.CCYellowLeader;
    }
    public static bool IsCCCatRoles(this CustomRoles role) => role.IsCCColorCatRoles() || role == CustomRoles.CCNoCat;
    public static bool IsCCColorCatRoles(this CustomRoles role)
    {
        return
            role is
            CustomRoles.CCRedCat or
            CustomRoles.CCYellowCat or
            CustomRoles.CCBlueCat;
    }

    public static bool IsNotAssignedRoles(this CustomRoles role)
    {
        return
            role is
            CustomRoles.NotAssigned or
            CustomRoles.MaxMain or
            CustomRoles.ONStart or
            CustomRoles.MaxON or
            CustomRoles.CCStart or
            CustomRoles.MaxCC or
            CustomRoles.HASStart or
            CustomRoles.MaxHAS or
            CustomRoles.StartAddon or
            CustomRoles.MaxAddon;
    }

    public static CustomRoleTypes GetCustomRoleTypes(this CustomRoles role)
    {
        CustomRoleTypes type = CustomRoleTypes.Crewmate;

        var roleInfo = role.GetRoleInfo();
        if (roleInfo != null)
            return roleInfo.CustomRoleType;

        if (role.IsImpostor()) type = CustomRoleTypes.Impostor;
        if (role.IsNeutral()) type = CustomRoleTypes.Neutral;
        if (role.IsMadmate()) type = CustomRoleTypes.Madmate;
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
                CustomRoles.Tracker => roleOpt.GetNumPerGame(RoleTypes.Tracker),
                CustomRoles.Noisemaker => roleOpt.GetNumPerGame(RoleTypes.Noisemaker),
                CustomRoles.Shapeshifter => roleOpt.GetNumPerGame(RoleTypes.Shapeshifter),
                CustomRoles.Phantom => roleOpt.GetNumPerGame(RoleTypes.Phantom),
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
    public static int GetChance(this CustomRoles role)
    {
        if (role.IsVanilla())
        {
            var roleOpt = Main.NormalOptions.RoleOptions;
            return role switch
            {
                CustomRoles.Engineer => roleOpt.GetChancePerGame(RoleTypes.Engineer),
                CustomRoles.Scientist => roleOpt.GetChancePerGame(RoleTypes.Scientist),
                CustomRoles.Tracker => roleOpt.GetChancePerGame(RoleTypes.Tracker),
                CustomRoles.Noisemaker => roleOpt.GetChancePerGame(RoleTypes.Noisemaker),
                CustomRoles.Shapeshifter => roleOpt.GetChancePerGame(RoleTypes.Shapeshifter),
                CustomRoles.Phantom => roleOpt.GetChancePerGame(RoleTypes.Phantom),
                CustomRoles.GuardianAngel => roleOpt.GetChancePerGame(RoleTypes.GuardianAngel),
                CustomRoles.Crewmate => roleOpt.GetChancePerGame(RoleTypes.Crewmate),
                _ => 0
            };
        }
        else
        {
            return Options.GetRoleChance(role);
        }
    }
    public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
    public static bool CanMakeMadmate(this CustomRoles role)
    {
        if (role.GetRoleInfo() is SimpleRoleInfo info)
        {
            return info.CanMakeMadmate;
        }

        return false;
    }
    public static RoleTypes GetRoleTypes(this CustomRoles role)
    {
        var roleInfo = role.GetRoleInfo();
        if (roleInfo != null)
            return roleInfo.BaseRoleType.Invoke();
        return role switch
        {
            CustomRoles.GM => RoleTypes.GuardianAngel,

            _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
        };
    }
}
public enum CountTypes
{
    OutOfGame,
    None,
    Crew,
    Impostor,
    Jackal,
    Pirate,
}
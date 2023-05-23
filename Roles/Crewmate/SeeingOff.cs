using System;
using System.Collections.Generic;
using System.Text;

using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate
{
    class SeeingOff
    {
        public static string RealNameChange(string Name)
        {
            if (Main.ExiledPlayer != 253)
            {
                var SeeingOffColor = Utils.GetRoleColor(CustomRoles.SeeingOff);
                var ExiledPlayer = Utils.GetPlayerById(Main.ExiledPlayer);
                if (ExiledPlayer == null) { Logger.Info($"Debug:RealNameChange ExiledPlayer: {Main.ExiledPlayer}, PlayerControl=Null", "SeeingOff"); return Name; }
                var ExiledPlayerName = ExiledPlayer.Data.PlayerName;

                if (ExiledPlayer.Is(CustomRoleTypes.Impostor))
                    return Utils.ColorString(SeeingOffColor, string.Format(GetString("isImpostor"), ExiledPlayerName));
                else
                    return Utils.ColorString(SeeingOffColor, string.Format(GetString("isNotImpostor"), ExiledPlayerName));
            }
            else
                return Name;
        }
    }
}

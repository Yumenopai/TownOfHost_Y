using System.Collections.Generic;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor
{
    public static class ShapeKiller
    {
        private static readonly int Id = 3800;
        public static List<byte> playerIdList = new();

        public static OptionItem CanDeadReport;

        public static Dictionary<byte, PlayerControl> Target = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ShapeKiller);
            CanDeadReport = BooleanOptionItem.Create(Id + 10, "ShapeKillerCanDeadReport", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnOnOff[CustomRoles.ShapeKiller]);
        }
        public static void Init()
        {
            playerIdList = new();
            Target = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            Target[playerId] = null;
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void Shapeshift(PlayerControl player, PlayerControl target, bool shapeshifting)
        {
            if (!shapeshifting) target = null;
            Target[player.PlayerId] = target;
            Logger.Info($"{player.GetNameWithRole()}のターゲットを {target?.GetNameWithRole()} に設定", "ShepeKillerTarget");
        }
        public static PlayerControl GetTarget(PlayerControl killer) => Target[killer.PlayerId];
    }
}
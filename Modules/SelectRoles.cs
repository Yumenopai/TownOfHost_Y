using System.Collections.Generic;
using System.Linq;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Modules;

public static class SelectRoles
{
    private static readonly Dictionary<byte, CustomRoles> reserved = new();

    public static IReadOnlyDictionary<byte, CustomRoles> Reserved => reserved;
    public static int Count => reserved.Count;

    public static void Set(byte playerId, CustomRoles role) => reserved[playerId] = role;
    public static void Clear() => reserved.Clear();

    public static void AddToAssignRoleList(List<CustomRoles> assignRoleList)
    {
        foreach (var role in reserved.Values)
        {
            assignRoleList.Remove(role);
            assignRoleList.Insert(0, role);
        }
    }

    public static PlayerControl TakePlayer(CustomRoles role, List<PlayerControl> candidates)
    {
        var playerId = byte.MaxValue;
        foreach (var pair in reserved)
        {
            if (pair.Value != role) continue;
            if (!candidates.Any(pc => pc.PlayerId == pair.Key)) continue;
            playerId = pair.Key;
            break;
        }
        if (playerId == byte.MaxValue) return null;

        reserved.Remove(playerId);
        return candidates.First(pc => pc.PlayerId == playerId);
    }
}

using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Translator;

namespace TownOfHostY.Roles.Impostor;
public sealed class Administer : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Administer),
            player => new Administer(player),
            CustomRoles.Administer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 2200,
            null,
            "アドミニスター"
        );
    public Administer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!isForMeeting || !seen.IsAlive()) return string.Empty;

        // 最終場所の表示
        var room = PlayerState.GetByPlayerId(seen.PlayerId).LastRoom;
        var color = Color.green;
        var roomName = "";

        if (room == null)
        {
            roomName = GetString("FailToTrack");
            color = Color.gray;
        }
        else
        {
            roomName = GetString(room.RoomId.ToString());
        }
        return Utils.ColorString(color, $"<size=80%>\n@{roomName}</size>");
    }
}
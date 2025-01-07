using System.Collections.Generic;
using System.Linq;

using AmongUs.GameOptions;
using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Roles.Unit.JackOLantern;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Lantern : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Lantern),
            player => new Lantern(player),
            CustomRoles.Lantern,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.UnitMix + 100,//使用しない
            null,
            "ランタン",
            "#e5a323"
        );
    public Lantern(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        visionDuringFixLight = OptionLanternVisionDuringFixLight.GetInt();
        roleReceivedExcludeNormalCrewmate = OptionLanternRoleReceivedExcludeNormalCrewmate.GetBool();

        fixLightVision = (Main.DefaultCrewmateVision * 5) * (visionDuringFixLight / 100f);
        Logger.Info($"{Player.GetNameWithRole()} : 停電時の視界率({fixLightVision / 5})", "Lantern");
    }

    static int visionDuringFixLight;
    static bool roleReceivedExcludeNormalCrewmate;

    static float fixLightVision; 

    public override void Add()
    {
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);

        // 停電時視界の緩和
        if (Utils.IsActive(SystemTypes.Electrical))
            opt.SetFloat(FloatOptionNames.CrewLightMod, fixLightVision);
    }

    public override bool OnCompleteTask()
    {
        if (Player.IsAlive() && IsTaskFinished)
        {
            /* タスク完了で死亡プレイヤーが持つクルー役職を付与する */
            // 死亡プレイヤーの役職をリストに一旦登録
            List<CustomRoles> roleList = new(15);

            foreach (var pc in Main.AllDeadPlayerControls)
            {
                // クルー陣営じゃないなら除く
                if (!pc.Is(CustomRoleTypes.Crewmate)) continue;

                var role = pc.GetCustomRole();
                // 通常クルーを除くなら除く
                if (roleReceivedExcludeNormalCrewmate && role == CustomRoles.Crewmate) continue;

                // 登録
                roleList.Add(role);
            }
            Logger.Info($"死亡者役職リスト：{string.Join(", ", roleList)}", "Lantern");

            // 登録数が0なら通常クルーを割り当てる
            if (roleList.Count == 0)
            {
                Player.RpcSetCustomRole(CustomRoles.Crewmate);
                Logger.Info($"{Player.GetNameWithRole()}：死亡者役職リストが0の為通常クルーを割り当てます", "Lantern");
            }
            else
            {
                var nextRole = roleList[IRandom.Instance.Next(roleList.Count)];
                SetNextCustomRole(Player, nextRole);
            }
        }
        return true;
    }

    // 今後移植？
    void SetNextCustomRole(PlayerControl player, CustomRoles nextRole)
    {
        Logger.Info($"{player.GetNameWithRole()}：死亡者役職リストから{nextRole}を割り当てます", "Lantern");
        var nextRoleBaseTypes = nextRole.GetRoleTypes();
        RoleTypes roleTypes;

        switch(nextRoleBaseTypes)
        {
            case RoleTypes.Crewmate:
                // なにもしない
                break;
            case RoleTypes.Scientist:
            case RoleTypes.Engineer:
            case RoleTypes.Tracker:
            case RoleTypes.Noisemaker:
                foreach (var pc in Main.AllPlayerControls.Where(x => x != null && !x.Data.Disconnected))
                {
                    // base:能力持ちクルー視点
                    if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) player.SetRoleEx(nextRoleBaseTypes);
                    else pc.RpcSetRoleDesync(nextRoleBaseTypes, player.GetClientId());

                    if (pc.PlayerId == player.PlayerId) continue;

                    //他クルー視点
                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) player.SetRoleEx(nextRoleBaseTypes);
                    else player.RpcSetRoleDesync(nextRoleBaseTypes, pc.GetClientId());
                }
                break;

            // shepeShifterなどはホストのみアビリティボタンについての問題があるため一旦Impostor
            case RoleTypes.Impostor:
            case RoleTypes.Shapeshifter:
            case RoleTypes.Phantom:
                foreach (var pc in Main.AllPlayerControls.Where(x => x != null && !x.Data.Disconnected))
                {
                    // ランタン(base:impostor)視点
                    roleTypes = RoleTypes.Scientist;
                    if (pc.PlayerId == player.PlayerId) roleTypes = RoleTypes.Impostor;
                    else if (!pc.IsAlive()) roleTypes = RoleTypes.CrewmateGhost;
                    else if (pc.GetCustomRole().GetRoleTypes() == RoleTypes.Noisemaker) roleTypes = RoleTypes.Noisemaker;

                    if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) pc.StartCoroutine(pc.CoSetRole(roleTypes, true));
                    else pc.RpcSetRoleDesync(roleTypes, player.GetClientId());

                    if (pc.PlayerId == player.PlayerId) continue;

                    //他クルー視点
                    roleTypes = RoleTypes.Scientist;

                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) player.StartCoroutine(player.CoSetRole(roleTypes, true));
                    else player.RpcSetRoleDesync(roleTypes, pc.GetClientId());
                }
                break;
        }
        player.RpcSetCustomRole(nextRole);

        //色表示
        NameColorManager.RemoveAll(player.PlayerId);

        PlayerGameOptionsSender.SetDirty(player.PlayerId);
        Utils.NotifyRoles(SpecifySeer: player);
    }

}

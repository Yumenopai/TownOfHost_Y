using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Modules;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Potentialist : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Potentialist),
            player => new Potentialist(player),
            CustomRoles.Potentialist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewSpecial + 0,
            null,
            "ポテンシャリスト",
            "#ffff00"
        );
    public Potentialist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskTrigger = OptionTaskTrigger.GetInt();
        CanChangeMad = OptionCanChangeMad.GetBool();
        CanChangeNeutral = OptionCanChangeNeutral.GetBool();
    }

    private static OptionItem OptionTaskTrigger; //効果を発動するタスク完了数
    private static OptionItem OptionCanChangeMad;
    private static OptionItem OptionCanChangeNeutral;
    enum OptionName
    {
        poTask,
        poMad,
        poNeutral
    }
    private static int TaskTrigger;
    private static bool CanChangeMad;
    private static bool CanChangeNeutral;

    bool isPotentialistChanged;

    // 直接設置
    public static void SetupRoleOptions()
    {
        TextOptionItem.Create(40, "Head.LimitedTimeRole", TabGroup.CrewmateRoles)
            .SetColor(Color.yellow);
        var spawnOption = IntegerOptionItem.Create(RoleInfo.ConfigId, "Potentialist", new(0, 100, 10), 0, TabGroup.CrewmateRoles, false)
            .SetColor(RoleInfo.RoleColor)
            .SetValueFormat(OptionFormat.Percent)
            .SetHeader(true, Translator.GetString("PotentialistHedderInfo"))
            .SetGameMode(CustomGameMode.Standard) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(RoleInfo.ConfigId + 1, "Maximum", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.Standard);

        Options.CustomRoleSpawnChances.Add(RoleInfo.RoleName, spawnOption);
        Options.CustomRoleCounts.Add(RoleInfo.RoleName, countOption);

        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 10, OptionName.poTask, new(1, 30, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionCanChangeMad = BooleanOptionItem.Create(RoleInfo, 11, OptionName.poMad, true, false);
        OptionCanChangeNeutral = BooleanOptionItem.Create(RoleInfo, 12, OptionName.poNeutral, false, false);
    }

    public override void Add()
    {
        isPotentialistChanged = false;
    }
    public override bool OnCompleteTask()
    {
        var playerId = Player.PlayerId;
        var player = Player;
        if (Player.IsAlive()
            && !isPotentialistChanged
            && MyTaskState.HasCompletedEnoughCountOfTasks(TaskTrigger))
        {   //生きていて、変更済みでなく、全タスク完了orトリガー数までタスクを完了している場合
            var rand = IRandom.Instance;
            List<CustomRoles> Rand = new()
                {
                    CustomRoles.NiceWatcher,
                    CustomRoles.Bait,
                    CustomRoles.Lighter,
                    CustomRoles.Mayor,
                    CustomRoles.Snitch,
                    CustomRoles.SpeedBooster,
                    CustomRoles.Doctor,
                    CustomRoles.Trapper,
                    CustomRoles.Dictator,
                    CustomRoles.Seer,
                    CustomRoles.TimeManager,
                    CustomRoles.Bakery,
                    CustomRoles.TaskManager,
                    CustomRoles.Nekomata,
                    CustomRoles.Express,
                    CustomRoles.SeeingOff,
                    CustomRoles.Rainbow,
                    CustomRoles.Blinder,
                    CustomRoles.CandleLighter,
                    CustomRoles.FortuneTeller,
                    CustomRoles.Nimrod,
                    CustomRoles.Detector,
                    CustomRoles.Rabbit,
                    CustomRoles.NiceGuesser,
                    CustomRoles.Elder,

                    CustomRoles.Sheriff,
                    CustomRoles.Hunter,
                    CustomRoles.SillySheriff,
                    CustomRoles.GrudgeSheriff,
                    CustomRoles.Chairman,
                    CustomRoles.Medic,
                    CustomRoles.Psychic,

                    CustomRoles.NormalEngineer,
                    CustomRoles.NormalScientist,
                    CustomRoles.NormalTracker,
                    CustomRoles.NormalNoisemaker,
                };

            if (CanChangeMad)
            {
                Rand.Add(CustomRoles.Madmate);
                Rand.Add(CustomRoles.MadDictator);
                Rand.Add(CustomRoles.MadNimrod);
                Rand.Add(CustomRoles.MadJester);
                Rand.Add(CustomRoles.MadGuesser);

                Rand.Add(CustomRoles.MadSnitch);
                Rand.Add(CustomRoles.MadNatureCalls);
                Rand.Add(CustomRoles.MadBrackOuter);
                Rand.Add(CustomRoles.MadSheriff);
                Rand.Add(CustomRoles.MadScientist);
                Rand.Add(CustomRoles.MadConnecter);
            }
            if (CanChangeNeutral)
            {
                Rand.Add(CustomRoles.Jester);
                Rand.Add(CustomRoles.Opportunist);
                Rand.Add(CustomRoles.Terrorist);
                Rand.Add(CustomRoles.SchrodingerCat);
                Rand.Add(CustomRoles.AntiComplete);
                Rand.Add(CustomRoles.LoveCutter);
                Rand.Add(CustomRoles.God);
                Rand.Add(CustomRoles.ChainShifter);

                Rand.Add(CustomRoles.Totocalcio);
            }
            if ((MapNames)Main.NormalOptions.MapId is not MapNames.Polus and not MapNames.Fungle)
            {
                Rand.Add(CustomRoles.VentManager);
            }
            var Role = Rand[rand.Next(Rand.Count)];
            SetNextCustomRole(Player, Role);

            isPotentialistChanged = true;
            Logger.Info(player.GetRealName() + " 役職変更先:" + Role, "Potentialist");

            if (AmongUsClient.Instance.AmHost && Role == CustomRoles.VentManager)
            {
                player.Data.RpcSetTasks(Array.Empty<byte>()); //タスクを再配布
                player.SyncSettings();
                Utils.NotifyRoles();
            }
        }
        return true;
    }
    // 今後移植？
    void SetNextCustomRole(PlayerControl player, CustomRoles nextRole)
    {
        Logger.Info($"{player.GetNameWithRole()}：役職リストから{nextRole}を割り当てます", "Potentialist");
        var nextRoleBaseTypes = nextRole.GetRoleTypes();
        RoleTypes roleTypes;

        switch (nextRoleBaseTypes)
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
                    // base:impostor視点
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

    public override void OverrideShowMainRoleText(ref Color roleColor, ref string roleText)
    {
        roleText = Utils.GetRoleName(CustomRoles.Crewmate);
        roleColor = Utils.GetRoleColor(CustomRoles.Crewmate);
    }
}
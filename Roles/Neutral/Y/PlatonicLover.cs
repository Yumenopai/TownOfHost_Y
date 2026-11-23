using System.Linq;
using AmongUs.GameOptions;
using TownOfHostY.Roles.AddOns.Common;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral;

public sealed class PlatonicLover : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(PlatonicLover),
            player => new PlatonicLover(player),
            CustomRoles.PlatonicLover,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuY + 400,
            SetupOptionItem,
            "純愛者",
            "#ff6be4",
            true,
            assignInfo: new RoleAssignInfo(CustomRoles.PlatonicLover, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public PlatonicLover(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AddWin = OptionAddWin.GetBool();
        limitTurn = OptionLimitTurn.GetInt();

        TurnNumber = 1;
    }
    public static OptionItem OptionAddWin;
    public static OptionItem OptionLimitTurn;
    enum OptionName
    {
        LoversAddWin,
        PlatonicLoverLimitTurn,
    }
    public bool isMadeLover;
    public static bool AddWin;
    public static int limitTurn;
    public static int TurnNumber;

    private static void SetupOptionItem()
    {
        OptionLimitTurn = IntegerOptionItem.Create(RoleInfo, 11, OptionName.PlatonicLoverLimitTurn, new(1, 30, 1), 3, false)
            .SetValueFormat(OptionFormat.Turns);
        OptionAddWin = BooleanOptionItem.Create(RoleInfo, 10, OptionName.LoversAddWin, false, false);
    }

    public override void Add()
    {
        var playerId = Player.PlayerId;
        isMadeLover = false;
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? 0.1f : 0f;
    public bool CanUseKillButton() => Player.IsAlive() && !isMadeLover;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);

    public override void OnStartMeeting() => TurnNumber++;
    public override string GetProgressText(bool comms = false)
    {
        if (limitTurn < TurnNumber || !Player.IsAlive() || isMadeLover) return string.Empty;

        return Utils.ColorString(RoleInfo.RoleColor, $"[{TurnNumber}/{limitTurn}]");
    }
    public override void AfterMeetingTasks()
    {
        if (limitTurn >= TurnNumber) return;
        if (!Player.IsAlive() || isMadeLover) return;
        
        Main.AfterMeetingDeathPlayers.TryAdd(Player.PlayerId, CustomDeathReason.Suicide);
        Logger.Info($"PlatonicLover:dead, Turn:{TurnNumber} > {limitTurn}", "PlatonicLover");
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // ガード持ちに関わらず能力発動する直接キル役職

        isMadeLover = true;
        info.DoKill = false;
        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        Logger.Info($"{killer.GetNameWithRole()} : 恋人を作った", "PlatonicLover");

        Lovers.playersList.Clear();
        killer.SetCustomRole(CustomRoles.Lovers);
        target.SetCustomRole(CustomRoles.Lovers);

        Utils.NotifyRoles();
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("PlatonicLoverButtonText");
        return true;
    }
}

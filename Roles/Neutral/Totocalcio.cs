using AmongUs.GameOptions;
using System.Linq;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
namespace TownOfHostY.Roles.Neutral;

public sealed class Totocalcio : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Totocalcio),
            player => new Totocalcio(player),
            CustomRoles.Totocalcio,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            60600,
            SetupOptionItem,
            "トトカルチョ",
            "#00ff00",
            true
        );
    public Totocalcio(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        InitialCoolDown = OptionInitialCoolDown.GetFloat();
        FinalCoolDown = OptionFinalCoolDown.GetFloat();
        BetChangeCount = OptionBetChangeCount.GetInt();

        AllPlayer = Main.AllPlayerControls.Count();
        if (AllPlayer > 3) Coolrate = (FinalCoolDown - InitialCoolDown) / (AllPlayer - 3);
        else Coolrate = (FinalCoolDown - InitialCoolDown) / AllPlayer;
    }
    public static PlayerControl BetTarget;
    public static int BetTargetCount;

    private static OptionItem OptionInitialCoolDown;
    private static OptionItem OptionFinalCoolDown;
    private static OptionItem OptionBetChangeCount;
    enum OptionName
    {
        TotocalcioInitialCoolDown,
        TotocalcioBetChangeCount,
        TotocalcioFinalCoolDown,
    }
    private static float InitialCoolDown;
    private static float FinalCoolDown;
    private static int BetChangeCount;

    private static float Coolrate;
    private static int AllPlayer;

    private static void SetupOptionItem()
    {
        OptionInitialCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.TotocalcioInitialCoolDown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionBetChangeCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.TotocalcioBetChangeCount, new(0, 10, 1), 0, false)
            .SetValueFormat(OptionFormat.Times);
        OptionFinalCoolDown = FloatOptionItem.Create(RoleInfo, 12, OptionName.TotocalcioFinalCoolDown, new(0f, 180f, 2.5f), 60f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public bool CheckWin(out AdditionalWinners winnerType)
    {
        winnerType = AdditionalWinners.Totocalcio;
        return Player.IsAlive() && CustomWinnerHolder.WinnerIds.Contains(BetTarget.PlayerId);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        BetTarget = null;
        BetTargetCount = BetChangeCount + 1;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public float CalculateKillCooldown()
    {
        float plusCool = Coolrate * (AllPlayer - Main.AllAlivePlayerControls.Count());
        return CanUseKillButton() ? InitialCoolDown + plusCool : 300f;
    }
    public bool CanUseKillButton() => Player.IsAlive() && BetTargetCount > 0;
    public override bool OnInvokeSabotage(SystemTypes systemType) => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // ガード持ちに関わらず能力発動する直接キル役職

        BetTarget = target;
        BetTargetCount--;
        killer.RpcGuardAndKill(target);
        Logger.Info($"{killer.GetNameWithRole()} : {target.GetRealName(Options.GetNameChangeModes() == NameChange.Crew)}に賭けた", "Totocalcio");

        Utils.NotifyRoles(SpecifySeer : killer);
        info.DoKill = false;
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("TotocalcioButtonText");
        return true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (BetTarget == seen) return Utils.ColorString(RoleInfo.RoleColor, "▲");

        return string.Empty;
    }
}

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
            (int)Options.offsetId.NeuY + 600,
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
    private byte betTargetId = byte.MaxValue;
    private int betTargetCount;

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

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (!Player.IsAlive()) return false;
        if (betTargetId == byte.MaxValue) return false;
        var betTarget = Utils.GetPlayerById(betTargetId);
        if (betTarget == null) return false;

        //トトカルチョ→トトカルチョの勝利連鎖は行わない
        if (betTarget.Is(CustomRoles.Totocalcio)) return false;

        return CustomWinnerHolder.WinnerIds.Contains(betTargetId) ||
               CustomWinnerHolder.WinnerRoles.Any(team => betTarget.Is(team));
    }
    public override void Add()
    {
        betTargetId = byte.MaxValue;
        betTargetCount = BetChangeCount + 1;
    }
    public float CalculateKillCooldown()
    {
        float plusCool = Coolrate * (AllPlayer - Main.AllAlivePlayerControls.Count());
        return CanUseKillButton() ? InitialCoolDown + plusCool : 300f;
    }
    public bool CanUseKillButton() => Player.IsAlive() && betTargetCount > 0;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // ガード持ちに関わらず能力発動する直接キル役職

        betTargetId = target.PlayerId;
        betTargetCount--;
        killer.RpcProtectedMurderPlayer(target);
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
        if (betTargetId == seen.PlayerId) return Utils.ColorString(RoleInfo.RoleColor, "▲");

        return string.Empty;
    }
}

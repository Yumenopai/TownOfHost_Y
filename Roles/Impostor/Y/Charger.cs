using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Translator;

namespace TownOfHostY.Roles.Impostor;
public sealed class Charger : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Charger),
            player => new Charger(player),
            CustomRoles.Charger,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1700,
            SetUpOptionItem,
            "チャージャー"
        );
    public Charger(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        killCooldown = OptionkillCooldown.GetFloat();
        chargeKillCooldown = OptionChargeKillCooldown.GetFloat();
        oneGaugeChargeCount = OptionOneGaugeChargeCount.GetInt();
        oneGaugeAddKillCount = OptionOneGaugeAddKillCount.GetInt();
        killCountAtStartGame = OptionKillCountAtStartGame.GetInt();
    }
    private static OptionItem OptionkillCooldown;
    private static OptionItem OptionChargeKillCooldown;
    private static OptionItem OptionOneGaugeChargeCount;
    private static OptionItem OptionOneGaugeAddKillCount;
    private static OptionItem OptionKillCountAtStartGame;
    enum OptionName
    {
        ChargerFirstKillCooldown,
        GrudgeChargerChargeKillCooldown,
        GrudgeChargerOneGaugeChargeCount,
        GrudgeChargerOneGaugeAddKillCount,
        GrudgeChargerKillCountAtStartGame,
    }
    private static float killCooldown;
    private static float chargeKillCooldown;
    private static int oneGaugeChargeCount;
    private static int oneGaugeAddKillCount;
    private static int killCountAtStartGame;

    int killLimit;
    bool killThisTurn;
    /// <summary> チャージ回数 </summary>
    int chargeCount;

    private static void SetUpOptionItem()
    {
        OptionkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ChargerFirstKillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionChargeKillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.GrudgeChargerChargeKillCooldown, new(0.5f, 180f, 0.5f), 2f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionOneGaugeChargeCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.GrudgeChargerOneGaugeChargeCount, new(1, 30, 1), 6, false)
            .SetValueFormat(OptionFormat.Times);
        OptionOneGaugeAddKillCount = IntegerOptionItem.Create(RoleInfo, 14, OptionName.GrudgeChargerOneGaugeAddKillCount, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionKillCountAtStartGame = IntegerOptionItem.Create(RoleInfo, 13, OptionName.GrudgeChargerKillCountAtStartGame, new(0, 2, 1), 0, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public float CalculateKillCooldown() => chargeKillCooldown;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = killThisTurn ? 0f : killCooldown;
        AURoleOptions.PhantomDuration = 1f;
    }

    public override void Add()
    {
        killThisTurn = false;
        killLimit = killCountAtStartGame;
        chargeCount = 0;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var killer = info.AttemptKiller;
        chargeCount++;
        if (chargeCount >= oneGaugeChargeCount)
        {
            killLimit += oneGaugeAddKillCount;
            chargeCount = 0;
        }
        Logger.Info($"{Player.GetNameWithRole()} : チャージ({chargeCount}/{oneGaugeChargeCount})", "Charger");
        Utils.NotifyRoles(SpecifySeer: Player);

        killer.SetKillCooldown();
        info.DoKill = false;
    }

    public override void AfterMeetingTasks()
    {
        Player.MarkDirtySettings();
        Player.RpcResetAbilityCooldown();
    }

    public override bool OnCheckVanish(ref float killCooldown, ref bool canResetAbilityCooldown)
    {
        // キルクール設定
        killCooldown = chargeKillCooldown;

        if (killLimit <= 0) return false;

        // 全体内での最短距離のターゲット
        (PlayerControl target, float dist) minDistance = (null, float.MaxValue);
        Vector2 playerPos = Player.transform.position;
        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (target == Player) continue;
            float targetDistance = Vector2.Distance(playerPos, target.transform.position);
            if (targetDistance < minDistance.dist)
            {
                minDistance = (target, targetDistance);
            }
        }
        Logger.Info($"最短距離プレイヤー確定 : {minDistance.target.GetNameWithRole()}・{minDistance.dist}m", "Charger");

        var KillRange = Utils.SafeGetKillDistance();
        Logger.Info($"距離 : {minDistance.dist}m <= {KillRange}m", "Charger");
        if (minDistance.dist <= KillRange && Player.CanMove && minDistance.target.CanMove)
        {
            if (CustomRoleManager.OnCheckMurder(Player, minDistance.target, true))
            {
                killLimit--;
                minDistance.target.SetRealKiller(Player);
                Logger.Info($"{Player.GetNameWithRole()} : 残り{killLimit}発", "Charger");
                Utils.NotifyRoles(SpecifySeer: Player);
            }

            killThisTurn = true;
            Player.MarkDirtySettings();
            Player.RpcResetAbilityCooldown();
            killThisTurn = false;
        }
        return false;
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("GrudgeChargerCharge");
        return true;
    }
    public override string GetAbilityButtonText() => GetString("ChargerKill");

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || !Player.IsAlive() || isForMeeting) return "";

        var str = new StringBuilder();
        int charge = chargeCount;
        int empty = oneGaugeChargeCount - chargeCount;

        int newLine = 0;
        int count = 1;
        if (oneGaugeChargeCount > 15)
        {
            newLine = oneGaugeChargeCount / 2;
        }

        str.Append("<size=80%><line-height=85%><color=#ff6347>");
        for (int i = 0; i < charge; i++, count++)
        {
            str.Append('█');
            if (count == newLine) str.Append('\n');
        }
        str.Append("</color><color=#888888>");
        for (int i = 0; i < empty; i++, count++)
        {
            str.Append('■');
            if (count == newLine) str.Append('\n');
        }
        str.Append("</color></line-height></size>");

        return str.ToString();
    }

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(Color.yellow, $"〈{killLimit}〉");
}
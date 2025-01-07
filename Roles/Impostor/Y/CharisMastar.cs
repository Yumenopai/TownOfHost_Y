using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class CharismaStar : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CharismaStar),
            player => new CharismaStar(player),
            CustomRoles.CharismaStar,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1900,
            SetUpOptionItem,
            "カリスマスター"
        );
    public CharismaStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        GatherCooldown = OptionGatherCooldown.GetFloat();
        // 集合クールがキルクールより短い時、集合クールはキルクールと同じにする
        if (GatherCooldown < KillCooldown) GatherCooldown = KillCooldown;

        GatherMaxCount = OptionGatherMaxCount.GetInt();
        NotGatherPlayerKill = OptionNotGatherPlayerKill.GetBool();
        CanAllPlayerGather = OptionCanAllPlayerGather.GetBool();
    }
    enum OptionName
    {
        CharismaStarGatherCooldown,
        CharismaStarGatherMaxCount,
        CharismaStarNotGatherPlayerKill,
        CharismaStarCanAllPlayerGather,
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionGatherCooldown;
    private static OptionItem OptionGatherMaxCount;
    private static OptionItem OptionNotGatherPlayerKill;
    private static OptionItem OptionCanAllPlayerGather;
    private static float KillCooldown;
    private static float GatherCooldown;
    private static int GatherMaxCount;
    private static bool NotGatherPlayerKill;
    private static bool CanAllPlayerGather;

    private HashSet<byte> GatherChoosePlayer;
    private int GatherLimitCount;

    private static Vector2 LiftPosition = new(7.76f, 8.56f); //昇降機の座標

    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionGatherCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.CharismaStarGatherCooldown, new(2.5f, 180f, 2.5f), 30f, false)
        .SetValueFormat(OptionFormat.Seconds);
        OptionGatherMaxCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.CharismaStarGatherMaxCount, new(1, 10, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionNotGatherPlayerKill = BooleanOptionItem.Create(RoleInfo, 13, OptionName.CharismaStarNotGatherPlayerKill, true, false);
        OptionCanAllPlayerGather = BooleanOptionItem.Create(RoleInfo, 14, OptionName.CharismaStarCanAllPlayerGather, true, false);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = GatherCooldown;

    public override void Add()
    {
        Player.AddDoubleTrigger();

        GatherLimitCount = GatherMaxCount;
        GatherChoosePlayer = new();
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.CanKill) return;

        // 集合回数がまだあるならダブルクリック有効
        if (GatherLimitCount > 0)
        {
            var (killer, target) = info.AttemptTuple;
            info.DoKill = killer.CheckDoubleTrigger(target, () =>
            {
                // 集めるターゲット登録
                GatherChoosePlayer.Add(target.PlayerId);
                Logger.Info($"{killer.GetNameWithRole()} → {target.GetNameWithRole()}：ターゲット選択", "CharismaStar");
                // 表示更新
                Utils.NotifyRoles(SpecifySeer: killer);
            });
        }
        if (info.DoKill) // 指名時はクールリセットしない
        {
            // アビリティクールリセット
            Player.RpcResetAbilityCooldown();
            // ホストが集合後のキルクールが0.1のままになってしまう為ここでもリセット
            if (Player == PlayerControl.LocalPlayer)
            {
                _ = new LateTask(() =>
                {
                    Player.SetKillCooldown(KillCooldown);
                }, 0.2f, "CharismaStar_HostSetKillCooldown");
            }
        }
    }
    public override bool OnCheckVanish(ref float killCooldown, ref bool canResetAbilityCooldown)
    {
        // キルクール設定(使用時は既にキルクールがない想定)
        killCooldown = 0.1f;
        canResetAbilityCooldown = true;

        // 使用回数がない時は無視
        if (GatherLimitCount == 0) return false;

        // リストに誰も登録されていない
        if (GatherChoosePlayer.Count == 0)
        {
            // 全員集合できるモードでないなら能力は使わない
            if (!CanAllPlayerGather) return false;

            // 全員をリストに登録する
            Main.AllAlivePlayerControls.Do(target => GatherChoosePlayer.Add(target.PlayerId));
        }
        else
        {
            // 集まる直前に自身を登録する
            GatherChoosePlayer.Add(Player.PlayerId);
        }

        // リストに登録されている人たち
        foreach (var targetId in GatherChoosePlayer)
        {
            var target = Utils.GetPlayerById(targetId);
            // 死亡していたら関係ない
            if (!target.IsAlive()) continue;

            // ターゲットが梯子またはヌーンを使っている
            if ((target.MyPhysics.Animations.IsPlayingAnyLadderAnimation()
                || ((MapNames)Main.NormalOptions.MapId == MapNames.Airship && Vector2.Distance(target.GetTruePosition(), LiftPosition) <= 1.9f))
                && !target.Is(CustomRoleTypes.Impostor))
            {
                // 集まらないプレイヤーをキルするがONの時
                if (NotGatherPlayerKill)
                {
                    target.SetRealKiller(Player);
                    target.RpcMurderPlayer(target);
                    PlayerState.GetByPlayerId(targetId).DeathReason = CustomDeathReason.NotGather;
                    // キルフラッシュを自視点に鳴らす
                    Player.KillFlash();
                }
                Logger.Info($"{target.GetNameWithRole()} : ワープできませんでした。", "CharismaStar");
                continue;
            }

            // ベントに集合
            target.MyPhysics.RpcExitVent(GetNearestVent().Id);
        }

        // 能力使用後のリセット
        GatherChoosePlayer.Clear();
        GatherLimitCount--;
        Logger.Info($"{Player.GetNameWithRole()} : 残り{GatherLimitCount}回", "CharismaStar");
        // 表示更新
        Utils.NotifyRoles(SpecifySeer: Player);
        return false;
    }
    Vent GetNearestVent()
    {
        var vents = ShipStatus.Instance.AllVents.OrderBy(v => (Player.transform.position - v.transform.position).magnitude);
        return vents.First();
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        // seenが省略の場合seer
        seen ??= seer;

        if (GatherChoosePlayer.Contains(seen.PlayerId))
        {
            return Utils.ColorString(RoleInfo.RoleColor, "◎");
        }

        return string.Empty;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(GatherLimitCount > 0 ? RoleInfo.RoleColor : Color.gray, $"[{GatherLimitCount}]");
    public override string GetAbilityButtonText() => Translator.GetString("CharismaStarGatherButtonText");
}
using System.Text;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Roles.Unit.GodfatherAndJanitor;

namespace TownOfHostY.Roles.Impostor;
public sealed class Janitor : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Janitor),
            player => new Janitor(player),
            CustomRoles.Janitor,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 100,//使用しない
            null,
            "ジャニター"
        );
    public Janitor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CleanCooldown = OptionJanitorCleanCooldown.GetFloat();
        TrackTarget = OptionJanitorTrackTarget.GetBool();
        TrackGodfather = OptionJanitorTrackGodfather.GetBool();
        IsAliveAfterGodfatherDead = OptionJanitorIsAliveAfterGodfatherDead.GetBool();
        JanitorCanKill = OptionJanitorCanKill.GetBool();
        JanitorKillCooldown = OptionJanitorKillCooldown.GetFloat();
    }
    private static float CleanCooldown;
    public static bool TrackTarget;
    private static bool TrackGodfather;
    private static bool IsAliveAfterGodfatherDead;
    private static bool JanitorCanKill;
    private static float JanitorKillCooldown;

    // ゴッドファーザーの死亡後、通常キルをする際にtrueにする
    private static bool canNormalKill;
    // murderPlayerの無限ループ防止
    private static bool isSuiside;

    public override void Add()
    {
        janitor = Player;
        Logger.Info($"{Player.GetNameWithRole()} : Janitor登録", "G&J");
        canNormalKill = false;
        isSuiside = false;

        // ジャニター視点の矢印表示追加
        if (TrackGodfather)
        {
            var god = Main.AllPlayerControls.Where(pc => pc.Is(CustomRoles.Godfather)).FirstOrDefault();
            if (god != null)
            {
                TargetArrow.Add(janitor.PlayerId, god.PlayerId);
                Logger.Info($"{Player.GetNameWithRole()} : Janitor.TargetArrowAdd", "G&J");
            }
        }
    }

    public float CalculateKillCooldown() => canNormalKill ? JanitorKillCooldown : CleanCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        // ゴッドファーザー死亡後、設定により通常キル可
        if (canNormalKill) return;

        // Janitorは必ずキルを防ぐ
        info.DoKill = false;

        var (killer, target) = info.AttemptTuple;

        // ターゲットがいない場合、ターゲットが対象じゃない場合は処理しない
        if (JanitorTarget.Count <= 0 || !JanitorTarget.Contains(target.PlayerId)) return;

        // ターゲットの状態を取得
        var targetPlayerState = PlayerState.GetByPlayerId(target.PlayerId);

        /* ターゲットを死体なしで霊界転送する */
        targetPlayerState.SetDead();
        target.RpcExileV2();
        targetPlayerState.DeathReason = CustomDeathReason.Clean;
        Logger.Info($"{Player.GetNameWithRole()} : ターゲット({target.GetNameWithRole()})を掃除", "G&J");

        // 掃除したプレイヤーはリストから削除
        JanitorTarget.Remove(target.PlayerId);
        // ターゲットの足止め解除
        Main.AllPlayerSpeed[target.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
        target.MarkDirtySettings();

        // 自身のキルクールリセット
        killer.SetKillCooldown();

        // バニラの表示更新:ターゲットの名前色、マーク表示更新の為全員をまわす
        Utils.NotifyRoles(ForceLoop: true);
    }

    public static void KillSuicide(byte deadTargetId)
    {
        // 後追い起こすかの判定処理
        if (!commonSuiside(deadTargetId)) return;

        /* 後追い処理 */
        isSuiside = true;
        PlayerState.GetByPlayerId(janitor.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
        janitor.RpcMurderPlayer(janitor);
        godfather.SetRealKiller(janitor);
        Logger.Info($"{janitor.GetNameWithRole()}の後追い:{godfather.GetNameWithRole()}", "KillFollowingSuicide");
    }
    public static void VoteSuicide(byte deadTargetId)
    {
        // 後追い起こすかの判定処理
        if (!commonSuiside(deadTargetId)) return;

        /* 後追い処理 */
        isSuiside = true;
        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, janitor.PlayerId);
        godfather.SetRealKiller(janitor);
        Logger.Info($"{janitor.GetNameWithRole()}の後追い:{godfather.GetNameWithRole()}", "VoteFollowingSuicide");
    }
    /// <summary>
    /// 共通処理
    /// </summary>
    /// <returns>falseは後追いしない</returns>
    private static bool commonSuiside(byte deadTargetId)
    {
        // 既にジャニターが死んでいる時は関係ない
        if (!janitor.IsAlive()) return false;
        if (isSuiside) return false;

        // 生き残り設定でない場合
        if (!IsLastImpostor(deadTargetId))
        {
            // ゴッドファーザー死亡後、生き残るか
            if (JanitorIsAliveSetting(deadTargetId)) return false;
        }

        return true;
    }
    /// <summary>
    /// ジャニターが最後のインポスターか
    /// 死亡対象がインポスターの時のみ判定する。
    /// </summary>
    private static bool IsLastImpostor(byte deadTargetId)
    {
        // 死亡したのがインポスターでない時(ジャニター自身を除く)は関係ない
        var target = Utils.GetPlayerById(deadTargetId);
        if (!target.Is(CustomRoleTypes.Impostor) || target == janitor) return false;
        // 生き残り設定の時のみ判定を使用する
        if (!IsAliveAfterGodfatherDead || JanitorCanKill) return false;

        var impostorCount = Utils.AlivePlayersCount(CountTypes.Impostor);
        Logger.Info($"ImpostorCount = {impostorCount}", "Janitor");
        return impostorCount == 2; //死亡する判定前なので2。2 - 1で残りジャニターが1になった時にtrueを返す。
    }
    /// <summary>
    /// ゴッドファーザーが死亡した際の共通処理
    /// </summary>
    /// <returns>trueで生き残る</returns>
    private static bool JanitorIsAliveSetting(byte deadTargetId)
    {
        // 死亡したのがゴッドファーザーでない時は関係ない
        if (Utils.GetPlayerById(deadTargetId) != godfather) return true;
        // 生き残れない設定の時、falseで後追い処理へ続く
        if (!IsAliveAfterGodfatherDead) return false;

        // キルできる設定の時
        if (JanitorCanKill)
        {
            // ノーマルキルできる設定への切り替え
            canNormalKill = true;
            janitor.ResetKillCooldown();
        }
        return true;
    }

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref string roleText)
    {
        // 相方の役職名を表示させる
        if (seen.Is(CustomRoles.Godfather)) enabled = true;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //seerおよびseenが自分である場合以外、または会議中は関係なし
        if (!Is(seer) || !Is(seen) || isForMeeting) return string.Empty;

        StringBuilder sb = new();
        // ゴッドファーザーへの矢印表示
        if (TrackGodfather)
        {
            // 矢印の取得
            string arrow = TargetArrow.GetArrows(Player, godfather.PlayerId);
            // 矢印表示があれば
            if (arrow.Length >= 0)
            {
                // 色を付けてsb追加
                sb.Append(arrow.Color(Palette.ImpostorRed));
            }
        }

        // ジャニターターゲットへの矢印表示
        if (TrackTarget && JanitorTarget.Count > 0)
        {
            // 矢印の取得
            string arrow = TargetArrow.GetArrows(Player, JanitorTarget.ToArray());
            // 矢印表示があれば
            if (arrow.Length >= 0)
            {
                // 色を付けてsb追加
                sb.Append("<color=#cc00cc>").Append(arrow).Append("</color>");
            }
        }

        return sb.ToString();
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("DeathReason.Clean");
        return true;
    }
}
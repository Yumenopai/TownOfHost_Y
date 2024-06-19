using System.Linq;
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using UnityEngine;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Elder : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Elder),
            player => new Elder(player),
            CustomRoles.Elder,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1900,//仮
            SetupOptionItem,
            "長老",
            "#2B6442"//千歳緑
        );
    public Elder(PlayerControl player) : base(RoleInfo, player)
    {
        DiaInLife = OptionDiaInLife.GetBool();
        Lifetime = OptionLifetime.GetFloat();
        roleChanged = false;
        GuardCount = 0;
    }

    private static OptionItem OptionDiaInLife;
    private static OptionItem OptionLifetime;
    private static bool roleChanged;
    private int GuardCount;
    private static bool DiaInLife;
    private float Lifetime; // 寿命の時間を管理するプロパティ
    public static readonly CustomRoles[] ChangeRoles = { CustomRoles.Crewmate };

    enum OptionName
    {
        ElderDiaInLife,
        ElderLifetime,
    }
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        OptionDiaInLife = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ElderDiaInLife, false, false);
        OptionLifetime = FloatOptionItem.Create(RoleInfo, 11, OptionName.ElderLifetime, new(5f, 1800f, 5f), 900f, false, OptionDiaInLife)
                .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        if (!target.Is(CustomRoles.Elder)) return false;

        if (IsTaskFinished)//タスク完了時の処理(自身のkillを通す＆killer側も死ぬ。)
        {
            info.CanKill = true;
            killer.RpcMurderPlayer(killer);
            PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.CounterAttack;
        }
        else
        {
            if (GuardCount > 0)// GuardCountが1以上の場合はキルを通す
            {
                info.CanKill = true;
            }
            else// GuardCountが0の場合はキルを通さない。
            {
                info.CanKill = false;
                roleChanged = true;
                killer.RpcProtectedMurderPlayer(target);
                killer.SetKillCooldown();
            }

            GuardCount++; // GuardCountを増やす
        }
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (DiaInLife)
        {
            // 寿命のカウントダウン
            Lifetime -= Time.fixedDeltaTime;

            // 寿命が尽きたかどうかをチェック
            if (Lifetime <= 0f)
            {
                // プレイヤーを死亡させる
                MyState.DeathReason = CustomDeathReason.Senility;//死因：老衰
                Player.RpcMurderPlayer(Player);
                DiaInLife = false;
            }
        }
        if (!Player.IsAlive() && roleChanged && !IsTaskFinished)
        {//エルダーが死んでいる＆roleChangedがtrueである＆タスクが終わってない場合
            ChangeRole();
            DiaInLife = false;
        }
        else if (!Player.IsAlive())
        {
            DiaInLife = false; // プレイヤーが死亡している間もDiaInLifeをfalseに設定する
        }
    }
    public void ChangeRole()
    {
        var playersCrewmate = Main.AllAlivePlayerControls.Where(player => player.Is(CustomRoleTypes.Crewmate));
        foreach (var player in playersCrewmate)
        {
            player.RpcSetCustomRole(ChangeRoles[0]); // クルーメイトに変更
        }
        Utils.NotifyRoles(); // 役職変更を通知
        Utils.MarkEveryoneDirtySettings();

        roleChanged = false;
    }
}
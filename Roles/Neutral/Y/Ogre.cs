using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral
{
    public sealed class Ogre : RoleBase, IKiller, ISchrodingerCatOwner, IAdditionalWinner
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Ogre),
                player => new Ogre(player),
                CustomRoles.Ogre,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                (int)Options.offsetId.NeuY + 900,
                SetupOptionItem,
                "鬼",
                "#fe8b04",
                true,
                countType: CountTypes.Crew
            );
        public Ogre(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            HasImpostorVision = OptionHasImpostorVision.GetBool();
            CanVent = OptionCanVent.GetBool();
            KillSuccessRate = OptionKillSuccessRate.GetInt();
            KilledGuardRate = OptionKilledGuardRate.GetInt();
        }

        private static OptionItem OptionKillCooldown;
        private static OptionItem OptionHasImpostorVision;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionKillSuccessRate;
        public static OptionItem OptionKilledGuardRate;
        enum OptionName
        {
            OgreKillSuccessRate,
            OgreKilledGuardRate,
        }
        private static float KillCooldown;
        private static bool HasImpostorVision;
        private static bool CanVent;
        private static int KillSuccessRate;
        private static int KilledGuardRate;

        int nowKillRate = 100;

        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Ogre;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.ImpostorVision, false, false);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, false, false);
            OptionKillSuccessRate = IntegerOptionItem.Create(RoleInfo, 13, OptionName.OgreKillSuccessRate, new(5, 100, 5), 20, false)
                .SetValueFormat(OptionFormat.Percent);
            OptionKilledGuardRate = IntegerOptionItem.Create(RoleInfo, 14, OptionName.OgreKilledGuardRate, new(5, 100, 5), 30, false)
                .SetValueFormat(OptionFormat.Percent);
        }
        public override void Add()
        {
            nowKillRate = 100;
        }

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!Is(info.AttemptKiller) || info.IsSuicide || !info.CanKill) return;
            (var killer, var target) = info.AttemptTuple;

            int chance = IRandom.Instance.Next(1, 101);
            if (chance > nowKillRate)
            {
                info.CanKill = false;
                killer.RpcProtectedMurderPlayer(target);
                killer.ResetKillCooldown();
                return;
            }

            // 次回の確率計算
            nowKillRate = nowKillRate * KillSuccessRate / 100;
            if (nowKillRate < 1) nowKillRate = 1;

            Logger.Info($"{killer.GetNameWithRole()} : 次回キル確率{nowKillRate}%", "Ogre");
        }
        public override bool OnCheckMurderAsTarget(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;
            // 直接キル出来る役職チェック
            if (killer.GetCustomRole().IsDirectKillRole()) return true;

            int chance = IRandom.Instance.Next(1, 101);
            if (chance > KilledGuardRate) return true; //そのままキル

            killer.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(target);
            killer.ResetKillCooldown();
            target.ResetKillCooldown();

            if (!killer.Is(CustomRoleTypes.Impostor)) //インポスター以外からのキル
            {
                target.RpcMurderPlayer(killer);
            }

            info.CanKill = false;
            return true;
        }
        public bool CheckWin(ref CustomRoles winnerRole)
        {
            return Player.IsAlive() && Main.AliveImpostorCount >= 1;
        }

        public override string GetProgressText(bool comms = false) => $"[{nowKillRate}%]".Color(RoleInfo.RoleColor);
        public float CalculateKillCooldown() => KillCooldown;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
        public bool CanUseImpostorVentButton() => CanVent;
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
    }
}

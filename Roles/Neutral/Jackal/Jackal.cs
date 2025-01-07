using System.Linq;
using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Modules;
using AmongUs.Data.Player;

namespace TownOfHostY.Roles.Neutral
{
    public sealed class Jackal : RoleBase, IKiller, ISchrodingerCatOwner
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Jackal),
                player => new Jackal(player),
                CustomRoles.Jackal,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                (int)Options.offsetId.NeuJackal + 0,
                SetupOptionItem,
                "ジャッカル",
                "#00b4eb",
                true,
                countType: CountTypes.Jackal,
                assignInfo: new RoleAssignInfo(CustomRoles.Jackal, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                }
            );
        public Jackal(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            HasImpostorVision = OptionHasImpostorVision.GetBool();
            CanSeeNameMushroomMixup = OptionCanSeeNameMushroomMixup.GetBool();
            canCreateSidekick = OptionCanCreateSidekick.GetBool();

            canSidekickCount = canCreateSidekick ? 1 : 0;
            sidekickTarget = new();
        }

        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionHasImpostorVision;
        private static OptionItem OptionCanSeeNameMushroomMixup;
        private static OptionItem OptionCanCreateSidekick;
        enum OptionName
        {
            JackalCanSeeNameMushroomMixup,
            JackalCanCreateSidekick,
        }
        public static float KillCooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        public static bool HasImpostorVision;
        public static bool CanSeeNameMushroomMixup;
        private static bool canCreateSidekick;

        private int canSidekickCount;
        private static List<byte> sidekickTarget = new();
        public static CustomRoles[] AdditionalRoles => canCreateSidekick ? [CustomRoles.JSidekick] : [];

        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
            OptionCanSeeNameMushroomMixup = BooleanOptionItem.Create(RoleInfo, 14, OptionName.JackalCanSeeNameMushroomMixup, true, false);
            OptionCanCreateSidekick = BooleanOptionItem.Create(RoleInfo, 15, OptionName.JackalCanCreateSidekick, true, false);
            Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
        }
        public override void Add()
        {
            Player.AddDoubleTrigger();
        }

        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
        public override bool OnInvokeSabotage(SystemTypes systemType) => CanUseSabotage;
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;

            if (target != null && target.Is(CustomRoles.JSidekick))
            {
                info.DoKill = false;
                Logger.Info($"cantKillSidekick jackal: {killer?.name}, sidekick: {target?.name}", "Jackal");
                return;
            }

            if (canSidekickCount <= 0) return;

            if (killer.CheckDoubleTrigger(target, () => { SetSidekick(killer, target); }))
            {
                sidekickTarget.Remove(target.PlayerId);
                return;
            }

            //サイドキック中のターゲットはキルができない
            sidekickTarget.Add(target.PlayerId);

            info.DoKill = false;
        }
        public void SetSidekick(PlayerControl jackal, PlayerControl sidekick)
        {
            jackal.RpcProtectedMurderPlayer(sidekick);
            jackal.SetKillCooldown();

            canSidekickCount--;

            Logger.Info($"Create JSidekick:{sidekick.name} ({sidekick.GetCustomRole()}=>{CustomRoles.JSidekick})", "Jackal");
            RoleTypes roleTypes;
            foreach (var pc in Main.AllPlayerControls.Where(x => x != null && !x.Data.Disconnected))
            {
                //サイドキック視点
                roleTypes = RoleTypes.Scientist;
                if (pc.PlayerId == sidekick.PlayerId) roleTypes = RoleTypes.Impostor;
                else if (!pc.IsAlive()) roleTypes = RoleTypes.CrewmateGhost;
                else if (pc.GetCustomRole().GetRoleInfo()?.CountType == CountTypes.Jackal) roleTypes = RoleTypes.Impostor;
                else if (pc.GetCustomRole().GetRoleTypes() == RoleTypes.Noisemaker) roleTypes = RoleTypes.Noisemaker;

                if (sidekick.PlayerId == PlayerControl.LocalPlayer.PlayerId) pc.StartCoroutine(pc.CoSetRole(roleTypes, true));
                else pc.RpcSetRoleDesync(roleTypes, sidekick.GetClientId());

                if (pc.PlayerId == sidekick.PlayerId) continue;

                //他クルー視点
                roleTypes = RoleTypes.Scientist;
                if (pc.GetCustomRole().GetRoleInfo()?.CountType == CountTypes.Jackal) roleTypes = RoleTypes.Impostor;

                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) sidekick.StartCoroutine(sidekick.CoSetRole(roleTypes, true));
                else sidekick.RpcSetRoleDesync(roleTypes, pc.GetClientId());
            }
            sidekick.RpcSetCustomRole(CustomRoles.JSidekick);

            //サイドキック⇔ジャッカル色表示
            NameColorManager.Add(jackal.PlayerId, sidekick.PlayerId, jackal.GetRoleColorCode());
            NameColorManager.RemoveAll(sidekick.PlayerId);
            NameColorManager.Add(sidekick.PlayerId, jackal.PlayerId, jackal.GetRoleColorCode());

            PlayerGameOptionsSender.SetDirty(Player.PlayerId);
            PlayerGameOptionsSender.SetDirty(sidekick.PlayerId);
            Utils.NotifyRoles(SpecifySeer: jackal);
            Utils.NotifyRoles(SpecifySeer: sidekick);

            //サイドキックターゲットの解除
            sidekickTarget.Remove(sidekick.PlayerId);
        }
        public override bool OnCheckMurderAsTarget(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;

            //キル可能判定（サイドキック中ターゲットはキル不可）
            if (sidekickTarget.Contains(killer.PlayerId))
            {
                Logger.Info($"{killer.GetNameWithRole()}はサイドキックターゲットのため、キルはキャンセルされました。", "Jackal");
                return false;
            }

            return true;
        }
        public override void OnMurderPlayerAsTarget(MurderInfo info)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Player.PlayerId != info.AttemptTarget.PlayerId) return;

            Logger.Info($"checkPromoted byKill Jackal:{Player?.Data?.name}", "Jackal");
            new LateTask(() => CheckPromoted(), 0.5f, "JackalPromotedByKill");
        }
        public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Player.PlayerId != exiled.PlayerId) return;

            Logger.Info($"checkPromoted byExiled Jackal:{Player?.Data?.name}", "Jackal");
            new LateTask(() => CheckPromoted(), 0.5f, "JackalPromotedByExiled");
        }
        public override void AfterMeetingTasks()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            if (Main.AllAlivePlayerControls.Any(pc => pc.Is(CustomRoles.Jackal))) return;

            Logger.Info($"checkPromoted AfterMeeting Jackal:{Player?.Data?.PlayerName}", "Jackal");
            new LateTask(() => CheckPromoted(), 0.5f, "JackalPromotedAfterMeeting");
        }
        public static void CheckPromoted()
        {
            if (Main.AllAlivePlayerControls.Any(pc => pc.Is(CustomRoles.JSidekick) &&
            ((JSidekick)pc.GetRoleClass()).Promoted))
            {
                Logger.Info($"NotPromote exists promoted", "Jackal");
                return;
            }

            var list = Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoles.JSidekick)).ToArray();

            Logger.Info($"CheckPromote sidekick count: {list.Count()}", "Jackal");
            if (list.Count() < 1) return;

            var sidekick = list[IRandom.Instance.Next(list.Count())];
            ((JSidekick)sidekick.GetRoleClass()).BePromoted();
        }
    }
}
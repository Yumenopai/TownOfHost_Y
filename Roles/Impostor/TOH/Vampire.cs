using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Translator;

namespace TownOfHostY.Roles.Impostor
{
    public sealed class Vampire : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Vampire),
                player => new Vampire(player),
                CustomRoles.Vampire,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                (int)Options.offsetId.ImpTOH + 400,
                SetupOptionItem,
                "ヴァンパイア",
                introSound: () => GetIntroSound(RoleTypes.Shapeshifter)
            );
        public Vampire(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillDelay = OptionKillDelay.GetFloat();

            BittenPlayers.Clear();
        }

        static OptionItem OptionKillDelay;
        enum OptionName
        {
            VampireKillDelay
        }

        static float KillDelay;

        Dictionary<byte, float> BittenPlayers = new(14);

        private static void SetupOptionItem()
        {
            OptionKillDelay = FloatOptionItem.Create(RoleInfo, 10, OptionName.VampireKillDelay, new(1f, 1000f, 1f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
        }
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;

            if (target.Is(CustomRoles.Bait)) return;
            if (info.IsFakeSuicide) return;

            //誰かに噛まれていなければ登録
            if (!BittenPlayers.ContainsKey(target.PlayerId))
            {
                killer.SetKillCooldown();
                BittenPlayers.Add(target.PlayerId, 0f);
                // プレイヤーがプラットフォームに乗ることを防ぐ
                PlayerState.GetByPlayerId(target.PlayerId).CanUseMovingPlatform = false;
            }
            info.DoKill = false;
        }
        public override void OnFixedUpdate(PlayerControl _)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;

            foreach (var (targetId, timer) in BittenPlayers.ToArray())
            {
                if (timer >= KillDelay)
                {
                    var target = Utils.GetPlayerById(targetId);
                    KillBitten(target);
                    BittenPlayers.Remove(targetId);
                }
                else
                {
                    BittenPlayers[targetId] += Time.fixedDeltaTime;
                }
            }
        }
        public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __)
        {
            foreach (var targetId in BittenPlayers.Keys)
            {
                var target = Utils.GetPlayerById(targetId);
                KillBitten(target, true);
            }
            BittenPlayers.Clear();
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("VampireBiteButtonText");
            return true;
        }

        private void KillBitten(PlayerControl target, bool isButton = false)
        {
            var vampire = Player;
            if (target.IsAlive())
            {

                if (target.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    // プレイヤーが梯子を使用している場合の特別なキル処理
                    target.SetRealKiller(vampire);
                    target.RpcMurderPlayer(target);
                    PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bite;
                    Logger.Info($"Vampireに噛まれている{target.name}は梯子を使用中にキルされました。", "Vampire.KillBitten");
                }
                else
                {

                    if (CustomRoleManager.OnCheckMurder(
                        vampire, target,
                        target, target))
                    {
                        PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bite;
                        target.SetRealKiller(vampire);
                        Logger.Info($"Vampireに噛まれている{target.name}を自爆させました。", "Vampire.KillBitten");
                        if (!isButton && vampire.IsAlive())
                        {
                            RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);
                        }
                    }
                    else
                    {
                        Logger.Info($"Vampireに噛まれている{target.name}はキルされませんでした。", "Vampire.KillBitten");
                    }
                }
            }
            else
            {
                Logger.Info($"Vampireに噛まれている{target.name}はすでに死んでいました。", "Vampire.KillBitten");
            }
        }
    }
}

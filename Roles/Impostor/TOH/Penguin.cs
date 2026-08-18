using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Translator;

namespace TownOfHostY.Roles.Impostor;

class Penguin : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Penguin),
            player => new Penguin(player),
            CustomRoles.Penguin,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpTOH + 1800,
            SetupOptionItem,
            "ペンギン"
        );
    public Penguin(PlayerControl player)
        : base(RoleInfo, player)
    {
        AbductTimerLimit = OptionAbductTimerLimit.GetFloat();
        MeetingKill = OptionMeetingKill.GetBool();
        KilledAbductVictim = OptionKilledAbductVictim.GetBool();

    }
    public override void OnDestroy()
    {
        PenguinList?.Remove(this);
        AbductVictim = null;
    }

    static OptionItem OptionAbductTimerLimit;
    static OptionItem OptionMeetingKill;
    static OptionItem OptionKilledAbductVictim;

    enum OptionName
    {
        PenguinAbductTimerLimit,
        PenguinMeetingKill,
        PenguinKilledAbductVictim,
    }
    private static float AbductTimerLimit;
    private static bool MeetingKill;
    private static bool KilledAbductVictim;

    static readonly HashSet<Penguin> PenguinList = new();
    private sealed class AbductState
    {
        public byte VictimId;
        public float Timer;
    }
    private static readonly Dictionary<byte, AbductState> AbductStates = new();

    public PlayerControl AbductVictim;
    private float AbductTimer;
    private bool stopCount;
    private bool killScheduled;

    //拉致中にキルしそうになった相手の能力を使わせないための処置
    public bool IsKiller => AbductVictim == null;
    public static void SetupOptionItem()
    {
        OptionAbductTimerLimit = FloatOptionItem.Create(RoleInfo, 11, OptionName.PenguinAbductTimerLimit, new(5f, 20f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMeetingKill = BooleanOptionItem.Create(RoleInfo, 12, OptionName.PenguinMeetingKill, false, false);
        OptionKilledAbductVictim = BooleanOptionItem.Create(RoleInfo, 13, OptionName.PenguinKilledAbductVictim, true, false);
    }
    public override void Add()
    {
        PenguinList.Add(this);
        stopCount = false;

        if (AbductStates.TryGetValue(Player.PlayerId, out var state))
        {
            AbductVictim = Utils.GetPlayerById(state.VictimId);
            AbductTimer = state.Timer;

            if (AbductVictim == null || !AbductVictim.IsAlive())
            {
                AbductStates.Remove(Player.PlayerId);
                AbductVictim = null;
                AbductTimer = 255f;
            }
        }
        else
        {
            AbductVictim = null;
            AbductTimer = 255f;
        }

        PushShapeshiftCooldown();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown =
            AbductVictim != null ? Mathf.Max(0f, AbductTimer) : 255f;
    }
    private void PushShapeshiftCooldown()
    {
        ApplyGameOptions(null);
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.PenguinSync);

        sender.Writer.Write(AbductVictim?.PlayerId ?? 255);
    }

    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.PenguinSync) return;

        var victim = reader.ReadByte();
        if (victim == 255)
        {
            AbductVictim = null;
            AbductTimer = 255f;
            AbductStates.Remove(Player.PlayerId);
        }
        else
        {
            AbductVictim = Utils.GetPlayerById(victim);
            AbductTimer = AbductTimerLimit;
            AbductStates[Player.PlayerId] = new AbductState
            {
                VictimId = victim,
                Timer = AbductTimer
            };
        }
    }

    // ペンギン拉致られ中のターゲットからキルできるか falseはその後のキル処理をキャンセル
    public static bool CanKilledByTarget(PlayerControl pc)
    {
        if (!CustomRoles.Penguin.IsPresent() || KilledAbductVictim) return true;
        foreach (var pen in PenguinList)
        {
            if (pen.AbductVictim == pc) return false;
        }
        return true;
    }

    void AddVictim(PlayerControl target)
    {
        stopCount = false;
        PlayerState.GetByPlayerId(target.PlayerId).CanUseMovingPlatform = MyState.CanUseMovingPlatform = false;
        AbductVictim = target;
        AbductTimer = AbductTimerLimit;
        AbductStates[Player.PlayerId] = new AbductState
        {
            VictimId = target.PlayerId,
            Timer = AbductTimer
        };
        PushShapeshiftCooldown();
        SendRPC();
    }

    void RemoveVictim()
    {
        if (AbductVictim != null)
        {
            PlayerState.GetByPlayerId(AbductVictim.PlayerId).CanUseMovingPlatform = true;
            AbductVictim = null;
        }
        MyState.CanUseMovingPlatform = true;
        AbductStates.Remove(Player.PlayerId);
        AbductTimer = 255f;
        killScheduled = false;
        PushShapeshiftCooldown();
        SendRPC();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var target = info.AttemptTarget;
        if (AbductVictim != null)
        {
            if (target != AbductVictim)
            {
                //拉致中は拉致相手しか切れない
                Player.RpcMurderPlayer(AbductVictim);
                Player.ResetKillCooldown();
                info.DoKill = false;
            }
            RemoveVictim();
        }
        else
        {
            info.DoKill = false;
            AddVictim(target);
        }
    }
    public bool OverrideKillButtonText(out string text)
    {
        if (AbductVictim != null)
        {
            text = GetString("KillButtonText");
        }
        else
        {
            text = GetString("PenguinKillButtonText");
        }
        return true;
    }
    public override string GetAbilityButtonText()
    {
        return GetString("PenguinTimerText");
    }
    public override bool CanUseAbilityButton()
    {
        return AbductVictim != null;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        stopCount = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (AbductVictim == null) return;
        if (MeetingKill)
        {
            var victim = AbductVictim;
            Player.RpcMurderPlayer(victim);
            RemoveVictim();
            return;
        }

        RemoveVictim();
    }
    public void RestartAbduct()
    {
        RemoveVictim();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask) return;
        if (AbductVictim == null && AbductStates.TryGetValue(Player.PlayerId, out var saved))
        {
            AbductVictim = Utils.GetPlayerById(saved.VictimId);
            AbductTimer = saved.Timer;
        }

        if (AbductVictim == null) return;

        if (!Player.IsAlive() || !AbductVictim.IsAlive())
        {
            RemoveVictim();
            return;
        }

        if (!stopCount)
            AbductTimer -= Time.fixedDeltaTime;

        AbductTimer = Mathf.Max(0f, AbductTimer);
        if (AbductStates.TryGetValue(Player.PlayerId, out var state))
            state.Timer = AbductTimer;

        AURoleOptions.ShapeshifterCooldown = AbductTimer;

        if (AbductTimer <= 0f)
        {
            if (killScheduled) return;
            if (Player.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
                AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                return;

            killScheduled = true;

            var abductVictim = AbductVictim;
            abductVictim.Data.IsDead = true;
            GameData.Instance.DirtyAllData();

            _ = new LateTask(() =>
            {
                if (abductVictim == null) return;

                var sId = abductVictim.NetTransform.lastSequenceId + 5;
                abductVictim.NetTransform.SnapTo(Player.transform.position, (ushort)sId);
                Player.MurderPlayer(abductVictim);

                var sender = CustomRpcSender.Create("PenguinMurder");
                {
                    sender.AutoStartRpc(abductVictim.NetTransform.NetId, (byte)RpcCalls.SnapTo);
                    {
                        NetHelpers.WriteVector2(Player.transform.position, sender.stream);
                        sender.Write(abductVictim.NetTransform.lastSequenceId);
                    }
                    sender.EndRpc();

                    sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.MurderPlayer);
                    {
                        sender.WriteNetObject(abductVictim);
                        sender.Write((int)ExtendedPlayerControl.SucceededFlags);
                    }
                    sender.EndRpc();
                }
                sender.SendMessage();

                RemoveVictim();
            }, 0.3f, "PenguinMurder");

            return;
        }
        if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            var position = Player.transform.position;
            if (Player.PlayerId != 0)
            {
                AbductVictim.RpcSnapTo(position);
            }
            else
            {
                var victim = AbductVictim;
                _ = new LateTask(() =>
                {
                    if (victim != null && victim.IsAlive())
                        victim.RpcSnapTo(position);
                }, 0.25f, "");
            }
        }
    }

}
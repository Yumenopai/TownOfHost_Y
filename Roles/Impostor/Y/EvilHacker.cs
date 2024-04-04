using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHostY.Roles.Impostor;

public sealed class EvilHacker : RoleBase, IImpostor, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilHacker),
            player => new EvilHacker(player),
            CustomRoles.EvilHacker,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpSpecial + 0,
            null,
            "イビルハッカー"
        );
    public EvilHacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        if (OptionEvilHackerFixed.GetBool())
        {
            List<Role> selectRole = new();
            foreach (var role in SetRoleDic.Keys)
            {
                if (SetRoleDic[role].GetBool()) selectRole.Add(role);
            }

            if (selectRole.Count == 0) nowRole = Role.EvilHakka;
            else
            {
                int chance = IRandom.Instance.Next(0, selectRole.Count);
                nowRole = selectRole[chance];
            }
        }
        else
        {
            int chance = IRandom.Instance.Next(0, (int)Role.enumCount);
            nowRole = (Role)chance;
        }
        Logger.Info($"EvilHackerRole : {nowRole}", "EvilHacker");
        if (nowRole == Role.EvilVipper)
        {
            foreach (var addon in sub)
            {
                player.RpcSetCustomRole(addon);
            }
        }

        CustomRoleManager.OnMurderPlayerOthers.Add(HandleMurderRoomNotify);
        instances.Add(this);
    }
    public override void OnDestroy()
    {
        instances.Remove(this);
    }
    private static HashSet<EvilHacker> instances = new(1);
    private HashSet<MurderNotify> activeNotifies = new(2);
    public static bool IsColorCamouflage = false;
    enum Role
    {
        EvilHacker,
        EvilWhiter,
        EvilReder,
        EvilFaller,
        EvilFire,
        EvilVipper,
        EvilHakka,

        enumCount,
    }
    Role nowRole = Role.EvilHacker;
    public static bool IsExistEvilWhiterOrReder()
    { return instances.Where(e => e.nowRole is Role.EvilWhiter or Role.EvilReder).Any(); }
    public static bool IsExistEvilFaller()
    { return instances.Where(e => e.nowRole is Role.EvilFaller).Any(); }

    CustomRoles[] sub = new[]
{
        CustomRoles.AddLight,
        CustomRoles.AddWatch,
        CustomRoles.Autopsy,
        CustomRoles.Management,
        CustomRoles.AddSeer,
        CustomRoles.TieBreaker,
        CustomRoles.VIP,
    };

    private static OptionItem OptionEvilHackerFixed;
    private static Dictionary<Role, OptionItem> SetRoleDic = new();
    enum OptionName
    {
        EvilHackerFixedRole,
        SetEHRole,
    }


    // 直接設置
    public static void SetupRoleOptions()
    {
        TextOptionItem.Create(41, "Head.LimitedTimeRole", TabGroup.ImpostorRoles)
            .SetColor(Color.yellow);
        var spawnOption = IntegerOptionItem.Create(RoleInfo.ConfigId, "EvilHacker", new(0, 100, 10), 0, TabGroup.ImpostorRoles, false)
            .SetColor(RoleInfo.RoleColor)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.Standard) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(RoleInfo.ConfigId + 1, "Maximum", new(1, 15, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.Standard);

        Options.CustomRoleSpawnChances.Add(RoleInfo.RoleName, spawnOption);
        Options.CustomRoleCounts.Add(RoleInfo.RoleName, countOption);

        OptionEvilHackerFixed = BooleanOptionItem.Create(RoleInfo, 10, OptionName.EvilHackerFixedRole, false, false);

        SetUpHackerRoleOptions(20);
    }
    private static void SetUpHackerRoleOptions(int idOffset)
    {
        foreach (var role in EnumHelper.GetAllValues<Role>())
        {
            if (role is Role.enumCount) continue;

            SetUpHROption(role, idOffset, true, OptionEvilHackerFixed);
            idOffset++;
        }
    }
    private static void SetUpHROption(Role role, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        if (parent == null) parent = RoleInfo.RoleOption;
        var roleName = Translator.GetRoleString($"R{role}");
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Palette.ImpostorRed, roleName) } };
        SetRoleDic[role] = BooleanOptionItem.Create(id, OptionName.SetEHRole + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
        SetRoleDic[role].ReplacementDictionary = replacementDic;
    }

    /// <summary>相方がキルした部屋を通知する設定がオンなら各プレイヤーに通知を行う</summary>
    private static void HandleMurderRoomNotify(MurderInfo info)
    {
        if (info.IsMeeting) return;

        foreach (var evilHacker in instances)
        {
            if(evilHacker.nowRole == Role.EvilHacker)
                evilHacker.OnMurderPlayer(info);
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (!Player.IsAlive())
        {
            return;
        }

        if (nowRole == Role.EvilHacker)
        {
            var admins = AdminProvider.CalculateAdmin();
            var builder = new StringBuilder(512);

            // 送信するメッセージを生成
            foreach (var admin in admins)
            {
                var entry = admin.Value;
                if (entry.TotalPlayers <= 0)
                {
                    continue;
                }
                // インポスターがいるなら星マークを付ける
                if (entry.NumImpostors > 0)
                {
                    builder.Append(ImpostorMark);
                }
                // 部屋名と合計プレイヤー数を表記
                builder.Append(DestroyableSingleton<TranslationController>.Instance.GetString(entry.Room));
                builder.Append(": ");
                builder.Append(entry.TotalPlayers);
                // 死体があったら死体の数を書く
                if (entry.NumDeadBodies > 0)
                {
                    builder.Append('(').Append(Translator.GetString("Deadbody"));
                    builder.Append('×').Append(entry.NumDeadBodies).Append(')');
                }
                builder.Append('\n');
            }

            // 送信
            var message = builder.ToString();
            var title = Utils.ColorString(Color.green, Translator.GetString("LastAdminInfo"));

            _ = new LateTask(() =>
            {
                if (GameStates.IsInGame)
                {
                    Utils.SendMessage(message, Player.PlayerId, title);
                }
            }, 4f, "EvilHacker Admin Message");
        }
        else if(nowRole is Role.EvilWhiter or Role.EvilReder)
        {
            if (IsColorCamouflage && AmongUsClient.Instance.AmHost)
            {
                Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(false, pc));
                Utils.NotifyRoles(NoCache: true);
            }
        }
    }
    // 7 = 白　0 = 赤
    public static GameData.PlayerOutfit CamouflageWhiteOutfit = new GameData.PlayerOutfit().Set("", 7, "", "", "", "");
    public static GameData.PlayerOutfit CamouflageRedOutfit = new GameData.PlayerOutfit().Set("", 0, "", "", "", "");
    private void OnMurderPlayer(MurderInfo info)
    {
        if (!Player.IsAlive()) return;
        // 生きてる間に相方のキルでキルフラが鳴った場合に通知を出す
        if (nowRole == Role.EvilHacker && CheckKillFlash(info) && info.AttemptKiller != Player)
        {
            RpcCreateMurderNotify(info.AttemptTarget.GetPlainShipRoom()?.RoomId ?? SystemTypes.Hallway);
        }
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            if (nowRole == Role.EvilWhiter)
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(true, pc, CamouflageWhiteOutfit));
                    IsColorCamouflage = true;
                    Utils.NotifyRoles(NoCache: true);
                }
                _ = new LateTask(() =>
                {
                    if (AmongUsClient.Instance.AmHost)
                    {
                        Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(false, pc));
                        IsColorCamouflage = false;
                        Utils.NotifyRoles(NoCache: true);
                    }
                }, 7f, "EvilWhiter IsCamouflage");
            }
            else if (nowRole == Role.EvilReder)
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(true, pc, CamouflageRedOutfit));
                    IsColorCamouflage = true;
                    Utils.NotifyRoles(NoCache: true);
                }
                _ = new LateTask(() =>
                {
                    if (AmongUsClient.Instance.AmHost)
                    {
                        Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(false, pc));
                        IsColorCamouflage = false;
                        Utils.NotifyRoles(NoCache: true);
                    }
                }, 7f, "EvilWhiter IsCamouflage");
            }
            else if (nowRole == Role.EvilFaller)
            {
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Fall;
            }
            else if (nowRole == Role.EvilHakka)
            {
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.etc;
            }
            else if (nowRole == Role.EvilFire)
            {
                //爆破処理はホストのみ
                if (AmongUsClient.Instance.AmHost)
                {
                    (float d, PlayerControl pc) nearTarget = (2.3f, null);
                    foreach (var fire in Main.AllAlivePlayerControls)
                    {
                        if (fire == killer || fire == target) continue;
                        var pos = target.transform.position;
                        var dis = Vector2.Distance(pos, fire.transform.position);

                        if (dis < nearTarget.d)
                        {
                            nearTarget = (dis, fire);
                        }
                    }
                    if (nearTarget.pc != null)
                    {
                        PlayerState.GetByPlayerId(nearTarget.pc.PlayerId).DeathReason = CustomDeathReason.Bombed;
                        nearTarget.pc.SetRealKiller(killer);
                        nearTarget.pc.RpcMurderPlayer(nearTarget.pc);
                        Player.MarkDirtySettings();
                    }
                }
            }
        }
    }

    private void RpcCreateMurderNotify(SystemTypes room)
    {
        if (nowRole == Role.EvilHacker)
        {
            CreateMurderNotify(room);
            if (AmongUsClient.Instance.AmHost)
            {
                using var sender = CreateSender(CustomRPC.EvilHackerCreateMurderNotify);
                sender.Writer.Write((byte)room);
            }
        }
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (nowRole == Role.EvilHacker)
        {
            if (rpcType == CustomRPC.EvilHackerCreateMurderNotify)
            {
                CreateMurderNotify((SystemTypes)reader.ReadByte());
            }
        }
    }
    /// <summary>
    /// 名前の下にキル発生通知を出す
    /// </summary>
    /// <param name="room">キルが起きた部屋</param>
    private void CreateMurderNotify(SystemTypes room)
    {
        if (nowRole == Role.EvilHacker)
        {
            activeNotifies.Add(new()
            {
                CreatedAt = DateTime.Now,
                Room = room,
            });
            if (AmongUsClient.Instance.AmHost)
            {
                Utils.NotifyRoles(SpecifySeer: Player);
            }
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (nowRole == Role.EvilHacker)
        {
            // 古い通知の削除処理 Mod入りは自分でやる
            if (!AmongUsClient.Instance.AmHost && Player != PlayerControl.LocalPlayer)
            {
                return;
            }
            if (activeNotifies.Count <= 0)
            {
                return;
            }
            // NotifyRolesを実行するかどうかのフラグ
            var doNotifyRoles = false;
            // 古い通知があれば削除
            foreach (var notify in activeNotifies)
            {
                if (DateTime.Now - notify.CreatedAt > NotifyDuration)
                {
                    activeNotifies.Remove(notify);
                    doNotifyRoles = true;
                }
            }
            if (doNotifyRoles && AmongUsClient.Instance.AmHost)
            {
                Utils.NotifyRoles(SpecifySeer: Player);
            }
        }
    }

    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
        => roleText = Translator.GetRoleString($"R{nowRole}");

    public static (string, int) AddMeetingDisplay()
    {
        if (!MeetingStates.FirstMeeting || instances.Count == 0) return ("", 0);

        string text = "";

        if (instances.Count == 1)
        {
            var nowRoleText = Translator.GetRoleString($"R{instances.FirstOrDefault().nowRole}");

            text = nowRoleText + Translator.GetString("MDisplay.EHnowRoleText")+ "\n";
            text = text.Color(Palette.ImpostorRed);
        }
        else
        {
            if (instances.Where(e => e.nowRole is Role.EvilHacker).Any())
            {
                text = $"{Translator.GetString("MDisplay.EHText")}\n".Color(Palette.ImpostorRed);
            }
            else
            {
                text = $"{Translator.GetString("MDisplay.EH?Text")}\n".Color(Palette.ImpostorRed);
            }
        }
        return (text, 1);
    }
    public static void FirstMeetingText()
    {
        if (!MeetingStates.FirstMeeting || instances.Count == 0) return;

        string text = "";
        string titleText = Translator.GetString("message.ReplicatorTitle").Color(Palette.ImpostorRed);
        foreach (var evil in instances)
        {
            text = Translator.GetRoleString($"R{evil.nowRole}") + "\n" + Translator.GetString($"{evil.nowRole}TInfo");

            Utils.SendMessage(text, title: titleText);
        }
    }


    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting || nowRole != Role.EvilHacker || seer != Player || seen != Player || activeNotifies.Count <= 0)
        {
            return base.GetSuffix(seer, seen, isForMeeting);
        }
        var roomNames = activeNotifies.Select(notify => DestroyableSingleton<TranslationController>.Instance.GetString(notify.Room));
        return Utils.ColorString(Color.green, $"{Translator.GetString("MurderNotify")}: {string.Join(", ", roomNames)}");
    }
    public bool CheckKillFlash(MurderInfo info) =>
        nowRole == Role.EvilHacker && !info.IsSuicide && !info.IsAccident && info.AttemptKiller.Is(CustomRoleTypes.Impostor);

    private static readonly string ImpostorMark = "★".Color(Palette.ImpostorRed);
    /// <summary>相方がキルしたときに名前の下に通知を表示する長さ</summary>
    private static readonly TimeSpan NotifyDuration = TimeSpan.FromSeconds(10);

    private readonly struct MurderNotify
    {
        /// <summary>通知が作成された時間</summary>
        public DateTime CreatedAt { get; init; }
        /// <summary>キルが起きた部屋</summary>
        public SystemTypes Room { get; init; }
    }
}

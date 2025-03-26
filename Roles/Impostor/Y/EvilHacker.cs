using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHostY.Roles.Impostor;
public sealed class EvilHacker : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilHacker),
            player => new EvilHacker(player),
            CustomRoles.EvilHacker,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpSpecial + 300,
            //(int)Options.offsetId.ImpY + 2000,
            SetUpOptionItem,
            "イビルハッカー",
            assignInfo: new(CustomRoles.EvilHacker, CustomRoleTypes.Impostor)
            {
                IsInitiallyAssignableCallBack = () => (MapNames)Main.NormalOptions.MapId is not MapNames.Fungle
            }
        );
    public EvilHacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        AdminCooldown = OptionAdminCooldown.GetFloat();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionAdminCooldown;
    private static float KillCooldown;
    private static float AdminCooldown;
    public float CalculateKillCooldown() => KillCooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = AdminCooldown;
    private Vector2 LastPosition; // 元の位置を保存する変数
    enum OptionName
    {
        EvilHackerAdminCooldown,
    }
    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionAdminCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.EvilHackerAdminCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
    }
    private Vector2 GetTeleportPosition()
    {
        // マップに応じた座標を選択
        switch ((MapNames)Main.NormalOptions.MapId)
        {
            case MapNames.Airship:
                return new Vector2(-22.13f, 0.56f);
            case MapNames.Skeld:
                return new Vector2(2.96f, -8.62f);
            case MapNames.Polus:
                return new Vector2(22.63f, -21.75f);
            case MapNames.MiraHQ:
                return new Vector2(22.22f, 18.77f);
            default:
                // デフォルトの座標（必要に応じて設定）
                return new Vector2(0f, 0f);
        }
    }
    public override bool OnCheckVanish(ref float killCooldown, ref bool canResetAbilityCooldown)
    {
        // 移動前の位置を保持
        LastPosition = Player.GetTruePosition();

        // プレイヤーの足止め
        Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
        Player.MarkDirtySettings();
        Logger.Info($"{Player.GetNameWithRole()} : プレイヤーの足止め", "EvilHacker");

        //透明化後に指定された位置へ強制移動する。
        _ = new LateTask(() =>
            {
                var teleportPosition = GetTeleportPosition();
                Player.SnapToTeleport(teleportPosition);
                SendRPC(Player.PlayerId);
                Utils.NotifyRoles();

                _ = new LateTask(() =>
                    {
                        Player.SnapToTeleport(LastPosition);//元の位置へ。
                        SendRPC(Player.PlayerId);
                        Utils.NotifyRoles();

                        // ターゲットの足止め解除
                        Main.AllPlayerSpeed[Player.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
                        Player.MarkDirtySettings();
                        Logger.Info($"{Player.GetNameWithRole()} : プレイヤーの足止め解除", "EvilHacker");
                        LastPosition = default;
                        Player.RpcResetAbilityCooldown();
                    }, 2.5f, "ReturnPosition");
            }, 2.5f, "Warp");
        return true;
    }
    private void SendRPC(byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.EvilHackerWarpSync);
        sender.Writer.Write(targetId);
    }

    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.EvilHackerWarpSync) return;

        var targetId = reader.ReadByte();
    }
}
using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using TownOfHostY.Attributes;
using TownOfHostY.Roles.Impostor;
using static UnityEngine.GraphicsBuffer;
namespace TownOfHostY;

static class PlayerOutfitExtension
{
    public static NetworkedPlayerInfo.PlayerOutfit Set(this NetworkedPlayerInfo.PlayerOutfit instance, string playerName, int colorId, string hatId, string skinId, string visorId, string petId, string namePlateId = "")
    {
        instance.PlayerName = playerName;
        instance.ColorId = colorId;
        instance.HatId = hatId;
        instance.SkinId = skinId;
        instance.VisorId = visorId;
        instance.PetId = petId;
        if (namePlateId != "")
        {
            instance.NamePlateId = namePlateId;
        }
        return instance;
    }
    public static bool Compare(this NetworkedPlayerInfo.PlayerOutfit instance, NetworkedPlayerInfo.PlayerOutfit targetOutfit)
    {
        return instance.ColorId == targetOutfit.ColorId &&
                instance.HatId == targetOutfit.HatId &&
                instance.SkinId == targetOutfit.SkinId &&
                instance.VisorId == targetOutfit.VisorId &&
                instance.PetId == targetOutfit.PetId;

    }
    public static string GetString(this NetworkedPlayerInfo.PlayerOutfit instance)
    {
        return $"{instance.PlayerName} Color:{instance.ColorId} {instance.HatId} {instance.SkinId} {instance.VisorId} {instance.PetId}";
    }
}
public static class Camouflage
{
    public static NetworkedPlayerInfo.PlayerOutfit CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set("", 15, "", "", "", "");

    public static bool IsCamouflage;
    public static Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> PlayerSkins = new();

    [GameModuleInitializer]
    public static void Init()
    {
        IsCamouflage = false;
        PlayerSkins.Clear();
    }
    public static void CheckCamouflage()
    {
        if (!Options.CommsCamouflage.GetBool() || Options.IsSyncColorMode) return;

        var oldIsCamouflage = IsCamouflage;

        IsCamouflage = Utils.IsActive(SystemTypes.Comms);

        if (oldIsCamouflage != IsCamouflage)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                if (!pc.Is(Roles.Core.CustomRoles.Rainbow))
                    RpcSetSkin(IsCamouflage, pc, CamouflageOutfit);

                // バニラのペットバグの対応
                if (!IsCamouflage && !pc.IsAlive()) pc.RpcSetPet("");
            }
            Utils.NotifyRoles(NoCache: true);
        }
    }
    public static void RpcSetSkin(bool isCamouflage, PlayerControl target, NetworkedPlayerInfo.PlayerOutfit camouflageOutfit = null, bool ForceRevert = false, bool RevertToDefault = false)
    {
        if (!(Options.CommsCamouflage.GetBool() || Roles.Core.CustomRoles.EvilDyer.IsEnable()) || Options.IsSyncColorMode) return;
        if (target == null) return;

        var id = target.PlayerId;

        if (isCamouflage)
        {
            //コミュサボ中

            //死んでいたら処理しない
            if (PlayerState.GetByPlayerId(id).IsDead) return;
        }

        var newOutfit = camouflageOutfit;
        if (newOutfit == null) newOutfit = CamouflageOutfit;

        if (!isCamouflage || ForceRevert)
        {
            //コミュサボ解除または強制解除

            if (Main.CheckShapeshift.TryGetValue(id, out var shapeshifting) && shapeshifting && !RevertToDefault)
            {
                //シェイプシフターなら今の姿のidに変更
                id = Main.ShapeshiftTarget[id];
            }

            newOutfit = PlayerSkins[id];
        }
        //if (newOutfit == null) return;
        if (newOutfit.Compare(target.Data.DefaultOutfit)) return;
        Logger.Info($"newOutfit={newOutfit.GetString()}", "RpcSetSkin");

        var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.PlayerName})");

        target.SetColor(newOutfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
            .Write(target.Data.NetId)
            .Write(newOutfit.ColorId)
            .EndRpc();

        target.SetHat(newOutfit.HatId, newOutfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
            .Write(newOutfit.HatId)
            .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

        target.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(newOutfit.SkinId)
            .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

        target.SetPet(newOutfit.PetId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
            .Write(newOutfit.PetId)
            .Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr))
            .EndRpc();

        sender.SendMessage();
    }
}

public static class SkinChangeMode
{
    public static void RpcSetSkin(PlayerControl target, PlayerControl Changed)
    {
        if (!Options.IsSyncColorMode || target == null) return;

        var newOutfit = Camouflage.PlayerSkins[Changed.PlayerId];
        Logger.Info("変更先：" + target.GetRealName() + " / 変身：" + Changed.GetRealName(), "RpcSetSkin");

        var sender = CustomRpcSender.Create(name: $"SkinChangeMode.RpcSetSkin({target.Data.PlayerName})");

        target.SetColor(newOutfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
            .Write(target.Data.NetId)
            .Write(newOutfit.ColorId)
            .EndRpc();

        target.SetHat(newOutfit.HatId, newOutfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
            .Write(newOutfit.HatId)
            .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

        target.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(newOutfit.SkinId)
            .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

        target.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
        
        target.SetPet(newOutfit.PetId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
            .Write(newOutfit.PetId)
            .Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr))
            .EndRpc();

        target.SetNamePlate(newOutfit.NamePlateId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetNamePlateStr)
            .Write(newOutfit.NamePlateId)
            .Write(target.GetNextRpcSequenceId(RpcCalls.SetNamePlateStr))
            .EndRpc();

        target.SetName(newOutfit.PlayerName);
        

        target.SetLevel(49);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetLevel)
            .Write(49)
            .EndRpc();

        sender.SendMessage();

        if (target == PlayerControl.LocalPlayer)
            Main.nickName = newOutfit.PlayerName;
    }
    public static void ChangeSkin()
    {
        if (!Options.IsSyncColorMode) return;

        List<PlayerControl> changePlayers = new();
        Main.AllPlayerControls.Do(pc => changePlayers.Add(pc));
        changePlayers = changePlayers.OrderBy(a => Guid.NewGuid()).ToList();

        int selectCount = 0;
        switch (Options.GetSyncColorMode())
        {
            case SyncColorMode.Twin:
                selectCount = (Main.AllPlayerControls.Count() + 1) / 2;
                break;
            default:
                selectCount = (int)Options.GetSyncColorMode();
                break;
        }

        var selects = new PlayerControl[selectCount];
        for (int i = 0; i < Main.AllPlayerControls.Count(); i++)
        {
            if (i < selectCount)
            {
                selects[i] = changePlayers[i];
                Logger.Info($"選定先{i}：{selects[i].GetRealName()}", "ChangeSkin");
            }

            RpcSetSkin(changePlayers[i], selects[i % selectCount]);
        }
    }
}

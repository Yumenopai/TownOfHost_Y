using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost
{
    static class PlayerOutfitExtension
    {
        public static GameData.PlayerOutfit Set(this GameData.PlayerOutfit instance, string playerName, int colorId, string hatId, string skinId, string visorId, string petId)
        {
            instance.PlayerName = playerName;
            instance.ColorId = colorId;
            instance.HatId = hatId;
            instance.SkinId = skinId;
            instance.VisorId = visorId;
            instance.PetId = petId;
            return instance;
        }
        public static GameData.PlayerOutfit Set(this GameData.PlayerOutfit instance, string playerName, int colorId, string hatId, string skinId, string visorId, string petId, string namePlateId)
        {
            instance.PlayerName = "crew";
            instance.ColorId = colorId;
            instance.HatId = hatId;
            instance.SkinId = skinId;
            instance.VisorId = visorId;
            instance.PetId = petId;
            instance.NamePlateId = namePlateId;
            return instance;
        }

        public static bool Compare(this GameData.PlayerOutfit instance, GameData.PlayerOutfit targetOutfit)
        {
            return instance.ColorId == targetOutfit.ColorId &&
                    instance.HatId == targetOutfit.HatId &&
                    instance.SkinId == targetOutfit.SkinId &&
                    instance.VisorId == targetOutfit.VisorId &&
                    instance.PetId == targetOutfit.PetId;

        }
        public static string GetString(this GameData.PlayerOutfit instance)
        {
            return $"{instance.PlayerName} Color:{instance.ColorId} {instance.HatId} {instance.SkinId} {instance.VisorId} {instance.PetId}";
        }
    }

    public static class Camouflage
    {
        static GameData.PlayerOutfit CamouflageOutfit = new GameData.PlayerOutfit().Set("", 15, "", "", "", "");

        public static bool IsCamouflage;
        public static Dictionary<byte, GameData.PlayerOutfit> PlayerSkins = new();

        public static void Init()
        {
            IsCamouflage = false;
            PlayerSkins.Clear();
        }
        public static void CheckCamouflage()
        {
            if (!(AmongUsClient.Instance.AmHost && Options.CommsCamouflage.GetBool())) return;

            var oldIsCamouflage = IsCamouflage;

            IsCamouflage = Utils.IsActive(SystemTypes.Comms);

            if (oldIsCamouflage != IsCamouflage)
            {
                Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc));
            }
        }
        public static void RpcSetSkin(PlayerControl target, bool ForceRevert = false, bool RevertToDefault = false)
        {
            if (!(AmongUsClient.Instance.AmHost && Options.CommsCamouflage.GetBool())) return;
            if (target == null) return;

            var id = target.PlayerId;

            if (IsCamouflage)
            {
                //コミュサボ中

                //死んでいたら処理しない
                if (Main.PlayerStates[id].IsDead) return;
            }
            var newOutfit = CamouflageOutfit;

            if (!IsCamouflage || ForceRevert)
            {
                //コミュサボ解除または強制解除

                if (Main.CheckShapeshift.TryGetValue(id, out var shapeshifting) && shapeshifting && !RevertToDefault)
                {
                    //シェイプシフターなら今の姿のidに変更
                    id = Main.ShapeshiftTarget[id];
                }

                newOutfit = PlayerSkins[id];
            }
            Logger.Info($"newOutfit={newOutfit.GetString()}", "RpcSetSkin");

            var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.PlayerName})");

            target.SetColor(newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                .Write(newOutfit.ColorId)
                .EndRpc();

            target.SetHat(newOutfit.HatId, newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(newOutfit.HatId)
                .EndRpc();

            target.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(newOutfit.SkinId)
                .EndRpc();

            target.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(newOutfit.VisorId)
                .EndRpc();

            target.SetPet(newOutfit.PetId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(newOutfit.PetId)
                .EndRpc();

            sender.SendMessage();
        }
    }
    public static class SkinChangeMode
    {
        public static Dictionary<byte, GameData.PlayerOutfit> PlayerSkins = new();

        public static void RpcSetSkin(PlayerControl target, PlayerControl Changed)
        {
            if (!(AmongUsClient.Instance.AmHost && Options.IsSyncColorMode)) return;
            if (target == null) return;

            var newOutfit = PlayerSkins[Changed.PlayerId];
            Logger.Info("変更先："+ target.GetRealName() + " / 変身：" + Changed.GetRealName(), "RpcSetSkin");

            var sender = CustomRpcSender.Create(name: $"SkinChangeMode.RpcSetSkin({target.Data.PlayerName})");

            target.SetColor(newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                .Write(newOutfit.ColorId)
                .EndRpc();

            target.SetHat(newOutfit.HatId, newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(newOutfit.HatId)
                .EndRpc();

            target.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(newOutfit.SkinId)
                .EndRpc();

            target.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(newOutfit.VisorId)
                .EndRpc();

            target.SetPet(newOutfit.PetId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(newOutfit.PetId)
                .EndRpc();

            /**********************/
            target.SetNamePlate(newOutfit.NamePlateId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetNamePlateStr)
                .Write(newOutfit.NamePlateId)
                .EndRpc();

            target.SetName(newOutfit.PlayerName);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetName)
                .Write(newOutfit.PlayerName)
                .EndRpc();

            target.SetLevel(98);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetLevel)
                .Write(98)
                .EndRpc();


            //SetLevel
            sender.SendMessage();
        }
        public static void ChangeSkin()
        {
            List<PlayerControl> chengePlayers = new();
            foreach (var p in Main.AllPlayerControls)
            {
                chengePlayers.Add(p);
            }
            chengePlayers = chengePlayers.OrderBy(a => Guid.NewGuid()).ToList();

            switch (Options.GetSyncColorMode())
            {
                case SyncColorMode.None:
                    break;

                case SyncColorMode.Clone:
                    PlayerControl target = chengePlayers[0];
                    Logger.Info("選定先:" + target.GetRealName(), "CloneMode");
                    Main.AllPlayerControls.Do(pc => RpcSetSkin(pc, target));
                    break;

                case SyncColorMode.fif_fif:
                    bool tar1 = true;
                    PlayerControl target1 = null;
                    PlayerControl target2 = null;
                    foreach (var p in chengePlayers)
                    {
                        if (target1 == null)
                        {
                            target1 = chengePlayers[p.PlayerId];
                            Logger.Info("選定先1:" + target1.GetRealName(), "fif_fifMode");
                        }
                        else if (target2 == null)
                        {
                            target2 = chengePlayers[p.PlayerId];
                            Logger.Info("選定先2:" + target2.GetRealName(), "fif_fifMode");
                        }

                        if (tar1) RpcSetSkin(p, target1);
                        else RpcSetSkin(p, target2);

                        tar1 = !tar1;
                    }
                    break;

                case SyncColorMode.ThreeCornered:

                    int Count = 0;
                    PlayerControl Target1 = null;
                    PlayerControl Target2 = null;
                    PlayerControl Target3 = null;
                    foreach (var p in chengePlayers)
                    {
                        if (Target1 == null)
                        {
                            Target1 = chengePlayers[p.PlayerId];
                            Logger.Info("選定先1:" + Target1.GetRealName(), "ThreeCornered");
                        }
                        else if (Target2 == null)
                        {
                            Target2 = chengePlayers[p.PlayerId];
                            Logger.Info("選定先2:" + Target2.GetRealName(), "ThreeCornered");
                        }
                        else if (Target3 == null)
                        {
                            Target3 = chengePlayers[p.PlayerId];
                            Logger.Info("選定先3:" + Target3.GetRealName(), "ThreeCornered");
                        }

                        switch(Count % 3)
                        {
                            case 0: RpcSetSkin(p, Target1); break;
                            case 1: RpcSetSkin(p, Target2); break;
                            case 2: RpcSetSkin(p, Target3); break;
                        }
                        Count++;
                    }
                    break;

                case SyncColorMode.Twin:

                    int count = 0;
                    PlayerControl targetT = null;
                    foreach (var p in chengePlayers)
                    {
                        count++;
                        if (count % 2 == 1)
                        {
                            targetT = chengePlayers[p.PlayerId];
                            Logger.Info("選定先:" + targetT.GetRealName(), "TwinMode");
                        }
                        RpcSetSkin(p, targetT);
                    }

                    break;
            }
        }
    }
}
using System.Linq;
using System.Text;

namespace TownOfHostY;
public static class SkinControle
{
    static readonly string Missing = "missing";
    public static NetworkedPlayerInfo.PlayerOutfit PlainOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set("", 15, "hat_NoHat", "skin_None", "visor_EmptyVisor", "pet_EmptyPet");
    public static readonly string[] FullFaceHat = new string[]
    {
        "hat_caiatl",
        "hat_erisMorn",
        "hat_hunter",
        "hat_maraSov",
        "hat_osiris",
        "hat_saint14",
        "hat_shaxx",
        "hat_titan",
        "hat_warlock",
        "hat_mareLwyd",
        "hat_schnapp",
        "hat_hl_fubuki",
        "hat_hl_gura",
        "hat_hl_korone",
        "hat_hl_marine",
        "hat_hl_mio",
        "hat_hl_moona",
        "hat_hl_okayu",
        "hat_hl_pekora",
        "hat_hl_risu",
        "hat_hl_watson",
        "hat_caitlin",
        "hat_enforcer",
        "hat_jinx",
        "hat_vi",
        "hat_Prototype",
        "hat_Rupert",
        "hat_ToppatHair",
        "hat_pk05_Ellie",
        "hat_pk05_Svenhat",
        "hat_AbominalHat",
        "hat_pk04_Vagabond"
    };

    public static bool IsHat(string hatId)
    {
        if (hatId == null || hatId == "") return false;
        if (hatId == Missing) return false;
        if (hatId == PlainOutfit.HatId) return false;
        return true;
    }
    public static bool IsSkin(string skinId)
    {
        if (skinId == null || skinId == "") return false;
        if (skinId == Missing) return false;
        if (skinId == PlainOutfit.SkinId) return false;
        return true;
    }
    public static bool IsVisor(string visorId)
    {
        if (visorId == null || visorId == "") return false;
        if (visorId == Missing) return false;
        if (visorId == PlainOutfit.VisorId) return false;
        return true;
    }
    public static bool IsPet(string petId)
    {
        if (petId == null || petId == "") return false;
        if (petId == Missing) return false;
        if (petId == PlainOutfit.PetId) return false;
        return true;
    }
    public static void DuplicateSkinCheck(PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Options.SkinControle.GetBool()) return;
        if (GameStates.IsInGame) return;
        if (target == null) return;

        Logger.Info($"CheckHat name: {target.name}, hat: {target.Data.DefaultOutfit.HatId}, skin: {target.Data.DefaultOutfit.SkinId}", "DuplicateSkinCheck");

        var hat = Options.NoDuplicateHat.GetBool() &&
                Main.AllPlayerControls.ToArray().Any(x => x.PlayerId != target.PlayerId &&
                x.Data.DefaultOutfit.HatId == target.Data.DefaultOutfit.HatId);
        var skin = Options.NoDuplicateSkin.GetBool() &&
                Main.AllPlayerControls.ToArray().Any(x => x.PlayerId != target.PlayerId &&
                x.Data.DefaultOutfit.SkinId == target.Data.DefaultOutfit.SkinId);

        if (hat || skin)
        {
            RpcSetSkin(target, hat: hat, skin: skin);
            Logger.Info($"ClearHat name: {target.name}, hat: {hat}, skin: {skin}", "DuplicateSkinCheck");
            if (hat)
                Utils.SendMessage($"重複しているためハットがリセットにされました", true, sendTo: target.PlayerId);
            if (skin)
                Utils.SendMessage($"重複しているためスキンがリセットにされました", true, sendTo: target.PlayerId);
        }
    }
    public static void ProhibitedSkinCheck(PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Options.SkinControle.GetBool()) return;
        if (GameStates.IsInGame) return;
        if (target == null) return;

        var hat = (Options.NoHat.GetBool() && IsHat(target.Data.DefaultOutfit.HatId))||
                (Options.NoFullFaceHat.GetBool() && FullFaceHat.ToArray().Any(x => x == target.Data.DefaultOutfit.HatId));
        var skin = Options.NoSkin.GetBool() &&  IsSkin(target.Data.DefaultOutfit.SkinId);
        var visor = Options.NoVisor.GetBool() && IsVisor(target.Data.DefaultOutfit.VisorId);
        var pet = Options.NoPet.GetBool() && IsPet(target.Data.DefaultOutfit.PetId);

        if (!hat && !skin && !visor && !pet) return;

        Logger.Info($"ClearHat name: {target.name}, hat: {hat} {target.Data.DefaultOutfit.HatId}, skin: {skin} {target.Data.DefaultOutfit.SkinId}, visor: {visor} {target.Data.DefaultOutfit.VisorId}, pet: {pet} {target.Data.DefaultOutfit.PetId}", "ProhibitedSkinCheck");
        RpcSetSkin(target, hat, skin, visor, pet);

        Utils.SendMessage($"{GetSetTypeName(hat, skin, visor, pet, !Options.NoHat.GetBool())} は設定で禁止されています。\nスキンがリセットにされました", true, sendTo: target.PlayerId);
    }
    public static string GetSetTypeName(bool hat = false, bool skin = false, bool visor = false, bool pet = false, bool fullFace = false)
    {
        var sb = new StringBuilder();
        var delimiter = "、";
        if (hat)
            sb.Append(fullFace ? "フルフェイスハット" : "ハット");
        if (skin)
            sb.Append((sb.Length > 0 ? delimiter : "") + "スキン");
        if (visor)
            sb.Append((sb.Length > 0 ? delimiter : "") + "バイザー");
        if (pet)
            sb.Append((sb.Length > 0 ? delimiter : "") + "ペット");
        return sb.ToString();
    }
    public static void RpcSetSkin(PlayerControl target, bool hat = false, bool skin = false, bool visor = false, bool pet = false)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        //if (GameStates.IsInGame) return;
        if (!hat && !skin && !visor && !pet) return;
        if (target == null) return;

        var id = target.PlayerId;

        var outfit = target.Data.DefaultOutfit;
        //Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);

        var newOutfit = PlainOutfit;
        Logger.Info($"ClearSkin name: {target.name}, hat: {hat}, skin: {skin}, visor: {visor}, pet: {pet}", "SkinControle.RpcSetSkin");

        var sender = CustomRpcSender.Create(name: $"SkinControle.RpcSetSkin({target.Data.PlayerName})");

        //target.SetColor(outfit.ColorId);
        //sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
        //    .Write(outfit.ColorId)
        //    .EndRpc();

        if (hat)
        {
            target.SetHat(newOutfit.HatId, outfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(newOutfit.HatId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                .EndRpc();
        }

        if (skin)
        {
            target.SetSkin(newOutfit.SkinId, outfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(newOutfit.SkinId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                .EndRpc();
        }

        if (visor)
        {
            target.SetVisor(newOutfit.VisorId, outfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(newOutfit.VisorId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                .EndRpc();
        }

        if (pet)
        {
            target.SetPet(newOutfit.PetId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(newOutfit.PetId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .EndRpc();
        }

        sender.SendMessage();
    }
}
using System.Globalization;
using HarmonyLib;
using InnerNet;
using Hazel;
using UnityEngine;
using TownOfHostY.Modules;
using static TownOfHostY.Translator;

namespace TownOfHostY;

class CannotUsePublicRoom
{
    public static string ShowMessage()
    {
        var message = "";
        if (!Main.AllowPublicRoom) message = GetString("DisabledByProgram");
        else if (!Main.IsPublicAvailableOnThisVersion) message = GetString("PublicNotAvailableOnThisVersion");
        else if (ModUpdater.hasUpdate) message = GetString("CanNotJoinPublicRoomNoLatest");
        else if (!VersionChecker.IsSupported) message = GetString("UnsupportedVersion");
        return message;
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
class MakePublicPatch
{
    public static bool Prefix(GameStartManager __instance)
    {
        // 定数設定による公開ルームブロック
        if (!Main.AllowPublicRoom || ModUpdater.hasUpdate || !VersionChecker.IsSupported || !Main.IsPublicAvailableOnThisVersion)
        {
            var message = CannotUsePublicRoom.ShowMessage();
            Logger.Info(message, "MakePublicPatch");
            Logger.SendInGame(message);
            return false;
        }

        return true;
    }
}
[HarmonyPatch(typeof(MMOnlineManager), nameof(MMOnlineManager.Start))]
class MMOnlineManagerStartPatch
{
    public static void Postfix(MMOnlineManager __instance)
    {
        if (VersionChecker.IsSupported && Main.AllowPublicRoom && Main.IsPublicAvailableOnThisVersion) return;

        var objF = GameObject.Find("Buttons/FindGameButton");
        if (objF)
        {
            objF?.SetActive(false);
        }
        var objJ = GameObject.Find("Buttons/JoinGameButton");
        if (objJ)
        {
            objJ?.SetActive(false);
        }

        var objmenu = GameObject.Find("NormalMenu");
        if (objmenu)
        {
            var objW = new GameObject("ModWarning");
            objW.transform.SetParent(objmenu.transform);
            objW.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            objW.transform.localScale = new Vector3(2f, 2f, 2f);
            var renderer = objW.AddComponent<SpriteRenderer>();
            renderer.sprite = Utils.LoadSprite($"TownOfHost_Y.Resources.warning_online.png", 400f);
        }
    }
}
[HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
class SplashLogoAnimatorPatch
{
    public static void Prefix(SplashManager __instance)
    {
        if (DebugModeManager.AmDebugger)
        {
            __instance.sceneChanger.AllowFinishLoadingScene();
            __instance.startedSceneLoad = true;
        }
    }
}
[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
class RunLoginPatch
{
    public static void Prefix(ref bool canOnline)
    {
#if DEBUG
        if (CultureInfo.CurrentCulture.Name != "ja-JP") canOnline = false;
#endif
    }
}
[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
class BanMenuSetVisiblePatch
{
    public static bool Prefix(BanMenu __instance, bool show)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;
        __instance.BanButton.gameObject.SetActive(AmongUsClient.Instance.CanBan());
        __instance.KickButton.gameObject.SetActive(AmongUsClient.Instance.CanKick());
        __instance.MenuButton.gameObject.SetActive(show);
        return false;
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.CanBan))]
class InnerNetClientCanBanPatch
{
    public static bool Prefix(InnerNet.InnerNetClient __instance, ref bool __result)
    {
        __result = __instance.AmHost;
        return false;
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.KickPlayer))]
class KickPlayerPatch
{
    public static void Prefix(InnerNet.InnerNetClient __instance, int clientId, bool ban)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (ban) BanManager.AddBanPlayer(AmongUsClient.Instance.GetRecentClient(clientId));
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendAllStreamedObjects))]
class InnerNetObjectSerializePatch
{
    public static bool Prefix(InnerNetClient __instance, ref bool __result)
    {
        if (AmongUsClient.Instance.AmHost)
            GameOptionsSender.SendAllGameOptions();

        //9人以上部屋で落ちる現象の対策コード
        if (!Options.FixSpawnPacketSize.GetBool()) return true;

        var sended = false;
        __result = false;
        var obj = __instance.allObjects;
        lock (obj)
        {
            for (int i = 0; i < __instance.allObjects.Count; i++)
            {
                InnerNetObject innerNetObject = __instance.allObjects[i];
                if (innerNetObject && innerNetObject.IsDirty && (innerNetObject.AmOwner ||
                    (innerNetObject.OwnerId == -2 && __instance.AmHost)))
                {
                    var messageWriter = __instance.Streams[(byte)innerNetObject.sendMode];
                    if (messageWriter.Length > 500)
                    {
                        if (!sended)
                        {
                            if (DebugModeManager.IsDebugMode)
                            {
                                Logger.Info($"SendAllStreamedObjects: Start", "InnerNetClient");
                            }
                            sended = true;
                        }
                        messageWriter.EndMessage();
                        __instance.SendOrDisconnect(messageWriter);
                        messageWriter.Clear(innerNetObject.sendMode);
                        messageWriter.StartMessage(5);
                        messageWriter.Write(__instance.GameId);
                    }
                    messageWriter.StartMessage(1);
                    messageWriter.WritePacked(innerNetObject.NetId);
                    try
                    {
                        if (innerNetObject.Serialize(messageWriter, false))
                        {
                            messageWriter.EndMessage();
                        }
                        else
                        {
                            messageWriter.CancelMessage();
                        }
                        if (innerNetObject.Chunked && innerNetObject.IsDirty)
                        {
                            Logger.Info($"SendAllStreamedObjects: Chunked", "InnerNetClient");
                            __result = true;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Info($"Exception:{ex.Message}", "InnerNetClient");
                        messageWriter.CancelMessage();
                    }
                }
            }
        }
        for (int j = 0; j < __instance.Streams.Length; j++)
        {
            MessageWriter messageWriter2 = __instance.Streams[j];
            if (messageWriter2.HasBytes(7))
            {
                if (!sended)
                {
                    if (DebugModeManager.IsDebugMode)
                    {
                        Logger.Info($"SendAllStreamedObjects: Start", "InnerNetClient");
                    }
                    sended = true;
                }
                messageWriter2.EndMessage();
                __instance.SendOrDisconnect(messageWriter2);
                messageWriter2.Clear((SendOption)j);
                messageWriter2.StartMessage(5);
                messageWriter2.Write(__instance.GameId);
            }
        }
        if (DebugModeManager.IsDebugMode && sended) Logger.Info($"SendAllStreamedObjects: End", "InnerNetClient");
        return false;
        }
    }
[HarmonyPatch]
class InnerNetClientPatch
{
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleMessage)), HarmonyPrefix]
    public static bool HandleMessagePatch(InnerNetClient __instance, MessageReader reader, SendOption sendOption)
    {
        if (DebugModeManager.IsDebugMode)
        {
            Logger.Info($"HandleMessagePatch:Packet({reader.Length}) ,SendOption:{sendOption}", "InnerNetClient");
        }
        else if (reader.Length > 1000)
        {
            Logger.Info($"HandleMessagePatch:Large Packet({reader.Length})", "InnerNetClient");
        }
        return true;
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendOrDisconnect)), HarmonyPrefix]
    public static void SendOrDisconnectPatch(InnerNetClient __instance, MessageWriter msg)
    {
        if (DebugModeManager.IsDebugMode)
        {
            Logger.Info($"SendOrDisconnectPatch:Packet({msg.Length}) ,SendOption:{msg.SendOption}", "InnerNetClient");
        }
        else if (msg.Length > 1000)
        {
            Logger.Info($"SendOrDisconnectPatch:Large Packet({msg.Length})", "InnerNetClient");
        }
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendInitialData)), HarmonyPrefix]
    public static bool SendInitialDataPatch(InnerNetClient __instance, int clientId)
    {
        if (!Options.FixSpawnPacketSize.GetBool()) return true;
        if (DebugModeManager.IsDebugMode)
        {
            Logger.Info($"SendInitialData: Start", "InnerNetClient");
        }
        MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
        messageWriter.StartMessage(6);
        messageWriter.Write(__instance.GameId);
        messageWriter.WritePacked(clientId);

        var obj = __instance.allObjects;
        lock (obj)
        {
            var hashSet = new System.Collections.Generic.HashSet<GameObject>();
            //まずはGameManagerを送信
            GameManager gameManager = GameManager.Instance;
            __instance.SendGameManager(clientId, gameManager);
            hashSet.Add(gameManager.gameObject);

            for (int i = 0; i < __instance.allObjects.Count; i++)
            {
                InnerNetObject innerNetObject = __instance.allObjects[i];
                if (innerNetObject && (innerNetObject.OwnerId != -4 || __instance.AmModdedHost) && hashSet.Add(innerNetObject.gameObject))
                {
                    if (messageWriter.Length > 500)
                    {
                        messageWriter.EndMessage();
                        __instance.SendOrDisconnect(messageWriter);
                        messageWriter.Clear(SendOption.Reliable);
                        messageWriter.StartMessage(6);
                        messageWriter.Write(__instance.GameId);
                        messageWriter.WritePacked(clientId);

                    }
                    __instance.WriteSpawnMessage(innerNetObject, innerNetObject.OwnerId, innerNetObject.SpawnFlags, messageWriter);
                }
            }
        }
        messageWriter.EndMessage();
        __instance.SendOrDisconnect(messageWriter);
        messageWriter.Recycle();
        if (DebugModeManager.IsDebugMode)
        {
            Logger.Info($"SendInitialData: End", "InnerNetClient");
        }
        return false;
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Spawn)), HarmonyPostfix]
    public static void SpawnPatch(InnerNetClient __instance, InnerNetObject netObjParent, int ownerId, SpawnFlags flags)
    {
        if (DebugModeManager.IsDebugMode)
        {
            Logger.Info($"SpawnPatch", "InnerNetClient");
        }
        var messageWriter = __instance.Streams[(byte)SendOption.Reliable];
        if (messageWriter.Length > 500)
        {
            messageWriter.EndMessage();
            __instance.SendOrDisconnect(messageWriter);
            messageWriter.Clear(SendOption.Reliable);
            messageWriter.StartMessage(5);
            messageWriter.Write(__instance.GameId);
        }
    }
}
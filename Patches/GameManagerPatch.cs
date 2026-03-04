using System;
using HarmonyLib;
using Hazel;
using AmongUs.GameOptions;
using System.Reflection;
using System.Text;
using System.IO;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Serialize))]
    class GameManagerSerializeFix
    {
        public static bool Prefix(GameManager __instance, [HarmonyArgument(0)] MessageWriter writer, [HarmonyArgument(1)] bool initialState, ref bool __result)
        {
            
            if (__instance == null)
            {
                Logger.Error("GameManager が null です", "GameManagerSerializeFix");
                __result = false;
                return false;
            }

            if (__instance.LogicComponents == null)
            {
                Logger.Error("LogicComponents が null です", "GameManagerSerializeFix");
                __result = false;
                return false;
            }

            if (writer == null)
            {
                Logger.Error("MessageWriter が null です", "GameManagerSerializeFix");
                __result = false;
                return false;
            }

            bool flag = false;
            for (int index = 0; index < __instance.LogicComponents.Count; ++index)
            {
                GameLogicComponent logicComponent = __instance.LogicComponents[index];

                
                if (logicComponent == null)
                {
                    Logger.Warn($"LogicComponent[{index}] が null です - スキップします", "GameManagerSerializeFix");
                    continue;
                }

                if (initialState || logicComponent.IsDirty)
                {
                    flag = true;
                    writer.StartMessage((byte)index);

                    try
                    {
                        bool hasBody = false;

                        
                        if (logicComponent.GetType().Name == "LogicOptions")
                        {
                            try
                            {
                                try
                                {
                                    hasBody = logicComponent.Serialize(writer);
                                }
                                catch (System.Exception firstEx)
                                {
                                    Logger.Error($"LogicComponent[{index}] (LogicOptions) 初回Serializeで例外: {firstEx.Message}", "GameManagerSerializeFix");
                                    Logger.Exception(firstEx, "GameManagerSerializeFix");

                                    var dump = DumpLogicOptionsState(logicComponent);
                                    SaveDiagnosticDump(dump);

                                    bool repaired = AttemptRepairLogicOptions(logicComponent);
                                    Logger.Info($"LogicOptions 修復を試みました: 成功={repaired}", "GameManagerSerializeFix");

                                    try
                                    {
                                        hasBody = logicComponent.Serialize(writer);
                                    }
                                    catch (System.Exception secondEx)
                                    {
                                        Logger.Error($"LogicComponent[{index}] (LogicOptions) 再Serializeで例外: {secondEx.Message}", "GameManagerSerializeFix");
                                        Logger.Exception(secondEx, "GameManagerSerializeFix");

                                        hasBody = SafeWriteEmptyLogicOptionsBody(writer);
                                        if (!hasBody)
                                        {
                                            try { writer.CancelMessage(); } catch { }
                                        }
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Logger.Error($"LogicComponent[{index}] (LogicOptions) の安全シリアライズに失敗: {ex.Message}", "GameManagerSerializeFix");
                                Logger.Exception(ex, "GameManagerSerializeFix");
                                try { writer.CancelMessage(); } catch { }
                                continue;
                            }
                        }
                        else
                        {
                            try
                            {
                                hasBody = logicComponent.Serialize(writer);
                            }
                            catch (System.NullReferenceException ex)
                            {
                                Logger.Error($"LogicComponent[{index}].Serialize で NullReferenceException が発生しました: {ex.Message}",
                                            "GameManagerSerializeFix");
                                Logger.Exception(ex, "GameManagerSerializeFix");
                                try { writer.CancelMessage(); } catch { }
                                continue;
                            }
                        }

                        if (hasBody) writer.EndMessage();
                        else writer.CancelMessage();
                        logicComponent.ClearDirtyFlag();
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Error($"LogicComponent[{index}].Serialize で予期しない例外が発生しました: {ex.Message}",
                                    "GameManagerSerializeFix");
                        Logger.Exception(ex, "GameManagerSerializeFix");
                        try { writer.CancelMessage(); } catch { }
                        continue;
                    }
                }
            }

            __instance.ClearDirtyBits();
            __result = flag;
            return false;
        }

        private static string DumpLogicOptionsState(object logicOptions)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("==== LogicOptions State Dump ====");
                sb.AppendLine($"Type: {logicOptions.GetType().FullName}");

                var fields = logicOptions.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    object val = null;
                    try { val = f.GetValue(logicOptions); } catch (System.Exception) { val = "<get failed>"; }
                    sb.AppendLine($"Field: {f.Name} = {(val == null ? "<null>" : val.ToString())}");
                }

                var props = logicOptions.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var p in props)
                {
                    object val = null;
                    try { if (p.GetIndexParameters().Length == 0) val = p.GetValue(logicOptions); else val = "<indexed>"; } catch (System.Exception) { val = "<get failed>"; }
                    sb.AppendLine($"Prop: {p.Name} = {(val == null ? "<null>" : val.ToString())}");
                }

                return sb.ToString();
            }
            catch (System.Exception ex)
            {
                return "Dump failed: " + ex.Message;
            }
        }

        private static void SaveDiagnosticDump(string dump)
        {
            try
            {
                var dir = Utils.GetLogFolder(true);
                var path = Path.Combine(dir.FullName, $"LogicOptionsDump-{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(path, dump);
                Logger.Info($"LogicOptions のダンプを保存しました: {path}", "GameManagerSerializeFix");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"ダンプ保存に失敗しました: {ex.Message}", "GameManagerSerializeFix");
            }
        }

        private static bool AttemptRepairLogicOptions(object logicOptions)
        {
            try
            {
                var go = GameOptionsManager.Instance?.CurrentGameOptions;
                if (go == null) return false;

                var mi = logicOptions.GetType().GetMethod("SetGameOptions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mi != null)
                {
                    mi.Invoke(logicOptions, new object[] { go });
                    var ms = logicOptions.GetType().GetMethod("SyncOptions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    ms?.Invoke(logicOptions, null);
                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Logger.Error($"LogicOptions 修復処理で例外: {ex.Message}", "GameManagerSerializeFix");
                Logger.Exception(ex, "GameManagerSerializeFix");
                return false;
            }
        }

        private static bool SafeWriteEmptyLogicOptionsBody(MessageWriter writer)
        {
            try
            {
                byte[] emptyBytes = Array.Empty<byte>();

                MethodInfo mi = typeof(MessageWriter).GetMethod("WriteBytesAndSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mi == null)
                {
                    Logger.Error("MessageWriter.WriteBytesAndSize が見つかりません", "GameManagerSerializeFix");
                    return false;
                }

                var param = mi.GetParameters()[0].ParameterType;
                try
                {
                    if (param == typeof(byte[]))
                    {
                        mi.Invoke(writer, new object[] { emptyBytes });
                        return true;
                    }
                    else if (param.FullName != null && param.FullName.Contains("Il2CppStructArray"))
                    {
                        var ilArr = new Il2CppStructArray<byte>(0);
                        mi.Invoke(writer, new object[] { ilArr });
                        return true;
                    }
                    else if (param.IsArray && param.GetElementType() == typeof(byte))
                    {
                        mi.Invoke(writer, new object[] { emptyBytes });
                        return true;
                    }
                    else
                    {
                        try
                        {
                            mi.Invoke(writer, new object[] { emptyBytes });
                            return true;
                        }
                        catch (TargetInvocationException) { }
                        catch (ArgumentException) { }

                        try
                        {
                            var ilArr = new Il2CppStructArray<byte>(0);
                            mi.Invoke(writer, new object[] { ilArr });
                            return true;
                        }
                        catch (Exception ex2)
                        {
                            Logger.Error($"SafeWriteEmptyLogicOptionsBody: 両方式とも失敗しました: {ex2.Message}", "GameManagerSerializeFix");
                            Logger.Exception(ex2, "GameManagerSerializeFix");
                            return false;
                        }
                    }
                }
                catch (TargetInvocationException tie)
                {
                    Logger.Error($"SafeWriteEmptyLogicOptionsBody invoke で TargetInvocationException: {tie.InnerException?.Message ?? tie.Message}", "GameManagerSerializeFix");
                    Logger.Exception(tie, "GameManagerSerializeFix");
                    try { writer.CancelMessage(); } catch { }
                    return false;
                }
                catch (ArgumentException aex)
                {
                    Logger.Error($"SafeWriteEmptyLogicOptionsBody invoke で ArgumentException: {aex.Message}", "GameManagerSerializeFix");
                    Logger.Exception(aex, "GameManagerSerializeFix");
                    try { writer.CancelMessage(); } catch { }
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"SafeWriteEmptyLogicOptionsBody で例外: {ex.Message}", "GameManagerSerializeFix");
                Logger.Exception(ex, "GameManagerSerializeFix");
                try { writer.CancelMessage(); } catch { }
                return false;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime;

namespace TownOfHostY;

public class CoroutinPatcher : Attribute
{
    Dictionary<string, Action> _prefixActions = [];
    Dictionary<string, Action> _postfixActions = [];
    private readonly Il2CppSystem.Collections.IEnumerator _enumerator;
    public CoroutinPatcher(Il2CppSystem.Collections.IEnumerator enumerator)
    {
        _enumerator = enumerator;
    }
    public void AddPrefix(Type type, string key, Action action)
    {
        Logger.Info($"AddPrefix: {key}", "CoroutinPatcher");
        _prefixActions[$"{type}+<{key}>"] = action;
    }
    public void AddPostfix(Type type, string key, Action action)
    {
        Logger.Info($"AddPostfix: {key}", "CoroutinPatcher");
        _postfixActions[$"{type}+<{key}>"] = action;
    }
    public Il2CppSystem.Collections.IEnumerator EnumerateWithPatch()
    {
        return EnumerateWithPatchInternal().WrapToIl2Cpp();
    }
    public System.Collections.IEnumerator EnumerateWithPatchInternal()
    {
        Logger.Info("ExecEnumerator", "CoroutinPatcher");
        while (_enumerator.MoveNext())
        {
            var fullName = _enumerator.Current?.GetIl2CppType()?.FullName;
            if (fullName == null)
            {
                Logger.Info("Current: null", "CoroutinPatcher");
                yield return _enumerator.Current;
                continue;
            }
            Logger.Info($"Current: {fullName}", "CoroutinPatcher");

            foreach (var info in _prefixActions)
            {
                if (fullName.Contains(info.Key))
                {
                    Logger.Info($"Exec Prefix: {fullName}", "CoroutinPatcher");
                    info.Value();
                }
            }

            Logger.Info($"Yield Return: {fullName}", "CoroutinPatcher");
            yield return _enumerator.Current;

            foreach (var info in _postfixActions)
            {
                if (fullName.Contains(info.Key))
                {
                    Logger.Info($"Exec Postfix: {fullName}", "CoroutinPatcher");
                    info.Value();
                }
            }
        }
        Logger.Info("ExecEnumerator End", "CoroutinPatcher");
    }
}

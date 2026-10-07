using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TownOfHostY.Modules;

public static class OptionSaver
{
    private static readonly DirectoryInfo SaveDataDirectoryInfo = new("./TOHY_DATA/SaveData/");
    private static readonly DirectoryInfo PresetsDirectoryInfo = new($"{SaveDataDirectoryInfo.FullName}/Presets/");
    private static readonly FileInfo OptionSaverFileInfo = new($"{SaveDataDirectoryInfo.FullName}/Options_TOHY.json");
    private static readonly LogHandler logger = Logger.Handler(nameof(OptionSaver));

    public static void Initialize()
    {
        if (!SaveDataDirectoryInfo.Exists)
        {
            SaveDataDirectoryInfo.Create();
            SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
        }
        if (!PresetsDirectoryInfo.Exists)
        {
            PresetsDirectoryInfo.Create();
        }
        if (!OptionSaverFileInfo.Exists)
        {
            OptionSaverFileInfo.Create().Dispose();
        }

        // Ensure preset files exist (create defaults if missing)
        for (int i = 0; i < OptionItem.NumPresets; i++)
        {
            var path = GetPresetFilePath(i);
            if (!File.Exists(path))
            {
                try
                {
                    File.WriteAllText(path, JsonSerializer.Serialize(new SerializablePresetData { Version = Version, PresetOptions = new Dictionary<int, int>() }, new JsonSerializerOptions { WriteIndented = true }));
                }
                catch (Exception ex)
                {
                    logger.Warn($"Preset file の作成に失敗しました: {ex.Message}");
                }
            }
        }
    }

    /// <summary>シングルオプションのみを生成</summary>
    private static SerializableSingleOptionsData GenerateSingleOptionsData()
    {
        Dictionary<int, int> singleOptions = new();
        foreach (var option in OptionItem.AllOptions)
        {
            if (option.IsSingleValue)
            {
                if (!singleOptions.TryAdd(option.Id, option.SingleValue))
                {
                    logger.Warn($"SingleOptionのID {option.Id} が重複");
                }
            }
        }
        return new SerializableSingleOptionsData
        {
            Version = Version,
            SingleOptions = singleOptions,
        };
    }

    /// <summary>指定プリセットのオプションデータを生成</summary>
    private static SerializablePresetData GeneratePresetData(int presetIndex)
    {
        Dictionary<int, int> presetOptions = new();
        foreach (var option in OptionItem.AllOptions)
        {
            if (!option.IsSingleValue && option.AllValues.Length > presetIndex)
            {
                if (!presetOptions.TryAdd(option.Id, option.AllValues[presetIndex]))
                {
                    logger.Warn($"プリセットオプションのID {option.Id} が重複");
                }
            }
        }
        return new SerializablePresetData
        {
            Version = Version,
            PresetOptions = presetOptions,
        };
    }

    /// <summary>シングルオプションデータを読み込み</summary>
    private static void LoadSingleOptionsData(SerializableSingleOptionsData serializableData)
    {
        if (serializableData?.Version != Version)
        {
            logger.Info($"読み込まれたシングルオプションのバージョンが一致しないためデフォルト値で上書きします");
            SaveSingleOptions();
            return;
        }

        Dictionary<int, int> singleOptions = serializableData.SingleOptions ?? new();
        foreach (var singleOption in singleOptions)
        {
            var id = singleOption.Key;
            var value = singleOption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetValue(value, doSave: false);
            }
        }
    }

    /// <summary>指定プリセットのデータを読み込み</summary>
    private static void LoadPresetData(int presetIndex, SerializablePresetData serializableData)
    {
        if (serializableData?.Version != Version)
        {
            logger.Info($"Preset {presetIndex} のバージョンが一致しないためスキップします");
            return;
        }

        Dictionary<int, int> presetOptions = serializableData.PresetOptions ?? new();
        foreach (var presetOption in presetOptions)
        {
            var id = presetOption.Key;
            var value = presetOption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                // 配列要素を直接設定
                if (optionItem.AllValues.Length > presetIndex)
                    optionItem.AllValues[presetIndex] = value;
            }
        }
    }

    /// <summary>シングルオプションをjsonファイルに保存</summary>
    private static void SaveSingleOptions()
    {
        // 接続済みで，ホストじゃなければ保存しない
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        try
        {
            var jsonString = JsonSerializer.Serialize(GenerateSingleOptionsData(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(OptionSaverFileInfo.FullName, jsonString);
        }
        catch (Exception ex)
        {
            logger.Warn($"シングルオプションの保存に失敗しました: {ex.Message}");
        }
    }

    /// <summary>指定プリセットをjsonファイルに保存</summary>
    private static void SavePreset(int presetIndex)
    {
        // 接続済みで，ホストじゃなければ保存しない
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        try
        {
            var presetFilePath = GetPresetFilePath(presetIndex);
            var jsonString = JsonSerializer.Serialize(GeneratePresetData(presetIndex), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(presetFilePath, jsonString);
        }
        catch (Exception ex)
        {
            logger.Warn($"Preset {presetIndex} の保存に失敗しました: {ex.Message}");
        }
    }

    /// <summary>全プリセットをjsonファイルに保存</summary>
    private static void SaveAllPresets()
    {
        for (int i = 0; i < OptionItem.NumPresets; i++)
        {
            SavePreset(i);
        }
    }

    /// <summary>現在のオプションをjsonファイルに保存</summary>
    public static void Save()
    {
        SaveSingleOptions();
        SaveAllPresets();
    }

    /// <summary>シングルオプションをjsonファイルから読み込み</summary>
    private static void LoadSingleOptions()
    {
        if (!OptionSaverFileInfo.Exists)
        {
            logger.Info("シングルオプションデータが存在しないためデフォルト値を保存");
            SaveSingleOptions();
            return;
        }

        try
        {
            var jsonString = File.ReadAllText(OptionSaverFileInfo.FullName);
            if (jsonString.Length <= 0)
            {
                logger.Info("シングルオプションデータが空のためデフォルト値を保存");
                SaveSingleOptions();
                return;
            }

            LoadSingleOptionsData(JsonSerializer.Deserialize<SerializableSingleOptionsData>(jsonString));
        }
        catch (Exception ex)
        {
            logger.Warn($"シングルオプションの読み込みに失敗しました。デフォルト値で上書きします: {ex.Message}");
            SaveSingleOptions();
        }
    }

    /// <summary>指定プリセットをjsonファイルから読み込み</summary>
    private static void LoadPreset(int presetIndex)
    {
        var presetFilePath = GetPresetFilePath(presetIndex);
        if (!File.Exists(presetFilePath))
        {
            logger.Info($"Preset {presetIndex} が存在しないため作成します");
            SavePreset(presetIndex);
            return;
        }

        try
        {
            var jsonString = File.ReadAllText(presetFilePath);
            if (jsonString.Length <= 0)
            {
                logger.Info($"Preset {presetIndex} が空のためデフォルト値を保存");
                SavePreset(presetIndex);
                return;
            }

            LoadPresetData(presetIndex, JsonSerializer.Deserialize<SerializablePresetData>(jsonString));
        }
        catch (Exception ex)
        {
            logger.Warn($"Preset {presetIndex} の読み込みに失敗しました: {ex.Message}");
            SavePreset(presetIndex);
        }
    }

    /// <summary>全プリセットをjsonファイルから読み込み</summary>
    private static void LoadAllPresets()
    {
        for (int i = 0; i < OptionItem.NumPresets; i++)
        {
            LoadPreset(i);
        }
    }

    /// <summary>すべてのオプションをjsonファイルから読み込み</summary>
    public static void Load()
    {
        LoadSingleOptions();
        LoadAllPresets();
    }

    /// <summary>プリセットファイルパスを取得</summary>
    private static string GetPresetFilePath(int presetIndex)
    {
        return Path.Combine(PresetsDirectoryInfo.FullName, $"Preset_{presetIndex}.json");
    }

    /// <summary>シングルオプションのみjsonに適した形式</summary>
    public class SerializableSingleOptionsData
    {
        public int Version { get; init; }
        public Dictionary<int, int> SingleOptions { get; init; }
    }

    /// <summary>プリセットオプションのみjsonに適した形式</summary>
    public class SerializablePresetData
    {
        public int Version { get; init; }
        public Dictionary<int, int> PresetOptions { get; init; }
    }

    /// <summary>オプションの形式に互換性のない変更(プリセット数変更など)を加えるときはここの数字を上げる</summary>
    public static readonly int Version = 0;
}

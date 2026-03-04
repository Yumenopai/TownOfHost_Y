using System;
using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TownOfHostY;

public static class ModGameOptionsMenu
{
    public static int TabIndex = 0;
    public static Dictionary<OptionBehaviour, int> OptionList = new();
    public static Dictionary<int, OptionBehaviour> BehaviourList = new();
    public static Dictionary<int, CategoryHeaderMasked> CategoryHeaderList = new();
}
[HarmonyPatch(typeof(GameOptionsMenu))]
public static class GameOptionsMenuPatch
{
    public static GameOptionsMenu Instance;
    private static Sprite _cachedLabelBgSprite;
    private static Sprite CachedLabelBgSprite
        => _cachedLabelBgSprite ??= Utils.LoadSprite(
            "TownOfHost_Y-ForkedbyTabasco.Resources.SettingMenu_LabelBackground.png", 100f);

    [HarmonyPatch(nameof(GameOptionsMenu.Initialize)), HarmonyPrefix]
    private static bool InitializePrefix(GameOptionsMenu __instance)
    {
        Instance ??= __instance;
        if (ModGameOptionsMenu.TabIndex < 3) return true;

        if (__instance.Children == null || __instance.Children.Count == 0)
        {
            __instance.MapPicker.gameObject.SetActive(false);
            //__instance.MapPicker.Initialize(20);
            //BaseGameSetting mapNameSetting = GameManager.Instance.GameSettingsList.MapNameSetting;
            //__instance.MapPicker.SetUpFromData(mapNameSetting, 20);
            __instance.Children = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
            //__instance.Children.Add(__instance.MapPicker);
            __instance.CreateSettings();
            __instance.cachedData = GameOptionsManager.Instance.CurrentGameOptions;
            for (int i = 0; i < __instance.Children.Count; i++)
            {
                OptionBehaviour optionBehaviour = __instance.Children[i];
                optionBehaviour.OnValueChanged = new Action<OptionBehaviour>(__instance.ValueChanged);
                //if (AmongUsClient.Instance && !AmongUsClient.Instance.AmHost)
                //{
                //    optionBehaviour.SetAsPlayer();
                //}
            }
            __instance.InitializeControllerNavigation();
        }

        return false;
    }
    [HarmonyPatch(nameof(GameOptionsMenu.CreateSettings)), HarmonyPrefix]
    private static bool CreateSettingsPrefix(GameOptionsMenu __instance)
    {
        Instance ??= __instance;
        if (ModGameOptionsMenu.TabIndex < 3) return true;
        var modTab = (TabGroup)(ModGameOptionsMenu.TabIndex - 3);

        //float num = 0.713f;
        float num = 2.0f;
        const float pos_x = 0.952f;
        const float pos_z = -2.0f;
        for (int index = 0; index < OptionItem.AllOptions.Count; index++)
        {
            var option = OptionItem.AllOptions[index];
            if (option.Tab != modTab) continue;

            var enabled = !option.IsHiddenOn(Options.CurrentGameMode)
                         && (option.Parent == null || (!option.Parent.IsHiddenOn(Options.CurrentGameMode) && option.Parent.GetBool()));

            if (option.IsHeader || option is TextOptionItem)
            {
                CategoryHeaderMasked categoryHeaderMasked = UnityEngine.Object.Instantiate<CategoryHeaderMasked>(__instance.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                categoryHeaderMasked.SetHeader(StringNames.RolesCategory, 20);

                categoryHeaderMasked.Title.text = option.IsHeaderName == "" ? option.GetName(colorLighter: true) : option.IsHeaderName.Color(option.NameColor.ShadeColor(-0.3f));
                categoryHeaderMasked.transform.localPosition = new Vector3(-0.903f, num, pos_z);
                categoryHeaderMasked.transform.localScale = Vector3.one * 0.63f;

                if (option.Tab != TabGroup.ModMainSettings && option is not TextOptionItem)
                {
                    categoryHeaderMasked.transform.FindChild("LabelSprite").transform.localPosition -= new Vector3(0f, 0.06f, 0f);
                    categoryHeaderMasked.transform.FindChild("LabelSprite").transform.localScale = new Vector3(1.5f, 1.25f, 1f);

                    categoryHeaderMasked.transform.FindChild("HeaderText").GetComponent<RectTransform>().sizeDelta = new Vector2(4.4f, 0.38f);
                    categoryHeaderMasked.transform.FindChild("HeaderText").GetComponent<RectTransform>().localPosition = new Vector3(0.55f, -0.22f, -1f);
                }

                categoryHeaderMasked.transform.FindChild("HeaderText").GetComponent<TMPro.TextMeshPro>().fontStyle = TMPro.FontStyles.Bold;
                categoryHeaderMasked.transform.FindChild("HeaderText").GetComponent<TMPro.TextMeshPro>().outlineWidth = 0.17f;
                categoryHeaderMasked.gameObject.SetActive(enabled);
                ModGameOptionsMenu.CategoryHeaderList.TryAdd(index, categoryHeaderMasked);

                if (enabled) num -= 0.63f;
            }
            if (option is TextOptionItem) continue;

            var baseGameSetting = GetSetting(option);
            if (baseGameSetting == null) continue;


            OptionBehaviour optionBehaviour;

            switch (baseGameSetting.Type)
            {
                case OptionTypes.Checkbox:
                    {
                        optionBehaviour = UnityEngine.Object.Instantiate<ToggleOption>(__instance.checkboxOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                        optionBehaviour.transform.localPosition = new Vector3(pos_x, num, pos_z);

                        OptionBehaviourSetSizeAndPosition(optionBehaviour, option, baseGameSetting.Type);

                        optionBehaviour.SetClickMask(__instance.ButtonClickMask);
                        optionBehaviour.SetUpFromData(baseGameSetting, 20);
                        ModGameOptionsMenu.OptionList.TryAdd(optionBehaviour, index);
                        //Logger.Info($"{option.Name}, {index}", "OptionList.TryAdd");
                        break;
                    }
                case OptionTypes.String:
                    {
                        optionBehaviour = UnityEngine.Object.Instantiate<StringOption>(__instance.stringOptionOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                        optionBehaviour.transform.localPosition = new Vector3(pos_x, num, pos_z);

                        OptionBehaviourSetSizeAndPosition(optionBehaviour, option, baseGameSetting.Type);

                        optionBehaviour.SetClickMask(__instance.ButtonClickMask);
                        optionBehaviour.SetUpFromData(baseGameSetting, 20);
                        ModGameOptionsMenu.OptionList.TryAdd(optionBehaviour, index);
                        break;
                    }
                case OptionTypes.Float:
                case OptionTypes.Int:
                    {
                        optionBehaviour = UnityEngine.Object.Instantiate<NumberOption>(__instance.numberOptionOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                        optionBehaviour.transform.localPosition = new Vector3(pos_x, num, pos_z);

                        OptionBehaviourSetSizeAndPosition(optionBehaviour, option, baseGameSetting.Type);

                        optionBehaviour.SetClickMask(__instance.ButtonClickMask);
                        optionBehaviour.SetUpFromData(baseGameSetting, 20);
                        ModGameOptionsMenu.OptionList.TryAdd(optionBehaviour, index);
                        //Logger.Info($"{option.Name}, {index}", "OptionList.TryAdd");
                        break;
                    }

                //case OptionTypes.Player:
                //    {
                //        OptionBehaviour optionBehaviour = UnityEngine.Object.Instantiate<PlayerOption>(__instance.playerOptionOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                //        break;
                //    }
                default:
                    continue;

            }
            optionBehaviour.transform.localPosition = new Vector3(0.952f, num, -2f);
            optionBehaviour.SetClickMask(__instance.ButtonClickMask);
            optionBehaviour.SetUpFromData(baseGameSetting, 20);
            ModGameOptionsMenu.OptionList.TryAdd(optionBehaviour, index);
            ModGameOptionsMenu.BehaviourList.TryAdd(index, optionBehaviour);
            optionBehaviour.gameObject.SetActive(enabled);
            __instance.Children.Add(optionBehaviour);

            if (enabled) num -= 0.45f;
        }

        __instance.ControllerSelectable.Clear();
        foreach (var x in __instance.scrollBar.GetComponentsInChildren<UiElement>())
            __instance.ControllerSelectable.Add(x);
        __instance.scrollBar.SetYBoundsMax(-num - 1.65f);

        return false;
    }
    private static void OptionBehaviourSetSizeAndPosition(OptionBehaviour optionBehaviour, OptionItem option, OptionTypes type)
    {
        optionBehaviour.transform.FindChild("LabelBackground").GetComponent<SpriteRenderer>().sprite = CachedLabelBgSprite;

        Vector3 positionOffset = new(0f, 0f, 0f);
        Vector3 scaleOffset = new(0f, 0f, 0f);
        Color color = new(0.7f, 0.7f, 0.7f);
        float sizeDelta_x = 5.7f;
        float sizeDelta_y = 0.37f;

        if (option.Parent?.Parent?.Parent != null)
        {
            scaleOffset = new(-0.18f, 0, 0);
            positionOffset = new(0.3f, 0f, 0f);
            color = new(0.7f, 0.5f, 0.5f);
            sizeDelta_x = 5.1f;
        }
        else if (option.Parent?.Parent != null)
        {
            scaleOffset = new(-0.12f, 0, 0);
            positionOffset = new(0.2f, 0f, 0f);
            color = new(0.5f, 0.5f, 0.7f);
            sizeDelta_x = 5.3f;
        }
        else if (option.Parent != null)
        {
            scaleOffset = new(-0.05f, 0, 0);
            positionOffset = new(0.1f, 0f, 0f);
            color = new(0.5f, 0.7f, 0.5f);
            sizeDelta_x = 5.5f;
        }
        else if (option.Parent == null && option.Tab != TabGroup.ModMainSettings)
        {
            sizeDelta_y = 0.43f;
        }

        optionBehaviour.transform.FindChild("LabelBackground").GetComponent<SpriteRenderer>().color = color;
        optionBehaviour.transform.FindChild("LabelBackground").localScale += new Vector3(0.9f, -0.2f, 0f) + scaleOffset;
        optionBehaviour.transform.FindChild("LabelBackground").localPosition += new Vector3(-0.4f, 0f, 0f) + positionOffset;

        optionBehaviour.transform.FindChild("Title Text").localPosition += new Vector3(-0.4f, 0f, 0f) + positionOffset; ;
        optionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(sizeDelta_x, sizeDelta_y);
        optionBehaviour.transform.FindChild("Title Text").GetComponent<TMPro.TextMeshPro>().alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        optionBehaviour.transform.FindChild("Title Text").GetComponent<TMPro.TextMeshPro>().fontStyle = TMPro.FontStyles.Bold;
        optionBehaviour.transform.FindChild("Title Text").GetComponent<TMPro.TextMeshPro>().outlineWidth = 0.17f;

        switch (type)
        {
            case OptionTypes.Checkbox:
                optionBehaviour.transform.FindChild("Toggle").localPosition = new Vector3(1.46f, -0.042f);
                break;

            case OptionTypes.String:
                optionBehaviour.transform.FindChild("PlusButton").localPosition += new Vector3(option.IsFixValue ? 100f : 1.7f, option.IsFixValue ? 100f : 0f, option.IsFixValue ? 100f : 0f);
                optionBehaviour.transform.FindChild("MinusButton").localPosition += new Vector3(option.IsFixValue ? 100f : 0.9f, option.IsFixValue ? 100f : 0f, option.IsFixValue ? 100f : 0f);
                optionBehaviour.transform.FindChild("Value_TMP (1)").localPosition += new Vector3(1.3f, 0f, 0f);
                optionBehaviour.transform.FindChild("Value_TMP (1)").GetComponent<RectTransform>().sizeDelta = new Vector2(2.3f, 0.4f);
                goto default;

            case OptionTypes.Float:
            case OptionTypes.Int:
                optionBehaviour.transform.FindChild("PlusButton").localPosition += new Vector3(option.IsFixValue ? 100f : 1.7f, option.IsFixValue ? 100f : 0f, option.IsFixValue ? 100f : 0f);
                optionBehaviour.transform.FindChild("MinusButton").localPosition += new Vector3(option.IsFixValue ? 100f : 0.9f, option.IsFixValue ? 100f : 0f, option.IsFixValue ? 100f : 0f);
                optionBehaviour.transform.FindChild("Value_TMP").localPosition += new Vector3(1.3f, 0f, 0f);
                goto default;

            default:// Number & String 共通
                optionBehaviour.transform.FindChild("ValueBox").localScale += new Vector3(0.2f, 0f, 0f);
                optionBehaviour.transform.FindChild("ValueBox").localPosition += new Vector3(1.3f, 0f, 0f);
                break;
        }
    }

    public static void UpdateSettings()
    {
        if (Instance == null) return;
        ReCreateSettings(Instance);
    }

    [HarmonyPatch(nameof(GameOptionsMenu.ValueChanged)), HarmonyPrefix]
    private static bool ValueChangedPrefix(GameOptionsMenu __instance, OptionBehaviour option)
    {
        if (__instance == null || ModGameOptionsMenu.TabIndex < 3) return true;

        if (ModGameOptionsMenu.OptionList.TryGetValue(option, out var index))
        {
            var item = OptionItem.AllOptions[index];
            if (item != null && item.Children.Count > 0) ReCreateSettings(__instance);
        }
        return false;
    }
    private static void ReCreateSettings(GameOptionsMenu __instance)
    {
        if (ModGameOptionsMenu.TabIndex < 3) return;
        var modTab = (TabGroup)(ModGameOptionsMenu.TabIndex - 3);

        //float num = 0.713f;
        float num = 2.0f;
        for (int index = 0; index < OptionItem.AllOptions.Count; index++)
        {
            var option = OptionItem.AllOptions[index];
            if (option.Tab != modTab) continue;

            var enabled = !option.IsHiddenOn(Options.CurrentGameMode)
                         && (option.Parent == null || (!option.Parent.IsHiddenOn(Options.CurrentGameMode) && option.Parent.GetBool()));

            if (ModGameOptionsMenu.CategoryHeaderList.TryGetValue(index, out var categoryHeaderMasked))
            {
                categoryHeaderMasked.transform.localPosition = new Vector3(-0.903f, num, -2f);
                categoryHeaderMasked.gameObject.SetActive(enabled);
                if (enabled) num -= 0.63f;
            }
            if (ModGameOptionsMenu.BehaviourList.TryGetValue(index, out var optionBehaviour))
            {
                optionBehaviour.transform.localPosition = new Vector3(0.952f, num, -2f);
                optionBehaviour.gameObject.SetActive(enabled);
                if (enabled) num -= 0.45f;
            }
        }

        __instance.ControllerSelectable.Clear();
        foreach (var x in __instance.scrollBar.GetComponentsInChildren<UiElement>())
            __instance.ControllerSelectable.Add(x);
        __instance.scrollBar.SetYBoundsMax(-num - 1.65f);
    }
    public class CheckboxGameSetting : BaseGameSetting
    {
        public bool Value; // 現在のチェック状態
        public Action<bool> OnValueChanged; // チェック変更時のコールバック
    }

    private static BaseGameSetting GetSetting(OptionItem item)
    {

        BaseGameSetting baseGameSetting = null;

        if (item is BooleanOptionItem boolItem)
        {
            var intSetting = ScriptableObject.CreateInstance<IntGameSetting>();

            intSetting.Type = OptionTypes.Int;
            intSetting.Value = boolItem.Bool ? 1 : 0;
            intSetting.Increment = 1;
            intSetting.ValidRange = new IntRange(0, 1);
            intSetting.FormatString = "";

            boolItem.SetValue(intSetting.Value);

            baseGameSetting = intSetting;
        }
        else if (item is IntegerOptionItem intItem)
        {
            var intSetting = ScriptableObject.CreateInstance<IntGameSetting>();
            intSetting.Type = OptionTypes.Int;
            intSetting.Value = intItem.GetInt();
            intSetting.Increment = intItem.Rule.Step;
            intSetting.ValidRange = new IntRange(intItem.Rule.MinValue, intItem.Rule.MaxValue);
            intSetting.ZeroIsInfinity = false;
            intSetting.SuffixType = NumberSuffixes.Multiplier;
            intSetting.FormatString = string.Empty;

            intItem.SetValue(intSetting.Value);

            baseGameSetting = intSetting;
        }
        else if (item is FloatOptionItem floatItem)
        {
            var floatSetting = ScriptableObject.CreateInstance<FloatGameSetting>();
            floatSetting.Type = OptionTypes.Float;
            floatSetting.Value = floatItem.GetFloat();
            floatSetting.Increment = floatItem.Rule.Step;
            floatSetting.ValidRange = new FloatRange(floatItem.Rule.MinValue, floatItem.Rule.MaxValue);
            floatSetting.ZeroIsInfinity = false;
            floatSetting.SuffixType = NumberSuffixes.Multiplier;
            floatSetting.FormatString = string.Empty;

            floatItem.SetValue(floatItem.Rule.GetNearestIndex(floatSetting.Value));

            baseGameSetting = floatSetting;
        }
        else if (item is StringOptionItem stringItem)
        {
            baseGameSetting = new StringGameSetting
            {
                Type = OptionTypes.String,
                Values = new StringNames[stringItem.Selections.Length],
                Index = stringItem.GetInt(),
            };
        }
        else if (item is PresetOptionItem presetItem)
        {
            baseGameSetting = new StringGameSetting
            {
                Type = OptionTypes.String,
                Values = new StringNames[OptionItem.NumPresets],
                Index = presetItem.GetInt(),
            };

        }

        if (baseGameSetting != null)
        {
            baseGameSetting.Title = StringNames.Accept;
        }


        return baseGameSetting;
    }



    [HarmonyPatch(typeof(NumberOption))]
    public static class NumberOptionPatch
    {
        [HarmonyPatch(nameof(NumberOption.Initialize)), HarmonyPrefix]
        private static bool InitializePrefix(NumberOption __instance)
        {

            switch (__instance.Title)
            {
                case StringNames.GameShortTasks:
                case StringNames.GameLongTasks:
                case StringNames.GameCommonTasks:
                    __instance.ValidRange = new FloatRange(0, 99);
                    break;
                case StringNames.GameKillCooldown:
                    __instance.ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.GameNumImpostors:
                    if (DebugModeManager.IsDebugMode)
                        __instance.ValidRange.min = 0;
                    break;
            }

            if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
            {
                var item = OptionItem.AllOptions[index];
                __instance.TitleText.text = item.GetName();

                // UIのValueがまだ初期状態ならOptionItemの値でセットする
                if (__instance.Value == default)
                {
                    if (item is BooleanOptionItem boolItem)
                    {
                        __instance.ValidRange = new FloatRange(0, 1);
                        __instance.Value = boolItem.GetValue() != 0 ? 1 : 0;
                    }
                    else if (item is IntegerOptionItem intItem)
                    {
                        __instance.Value = intItem.Rule.GetNearestIndex(intItem.GetValue());
                    }
                    else if (item is FloatOptionItem floatItem)
                    {
                        __instance.Value = floatItem.Rule.GetNearestIndex(floatItem.GetFloat());
                    }
                }

                __instance.UpdateValue();
                __instance.OnValueChanged?.Invoke(__instance);
                return false;
            }


            return true;
        }

        [HarmonyPatch(nameof(NumberOption.UpdateValue)), HarmonyPrefix]
        private static bool UpdateValuePrefix(NumberOption __instance)
        {
            if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
            {
                var item = OptionItem.AllOptions[index];

                if (item is BooleanOptionItem boolItem)
                {
                    boolItem.SetValue(__instance.GetInt() != 0 ? 1 : 0);
                }
                else if (item is IntegerOptionItem intItem)
                {
                    intItem.SetValue(intItem.Rule.GetNearestIndex(__instance.GetInt()));
                }
                else if (item is FloatOptionItem floatItem)
                {
                    floatItem.SetValue(floatItem.Rule.GetNearestIndex(__instance.GetFloat()));
                }

                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(NumberOption.Increase)), HarmonyPrefix]
        private static bool IncreasePrefix(NumberOption __instance)
        {
            if (!ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index)) return true;
            var item = OptionItem.AllOptions[index];


            if (item is BooleanOptionItem)
                __instance.Value = 1 - __instance.Value;
            else
            {
                float increment = __instance.Increment;
                if (Input.GetKey(KeyCode.LeftShift))
                    increment *= 5;

                __instance.Value += increment;
                if (__instance.Value > __instance.ValidRange.max)
                    __instance.Value = __instance.ValidRange.min;
            }

            __instance.UpdateValue();
            __instance.OnValueChanged?.Invoke(__instance);
            return false;
        }

        [HarmonyPatch(nameof(NumberOption.Decrease)), HarmonyPrefix]
        private static bool DecreasePrefix(NumberOption __instance)
        {
            if (!ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index)) return true;
            var item = OptionItem.AllOptions[index];


            if (item is BooleanOptionItem)
                __instance.Value = 1 - __instance.Value;
            else
            {
                float increment = __instance.Increment;
                if (Input.GetKey(KeyCode.LeftShift))
                    increment *= 5;

                __instance.Value -= increment;
                if (__instance.Value < __instance.ValidRange.min)
                    __instance.Value = __instance.ValidRange.max;
            }

            __instance.UpdateValue();
            __instance.OnValueChanged?.Invoke(__instance);
            return false;
        }

        [HarmonyPatch(nameof(NumberOption.FixedUpdate)), HarmonyPrefix]
        private static bool FixedUpdatePrefix(NumberOption __instance)
        {
            if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
            {
                var item = OptionItem.AllOptions[index];
                if (__instance.oldValue != __instance.Value)
                {
                    __instance.oldValue = __instance.Value;
                    __instance.ValueText.text = item != null ? item.GetString() : __instance.Value.ToString(__instance.FormatString);

                }

                return false;
            }
            return true;
        }
    }



    private static void ShowGameModeChangedNotice()
    {
        var hud = DestroyableSingleton<HudManager>.Instance;
        if (hud == null) return;
        hud.ShowPopUp(Translator.GetString("Warning.GameModeChangedNotice"));
    }

    [HarmonyPatch(typeof(StringOption))]
    public static class StringOptionPatch
    {
        [HarmonyPatch(nameof(StringOption.Initialize)), HarmonyPrefix]
        private static bool InitializePrefix(StringOption __instance)
        {
            if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
            {
                var item = OptionItem.AllOptions[index];
                __instance.TitleText.text = item.GetName();
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(StringOption.UpdateValue)), HarmonyPrefix]
        private static bool UpdateValuePrefix(StringOption __instance)
        {
            if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
            {
                var item = OptionItem.AllOptions[index];
                item.SetValue(__instance.GetInt());

                if (item is PresetOptionItem || item.Name == "GameMode")
                {
                    GameOptionsMenuPatch.UpdateSettings();
                    if (item.Name == "GameMode")
                        ShowGameModeChangedNotice();
                }

                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(StringOption.AdjustButtonsActiveState)), HarmonyPrefix]
        private static bool AdjustButtonsActiveStatePrefix(StringOption __instance) => false;

        [HarmonyPatch(nameof(StringOption.FixedUpdate)), HarmonyPrefix]
        private static bool FixedUpdatePrefix(StringOption __instance)
        {
            if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
            {
                var item = OptionItem.AllOptions[index];

                if (item is StringOptionItem stringItem && __instance.oldValue != __instance.Value)
                {
                    __instance.oldValue = __instance.Value;
                    __instance.ValueText.text = stringItem.GetString();
                }
                else if (item is PresetOptionItem presetItem && __instance.oldValue != __instance.Value)
                {
                    __instance.oldValue = __instance.Value;
                    __instance.ValueText.text = presetItem.GetString();
                }

                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(StringOption.Increase)), HarmonyPrefix]
        public static bool IncreasePrefix(StringOption __instance)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                __instance.Value += 5;
                if (__instance.Value > __instance.Values.Length - 1)
                    __instance.Value = __instance.Values.Length - 1;

                __instance.UpdateValue();
                __instance.OnValueChanged.Invoke(__instance);
                return false;
            }

            if (__instance.Value == __instance.Values.Length - 1)
            {
                __instance.Value = 0;
                __instance.UpdateValue();
                __instance.OnValueChanged.Invoke(__instance);
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(StringOption.Decrease)), HarmonyPrefix]
        public static bool DecreasePrefix(StringOption __instance)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                __instance.Value -= 5;
                if (__instance.Value < 0)
                    __instance.Value = 0;

                __instance.UpdateValue();
                __instance.OnValueChanged.Invoke(__instance);
                return false;
            }

            if (__instance.Value == 0)
            {
                __instance.Value = __instance.Values.Length - 1;
                __instance.UpdateValue();
                __instance.OnValueChanged.Invoke(__instance);
                return false;
            }

            return true;
        }
    }
}
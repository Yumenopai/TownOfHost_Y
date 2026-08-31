using System;
using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using TownOfHostY.Roles.Core;

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

        var gradient = __instance.transform.FindChild("Gradient");
        gradient.localPosition = new Vector3(3.88f, -4.44f, -20f);
        gradient.localScale = new Vector3(0.449f, 0.3605f, 7.2125f);

        var maskBg = __instance.MaskBg.transform;
        maskBg.localScale = new Vector3(6.6f, 5f, 0.56f);
        var maskArea = __instance.MaskArea.transform;
        maskArea.localScale = new Vector3(6.6f, 5f, 0.56f);

        if (ModGameOptionsMenu.TabIndex < 3) return true;
        var modTab = (TabGroup)(ModGameOptionsMenu.TabIndex - 3);

        var scrollbarTrack = __instance.transform.FindChild("UI_ScrollbarTrack");
        scrollbarTrack.localPosition -= new Vector3(1f, 0f, 0f);
        var scrollbar = __instance.transform.FindChild("UI_Scrollbar");
        scrollbar.localPosition -= new Vector3(1f, 0f, 0f);

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

                var dividerImage = categoryHeaderMasked.transform.FindChild("DividerImage");
                dividerImage.localPosition = new Vector3(2.36f, 0.05f, -2f);
                dividerImage.localScale = new Vector3(0.72f, 0.5f, 1f);

                var headerText = categoryHeaderMasked.transform.FindChild("HeaderText");
                if (option.Tab != TabGroup.ModMainSettings && option is not TextOptionItem)
                {
                    var labelSprite = categoryHeaderMasked.transform.FindChild("LabelSprite");
                    labelSprite.localPosition -= new Vector3(0f, 0.06f, 0f);
                    labelSprite.localScale = new Vector3(1.5f, 1.25f, 1f);

                    var headerTextRectTransform = headerText.GetComponent<RectTransform>();
                    headerTextRectTransform.sizeDelta = new Vector2(4.4f, 0.38f);
                    headerTextRectTransform.localPosition = new Vector3(0.55f, -0.22f, -1f);
                }
                var headerTextTMP = headerText.GetComponent<TMPro.TextMeshPro>();
                headerTextTMP.fontStyle = TMPro.FontStyles.Bold;
                headerTextTMP.outlineWidth = 0.17f;

                categoryHeaderMasked.gameObject.SetActive(enabled);
                ModGameOptionsMenu.CategoryHeaderList.TryAdd(index, categoryHeaderMasked);

                if (enabled)
                {
                    num -= 0.63f;
                }
            }
            if (option is TextOptionItem) continue;

            var baseGameSetting = GetSetting(option);
            if (baseGameSetting == null) continue;

            OptionBehaviour optionBehaviour;
            switch (baseGameSetting.Type)
            {
                case OptionTypes.String:
                    {
                        optionBehaviour = UnityEngine.Object.Instantiate<StringOption>(__instance.stringOptionOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                        optionBehaviour.transform.localPosition = new Vector3(pos_x, num, pos_z);

                        OptionBehaviourSetSizeAndPosition(optionBehaviour, option, baseGameSetting.Type);

                        optionBehaviour.SetClickMask(__instance.ButtonClickMask);
                        optionBehaviour.SetUpFromData(baseGameSetting, 20);
                        ModGameOptionsMenu.OptionList.TryAdd(optionBehaviour, index);
                        //Logger.Info($"{option.Name}, {index}", "OptionList.TryAdd");
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
            optionBehaviour.OnValueChanged = new Action<OptionBehaviour>((o) => { });
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
        Vector3 positionOffset = new(0f, 0f, 0f);
        Vector3 scaleOffset = new(0f, 0f, 0f);
        Color color = new(0.7f, 0.7f, 0.7f);
        Vector2 sizeDelta = new(4.7f, 0.37f);

        if (option.Parent?.Parent?.Parent != null)
        {
            positionOffset = new(0.3f, 0f, 0f);
            scaleOffset = new(-0.18f, 0, 0);
            color = new(0.7f, 0.5f, 0.5f);
            sizeDelta.x = 4.1f;
        }
        else if (option.Parent?.Parent != null)
        {
            positionOffset = new(0.2f, 0f, 0f);
            scaleOffset = new(-0.12f, 0, 0);
            color = new(0.5f, 0.5f, 0.7f);
            sizeDelta.x = 4.3f;
        }
        else if (option.Parent != null)
        {
            positionOffset = new(0.1f, 0f, 0f);
            scaleOffset = new(-0.05f, 0, 0);
            color = new(0.5f, 0.7f, 0.5f);
            sizeDelta.x = 4.5f;
        }
        else if (option.Parent == null && option.Tab != TabGroup.ModMainSettings)
        {
            sizeDelta.y = 0.43f;
        }

        var labelBackground = optionBehaviour.transform.FindChild("LabelBackground");
        var labelBackgroundSpriteRenderer = labelBackground.GetComponent<SpriteRenderer>();
        labelBackgroundSpriteRenderer.sprite = Utils.LoadSprite($"TownOfHost_Y.Resources.SettingMenu_LabelBackground.png", 100f);
        labelBackgroundSpriteRenderer.color = color;

        labelBackground.localScale = new Vector3(1.57f, 0.8f, 1f) + scaleOffset;
        labelBackground.localRotation = UnityEngine.Quaternion.identity;
        labelBackground.localPosition = new Vector3(-2.54f, -0.062f, 0f) + positionOffset;

        var titleText = optionBehaviour.transform.FindChild("Title Text");
        titleText.localPosition = new Vector3(-2.54f, -0.05f, 0f) + positionOffset;
        titleText.GetComponent<RectTransform>().sizeDelta = sizeDelta;

        var titleTextTMP = titleText.GetComponent<TMPro.TextMeshPro>();
        titleTextTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        titleTextTMP.fontStyle = TMPro.FontStyles.Bold;
        titleTextTMP.outlineWidth = 0.17f;

        try
        {
            var hoverComp = labelBackground.gameObject.GetComponent<LabelHoverBehaviour>() ?? labelBackground.gameObject.AddComponent<LabelHoverBehaviour>();
            hoverComp.InitializeMenuDescription(option);
        }
        catch { }

        if (type is OptionTypes.Int or OptionTypes.Float or OptionTypes.String)
        {
            string valueTMPName = "";
            switch (type)
            {
                case OptionTypes.String:
                    valueTMPName = "Value_TMP (1)";
                    optionBehaviour.transform.FindChild(valueTMPName).GetComponent<RectTransform>().sizeDelta = new Vector2(2.3f, 0.4f);
                    break;

                case OptionTypes.Float:
                case OptionTypes.Int:
                    valueTMPName = "Value_TMP";
                    break;
            }

            var plusButton = optionBehaviour.transform.FindChild("PlusButton");
            var minusButton = optionBehaviour.transform.FindChild("MinusButton");
            if (option.IsFixValue)
            {
                plusButton.gameObject.SetActive(false);
                minusButton.gameObject.SetActive(false);
            }
            else
            {
                plusButton.localPosition += new Vector3(0.1f, 0f, 0f);
                minusButton.localPosition += new Vector3(-0.7f, 0f, 0f);
            }

            var valueBox = optionBehaviour.transform.FindChild("ValueBox");
            valueBox.localPosition += new Vector3(-0.3f, 0f, 0f);
            valueBox.localScale += new Vector3(0.2f, 0f, 0f);

            optionBehaviour.transform.FindChild(valueTMPName).localPosition += new Vector3(-0.3f, 0f, 0f);
        }
    }

    public static void UpdateSettings()
    {
        foreach (var optionBehaviour in ModGameOptionsMenu.OptionList.Keys)
        {
            try
            {
                optionBehaviour.Initialize();
            }
            catch { }
        }
        if (Instance != null) ReCreateSettings(Instance);
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

    private static BaseGameSetting GetSetting(OptionItem item)
    {
        BaseGameSetting baseGameSetting = null;

        if (item is BooleanOptionItem booleanItem)
        {          
            baseGameSetting = new StringGameSetting
            {
                Type = OptionTypes.String,
                Values = new StringNames[2], 
                Index = booleanItem.GetBool() ? 1 : 0,
            };
        }
        else if (item is IntegerOptionItem)
        {
            IntegerOptionItem intItem = item as IntegerOptionItem;
            baseGameSetting = new IntGameSetting
            {
                Type = OptionTypes.Int,
                Value = intItem.GetInt(),
                Increment = intItem.Rule.Step,
                ValidRange = new IntRange(intItem.Rule.MinValue, intItem.Rule.MaxValue),
                ZeroIsInfinity = false,
                SuffixType = NumberSuffixes.Multiplier,
                FormatString = string.Empty,
            };
        }
        else if (item is FloatOptionItem)
        {
            FloatOptionItem floatItem = item as FloatOptionItem;
            baseGameSetting = new FloatGameSetting
            {
                Type = OptionTypes.Float,
                Value = floatItem.GetFloat(),
                Increment = floatItem.Rule.Step,
                ValidRange = new FloatRange(floatItem.Rule.MinValue, floatItem.Rule.MaxValue),
                ZeroIsInfinity = false,
                SuffixType = NumberSuffixes.Multiplier,
                FormatString = string.Empty,
            };
        }
        else if (item is StringOptionItem)
        {
            StringOptionItem stringItem = item as StringOptionItem;
            baseGameSetting = new StringGameSetting
            {
                Type = OptionTypes.String,
                Values = new StringNames[stringItem.Selections.Length], //ダミー
                Index = stringItem.GetInt(),
            };
        }
        else if (item is PresetOptionItem)
        {
            PresetOptionItem presetItem = item as PresetOptionItem;
            baseGameSetting = new StringGameSetting
            {
                Type = OptionTypes.String,
                Values = new StringNames[OptionItem.NumPresets], //ダミー
                Index = presetItem.GetInt(),
            };
        }

        if (baseGameSetting != null)
        {
            baseGameSetting.Title = StringNames.Accept; //ダミー
        }

        return baseGameSetting;
    }
}

public class LabelHoverBehaviour : UnityEngine.MonoBehaviour
{
    private OptionItem optionRef = null;
    private string originalText = string.Empty;
    private UnityEngine.Collider2D cachedCollider;
    private GameSettingMenu cachedMenu;

    public void InitializeMenuDescription(OptionItem option)
    {
        optionRef = option;
        cachedCollider = GetComponent<UnityEngine.Collider2D>() ?? gameObject.AddComponent<UnityEngine.BoxCollider2D>();
        if (cachedCollider is UnityEngine.BoxCollider2D bc) bc.isTrigger = true;

        cachedMenu = UnityEngine.Object.FindObjectOfType<GameSettingMenu>();
        if (cachedMenu != null && cachedMenu.MenuDescriptionText != null)
        {
            originalText = cachedMenu.MenuDescriptionText.text;
        }

        var sr = GetComponent<UnityEngine.SpriteRenderer>();
        if (sr != null && cachedCollider is UnityEngine.BoxCollider2D box)
        {
            var bounds = sr.bounds.size;
            var localSizeX = bounds.x / (transform.lossyScale.x == 0f ? 1f : transform.lossyScale.x);
            var localSizeY = bounds.y / (transform.lossyScale.y == 0f ? 1f : transform.lossyScale.y);
            box.size = new UnityEngine.Vector2(localSizeX, localSizeY);
            box.isTrigger = true;
        }
    }

    private void Update()
    {
        if (cachedCollider == null || UnityEngine.Camera.main == null) return;

        UnityEngine.Vector2 worldPoint = UnityEngine.Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
        UnityEngine.Collider2D hit = UnityEngine.Physics2D.OverlapPoint(worldPoint);

        if (cachedMenu == null)
        {
            cachedMenu = UnityEngine.Object.FindObjectOfType<GameSettingMenu>();
        }

        // クリックで説明文を取得して上書き
        if (UnityEngine.Input.GetMouseButtonDown(0) && hit == cachedCollider)
        {
            if (cachedMenu != null && cachedMenu.MenuDescriptionText != null)
            {
                string infoText = string.Empty;

                if (Enum.TryParse(typeof(CustomRoles), optionRef.Name, true, out var role))
                {
                    infoText = Utils.GetRoleInfoLong((CustomRoles)role);
                }
                else
                {
                    if (optionRef != null)
                    {
                        string title = optionRef.GetName(colorLighter: true);
                        string desc = Translator.GetString(optionRef.Name + "InfoLong", optionRef.ReplacementDictionary);
                        if (desc.StartsWith("<INVALID:"))
                        {
                            desc = Translator.GetString("OptionNoExplanation");
                        }

                        infoText = $"<b>{title}</b>\n{desc}";
                    }
                }

                cachedMenu.MenuDescriptionText.DestroyTranslator();
                cachedMenu.MenuDescriptionText.text = infoText;
            }
        }
    }
}

[HarmonyPatch(typeof(ToggleOption))]
public static class ToggleOptionPatch
{
    [HarmonyPatch(nameof(ToggleOption.Initialize)), HarmonyPrefix]
    private static bool InitializePrefix(ToggleOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            //Logger.Info($"{item.Name}, {index}", "ToggleOption.Initialize.TryGetValue");
            __instance.TitleText.text = item.GetName();
            __instance.CheckMark.enabled = item.GetBool();
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(ToggleOption.UpdateValue)), HarmonyPrefix]
    private static bool UpdateValuePrefix(ToggleOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            //Logger.Info($"{item.Name}, {index}", "ToggleOption.UpdateValue.TryGetValue");
            item.SetValue(__instance.GetBool() ? 1 : 0);
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(NumberOption))]
public static class NumberOptionPatch
{
    [HarmonyPatch(nameof(NumberOption.Initialize)), HarmonyPrefix]
    private static bool InitializePrefix(NumberOption __instance)
    {
        // バニラゲーム設定の拡張
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
                {
                    __instance.ValidRange.min = 0;
                }
                break;
            default:
                break;
        }

        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index]; 
            __instance.TitleText.text = item.GetName();
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
            //Logger.Info($"{item.Name}, {index}", "NumberOption.UpdateValue.TryGetValue");

            if (item is IntegerOptionItem integerOptionItem)
            {
                integerOptionItem.SetValue(integerOptionItem.Rule.GetNearestIndex(__instance.GetInt()));
            }
            else if (item is FloatOptionItem floatOptionItem)
            {
                floatOptionItem.SetValue(floatOptionItem.Rule.GetNearestIndex(__instance.GetFloat()));
            }

            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(NumberOption.AdjustButtonsActiveState)), HarmonyPrefix]
    private static bool AdjustButtonsActiveStatePrefix(NumberOption __instance)
    {
        return false;
    }
    [HarmonyPatch(nameof(NumberOption.FixedUpdate)), HarmonyPrefix]
    private static bool FixedUpdatePrefix(NumberOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            //Logger.Info($"{item.Name}, {index}", "NumberOption.FixedUpdate.TryGetValue");

            if (__instance.oldValue != __instance.Value)
            {
                __instance.oldValue = __instance.Value;
                __instance.ValueText.text = GetValueString(__instance, __instance.Value, item);
            }
            return false;
        }
        return true;
    }
    public static string GetValueString(NumberOption __instance, float value, OptionItem item)
    {
        if (__instance.ZeroIsInfinity && Mathf.Abs(value) < 0.0001f) return "<b>∞</b>";
        if (item == null) return value.ToString(__instance.FormatString);
        return item.GetString();
    }
    [HarmonyPatch(nameof(NumberOption.Increase)), HarmonyPrefix]
    public static bool IncreasePrefix(NumberOption __instance)
    {
        // Shift押しながらの値更新
        if (Input.GetKey(KeyCode.LeftShift))
        {
            __instance.Value = __instance.Value + (__instance.Increment * 5);
            // 超えている場合は最大値
            if (__instance.Value > __instance.ValidRange.max)
            {
                __instance.Value = __instance.ValidRange.max;
            }
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            return false;
        }

        if (__instance.Value == __instance.ValidRange.max)
        {
            __instance.Value = __instance.ValidRange.min;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(NumberOption.Decrease)), HarmonyPrefix]
    public static bool DecreasePrefix(NumberOption __instance)
    {
        // Shift押しながらの値更新
        if (Input.GetKey(KeyCode.LeftShift))
        {
            __instance.Value = __instance.Value - (__instance.Increment * 5);
            // 超えている場合は最小値
            if (__instance.Value < __instance.ValidRange.min)
            {
                __instance.Value = __instance.ValidRange.min;
            }
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            return false;
        }

        if (__instance.Value == __instance.ValidRange.min)
        {
            __instance.Value = __instance.ValidRange.max;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            return false;
        }
        return true;
    }
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
            //Logger.Info($"{item.Name}, {index}", "StringOption.Initialize.TryAdd");
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
            Logger.Info($"{item.Name}, {index}", "StringOption.UpdateValue.TryAdd");

            item.SetValue(__instance.GetInt());
            if (item is PresetOptionItem || item.Name == "GameMode")
            {
                GameOptionsMenuPatch.UpdateSettings();
            }
            if (item is PresetOptionItem)
            {
                GameSettingMenuPatch.NotifyPresetChanged();
            }
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(StringOption.AdjustButtonsActiveState)), HarmonyPrefix]
    private static bool AdjustButtonsActiveStatePrefix(StringOption __instance)
    {
        return false;
    }
    [HarmonyPatch(nameof(StringOption.FixedUpdate)), HarmonyPrefix]
    private static bool FixedUpdatePrefix(StringOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];

            if (item is StringOptionItem stringOptionItem)
            {
                if (__instance.oldValue != __instance.Value)
                {
                    __instance.oldValue = __instance.Value;
                    __instance.ValueText.text = stringOptionItem.GetString();
                }
            }
            if (item is PresetOptionItem presetOptionItem)
            {
                if (__instance.oldValue != __instance.Value)
                {
                    __instance.oldValue = __instance.Value;
                    __instance.ValueText.text = presetOptionItem.GetString();
                }
            }         
            if (item is BooleanOptionItem booleanOptionItem)
            {
                if (__instance.oldValue != __instance.Value)
                {
                    __instance.oldValue = __instance.Value;
                    __instance.ValueText.text = booleanOptionItem.GetString();
                }
            }
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(StringOption.Increase)), HarmonyPrefix]
    public static bool IncreasePrefix(StringOption __instance)
    {
        // Shift押しながらの値更新
        if (Input.GetKey(KeyCode.LeftShift))
        {
            __instance.Value = __instance.Value + 5;
            // 超えている場合は最大値
            if (__instance.Value > __instance.Values.Length - 1)
            {
                __instance.Value = __instance.Values.Length - 1;
            }
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
        // Shift押しながらの値更新
        if (Input.GetKey(KeyCode.LeftShift))
        {
            __instance.Value = __instance.Value - 5;
            // 超えている場合は最小値
            if (__instance.Value < 0)
            {
                __instance.Value = 0;
            }
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
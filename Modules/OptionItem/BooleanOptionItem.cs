using System;
using TownOfHostY.Roles.Core;
using UnityEngine; // ScriptableObject 用

namespace TownOfHostY
{
    public class BooleanOptionItem : OptionItem
    {
        public bool Bool => GetValue() == 1;

        public const string TEXT_true = "ColoredOn";
        public const string TEXT_false = "ColoredOff";

        // Boolean 専用の設定クラス
        public class BooleanGameSetting : ScriptableObject
        {
            public bool Value
            {
                get => value;
                set
                {
                    this.value = value;
                    OnValueChanged?.Invoke(value);
                }
            }
            private bool value;

            public Action<bool> OnValueChanged;
        }

        // 内部の BooleanGameSetting インスタンス
        public BooleanGameSetting Setting { get; private set; }

        // コンストラクタ
        public BooleanOptionItem(int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue)
            : base(id, name, defaultValue ? 1 : 0, tab, isSingleValue)
        {
            Setting = ScriptableObject.CreateInstance<BooleanGameSetting>();
            Setting.Value = defaultValue;

            // OptionItem と Setting を同期
            Setting.OnValueChanged += newValue => SetValue(newValue ? 1 : 0);
        }

        public static BooleanOptionItem Create(
            int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue
        )
        {
            return new BooleanOptionItem(id, name, defaultValue, tab, isSingleValue);
        }

        public static BooleanOptionItem Create(
            int id, Enum name, bool defaultValue, TabGroup tab, bool isSingleValue
        )
        {
            return new BooleanOptionItem(id, name.ToString(), defaultValue, tab, isSingleValue);
        }

        public static BooleanOptionItem Create(
            SimpleRoleInfo roleInfo, int idOffset, Enum name, bool defaultValue, bool isSingleValue, OptionItem parent = null
        )
        {
            var opt = new BooleanOptionItem(
                roleInfo.ConfigId + idOffset, name.ToString(), defaultValue, roleInfo.Tab, isSingleValue
            );
            opt.SetParent(parent ?? roleInfo.RoleOption);
            return opt;
        }

        // Getter
        public override string GetString()
        {
            return Translator.GetString(GetBool() ? TEXT_true : TEXT_false);
        }

        // Setter
        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(value % 2 == 0 ? 0 : 1, doSync);

            // ScriptableObject 側も同期
            if (Setting != null)
                Setting.Value = (value != 0);
        }
    }
}

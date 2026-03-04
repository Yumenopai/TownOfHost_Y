using AmongUs.GameOptions;

namespace TownOfHostY.Modules
{
    public class NormalGameOptionsSender : GameOptionsSender
    {
        public override IGameOptions BasedGameOptions
        {
            get
            {
                if (GameOptionsManager.Instance == null) return null;
                if (GameOptionsManager.Instance.CurrentGameOptions != null) return GameOptionsManager.Instance.CurrentGameOptions;
                var fallback = GameOptionsManager.Instance.currentNormalGameOptions;
                return fallback == null ? null : (IGameOptions)(object)fallback;
            }
        }

        public override bool IsDirty
        {
            get
            {
                if (GameManager.Instance == null)
                {
                    _logicOptions = null;
                    return false;
                }

                if (_logicOptions == null || GameManager.Instance.LogicComponents == null || !GameManager.Instance.LogicComponents.Contains(_logicOptions))
                {
                    _logicOptions = null;
                    foreach (var glc in GameManager.Instance.LogicComponents)
                    {
                        if (glc.TryCast<LogicOptions>(out var lo))
                        {
                            _logicOptions = lo;
                            break;
                        }
                    }
                }
                return _logicOptions?.IsDirty ?? false; // nullならfalse
            }
            protected set
            {
                _logicOptions?.ClearDirtyFlag(); // nullなら何もしない
            }
        }

        private LogicOptions _logicOptions;

        public override IGameOptions BuildGameOptions()
            => BasedGameOptions;
    }
}
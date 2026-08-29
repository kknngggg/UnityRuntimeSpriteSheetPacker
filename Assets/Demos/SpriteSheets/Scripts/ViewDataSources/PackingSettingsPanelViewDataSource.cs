using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets
{
    [Serializable]
    public sealed class PackingSettingsPanelViewDataSource
    {
        [SerializeField, DontCreateProperty]
        private SpriteSheet.PackingSettings _packingSettings = SpriteSheet.PackingSettings.Default;

        private string _effectiveMaxSizeText;

        public PackingSettingsPanelViewDataSource()
        {
            UpdateEffectiveMaxSizeText();
        }

        public SpriteSheet.PackingSettings PackingSettings => this._packingSettings;

        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public string PageName { get; set; } = "Page";

        [CreateProperty]
        public int Padding {
            get => this._packingSettings.Padding;
            set => this._packingSettings.Padding = value;
        }

        [CreateProperty]
        public int MaxSize {
            get => this._packingSettings.MaxSize;
            set {
                this._packingSettings.MaxSize = value;
                UpdateEffectiveMaxSizeText();
            }
        }

        [CreateProperty]
        public bool ForcePowerOfTwo {
            get => this._packingSettings.ForcePowerOfTwo;
            set {
                this._packingSettings.ForcePowerOfTwo = value;
                UpdateEffectiveMaxSizeText();
            }
        }

        [CreateProperty]
        public DisplayStyle EffectiveMaxSizeDisplay => this.ForcePowerOfTwo ? DisplayStyle.Flex : DisplayStyle.None;

        [CreateProperty] public string EffectiveMaxSize => this._effectiveMaxSizeText;

        private void UpdateEffectiveMaxSizeText()
        {
            if (this.ForcePowerOfTwo)
            {
                this._effectiveMaxSizeText = $"Effective Max Size: {this._packingSettings.EffectiveMaxSize}";
            }
        }
    }
}

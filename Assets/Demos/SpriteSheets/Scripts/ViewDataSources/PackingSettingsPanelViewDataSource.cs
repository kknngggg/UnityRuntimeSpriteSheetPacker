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
            set => this._packingSettings.MaxSize = value;
        }

        [CreateProperty]
        public bool ForcePowerOfTwo {
            get => this._packingSettings.ForcePowerOfTwo;
            set => this._packingSettings.ForcePowerOfTwo = value;
        }

        [CreateProperty]
        public StyleColor ForcePowerOfTwoToggleColor => this.ForcePowerOfTwo ?
            new Color(0.398f, 0.801f, 0.450f) : Color.gray2;
    }
}

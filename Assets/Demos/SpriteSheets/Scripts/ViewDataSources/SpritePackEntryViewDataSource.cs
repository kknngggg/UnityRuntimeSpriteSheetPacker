using System;
using System.IO;
using Unity.Properties;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets
{
    [Serializable]
    public sealed class SpritePackEntryViewDatasource
    {
        [SerializeField]
        [DontCreateProperty]
        private string _diskPath;

        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public string Name { get; set; }

        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public float PixelsPerUnit { get; set; } = 100;

        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);

        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public SpriteMeshType MeshType { get; set; } = SpriteMeshType.FullRect;

        [CreateProperty]
        public string DiskPath {
            get => this._diskPath;
            set {
                this._diskPath = value;

                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(this.Name))
                {
                    this.Name = Path.GetFileNameWithoutExtension(value);
                }
            }
        }
    }
}

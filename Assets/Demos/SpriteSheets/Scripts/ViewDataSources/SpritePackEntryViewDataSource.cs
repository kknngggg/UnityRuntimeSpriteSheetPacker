using System;
using Unity.Properties;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets
{
    [Serializable]
    public sealed class SpritePackEntryViewDatasource
    {
        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public string Name { get; set; }

        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public float PixelsPerUnit { get; set; }

        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public Vector2 Pivot { get; set; }

        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public SpriteMeshType MeshType { get; set; }

        [field: SerializeField]
        [field: DontCreateProperty]
        [CreateProperty]
        public string DiskPath { get; set; }
    }
}

using UnityEngine;

namespace kknngggg.Unity.Sprites
{
    public struct SpritePackEntry
    {
        public const float DEFAULT_PIXELS_PER_UNIT = 100f;
        public const SpriteMeshType DEFAULT_MESH_TYPE = SpriteMeshType.FullRect;
        public static Vector2 DEFAULT_PIVOT => new Vector2(0.5f, 0.5f);

        public readonly Texture2D Texture;
        public readonly string Name;
        public readonly float PixelsPerUnit;
        public readonly Vector2 Pivot;
        public readonly SpriteMeshType MeshType;

        public SpritePackEntry(Texture2D texture) : this(texture,
                                                         texture.name,
                                                         DEFAULT_PIXELS_PER_UNIT,
                                                         DEFAULT_PIVOT,
                                                         DEFAULT_MESH_TYPE)
        { }

        public SpritePackEntry(Texture2D texture, string name) : this(texture,
                                                                      name,
                                                                      DEFAULT_PIXELS_PER_UNIT,
                                                                      DEFAULT_PIVOT,
                                                                      DEFAULT_MESH_TYPE)
        { }

        public SpritePackEntry(Texture2D texture,
                               string name,
                               float pixelsPerUnit,
                               Vector2 pivot,
                               SpriteMeshType meshType)
        {
            this.Texture = texture;
            this.Name = name;
            this.PixelsPerUnit = pixelsPerUnit;
            this.Pivot = pivot;
            this.MeshType = meshType;
        }
    }
}

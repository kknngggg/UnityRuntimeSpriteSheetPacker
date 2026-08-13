using UnityEngine;

namespace kknngggg.Unity.Sprites
{
    public partial class SpriteSheet
    {
        public struct SliceInfo
        {
            public readonly string Name;
            public readonly Rect Rect;
            public readonly float PixelsPerUnit;
            public readonly Vector2 Pivot;
            public readonly SpriteMeshType MeshType;
            public readonly int Page;

            public SliceInfo(string name,
                             Rect rect,
                             int page,
                             float pixelsPerUnit,
                             Vector2 pivot,
                             SpriteMeshType meshType)
            {
                this.Name = name;
                this.Rect = rect;
                this.Page = page;
                this.PixelsPerUnit = pixelsPerUnit;
                this.Pivot = pivot;
                this.MeshType = meshType;
            }
        }
    }
}

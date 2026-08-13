using System.Collections.Generic;
using UnityEngine;

namespace kknngggg.Unity.Sprites
{
    internal partial class SpritePackingSheet
    {
        private struct PackedPage
        {
            public readonly Texture2D Texture;
            public readonly List<SpriteSheet.SliceInfo> Slices;
            public readonly List<int> PackedIndices;

            public PackedPage(Texture2D texture, List<SpriteSheet.SliceInfo> slices, List<int> packedIndices)
            {
                this.Texture = texture;
                this.Slices = slices;
                this.PackedIndices = packedIndices;
            }
        }
    }
}

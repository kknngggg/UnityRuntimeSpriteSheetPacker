using NUnit.Framework;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Tests
{
    public class SpriteSheetSliceInfoTests
    {
        [Test]
        public void Constructor_AssignsEveryField()
        {
            Rect rect = new Rect(2f, 4f, 8f, 16f);
            Vector2 pivot = new Vector2(0.1f, 0.9f);

            SpriteSheet.SliceInfo slice = new SpriteSheet.SliceInfo("hero",
                                                                    rect,
                                                                    2,
                                                                    50f,
                                                                    pivot,
                                                                    SpriteMeshType.Tight);

            Assert.AreEqual("hero", slice.Name);
            Assert.AreEqual(rect, slice.Rect);
            Assert.AreEqual(2, slice.Page);
            Assert.AreEqual(50f, slice.PixelsPerUnit);
            Assert.AreEqual(pivot, slice.Pivot);
            Assert.AreEqual(SpriteMeshType.Tight, slice.MeshType);
        }
    }
}

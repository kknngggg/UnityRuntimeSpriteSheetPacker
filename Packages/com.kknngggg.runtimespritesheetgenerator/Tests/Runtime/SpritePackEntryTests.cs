using NUnit.Framework;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Tests
{
    public class SpritePackEntryTests : SpriteSheetTestBase
    {
        [Test]
        public void TextureOnlyConstructor_UsesTextureNameAndDefaults()
        {
            Texture2D texture = CreateTexture(4, 4, "hero");

            SpritePackEntry entry = new SpritePackEntry(texture);

            Assert.AreSame(texture, entry.Texture);
            Assert.AreEqual("hero", entry.Name);
            Assert.AreEqual(SpritePackEntry.DEFAULT_PIXELS_PER_UNIT, entry.PixelsPerUnit);
            Assert.AreEqual(SpritePackEntry.DEFAULT_PIVOT, entry.Pivot);
            Assert.AreEqual(SpritePackEntry.DEFAULT_MESH_TYPE, entry.MeshType);
        }

        [Test]
        public void NameConstructor_KeepsDefaults()
        {
            Texture2D texture = CreateTexture(4, 4, "file");

            SpritePackEntry entry = new SpritePackEntry(texture, "idle_0");

            Assert.AreSame(texture, entry.Texture);
            Assert.AreEqual("idle_0", entry.Name);
            Assert.AreEqual(100f, entry.PixelsPerUnit);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), entry.Pivot);
            Assert.AreEqual(SpriteMeshType.FullRect, entry.MeshType);
        }

        [Test]
        public void FullConstructor_AssignsEveryField()
        {
            Texture2D texture = CreateTexture(8, 8, "src");
            Vector2 pivot = new Vector2(0.25f, 0.75f);

            SpritePackEntry entry = new SpritePackEntry(texture, "fx", 50f, pivot, SpriteMeshType.Tight);

            Assert.AreSame(texture, entry.Texture);
            Assert.AreEqual("fx", entry.Name);
            Assert.AreEqual(50f, entry.PixelsPerUnit);
            Assert.AreEqual(pivot, entry.Pivot);
            Assert.AreEqual(SpriteMeshType.Tight, entry.MeshType);
        }

        [Test]
        public void Defaults_MatchDocumentedValues()
        {
            Assert.AreEqual(100f, SpritePackEntry.DEFAULT_PIXELS_PER_UNIT);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), SpritePackEntry.DEFAULT_PIVOT);
            Assert.AreEqual(SpriteMeshType.FullRect, SpritePackEntry.DEFAULT_MESH_TYPE);
        }
    }
}

using System;
using kknngggg.Unity.Sprites.Errors;
using NUnit.Framework;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Tests
{
    public class SpritePackingSheetTests : SpriteSheetTestBase
    {
        [Test]
        public void PackThisSheet_NullEntries_Fails()
        {
            SpritePackingSheet packingSheet = new SpritePackingSheet(null, SmallSettings());
            AssertPackFailed(packingSheet.PackThisSheet(), PackingErrorCodes.NULL_ENTRIES);
        }

        [Test]
        public void PackThisSheet_NullTexture_Fails()
        {
            SpritePackEntry[] entries = { new SpritePackEntry(null, "ghost") };
            SpritePackingSheet packingSheet = new SpritePackingSheet(entries, SmallSettings());
            AssertPackFailed(packingSheet.PackThisSheet(), PackingErrorCodes.NULL_TEXTURE);
        }

        [Test]
        public void Constructor_KeepsCallerNameAndSpriteFields()
        {
            Texture2D texture = CreateTexture(4, 4, "file");
            Vector2 pivot = new Vector2(0.2f, 0.8f);
            SpritePackEntry[] entries = {
                new SpritePackEntry(texture, "custom", 25f, pivot, SpriteMeshType.Tight),
            };

            SpritePackingSheet packingSheet = new SpritePackingSheet(entries, SmallSettings());
            PackingResult result = packingSheet.PackThisSheet();
            Assert.IsTrue(result.IsSuccess, result.Error != null ? result.Error.Message : "pack failed");
            this.Sheet = result.SpriteSheet;

            Assert.IsTrue(this.Sheet.TryGetSlice("custom", out SpriteSheet.SliceInfo slice));
            Assert.IsFalse(this.Sheet.TryGetSlice("file", out _));
            Assert.AreEqual(25f, slice.PixelsPerUnit);
            Assert.AreEqual(pivot, slice.Pivot);
            Assert.AreEqual(SpriteMeshType.Tight, slice.MeshType);
            Assert.AreEqual(0, slice.Page);
        }

        [Test]
        public void PackThisSheet_Empty_Fails()
        {
            SpritePackingSheet packingSheet = new SpritePackingSheet(Array.Empty<SpritePackEntry>(), SmallSettings());
            AssertPackFailed(packingSheet.PackThisSheet(), PackingErrorCodes.EMPTY_ENTRIES);
        }

        [Test]
        public void PackThisSheet_AssignsSequentialPageIndices()
        {
            Texture2D a = CreateTexture(8, 8, "a");
            Texture2D b = CreateTexture(8, 8, "b");
            SpritePackEntry[] entries = {
                new SpritePackEntry(a),
                new SpritePackEntry(b),
            };

            SpritePackingSheet packingSheet =
                new SpritePackingSheet(entries, SmallSettings(maxSize: 8, padding: 0, forcePowerOfTwo: false));
            PackingResult result = packingSheet.PackThisSheet();
            Assert.IsTrue(result.IsSuccess, result.Error != null ? result.Error.Message : "pack failed");
            this.Sheet = result.SpriteSheet;

            Assert.AreEqual(2, this.Sheet.PageCount);
            int pageA = this.Sheet.Slices["a"].Page;
            int pageB = this.Sheet.Slices["b"].Page;
            Assert.AreNotEqual(pageA, pageB);
            Assert.AreEqual(0, Mathf.Min(pageA, pageB));
            Assert.AreEqual(1, Mathf.Max(pageA, pageB));
        }

        [Test]
        public void PackedPage_ExposesTextureSlicesAndIndices()
        {
            Texture2D texture = CreateTexture(4, 4, "solo");
            SpritePackingSheet packingSheet =
                new SpritePackingSheet(new[] { new SpritePackEntry(texture) },
                                       SmallSettings(maxSize: 16, padding: 0, forcePowerOfTwo: false));
            PackingResult result = packingSheet.PackThisSheet();
            Assert.IsTrue(result.IsSuccess, result.Error != null ? result.Error.Message : "pack failed");
            this.Sheet = result.SpriteSheet;

            Assert.AreEqual(1, this.Sheet.PageCount);
            Assert.AreEqual(1, this.Sheet.Slices.Count);
            Assert.AreEqual(new Rect(0f, 0f, 4f, 4f), this.Sheet.Slices["solo"].Rect);
            Assert.AreSame(this.Sheet.GetPage(0), this.Sheet.GetSprite("solo").texture);
        }
    }
}

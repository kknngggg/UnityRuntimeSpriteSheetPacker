using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Tests
{
    public class SpriteSheetTests : SpriteSheetTestBase
    {
        [Test]
        public void Pack_SingleEntry_CreatesPageAndSlice()
        {
            Texture2D texture = CreateTexture(8, 8, "hero", Color.red);
            SpritePackEntry[] entries = { new SpritePackEntry(texture) };

            this.Sheet = SpriteSheet.Pack(entries, SmallSettings(maxSize: 64, padding: 0, forcePowerOfTwo: false), "testAtlas");

            Assert.AreEqual(1, this.Sheet.PageCount);
            Assert.AreEqual(1, this.Sheet.Slices.Count);

            Texture2D page = this.Sheet.GetPage(0);
            Assert.AreEqual("testAtlas_0", page.name);
            Assert.AreEqual(8, page.width);
            Assert.AreEqual(8, page.height);
            Assert.IsFalse(page.isReadable);

            Assert.IsTrue(this.Sheet.TryGetSlice("hero", out SpriteSheet.SliceInfo slice));
            Assert.AreEqual("hero", slice.Name);
            Assert.AreEqual(0, slice.Page);
            Assert.AreEqual(8f, slice.Rect.width);
            Assert.AreEqual(8f, slice.Rect.height);
            Assert.AreEqual(SpritePackEntry.DEFAULT_PIXELS_PER_UNIT, slice.PixelsPerUnit);
            Assert.AreEqual(SpritePackEntry.DEFAULT_PIVOT, slice.Pivot);
            Assert.AreEqual(SpritePackEntry.DEFAULT_MESH_TYPE, slice.MeshType);
        }

        [Test]
        public void Pack_CustomSpriteFields_CopiedIntoSliceAndSprite()
        {
            Texture2D texture = CreateTexture(8, 8, "src");
            Vector2 pivot = new Vector2(0.5f, 0f);
            SpritePackEntry[] entries = {
                new SpritePackEntry(texture, "walk_0", 50f, pivot, SpriteMeshType.FullRect),
            };

            this.Sheet = SpriteSheet.Pack(entries, SmallSettings());

            Assert.IsTrue(this.Sheet.TryGetSlice("walk_0", out SpriteSheet.SliceInfo slice));
            Assert.AreEqual(50f, slice.PixelsPerUnit);
            Assert.AreEqual(pivot, slice.Pivot);
            Assert.AreEqual(SpriteMeshType.FullRect, slice.MeshType);

            Sprite sprite = this.Sheet.GetSprite("walk_0");
            Assert.IsTrue(sprite != null);
            Assert.AreEqual("walk_0", sprite.name);
            Assert.AreEqual(50f, sprite.pixelsPerUnit);
            Assert.AreEqual(this.Sheet.GetPage(slice.Page), sprite.texture);
            Assert.AreEqual(slice.Rect, sprite.rect);
            Assert.AreEqual(new Vector2(4f, 0f), sprite.pivot);
        }

        [Test]
        public void GetSprite_CachesSameInstance()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(texture) }, SmallSettings());

            Sprite first = this.Sheet.GetSprite("icon");
            Sprite second = this.Sheet.GetSprite("icon");

            Assert.AreSame(first, second);
        }

        [Test]
        public void GetSprite_UnknownName_ReturnsNull()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(texture) }, SmallSettings());

            Assert.IsTrue(this.Sheet.GetSprite("missing") == null);
        }

        [Test]
        public void TryGetSlice_UnknownName_ReturnsFalse()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(texture) }, SmallSettings());

            bool found = this.Sheet.TryGetSlice("missing", out SpriteSheet.SliceInfo slice);
            Assert.IsFalse(found);
            Assert.AreEqual(default(SpriteSheet.SliceInfo), slice);
        }

        [Test]
        public void GetPage_OutOfRange_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(texture) }, SmallSettings());

            Assert.Throws<ArgumentOutOfRangeException>(() => this.Sheet.GetPage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => this.Sheet.GetPage(1));
        }

        [Test]
        public void ForcePowerOfTwo_RoundsPageSize()
        {
            Texture2D texture = CreateTexture(3, 3, "odd");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(texture) },
                                          SmallSettings(maxSize: 64, padding: 0, forcePowerOfTwo: true));

            Texture2D page = this.Sheet.GetPage(0);
            Assert.AreEqual(4, page.width);
            Assert.AreEqual(4, page.height);
        }

        [Test]
        public void ForcePowerOfTwo_NonPowerOfTwoMaxSize_UsesLargestPowerOfTwoEffectiveMax()
        {
            Texture2D texture = CreateTexture(3, 3, "odd");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(texture) },
                                          SmallSettings(maxSize: 5, padding: 0, forcePowerOfTwo: true));

            Texture2D page = this.Sheet.GetPage(0);
            Assert.AreEqual(4, page.width);
            Assert.AreEqual(4, page.height);
        }

        [Test]
        public void ForcePowerOfTwo_IndependentAxes()
        {
            Texture2D texture = CreateTexture(3, 5, "rect");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(texture) },
                                          SmallSettings(maxSize: 64, padding: 0, forcePowerOfTwo: true));

            Texture2D page = this.Sheet.GetPage(0);
            Assert.AreEqual(4, page.width);
            Assert.AreEqual(8, page.height);
        }

        [Test]
        public void ForcePowerOfTwo_TextureLargerThanEffectiveMaxSize_Throws()
        {
            Texture2D texture = CreateTexture(5, 5, "five");
            Assert.Throws<ArgumentException>(
                () => SpriteSheet.Pack(new[] { new SpritePackEntry(texture) },
                                       SmallSettings(maxSize: 5, padding: 0, forcePowerOfTwo: true)));
        }

        [Test]
        public void TwoTexturesThatDoNotFitOnePage_SpanPages()
        {
            Texture2D a = CreateTexture(8, 8, "a", Color.red);
            Texture2D b = CreateTexture(8, 8, "b", Color.blue);
            SpritePackEntry[] entries = {
                new SpritePackEntry(a),
                new SpritePackEntry(b),
            };

            this.Sheet = SpriteSheet.Pack(entries, SmallSettings(maxSize: 8, padding: 0, forcePowerOfTwo: false));

            Assert.AreEqual(2, this.Sheet.PageCount);
            Assert.AreEqual(2, this.Sheet.Slices.Count);

            Assert.IsTrue(this.Sheet.TryGetSlice("a", out SpriteSheet.SliceInfo sliceA));
            Assert.IsTrue(this.Sheet.TryGetSlice("b", out SpriteSheet.SliceInfo sliceB));
            Assert.AreNotEqual(sliceA.Page, sliceB.Page);
            Assert.AreEqual(this.Sheet.GetPage(sliceA.Page), this.Sheet.GetSprite("a").texture);
            Assert.AreEqual(this.Sheet.GetPage(sliceB.Page), this.Sheet.GetSprite("b").texture);
        }

        [Test]
        public void TwoTexturesOnOnePage_RectsDoNotOverlap()
        {
            Texture2D a = CreateTexture(4, 4, "a");
            Texture2D b = CreateTexture(4, 4, "b");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(a), new SpritePackEntry(b) },
                                          SmallSettings(maxSize: 64, padding: 1, forcePowerOfTwo: false));

            Assert.AreEqual(1, this.Sheet.PageCount);
            SpriteSheet.SliceInfo sliceA = this.Sheet.Slices["a"];
            SpriteSheet.SliceInfo sliceB = this.Sheet.Slices["b"];
            Assert.AreEqual(0, sliceA.Page);
            Assert.AreEqual(0, sliceB.Page);
            Assert.IsFalse(sliceA.Rect.Overlaps(sliceB.Rect));
        }

        [Test]
        public void Dispose_ThenQueriesThrow()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(texture) }, SmallSettings());
            this.Sheet.GetSprite("icon");
            this.Sheet.Dispose();

            Assert.Throws<ObjectDisposedException>(() => this.Sheet.GetSprite("icon"));
            Assert.Throws<ObjectDisposedException>(() => this.Sheet.GetPage(0));
            Assert.Throws<ObjectDisposedException>(() => this.Sheet.TryGetSlice("icon", out _));
            Assert.AreEqual(0, this.Sheet.PageCount);
            Assert.AreEqual(0, this.Sheet.Slices.Count);
        }

        [Test]
        public void Dispose_Twice_DoesNotThrow()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            this.Sheet = SpriteSheet.Pack(new[] { new SpritePackEntry(texture) }, SmallSettings());

            this.Sheet.Dispose();
            Assert.DoesNotThrow(() => this.Sheet.Dispose());
        }

        [Test]
        public void Pack_NullEntries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SpriteSheet.Pack(null, SmallSettings()));
        }

        [Test]
        public void Pack_EmptyEntries_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => SpriteSheet.Pack(new SpritePackEntry[0], SmallSettings()));
        }

        [Test]
        public void Pack_NullTexture_Throws()
        {
            SpritePackEntry[] entries = { new SpritePackEntry(null, "ghost") };
            Assert.Throws<ArgumentException>(() => SpriteSheet.Pack(entries, SmallSettings()));
        }

        [Test]
        public void Pack_EmptyName_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "file");
            SpritePackEntry[] entries = { new SpritePackEntry(texture, "") };
            Assert.Throws<ArgumentException>(() => SpriteSheet.Pack(entries, SmallSettings()));
        }

        [Test]
        public void Pack_DuplicateName_Throws()
        {
            Texture2D a = CreateTexture(4, 4, "a");
            Texture2D b = CreateTexture(4, 4, "b");
            SpritePackEntry[] entries = {
                new SpritePackEntry(a, "same"),
                new SpritePackEntry(b, "same"),
            };
            Assert.Throws<ArgumentException>(() => SpriteSheet.Pack(entries, SmallSettings()));
        }

        [Test]
        public void Pack_NonReadableTexture_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "locked");
            texture.Apply(false, true);
            SpritePackEntry[] entries = { new SpritePackEntry(texture) };
            Assert.Throws<ArgumentException>(() => SpriteSheet.Pack(entries, SmallSettings()));
        }

        [Test]
        public void Pack_PixelsPerUnitNotPositive_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "ppu");
            SpritePackEntry[] entries = {
                new SpritePackEntry(texture, "ppu", 0f, SpritePackEntry.DEFAULT_PIVOT, SpriteMeshType.FullRect),
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => SpriteSheet.Pack(entries, SmallSettings()));
        }

        [Test]
        public void Pack_NegativePixelsPerUnit_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "ppu");
            SpritePackEntry[] entries = {
                new SpritePackEntry(texture, "ppu", -1f, SpritePackEntry.DEFAULT_PIVOT, SpriteMeshType.FullRect),
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => SpriteSheet.Pack(entries, SmallSettings()));
        }

        [Test]
        public void Pack_TextureLargerThanMaxSize_Throws()
        {
            Texture2D texture = CreateTexture(16, 8, "big");
            SpritePackEntry[] entries = { new SpritePackEntry(texture) };
            Assert.Throws<ArgumentException>(
                () => SpriteSheet.Pack(entries, SmallSettings(maxSize: 8, padding: 0)));
        }

        [Test]
        public void Pack_NegativePadding_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "pad");
            SpriteSheet.PackingSettings settings = SmallSettings();
            settings.Padding = -1;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpriteSheet.Pack(new[] { new SpritePackEntry(texture) }, settings));
        }

        [Test]
        public void Pack_InvalidMaxSize_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "max");
            SpriteSheet.PackingSettings settings = SmallSettings();
            settings.MaxSize = 0;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpriteSheet.Pack(new[] { new SpritePackEntry(texture) }, settings));
        }

        [Test]
        public void AddPage_DuplicateSliceName_Throws()
        {
            Texture2D page = CreateTexture(4, 4, "page");
            SpriteSheet.SliceInfo slice = new SpriteSheet.SliceInfo("dup",
                                                                    new Rect(0f, 0f, 4f, 4f),
                                                                    0,
                                                                    100f,
                                                                    SpritePackEntry.DEFAULT_PIVOT,
                                                                    SpriteMeshType.FullRect);
            List<SpriteSheet.SliceInfo> slices = new List<SpriteSheet.SliceInfo> { slice };

            this.Sheet = new SpriteSheet();
            this.Sheet.AddPage(page, slices);

            Assert.Throws<ArgumentException>(() => this.Sheet.AddPage(page, slices));
        }
    }
}

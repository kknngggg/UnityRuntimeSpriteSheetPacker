using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Tests
{
    public class SpriteSheetSerializationTests : SpriteSheetTestBase
    {
        [Test]
        public void ToBytes_ThenLoad_RestoresPagesAndSlices()
        {
            Texture2D texture = CreateTexture(8, 8, "hero", Color.red);
            PackSuccessfully(new[] { new SpritePackEntry(texture, "hero") },
                            SmallSettings(maxSize: 64, padding: 0, forcePowerOfTwo: false),
                            "testAtlas");

            SpriteSheet loaded = SpriteSheet.Load(this.Sheet.ToBytes());
            this.Sheet.Dispose();
            this.Sheet = loaded;

            Assert.AreEqual(1, loaded.PageCount);
            Assert.AreEqual("testAtlas_0", loaded.GetPage(0).name);
            Assert.AreEqual(8, loaded.GetPage(0).width);
            Assert.AreEqual(8, loaded.GetPage(0).height);
            Assert.IsFalse(loaded.GetPage(0).isReadable);

            Assert.IsTrue(loaded.TryGetSlice("hero", out SpriteSheet.SliceInfo slice));
            Assert.AreEqual("hero", slice.Name);
            Assert.AreEqual(0, slice.Page);
            Assert.AreEqual(new Rect(0f, 0f, 8f, 8f), slice.Rect);
            Assert.AreEqual(SpritePackEntry.DEFAULT_PIXELS_PER_UNIT, slice.PixelsPerUnit);
            Assert.AreEqual(SpritePackEntry.DEFAULT_PIVOT, slice.Pivot);
            Assert.AreEqual(SpritePackEntry.DEFAULT_MESH_TYPE, slice.MeshType);

            Sprite sprite = loaded.GetSprite("hero");
            Assert.IsTrue(sprite != null);
            Assert.AreEqual(loaded.GetPage(0), sprite.texture);
        }

        [Test]
        public void ToBytes_ThenLoad_RestoresCustomSpriteFields()
        {
            Texture2D texture = CreateTexture(8, 8, "src");
            Vector2 pivot = new Vector2(0.5f, 0f);
            PackSuccessfully(
                new[] { new SpritePackEntry(texture, "walk_0", 50f, pivot, SpriteMeshType.FullRect) },
                SmallSettings());

            SpriteSheet loaded = SpriteSheet.Load(this.Sheet.ToBytes());
            this.Sheet.Dispose();
            this.Sheet = loaded;

            Assert.IsTrue(loaded.TryGetSlice("walk_0", out SpriteSheet.SliceInfo slice));
            Assert.AreEqual(50f, slice.PixelsPerUnit);
            Assert.AreEqual(pivot, slice.Pivot);
            Assert.AreEqual(SpriteMeshType.FullRect, slice.MeshType);

            Sprite sprite = loaded.GetSprite("walk_0");
            Assert.AreEqual(50f, sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(4f, 0f), sprite.pivot);
        }

        [Test]
        public void ToBytes_ThenLoad_RestoresUnicodeSliceName()
        {
            Texture2D texture = CreateTexture(4, 4, "file");
            PackSuccessfully(new[] { new SpritePackEntry(texture, "café_ヒーロー") },
                            SmallSettings(maxSize: 16, padding: 0, forcePowerOfTwo: false));

            SpriteSheet loaded = SpriteSheet.Load(this.Sheet.ToBytes());
            this.Sheet.Dispose();
            this.Sheet = loaded;

            Assert.IsTrue(loaded.TryGetSlice("café_ヒーロー", out _));
            Assert.IsTrue(loaded.GetSprite("café_ヒーロー") != null);
        }

        [Test]
        public void ToBytes_ThenLoad_RestoresMultiplePages()
        {
            Texture2D a = CreateTexture(8, 8, "a", Color.red);
            Texture2D b = CreateTexture(8, 8, "b", Color.blue);
            PackSuccessfully(new[] { new SpritePackEntry(a), new SpritePackEntry(b) },
                            SmallSettings(maxSize: 8, padding: 0, forcePowerOfTwo: false));

            SpriteSheet loaded = SpriteSheet.Load(this.Sheet.ToBytes());
            this.Sheet.Dispose();
            this.Sheet = loaded;

            Assert.AreEqual(2, loaded.PageCount);
            Assert.IsTrue(loaded.TryGetSlice("a", out SpriteSheet.SliceInfo sliceA));
            Assert.IsTrue(loaded.TryGetSlice("b", out SpriteSheet.SliceInfo sliceB));
            Assert.AreNotEqual(sliceA.Page, sliceB.Page);
            Assert.AreEqual(loaded.GetPage(sliceA.Page), loaded.GetSprite("a").texture);
            Assert.AreEqual(loaded.GetPage(sliceB.Page), loaded.GetSprite("b").texture);
        }

        [Test]
        public void Load_AddPageSheet_RestoresTightSlice()
        {
            Texture2D page = CreateTexture(4, 4, "page", Color.red);
            SpriteSheet.SliceInfo slice = new SpriteSheet.SliceInfo("dot",
                                                                    new Rect(0f, 0f, 4f, 4f),
                                                                    0,
                                                                    100f,
                                                                    SpritePackEntry.DEFAULT_PIVOT,
                                                                    SpriteMeshType.Tight);

            this.Sheet = new SpriteSheet();
            this.Sheet.AddPage(page, new List<SpriteSheet.SliceInfo> { slice });

            SpriteSheet loaded = SpriteSheet.Load(this.Sheet.ToBytes());
            this.Sheet.Dispose();
            this.Sheet = loaded;

            Assert.IsTrue(loaded.TryGetSlice("dot", out SpriteSheet.SliceInfo loadedSlice));
            Assert.AreEqual(SpriteMeshType.Tight, loadedSlice.MeshType);
            Assert.AreEqual(4, loaded.GetPage(0).width);
            Assert.AreEqual(4, loaded.GetPage(0).height);
        }

        [Test]
        public void ToBytes_Disposed_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            PackSuccessfully(new[] { new SpritePackEntry(texture) }, SmallSettings());
            this.Sheet.Dispose();

            Assert.Throws<ObjectDisposedException>(() => this.Sheet.ToBytes());
        }

        [Test]
        public void Load_NullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SpriteSheet.Load(null));
        }

        [Test]
        public void Load_EmptyData_Throws()
        {
            Assert.Throws<InvalidDataException>(() => SpriteSheet.Load(Array.Empty<byte>()));
        }

        [Test]
        public void Load_BadMagic_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            PackSuccessfully(new[] { new SpritePackEntry(texture) },
                            SmallSettings(maxSize: 16, padding: 0, forcePowerOfTwo: false));

            byte[] data = this.Sheet.ToBytes();
            data[0] = (byte)'X';

            Assert.Throws<InvalidDataException>(() => SpriteSheet.Load(data));
        }

        [Test]
        public void Load_UnsupportedVersion_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            PackSuccessfully(new[] { new SpritePackEntry(texture) },
                            SmallSettings(maxSize: 16, padding: 0, forcePowerOfTwo: false));

            byte[] data = this.Sheet.ToBytes();
            data[4] = 99;

            Assert.Throws<InvalidDataException>(() => SpriteSheet.Load(data));
        }

        [Test]
        public void Load_TruncatedData_Throws()
        {
            Texture2D texture = CreateTexture(4, 4, "icon");
            PackSuccessfully(new[] { new SpritePackEntry(texture) },
                            SmallSettings(maxSize: 16, padding: 0, forcePowerOfTwo: false));

            byte[] data = this.Sheet.ToBytes();
            byte[] truncated = new byte[data.Length - 1];
            Array.Copy(data, truncated, truncated.Length);

            Assert.Throws<InvalidDataException>(() => SpriteSheet.Load(truncated));
        }
    }
}

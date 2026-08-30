using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace kknngggg.Unity.Sprites.Tests
{
    public abstract class SpriteSheetTestBase
    {
        private readonly List<Texture2D> _ownedTextures = new List<Texture2D>();
        protected SpriteSheet Sheet;

        [TearDown]
        public void TearDown()
        {
            if (this.Sheet != null)
            {
                this.Sheet.Dispose();
                this.Sheet = null;
            }

            foreach (Texture2D texture in this._ownedTextures)
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }
            }

            this._ownedTextures.Clear();
        }

        protected Texture2D CreateTexture(int width, int height, string name)
        {
            return CreateTexture(width, height, name, Color.white);
        }

        protected Texture2D CreateTexture(int width, int height, string name, Color color)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
            };

            Color32[] pixels = new Color32[width * height];
            Color32 color32 = color;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color32;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            this._ownedTextures.Add(texture);
            return texture;
        }

        protected static SpriteSheet.PackingSettings SmallSettings(int maxSize = 64,
                                                                   int padding = 1,
                                                                   bool forcePowerOfTwo = true)
        {
            return new SpriteSheet.PackingSettings {
                Padding = padding,
                MaxSize = maxSize,
                ForcePowerOfTwo = forcePowerOfTwo,
            };
        }

        protected SpriteSheet PackSuccessfully(IEnumerable<SpritePackEntry> entries,
                                            SpriteSheet.PackingSettings settings,
                                            string texturePageName = null)
        {
            PackingResult result = SpriteSheet.Pack(entries, settings, texturePageName);
            Assert.IsTrue(result.IsSuccess, result.Error != null ? result.Error.Message : "pack failed");
            this.Sheet = result.SpriteSheet;
            return this.Sheet;
        }

        protected static void AssertPackFailed(PackingResult result, int expectedCode)
        {
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(expectedCode, result.Error.Code);
            Assert.IsNull(result.SpriteSheet);
        }
    }
}

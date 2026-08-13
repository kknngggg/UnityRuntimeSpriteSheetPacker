using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace kknngggg.Unity.Sprites
{
    /// <summary>
    ///     Runtime packed sprite atlas. API shaped like Unity SpriteAtlas: pack <see cref="SpritePackEntry" /> list, fetch sprites by name.
    ///     May span multiple pages when content exceeds <see cref="PackingSettings.MaxSize" />.
    /// </summary>
    public partial class SpriteSheet : IDisposable
    {
        private readonly List<Texture2D> _pages = new List<Texture2D>();
        private readonly Dictionary<string, SliceInfo> _slices = new Dictionary<string, SliceInfo>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        private bool _disposed;

        public int PageCount => this._pages.Count;

        public IReadOnlyDictionary<string, SliceInfo> Slices {
            get {
                return this._slices;
            }
        }

        public static SpriteSheet Pack(IEnumerable<SpritePackEntry> entries, PackingSettings settings, string texturePageName = null)
        {
            SpritePackingSheet packingSheet = new SpritePackingSheet(entries, settings, texturePageName);
            return packingSheet.PackThisSheet();
        }

        public Texture2D GetPage(int pageIndex)
        {
            ThrowIfDisposed();

            if (pageIndex < 0 || pageIndex >= this._pages.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex));
            }

            return this._pages[pageIndex];
        }

        public bool TryGetSlice(string spriteName, out SliceInfo sliceInfo)
        {
            ThrowIfDisposed();
            return this._slices.TryGetValue(spriteName, out sliceInfo);
        }

        public Sprite GetSprite(string spriteName)
        {
            ThrowIfDisposed();

            if (this._sprites.TryGetValue(spriteName, out Sprite cached))
            {
                return cached;
            }

            if (this._slices.TryGetValue(spriteName, out SliceInfo sliceInfo) == false)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(this._pages[sliceInfo.Page],
                                          sliceInfo.Rect,
                                          sliceInfo.Pivot,
                                          sliceInfo.PixelsPerUnit,
                                          0,
                                          sliceInfo.MeshType);

            sprite.name = spriteName;
            this._sprites[spriteName] = sprite;

            return sprite;
        }

        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;

            foreach (KeyValuePair<string, Sprite> pair in this._sprites)
            {
                if (pair.Value != null)
                {
                    DestroyUnityObject(pair.Value);
                }
            }

            this._sprites.Clear();
            this._slices.Clear();

            foreach (Texture2D texture in this._pages)
            {
                if (texture != null)
                {
                    DestroyUnityObject(texture);
                }
            }

            this._pages.Clear();
        }

        private static void DestroyUnityObject(Object obj)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                Object.DestroyImmediate(obj);
            }
            else
            {
                Object.Destroy(obj);
            }
#else
            Object.Destroy(obj);
#endif
        }

        private void ThrowIfDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteSheet));
            }
        }

        internal void AddPage(Texture2D pageTexture, IList<SliceInfo> pageSlices)
        {
            this._pages.Add(pageTexture);

            foreach (SliceInfo slice in pageSlices)
            {
                if (this._slices.ContainsKey(slice.Name))
                {
                    throw new ArgumentException("Duplicate sprite name in atlas: " + slice.Name);
                }

                this._slices.Add(slice.Name, slice);
            }
        }
    }
}

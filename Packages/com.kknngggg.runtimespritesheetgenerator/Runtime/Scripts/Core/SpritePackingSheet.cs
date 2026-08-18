using System;
using System.Collections.Generic;
using DaVikingCode.RectanglePacking;
using UnityEngine;

namespace kknngggg.Unity.Sprites
{
    internal sealed partial class SpritePackingSheet
    {
        private readonly string _texturePageName;
        private readonly List<SpritePackEntry> _entries = new List<SpritePackEntry>();
        private readonly SpriteSheet.PackingSettings _settings;

        public SpritePackingSheet(IEnumerable<SpritePackEntry> entries, SpriteSheet.PackingSettings settings, string texturePageName = null)
        {
            this._texturePageName = texturePageName ?? Guid.NewGuid().ToString("N");

            this._settings = settings;

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            foreach (SpritePackEntry entry in entries)
            {
                if (entry.Texture == null)
                {
                    throw new ArgumentException("entries contains null texture");
                }

                this._entries.Add(entry);
            }
        }

        public SpriteSheet PackThisSheet()
        {
            this._settings.Validate();

            if (this._entries.Count == 0)
            {
                throw new InvalidOperationException("No textures to pack");
            }

            ValidateEntries();

            SpriteSheet sheet = new SpriteSheet();
            List<SpritePackEntry> remaining = new List<SpritePackEntry>(this._entries);

            while (remaining.Count > 0)
            {
                PackedPage page = PackPage(remaining, sheet.PageCount);
                sheet.AddPage(page.Texture, page.Slices);

                foreach (int packedIndex in page.PackedIndices)
                {
                    remaining[packedIndex] = default;
                }

                remaining.RemoveAll(IsClearedEntry);

                if (page.PackedIndices.Count == 0)
                {
                    throw new InvalidOperationException("Failed to pack any remaining textures into an atlas page");
                }
            }

            return sheet;
        }

        private void ValidateEntries()
        {
            HashSet<string> names = new HashSet<string>();

            for (int i = 0; i < this._entries.Count; i++)
            {
                SpritePackEntry entry = this._entries[i];
                Texture2D texture = entry.Texture;

                if (string.IsNullOrEmpty(entry.Name))
                {
                    throw new ArgumentException("Texture at index " + i + " has empty name");
                }

                if (names.Add(entry.Name) == false)
                {
                    throw new ArgumentException("Duplicate sprite name: " + entry.Name);
                }

                if (texture.isReadable == false)
                {
                    throw new ArgumentException($"Texture '{entry.Name}' is not readable. Enable Read/Write in import settings.");
                }

                if (entry.PixelsPerUnit <= 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(SpritePackEntry.PixelsPerUnit),
                                                          "PixelsPerUnit must be > 0 for '" + entry.Name + "'");
                }

                if (texture.width > this._settings.MaxSize || texture.height > this._settings.MaxSize)
                {
                    throw new ArgumentException($"Texture '{entry.Name}' ({texture.width}x{texture.height}) exceeds maxSize {this._settings.MaxSize}");
                }
            }
        }

        private PackedPage PackPage(List<SpritePackEntry> remaining, int pageIndex)
        {
            int maxSize = this._settings.MaxSize;
            int padding = this._settings.Padding;

            RectanglePacker packer = new RectanglePacker(maxSize, maxSize, padding);

            for (int i = 0; i < remaining.Count; i++)
            {
                packer.insertRectangle(remaining[i].Texture.width, remaining[i].Texture.height, i);
            }

            packer.packRectangles();

            if (packer.rectangleCount == 0)
            {
                throw new InvalidOperationException("RectanglePacker placed zero rectangles");
            }

            int atlasWidth = packer.packedWidth;
            int atlasHeight = packer.packedHeight;

            if (this._settings.ForcePowerOfTwo)
            {
                atlasWidth = Mathf.NextPowerOfTwo(Mathf.Max(1, atlasWidth));
                atlasHeight = Mathf.NextPowerOfTwo(Mathf.Max(1, atlasHeight));
            }

            atlasWidth = Mathf.Clamp(atlasWidth, 1, maxSize);
            atlasHeight = Mathf.Clamp(atlasHeight, 1, maxSize);

            Texture2D atlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false);
            Color32[] clear = new Color32[atlasWidth * atlasHeight];
            atlas.SetPixels32(clear);

            List<SpriteSheet.SliceInfo> slices = new List<SpriteSheet.SliceInfo>(packer.rectangleCount);
            List<int> packedIndices = new List<int>(packer.rectangleCount);
            IntegerRectangle rect = new IntegerRectangle();

            for (int j = 0; j < packer.rectangleCount; j++)
            {
                rect = packer.getRectangle(j, rect);
                int index = packer.getRectangleId(j);
                SpritePackEntry entry = remaining[index];

                atlas.SetPixels32(rect.x, rect.y, rect.width, rect.height, entry.Texture.GetPixels32());

                slices.Add(new SpriteSheet.SliceInfo(
                               entry.Name,
                               new Rect(rect.x, rect.y, rect.width, rect.height),
                               pageIndex,
                               entry.PixelsPerUnit,
                               entry.Pivot,
                               entry.MeshType));

                packedIndices.Add(index);
            }

            atlas.Apply(false, true);
            atlas.name = $"{this._texturePageName}_{pageIndex}";

            return new PackedPage(atlas, slices, packedIndices);
        }

        private static bool IsClearedEntry(SpritePackEntry entry)
        {
            return entry.Texture == null;
        }
    }
}

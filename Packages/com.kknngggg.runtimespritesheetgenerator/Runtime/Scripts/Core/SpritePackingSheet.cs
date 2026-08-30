using System;
using System.Collections.Generic;
using DaVikingCode.RectanglePacking;
using kknngggg.Unity.Sprites.Errors;
using UnityEngine;

namespace kknngggg.Unity.Sprites
{
    internal sealed partial class SpritePackingSheet
    {
        private readonly string _texturePageName;
        private readonly List<SpritePackEntry> _entries;
        private readonly SpriteSheet.PackingSettings _settings;

        public SpritePackingSheet(IEnumerable<SpritePackEntry> entries, SpriteSheet.PackingSettings settings, string texturePageName = null)
        {
            this._texturePageName = texturePageName ?? Guid.NewGuid().ToString("N");
            this._settings = settings;

            if (entries != null)
            {
                this._entries = new List<SpritePackEntry>(entries);
            }
        }

        public PackingResult PackThisSheet()
        {
            if (this._entries == null)
            {
                return Fail(PackingErrorCodes.NULL_ENTRIES, "entries is null");
            }

            PackingError settingsError = this._settings.Validate();

            if (settingsError != PackingError.None)
            {
                return PackingResult.Failed(settingsError);
            }

            if (this._entries.Count == 0)
            {
                return Fail(PackingErrorCodes.EMPTY_ENTRIES, "No textures to pack");
            }

            PackingError entriesError = ValidateEntries();

            if (entriesError != PackingError.None)
            {
                return PackingResult.Failed(entriesError);
            }

            SpriteSheet sheet = new SpriteSheet();
            List<SpritePackEntry> remaining = new List<SpritePackEntry>(this._entries);

            while (remaining.Count > 0)
            {
                PackingError pageError = TryPackPage(remaining, sheet.PageCount, out PackedPage page);

                if (pageError != PackingError.None)
                {
                    sheet.Dispose();
                    return PackingResult.Failed(pageError);
                }

                sheet.AddPage(page.Texture, page.Slices);

                foreach (int packedIndex in page.PackedIndices)
                {
                    remaining[packedIndex] = default;
                }

                remaining.RemoveAll(IsClearedEntry);

                if (page.PackedIndices.Count > 0)
                {
                    continue;
                }

                sheet.Dispose();
                return Fail(PackingErrorCodes.PACK_FAILED, "Failed to pack any remaining textures into an atlas page");
            }

            return PackingResult.Success(sheet);
        }

        private PackingError ValidateEntries()
        {
            HashSet<string> names = new HashSet<string>();

            for (int i = 0; i < this._entries.Count; i++)
            {
                SpritePackEntry entry = this._entries[i];

                if (IsEntryValid(entry, i, names, out PackingError entryError) == false)
                {
                    return entryError;
                }
            }

            return PackingError.None;
        }

        private bool IsEntryValid(SpritePackEntry entry,
                                   int entryIndex,
                                   HashSet<string> names,
                                   out PackingError entryError)
        {
            entryError = PackingError.None;
            Texture2D texture = entry.Texture;

            if (texture == null)
            {
                entryError = new PackingError(PackingErrorCodes.NULL_TEXTURE,
                                              $"Entries contains null texture at index {entryIndex}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                entryError = new PackingError(PackingErrorCodes.EMPTY_NAME,
                                              $"Entry at index {entryIndex} has empty name");
                return false;
            }

            if (names.Add(entry.Name) == false)
            {
                entryError = new PackingError(PackingErrorCodes.DUPLICATE_NAME,
                                              $"Duplicate Entry Name: {entry.Name} at index {entryIndex}");
                return false;
            }

            if (texture.isReadable == false)
            {
                entryError = new PackingError(PackingErrorCodes.TEXTURE_NOT_READABLE,
                                              $"Texture of Entry at index '{entryIndex}' is not readable. Enable Read/Write in import settings.");
                return false;
            }

            if (entry.PixelsPerUnit <= 0f)
            {
                entryError = new PackingError(PackingErrorCodes.INVALID_PIXELS_PER_UNIT,
                                              $"PixelsPerUnit must be > 0 for '{entry.Name}' at index {entryIndex}");
                return false;
            }

            int effectiveMaxSize = this._settings.EffectiveMaxSize;

            if (texture.width > effectiveMaxSize || texture.height > effectiveMaxSize)
            {
                entryError = new PackingError(PackingErrorCodes.TEXTURE_EXCEEDS_MAX_SIZE,
                                              $"Texture '{entry.Name}' at index {entryIndex} ({texture.width}x{texture.height}) exceeds effective max size {effectiveMaxSize}");
                return false;
            }

            return true;
        }

        private PackingError TryPackPage(List<SpritePackEntry> remaining,
                                         int pageIndex,
                                         out PackedPage page)
        {
            page = default;

            int effectiveMaxSize = this._settings.EffectiveMaxSize;
            int padding = this._settings.Padding;

            RectanglePacker packer = new RectanglePacker(effectiveMaxSize, effectiveMaxSize, padding);

            for (int i = 0; i < remaining.Count; i++)
            {
                packer.insertRectangle(remaining[i].Texture.width, remaining[i].Texture.height, i);
            }

            packer.packRectangles();

            if (packer.rectangleCount == 0)
            {
                return new PackingError(PackingErrorCodes.PACK_FAILED,
                                        "RectanglePacker placed zero rectangles");
            }

            int atlasWidth = packer.packedWidth;
            int atlasHeight = packer.packedHeight;

            if (this._settings.ForcePowerOfTwo)
            {
                atlasWidth = Mathf.NextPowerOfTwo(Mathf.Max(1, atlasWidth));
                atlasHeight = Mathf.NextPowerOfTwo(Mathf.Max(1, atlasHeight));
            }

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

            page = new PackedPage(atlas, slices, packedIndices);
            return PackingError.None;
        }

        private static PackingResult Fail(int code, string message)
        {
            return PackingResult.Failed(new PackingError(code, message));
        }

        private static bool IsClearedEntry(SpritePackEntry entry)
        {
            return entry.Texture == null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace kknngggg.Unity.Sprites
{
    internal static class SpriteSheetBinary
    {
        private const string MAGIC = "SPSH";
        private const uint VERSION = 1;
        private const string BLOCK_HEAD = "HEAD";
        private const string BLOCK_PAGE = "PAGE";
        private const string BLOCK_SLICE = "SLCE";
        private const int FOUR_CC_SIZE = 4;
        private const int BLOCK_HEADER_SIZE = 8;

        public static byte[] Serialize(SpriteSheet sheet)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true);

            WriteFourCC(writer, MAGIC);
            writer.Write(VERSION);

            WriteBlock(writer, BLOCK_HEAD, WriteHead(sheet.PageCount, sheet.Slices.Count));

            for (int i = 0; i < sheet.PageCount; i++)
            {
                WriteBlock(writer, BLOCK_PAGE, WritePage(sheet.GetPage(i)));
            }

            foreach (KeyValuePair<string, SpriteSheet.SliceInfo> pair in sheet.Slices)
            {
                WriteBlock(writer, BLOCK_SLICE, WriteSlice(pair.Value));
            }

            writer.Flush();
            return stream.ToArray();
        }

        public static SpriteSheet Deserialize(byte[] data)
        {
            if (data.Length < FOUR_CC_SIZE + sizeof(uint))
            {
                throw new InvalidDataException("Truncated .spritesheet file.");
            }

            using MemoryStream stream = new MemoryStream(data, 0, data.Length, false, true);
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true);

            string magic = ReadFourCC(reader);
            if (magic != MAGIC)
            {
                throw new InvalidDataException("Not a .spritesheet file.");
            }

            uint version = reader.ReadUInt32();
            if (version != VERSION)
            {
                throw new InvalidDataException($"Unsupported .spritesheet version {version}.");
            }

            int pageCount = -1;
            int sliceCount = -1;
            bool seenHead = false;
            List<Texture2D> pages = new List<Texture2D>();
            List<SpriteSheet.SliceInfo> slices = new List<SpriteSheet.SliceInfo>();

            try
            {
                while (stream.Position < stream.Length)
                {
                    long remaining = stream.Length - stream.Position;
                    if (remaining == 0)
                    {
                        break;
                    }

                    if (remaining < BLOCK_HEADER_SIZE)
                    {
                        throw new InvalidDataException("Truncated .spritesheet block header.");
                    }

                    string blockType = ReadFourCC(reader);
                    uint payloadSize = reader.ReadUInt32();

                    if (payloadSize > int.MaxValue)
                    {
                        throw new InvalidDataException($"Block '{blockType}' is too large.");
                    }

                    int size = (int)payloadSize;
                    if (stream.Length - stream.Position < size)
                    {
                        throw new InvalidDataException($"Truncated '{blockType}' block.");
                    }

                    byte[] payload = reader.ReadBytes(size);
                    if (payload.Length != size)
                    {
                        throw new InvalidDataException($"Truncated '{blockType}' block.");
                    }

                    SkipPadding(reader, size);

                    if (seenHead == false)
                    {
                        if (blockType != BLOCK_HEAD)
                        {
                            throw new InvalidDataException("First .spritesheet block must be HEAD.");
                        }

                        ReadHead(payload, out pageCount, out sliceCount);
                        seenHead = true;
                        continue;
                    }

                    switch (blockType)
                    {
                        case BLOCK_HEAD:
                            throw new InvalidDataException("Duplicate HEAD block.");
                        case BLOCK_PAGE:
                            pages.Add(ReadPage(payload));
                            break;
                        case BLOCK_SLICE:
                            slices.Add(ReadSlice(payload));
                            break;
                    }
                }

                if (seenHead == false)
                {
                    throw new InvalidDataException("Missing HEAD block.");
                }

                if (pages.Count != pageCount)
                {
                    throw new InvalidDataException($"HEAD pageCount {pageCount} does not match {pages.Count} PAGE blocks.");
                }

                if (slices.Count != sliceCount)
                {
                    throw new InvalidDataException($"HEAD sliceCount {sliceCount} does not match {slices.Count} SLCE blocks.");
                }

                return BuildSheet(pages, slices);
            }
            catch (EndOfStreamException exception)
            {
                DestroyPages(pages);
                throw new InvalidDataException("Truncated .spritesheet block.", exception);
            }
            catch
            {
                DestroyPages(pages);
                throw;
            }
        }

        private static void DestroyPages(List<Texture2D> pages)
        {
            foreach (Texture2D texture in pages)
            {
                SpriteSheet.DestroyUnityObject(texture);
            }
        }

        private static SpriteSheet BuildSheet(List<Texture2D> pages, List<SpriteSheet.SliceInfo> slices)
        {
            List<SpriteSheet.SliceInfo>[] slicesByPage = new List<SpriteSheet.SliceInfo>[pages.Count];
            for (int i = 0; i < slicesByPage.Length; i++)
            {
                slicesByPage[i] = new List<SpriteSheet.SliceInfo>();
            }

            foreach (SpriteSheet.SliceInfo slice in slices)
            {
                if (slice.Page < 0 || slice.Page >= pages.Count)
                {
                    throw new InvalidDataException($"Slice '{slice.Name}' page {slice.Page} is out of range.");
                }

                slicesByPage[slice.Page].Add(slice);
            }

            SpriteSheet sheet = new SpriteSheet();
            try
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    sheet.AddPage(pages[i], slicesByPage[i]);
                }

                return sheet;
            }
            catch
            {
                sheet.Dispose();
                throw;
            }
        }

        private static byte[] WriteHead(int pageCount, int sliceCount)
        {
            using MemoryStream stream = new MemoryStream(8);
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(pageCount);
            writer.Write(sliceCount);
            writer.Flush();
            return stream.ToArray();
        }

        private static void ReadHead(byte[] payload, out int pageCount, out int sliceCount)
        {
            using BinaryReader reader = OpenPayload(payload);
            if (payload.Length < sizeof(int) * 2)
            {
                throw new InvalidDataException("Truncated HEAD block.");
            }

            pageCount = reader.ReadInt32();
            sliceCount = reader.ReadInt32();

            if (pageCount < 0)
            {
                throw new InvalidDataException("HEAD pageCount is negative.");
            }

            if (sliceCount < 0)
            {
                throw new InvalidDataException("HEAD sliceCount is negative.");
            }
        }

        private static byte[] WritePage(Texture2D page)
        {
            byte[] png = EncodePageToPng(page);

            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true);
            WriteUtf8(writer, page.name);
            writer.Write(page.width);
            writer.Write(page.height);
            writer.Write(png.Length);
            writer.Write(png);
            writer.Flush();
            return stream.ToArray();
        }

        private static Texture2D ReadPage(byte[] payload)
        {
            using BinaryReader reader = OpenPayload(payload);
            string name = ReadUtf8(reader);
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int pngLength = reader.ReadInt32();

            if (width < 1 || height < 1)
            {
                throw new InvalidDataException($"PAGE '{name}' has invalid size {width}x{height}.");
            }

            if (pngLength < 1)
            {
                throw new InvalidDataException($"PAGE '{name}' has empty PNG data.");
            }

            byte[] png = reader.ReadBytes(pngLength);
            if (png.Length != pngLength)
            {
                throw new InvalidDataException($"Truncated PNG in PAGE '{name}'.");
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            try
            {
                if (texture.LoadImage(png, true) == false)
                {
                    throw new InvalidDataException($"Failed to decode PNG in PAGE '{name}'.");
                }

                if (texture.width != width || texture.height != height)
                {
                    throw new InvalidDataException(
                        $"PAGE '{name}' size {texture.width}x{texture.height} does not match {width}x{height}.");
                }

                texture.name = name;
                return texture;
            }
            catch
            {
                SpriteSheet.DestroyUnityObject(texture);
                throw;
            }
        }

        private static byte[] WriteSlice(SpriteSheet.SliceInfo slice)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true);
            WriteUtf8(writer, slice.Name ?? string.Empty);
            writer.Write(slice.Rect.x);
            writer.Write(slice.Rect.y);
            writer.Write(slice.Rect.width);
            writer.Write(slice.Rect.height);
            writer.Write(slice.Page);
            writer.Write(slice.PixelsPerUnit);
            writer.Write(slice.Pivot.x);
            writer.Write(slice.Pivot.y);
            writer.Write((int)slice.MeshType);
            writer.Flush();
            return stream.ToArray();
        }

        private static SpriteSheet.SliceInfo ReadSlice(byte[] payload)
        {
            using BinaryReader reader = OpenPayload(payload);
            string name = ReadUtf8(reader);
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidDataException("SLCE name is empty.");
            }

            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float width = reader.ReadSingle();
            float height = reader.ReadSingle();
            int page = reader.ReadInt32();
            float pixelsPerUnit = reader.ReadSingle();
            float pivotX = reader.ReadSingle();
            float pivotY = reader.ReadSingle();
            int meshTypeValue = reader.ReadInt32();

            if (Enum.IsDefined(typeof(SpriteMeshType), meshTypeValue) == false)
            {
                throw new InvalidDataException($"SLCE '{name}' has unknown mesh type {meshTypeValue}.");
            }

            return new SpriteSheet.SliceInfo(name,
                                             new Rect(x, y, width, height),
                                             page,
                                             pixelsPerUnit,
                                             new Vector2(pivotX, pivotY),
                                             (SpriteMeshType)meshTypeValue);
        }

        private static byte[] EncodePageToPng(Texture2D source)
        {
            if (source.isReadable)
            {
                return RequirePng(source.EncodeToPNG(), source.name);
            }

            RenderTexture renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, renderTexture);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
            try
            {
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply(false, false);
                return RequirePng(readable.EncodeToPNG(), source.name);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
                SpriteSheet.DestroyUnityObject(readable);
            }
        }

        private static byte[] RequirePng(byte[] png, string pageName)
        {
            if (png is not { Length: > 0 })
            {
                throw new InvalidOperationException($"Failed to encode page '{pageName}' to PNG.");
            }

            return png;
        }

        private static void WriteBlock(BinaryWriter writer, string fourCC, byte[] payload)
        {
            WriteFourCC(writer, fourCC);
            writer.Write((uint)payload.Length);
            writer.Write(payload);

            int pad = PaddingLength(payload.Length);
            for (int i = 0; i < pad; i++)
            {
                writer.Write((byte)0);
            }
        }

        private static void SkipPadding(BinaryReader reader, int payloadSize)
        {
            int pad = PaddingLength(payloadSize);
            if (pad == 0)
            {
                return;
            }

            Stream stream = reader.BaseStream;
            if (stream.Length - stream.Position < pad)
            {
                throw new InvalidDataException("Truncated .spritesheet block padding.");
            }

            reader.ReadBytes(pad);
        }

        private static int PaddingLength(int payloadSize)
        {
            return (4 - (payloadSize & 3)) & 3;
        }

        private static void WriteFourCC(BinaryWriter writer, string fourCC)
        {
            writer.Write(Encoding.ASCII.GetBytes(fourCC));
        }

        private static string ReadFourCC(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(FOUR_CC_SIZE);
            if (bytes.Length != FOUR_CC_SIZE)
            {
                throw new InvalidDataException("Truncated .spritesheet FourCC.");
            }

            return Encoding.ASCII.GetString(bytes);
        }

        private static void WriteUtf8(BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write(0);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static string ReadUtf8(BinaryReader reader)
        {
            int byteCount = reader.ReadInt32();
            if (byteCount < 0)
            {
                throw new InvalidDataException("Negative string length.");
            }

            if (byteCount == 0)
            {
                return string.Empty;
            }

            byte[] bytes = reader.ReadBytes(byteCount);
            if (bytes.Length != byteCount)
            {
                throw new InvalidDataException("Truncated string.");
            }

            return Encoding.UTF8.GetString(bytes);
        }

        private static BinaryReader OpenPayload(byte[] payload)
        {
            return new BinaryReader(new MemoryStream(payload, 0, payload.Length, false, true), Encoding.UTF8, false);
        }
    }
}

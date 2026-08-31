# About Runtime SpriteSheet Packer

Use the Runtime SpriteSheet Packer package to pack readable `Texture2D` assets into atlas pages at runtime and create Unity `Sprite` objects from those pages. Use it when sprites are unknown until play mode: user content, downloaded textures, or player builds where Editor atlas baking is not available.

The public API is `kknngggg.Unity.Sprites.SpriteSheet`. It is shaped like Unity `SpriteAtlas`: build a `SpritePackEntry` list, pack once, then `GetSprite` by name. Each entry carries sprite name, pixels per unit, pivot, and mesh type into `SliceInfo`. Packing may span multiple pages when content exceeds `PackingSettings.MaxSize`.

`Pack` returns a `PackingResult`. On success, `IsSuccess` is true and `SpriteSheet` is the packed atlas. On failure, `Error.Code` is a `PackingErrorCodes` value and `SpriteSheet` is null. Pack does not throw.

# Installing Runtime SpriteSheet Packer

To install this package, follow the instructions in the [Unity Package Manager](https://docs.unity3d.com/2021.3/Documentation/Manual/upm-ui-install.html) documentation.

Minimum Unity version: **2021.3** LTS.

Git URL:

```
https://github.com/kknngggg/UnityRuntimeSpriteSheetPacker.git?path=Packages/com.kknngggg.runtimespritesheetgenerator
```

No extra Editor menus or resources are required. Add a reference to assembly `kknngggg.RuntimeSpriteSheetPacker` if your scripts use a custom assembly definition.

# Using Runtime SpriteSheet Packer

## Pack textures

1. Ensure every source `Texture2D` has **Read/Write Enabled** (import settings, or a runtime-created readable texture).
2. Build a `SpritePackEntry` list. Names must be unique. Empty name falls back to `texture.name`.
3. Call `SpriteSheet.Pack(entries, settings)` and check `PackingResult.IsSuccess`.
4. Use `GetSprite(name)` on `SpriteRenderer`, `Image`, or animation code.
5. Call `Dispose()` when the sheet is no longer needed.

```csharp
using kknngggg.Unity.Sprites;
using UnityEngine;

SpritePackEntry[] entries =
{
    new SpritePackEntry(textureA, "hero"),
    new SpritePackEntry(textureB, "ui_icon_ok"),
};

PackingResult result = SpriteSheet.Pack(entries, SpriteSheet.PackingSettings.Default);
if (result.IsSuccess == false)
{
    Debug.LogError(result.Error.Message);
    return;
}

SpriteSheet sheet = result.SpriteSheet;
Sprite sprite = sheet.GetSprite("hero");
sheet.Dispose();
```

Per-sprite pixels per unit, pivot, and mesh type:

```csharp
SpritePackEntry[] entries =
{
    new SpritePackEntry(textureA, "hero", 100f, new Vector2(0.5f, 0f), SpriteMeshType.FullRect),
    new SpritePackEntry(textureB, "fx", 50f, new Vector2(0.5f, 0.5f), SpriteMeshType.Tight),
};

PackingResult result = SpriteSheet.Pack(entries, SpriteSheet.PackingSettings.Default);
```

## SpritePackEntry

| Field | Default | Meaning |
|---|---|---|
| `Texture` | required | Readable `Texture2D` to blit into the atlas. |
| `Name` | `texture.name` | Sprite lookup key. Must be unique. |
| `PixelsPerUnit` | `100` | Passed to `Sprite.Create`. Must be `> 0`. |
| `Pivot` | `(0.5, 0.5)` | Normalized sprite pivot. |
| `MeshType` | `SpriteMeshType.FullRect` | `FullRect` or `Tight`. |

`new SpritePackEntry(texture, name)` and `new SpritePackEntry(texture)` fills name, PPU, pivot, and mesh type with the defaults above.

## PackingSettings

| Field | Default | Meaning |
|---|---|---|
| `Padding` | `1` | Pixels between packed rects. Must be `>= 0`. |
| `MaxSize` | `2048` | Max page width and height in pixels. Must be `>= 1`. A source texture larger than the effective max size fails the pack. |
| `ForcePowerOfTwo` | `true` | Pack into the largest power-of-two size `<= MaxSize`, then round each packed page axis up to the next power of two. |
| `EffectiveMaxSize` | derived | `MaxSize` when `ForcePowerOfTwo` is off; otherwise the largest power of two `<= MaxSize`. Source textures larger than this fail the pack. |

`PackingSettings.Default` returns `Padding` 1, `MaxSize` 2048, `ForcePowerOfTwo` true.

## SpriteSheet API

| Member | Description |
|---|---|
| `Pack(IEnumerable<SpritePackEntry>, PackingSettings, string texturePageName = null)` | Pack entries. Returns `PackingResult`. Empty name falls back to `texture.name`. Slice PPU, pivot, and mesh type come from the entry. Optional `texturePageName` prefixes page texture names (`{name}_{pageIndex}`); default is a GUID. Does not throw on pack failure. |
| `ToBytes()` | Serialize the packed sheet to a `.spritesheet` byte array (binary blocks). Unreadable page textures are captured with a GPU readback, then stored as PNG. |
| `Load(byte[] data)` | Rebuild pages and slices from `ToBytes()` output. Disk I/O is not in this package; write and read the bytes yourself. |
| `PageCount` | Number of atlas `Texture2D` pages. |
| `GetPage(int pageIndex)` | Page texture. Throws if disposed or out of range. |
| `GetSprite(string spriteName)` | Creates and caches a `Sprite` using that slice’s PPU, pivot, and mesh type. Returns `null` if the name is missing. |
| `TryGetSlice(string spriteName, out SliceInfo)` | Rect, page index, pivot, pixels per unit, mesh type. |
| `Slices` | All packed slices by name. |
| `Dispose()` | Destroys cached sprites and page textures. Further use throws `ObjectDisposedException`. |

`GetSprite` is lazy: the `Sprite` is created on first request and reused.

## PackingResult

| Member | Description |
|---|---|
| `IsSuccess` | `true` when `Error` is `PackingError.None`. |
| `SpriteSheet` | Packed atlas on success; `null` on failure. |
| `Error` | `PackingError` with `Code` and `Message`. Equals `PackingError.None` on success. |

Partial atlas pages are disposed on failure. Check `IsSuccess` before using `SpriteSheet`.

## Save and load `.spritesheet` files

A `.spritesheet` file is little-endian binary blocks. Packed pages, page names, and every `SliceInfo` round-trip. Packing settings are not stored; they only affect `Pack`.

Header:

- Magic `SPSH` (4 ASCII bytes)
- Version `uint32` (`1`)

Each block:

- FourCC (4 ASCII bytes)
- `payloadSize` (`uint32`)
- payload
- zero pad to a 4-byte boundary

| FourCC | Payload |
|---|---|
| `HEAD` | `pageCount` (`int32`), `sliceCount` (`int32`). Must be the first block. Extra bytes are ignored. |
| `PAGE` | UTF-8 name (`int32` byte length + bytes), `width` (`int32`), `height` (`int32`), PNG (`int32` byte length + bytes). One block per page, in page-index order. |
| `SLCE` | UTF-8 name, rect `x y width height` (`float32` × 4), `page` (`int32`), `pixelsPerUnit` (`float32`), pivot `x y` (`float32`), `meshType` (`int32`). |

Unknown FourCC blocks are skipped. Extra trailing bytes inside `HEAD` / `PAGE` / `SLCE` are ignored so newer writers can extend a block.

`Load` rebuilds GPU-only page textures (`isReadable` is false), matching `Pack`.

```csharp
using System.IO;
using kknngggg.Unity.Sprites;

byte[] data = sheet.ToBytes();
File.WriteAllBytes("Hero.spritesheet", data);

SpriteSheet loaded = SpriteSheet.Load(File.ReadAllBytes("Hero.spritesheet"));
Sprite sprite = loaded.GetSprite("hero");
loaded.Dispose();
```

`Load` throws `InvalidDataException` if the magic, version, block layout, PNG, or slice page index is invalid. `Load(null)` throws `ArgumentNullException`.

## Validation errors

`Pack` does not throw. Failures return `PackingResult` with `IsSuccess` false. `Error.Code` is a `PackingErrorCodes` value:

| Code | When |
|---|---|
| `INVALID_PADDING` | `Padding` is `< 0`. |
| `INVALID_MAX_SIZE` | `MaxSize` is `< 1`. |
| `NULL_ENTRIES` | Entry list is null. |
| `EMPTY_ENTRIES` | Entry list is empty. |
| `NULL_TEXTURE` | An entry has a null texture. |
| `EMPTY_NAME` | Sprite name is empty after fallback to `texture.name`. |
| `DUPLICATE_NAME` | Two entries share the same name. |
| `TEXTURE_NOT_READABLE` | A texture does not have Read/Write Enabled. |
| `INVALID_PIXELS_PER_UNIT` | `PixelsPerUnit` is `<= 0`. |
| `TEXTURE_EXCEEDS_MAX_SIZE` | Texture width or height exceeds `EffectiveMaxSize`. |
| `PACK_FAILED` | A remaining texture cannot be placed on a page (packer placed zero rects). |

`GetPage` still throws `ObjectDisposedException` / `ArgumentOutOfRangeException`. `Load` still throws on bad bytes.

## Requirements

This version is compatible with the following versions of the Unity Editor:

* 2021.3 LTS and later

No third-party Unity packages are required.

## Known limitations

Runtime SpriteSheet Packer version 0.1.0 includes the following known limitations:

* No Editor window or Sprite Atlas importer integration. Packing is code-only.
* Duplicate/empty names and oversized textures fail the whole pack.

## Document revision history

| Date | Reason |
|---|---|
| Aug 13, 2026 | Document created. |
| Aug 29, 2026 | Document `texturePageName` and `EffectiveMaxSize`. |
| Aug 30, 2026 | Document `.spritesheet` `ToBytes` / `Load(byte[])`. Path I/O stays outside the package. |
| Aug 31, 2026 | Document `PackingResult`. `Pack` no longer throws. Rename display name to Runtime SpriteSheet Packer. |

# About Runtime SpriteSheet Generator

Use the Runtime SpriteSheet Generator package to pack readable `Texture2D` assets into atlas pages at runtime and create Unity `Sprite` objects from those pages. Use it when sprites are unknown until play mode: user content, downloaded textures, or player builds where Editor atlas baking is not available.

The public API is `kknngggg.Unity.Sprites.SpriteSheet`. It is shaped like Unity `SpriteAtlas`: build a `SpritePackEntry` list, pack once, then `GetSprite` by name. Each entry carries sprite name, pixels per unit, pivot, and mesh type into `SliceInfo`. Packing may span multiple pages when content exceeds `PackingSettings.MaxSize`.

# Installing Runtime SpriteSheet Generator

To install this package, follow the instructions in the [Unity Package Manager](https://docs.unity3d.com/2021.3/Documentation/Manual/upm-ui-install.html) documentation.

Minimum Unity version: **2021.3** LTS.

No extra Editor menus or resources are required. Add a reference to assembly `kknngggg.RuntimeSpriteSheetGenerator` if your scripts use a custom assembly definition.

# Using Runtime SpriteSheet Generator

## Pack textures

1. Ensure every source `Texture2D` has **Read/Write Enabled** (import settings, or a runtime-created readable texture).
2. Build a `SpritePackEntry` list. Names must be unique. Empty name falls back to `texture.name`.
3. Call `SpriteSheet.Pack(entries, settings)`.
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

SpriteSheet sheet = SpriteSheet.Pack(entries, SpriteSheet.PackingSettings.Default);
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

SpriteSheet sheet = SpriteSheet.Pack(entries, SpriteSheet.PackingSettings.Default);
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
| `MaxSize` | `2048` | Max page width and height in pixels. Must be `>= 1`. A source texture larger than the effective max size throws. |
| `ForcePowerOfTwo` | `true` | Pack into the largest power-of-two size `<= MaxSize`, then round each packed page axis up to the next power of two. |

`PackingSettings.Default` returns those values.

## SpriteSheet API

| Member | Description |
|---|---|
| `Pack(IEnumerable<SpritePackEntry>, PackingSettings)` | Pack entries. Empty name falls back to `texture.name`. Slice PPU, pivot, and mesh type come from the entry. |
| `PageCount` | Number of atlas `Texture2D` pages. |
| `GetPage(int pageIndex)` | Page texture. Throws if disposed or out of range. |
| `GetSprite(string spriteName)` | Creates and caches a `Sprite` using that slice’s PPU, pivot, and mesh type. Returns `null` if the name is missing. |
| `TryGetSlice(string spriteName, out SliceInfo)` | Rect, page index, pivot, pixels per unit, mesh type. |
| `Slices` | All packed slices by name. |
| `Dispose()` | Destroys cached sprites and page textures. Further use throws `ObjectDisposedException`. |

`GetSprite` is lazy: the `Sprite` is created on first request and reused.

## Validation errors

Pack throws if:

- The entry list is null, empty, or contains a null texture
- A sprite name is empty or duplicated
- A texture is not readable
- `PixelsPerUnit` is `<= 0`
- A texture’s width or height exceeds the effective max size (`MaxSize`, or the largest power of two `<= MaxSize` when `ForcePowerOfTwo` is on)
- A remaining texture cannot be placed on a page (packer placed zero rects)

## Requirements

This version is compatible with the following versions of the Unity Editor:

* 2021.3 LTS and later

No third-party Unity packages are required.

## Known limitations

Runtime SpriteSheet Generator version 0.1.0 includes the following known limitations:

* No Editor window or Sprite Atlas importer integration. Packing is code-only.
* Duplicate/empty names and oversized textures fail the whole pack.

## Document revision history

| Date | Reason |
|---|---|
| Aug 13, 2026 | Document created. |

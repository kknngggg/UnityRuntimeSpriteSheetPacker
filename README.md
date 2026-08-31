# Runtime SpriteSheet Packer

Pack readable `Texture2D` assets into sprite sheets at runtime and fetch Unity `Sprite` objects by name. No Editor atlas bake.

This repository is a Unity development project. The installable UPM package is [`com.kknngggg.runtimespritesheetgenerator`](Packages/com.kknngggg.runtimespritesheetgenerator).

Public API lives in `kknngggg.Unity.Sprites`. Entry point is `SpriteSheet.Pack`, which returns a `PackingResult`.

## Requirements

- **Package:** Unity 2021.3 LTS or later
- **This project:** Unity 6 (`6000.3`) — demos and Play Mode tests

## Install

### Git (Package Manager)

Window > Package Manager > + > Add package from git URL:

```
https://github.com/kknngggg/UnityRuntimeSpriteSheetPacker.git?path=Packages/com.kknngggg.runtimespritesheetgenerator
```

### Local

Window > Package Manager > + > Add package from disk… > select `Packages/com.kknngggg.runtimespritesheetgenerator/package.json`.

This clone already embeds the package (`file:com.kknngggg.runtimespritesheetgenerator` in the lock file).

See [Install packages](https://docs.unity3d.com/2021.3/Documentation/Manual/upm-ui-install.html).

## Features

- `SpriteSheet.Pack(IEnumerable<SpritePackEntry>, PackingSettings, string texturePageName = null)` — returns `PackingResult` (`SpriteSheet` or `PackingError`). Does not throw on pack failure
- Name, pixels per unit, pivot, and mesh type come from each `SpritePackEntry`
- Multi-page sheets when content exceeds `PackingSettings.MaxSize`
- `ToBytes()` / `Load(byte[])` — round-trip a packed sheet as `.spritesheet` binary blocks
- `GetSprite(name)`, `TryGetSlice(name)`, `GetPage(index)`, `IDisposable`
- Settings: `Padding`, `MaxSize` (default 2048), `ForcePowerOfTwo` (default true), `EffectiveMaxSize`

## Quick start

Textures must have **Read/Write Enabled**. Names must be unique. No texture may be larger than the effective max size (`MaxSize`, or the largest power of two `<= MaxSize` when `ForcePowerOfTwo` is on).

Two-argument `SpritePackEntry` uses defaults: PPU `100`, pivot `(0.5, 0.5)`, `SpriteMeshType.FullRect`.

```csharp
using kknngggg.Unity.Sprites;
using UnityEngine;

public class RuntimeSheetExample : MonoBehaviour
{
    public Texture2D[] textures;
    public SpriteRenderer target;

    private SpriteSheet _sheet;

    private void Start()
    {
        SpriteSheet.PackingSettings settings = SpriteSheet.PackingSettings.Default;
        settings.Padding = 1;
        settings.MaxSize = 2048;
        settings.ForcePowerOfTwo = true;

        SpritePackEntry[] entries = new SpritePackEntry[textures.Length];
        for (int i = 0; i < textures.Length; i++)
        {
            entries[i] = new SpritePackEntry(textures[i], textures[i].name);
        }

        PackingResult result = SpriteSheet.Pack(entries, settings);
        if (result.IsSuccess == false)
        {
            Debug.LogError(result.Error.Message);
            return;
        }

        _sheet = result.SpriteSheet;
        target.sprite = _sheet.GetSprite(textures[0].name);
    }

    private void OnDestroy()
    {
        if (_sheet != null)
        {
            _sheet.Dispose();
            _sheet = null;
        }
    }
}
```

Custom name, PPU, pivot, mesh:

```csharp
SpritePackEntry[] entries =
{
    new SpritePackEntry(idle, "idle_0", 100f, new Vector2(0.5f, 0f), SpriteMeshType.FullRect),
    new SpritePackEntry(walk, "walk_0", 50f, new Vector2(0.5f, 0.5f), SpriteMeshType.Tight),
};

PackingResult result = SpriteSheet.Pack(entries, SpriteSheet.PackingSettings.Default);
if (result.IsSuccess)
{
    Sprite sprite = result.SpriteSheet.GetSprite("walk_0");
}
```

Fail codes live in `kknngggg.Unity.Sprites.Errors.PackingErrorCodes`: `NULL_ENTRIES`, `EMPTY_ENTRIES`, `NULL_TEXTURE`, `EMPTY_NAME`, `DUPLICATE_NAME`, `TEXTURE_NOT_READABLE`, `INVALID_PIXELS_PER_UNIT`, `TEXTURE_EXCEEDS_MAX_SIZE`, `INVALID_PADDING`, `INVALID_MAX_SIZE`, `PACK_FAILED`.

If your scripts use a custom assembly definition, add a reference to `kknngggg.RuntimeSpriteSheetPacker`.

## Repository layout

- `Packages/com.kknngggg.runtimespritesheetgenerator` — UPM package (runtime API, tests, docs)
- `Assets/Demos/SpriteSheets` — UI Toolkit demo: pick textures, pack, preview atlas pages, save/load `.spritesheet`
- `Assets/Demos/RectanglePacking` — rectangle packer visualization

Scenes: `Assets/Demos/SpriteSheets/Scenes/SpriteSheetsDemo.unity`, `Assets/Demos/RectanglePacking/Scenes/RectanglePacking.unity`.

## Docs

- Package readme: [Packages/com.kknngggg.runtimespritesheetgenerator/README.md](Packages/com.kknngggg.runtimespritesheetgenerator/README.md)
- API guide: [Packages/com.kknngggg.runtimespritesheetgenerator/Documentation/index.md](Packages/com.kknngggg.runtimespritesheetgenerator/Documentation/index.md)
- Changelog: [Packages/com.kknngggg.runtimespritesheetgenerator/CHANGELOG.md](Packages/com.kknngggg.runtimespritesheetgenerator/CHANGELOG.md)

## License

MIT. See [LICENSE.txt](LICENSE.txt).

This project rewrites the original [UnityRuntimeSpriteSheetsGenerator](https://github.com/DaVikingCode/UnityRuntimeSpriteSheetsGenerator) (`AssetPacker` MonoBehaviour) as a UPM package with a `SpriteSheet` API. Rectangle packing algorithm: Ville Koskela (AS3), ported to Unity by Da Viking Code.

Third-party licenses: [Third Party Notices.md](Third%20Party%20Notices.md) (same notices as the package).

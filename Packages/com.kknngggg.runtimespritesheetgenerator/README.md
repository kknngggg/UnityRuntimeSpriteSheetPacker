# Runtime SpriteSheet Packer

Pack `SpritePackEntry` lists into sprite sheets at runtime and fetch `Sprite` objects by name. No Editor atlas bake required.

Public API lives in `kknngggg.Unity.Sprites`. Entry point is `SpriteSheet.Pack`, which returns a `PackingResult`.

## Install

Unity 2021.3 LTS or later.

- **Git:** Window > Package Manager > + > Add package from git URL:

```
https://github.com/kknngggg/UnityRuntimeSpriteSheetPacker.git?path=Packages/com.kknngggg.runtimespritesheetgenerator
```

- **Local:** Window > Package Manager > + > Add package from disk… > select this folder’s `package.json`.

See [Install packages](https://docs.unity3d.com/2021.3/Documentation/Manual/upm-ui-install.html).

## Features

- `SpriteSheet.Pack(IEnumerable<SpritePackEntry>, PackingSettings, string texturePageName = null)` — returns `PackingResult` (`SpriteSheet` or `PackingError`). Does not throw on pack failure
- Name, pixels per unit, pivot, and mesh type come from each `SpritePackEntry`
- `SpriteSheet.ToBytes()` / `Load(byte[])` — round-trip a packed sheet as `.spritesheet` binary blocks
- Multi-page sheets when content exceeds `PackingSettings.MaxSize`
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

Fail codes live in `kknngggg.Unity.Sprites.Errors.PackingErrorCodes`. If your scripts use a custom assembly definition, add a reference to `kknngggg.RuntimeSpriteSheetPacker`.

## Docs

[Documentation/index.md](Documentation/index.md)

## License

MIT. See [LICENSE.md](LICENSE.md). Rectangle packing algorithm: Ville Koskela (AS3), ported by Da Viking Code. Rewrite of the original [UnityRuntimeSpriteSheetsGenerator](https://github.com/DaVikingCode/UnityRuntimeSpriteSheetsGenerator) `AssetPacker` API. See [Third Party Notices.md](Third%20Party%20Notices.md).

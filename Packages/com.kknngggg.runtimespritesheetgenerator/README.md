# Runtime SpriteSheet Generator

Pack `SpritePackEntry` lists into sprite sheets at runtime and fetch `Sprite` objects by name. No Editor atlas bake required.

Public API lives in `kknngggg.Unity.Sprites`. Entry point is `SpriteSheet.Pack`.

## Install

Unity 2021.3 LTS or later.

- **Git:** Window > Package Manager > + > Add package from git URL:

```
https://github.com/kknngggg/SpriteSheetPacker.git?path=Packages/com.kknngggg.runtimespritesheetgenerator
```

- **Local:** Window > Package Manager > + > Add package from disk… > select this folder’s `package.json`.

See [Install packages](https://docs.unity3d.com/2021.3/Documentation/Manual/upm-ui-install.html).

## Features

- `SpriteSheet.Pack(IEnumerable<SpritePackEntry>, PackingSettings)` — name, pixels per unit, pivot, and mesh type come from each entry
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

        _sheet = SpriteSheet.Pack(entries, settings);
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

SpriteSheet sheet = SpriteSheet.Pack(entries, SpriteSheet.PackingSettings.Default);
Sprite sprite = sheet.GetSprite("walk_0");
```

## Docs

[Documentation/index.md](Documentation/index.md)

## License

MIT. See [LICENSE.md](LICENSE.md). Rectangle packing algorithm: Ville Koskela (AS3), ported by Da Viking Code. Rewrite of the original [UnityRuntimeSpriteSheetsGenerator](https://github.com/DaVikingCode/UnityRuntimeSpriteSheetsGenerator) `AssetPacker` API. See [Third Party Notices.md](Third%20Party%20Notices.md).

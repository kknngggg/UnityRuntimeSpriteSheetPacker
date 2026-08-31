# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [v0.1.0] - 2026-08-31

### Added

- `.spritesheet` binary blocks: `SpriteSheet.ToBytes()` and `SpriteSheet.Load(byte[])`. Path I/O is not in this package.
- `PackingResult`, `PackingError`, and `PackingErrorCodes` — pack success or a fail code instead of an exception

### Changed

- `SpriteSheet.Pack` returns `PackingResult` and no longer throws on validation or pack failure. `GetPage` / `Load` still throw.

## [v0.1.0] - 2026-08-13

### Added

- UPM package `com.kknngggg.runtimespritesheetgenerator` (Unity 2021.3+)
- `SpriteSheet.Pack` / `GetSprite` / `TryGetSlice` / `GetPage` / `Dispose` in `kknngggg.Unity.Sprites`
- Per-entry name, pixels per unit, pivot, and mesh type via `SpritePackEntry`
- Multi-page packing when content exceeds `PackingSettings.MaxSize`
- `PackingSettings`: `Padding`, `MaxSize`, `ForcePowerOfTwo`, `EffectiveMaxSize`

### Changed

- Replaces the original Da Viking Code `AssetPacker` MonoBehaviour API

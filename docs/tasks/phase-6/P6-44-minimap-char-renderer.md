# P6-44 — Minimap character renderer, char sheet and pre-baked font data

- **Status:** todo
- **Depends on:** P5-96
- **Size:** M

## Goal

LoomEditor can paint a minimap character (or a block) into an RGBA pixel buffer at scale 1 and 2 from Monaco's pre-baked
font data, or from 16-px sample data at any scale, bit-identical to Monaco.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/minimap/minimapCharSheet.ts` | 6–43 | `Constants`, `allCharCodes`, `getCharIndex` |
| `src/vs/editor/browser/viewParts/minimap/minimapPreBaked.ts` | 8–63 | `charTable`, `decodeData`, `prebakedMiniMaps` (scales 1 and 2: copy the two hex strings **verbatim**) |
| `src/vs/editor/browser/viewParts/minimap/minimapCharRenderer.ts` | 1–134 | `MinimapCharRenderer` (`soften`, `renderChar`, `blockRenderChar`) |
| `src/vs/editor/browser/viewParts/minimap/minimapCharRendererFactory.ts` | 18–48 | `create` (cache of the last renderer; pre-baked data first) |
| same | 75–168 | `createFromSampleData`, `_downsampleChar`, `_downsample` |
| `src/vs/editor/common/core/misc/rgba.ts` | 1–57 | `RGBA8` |
| `src/vs/editor/test/browser/view/minimapCharRenderer.test.ts` | 12–128 | suite `MinimapCharRenderer` (`letter d @ 2x`, `letter d @ 1x`) |

## Files to create or change

- `src/LoomEditor.View/Minimap/MinimapCharSheet.cs` — create (namespace `LoomEditor.View.Minimap`; header `minimapCharSheet.ts`)
- `src/LoomEditor.View/Minimap/MinimapPreBaked.cs` — create (header `minimapPreBaked.ts`)
- `src/LoomEditor.View/Minimap/MinimapCharRenderer.cs` — create
- `src/LoomEditor.View/Minimap/MinimapCharRendererFactory.cs` — create (without `createSampleData`, see decisions)
- `src/LoomEditor.View/Minimap/MinimapImageData.cs` — create: `internal sealed class MinimapImageData(int width, int height)` with
  `Width`, `Height`, `byte[] Data` (RGBA, row-major) — Monaco's `ImageData`
- `src/LoomEditor.View/Minimap/Rgba8.cs` — create (header `rgba.ts`)
- `tests/LoomEditor.View.Tests/Minimap/MinimapCharRendererTests.cs` — create

## Design decisions (already made — do not change)

- The minimap logic lives in a new folder `src/LoomEditor.View/Minimap/` (it is a view part, not a feature, and has no UI
  dependency); the Win2D drawing is in `LoomEditor.Rendering` (P6-52).
- **`Uint8ClampedArray` semantics:** every write of a computed value goes through one helper
  `static byte ClampToByte(double value)` = round half to even, then clamp to 0–255
  (`(byte)Math.Clamp(Math.Round(value, MidpointRounding.ToEven), 0, 255)`), because JavaScript's `Uint8ClampedArray` does exactly that
  on assignment. Use it everywhere Monaco assigns into a `Uint8ClampedArray` (also in `soften`, `_downsample`, `decodeData` is integral).
- `Rgba8` is an `internal readonly record struct Rgba8(byte R, byte G, byte B, byte A)` with `Empty` and `_clamp` → `Clamp`
  (Monaco's constructor clamps its arguments: keep a static `Create(double r, double g, double b, double a)` that clamps like `_clamp`).
- `createSingleCallFunction(() => decodeData(...))` → `Lazy<byte[]>`.
- `MinimapCharRendererFactory.Create(int scale, string fontFamily, Func<string, byte[]>? sampleDataProvider)`: scales 1 and 2 use the
  pre-baked data; other scales call `sampleDataProvider(fontFamily)` (P6-45 supplies it from Win2D) and `CreateFromSampleData`. If the
  provider is `null`, fall back to the pre-baked scale-2 data and render at scale 2 (document it). Keep Monaco's "last created" cache
  (`static` fields: UI thread only; say so).
- The renderer methods take `MinimapImageData target` like Monaco's `ImageData`.
- The two tests copy `sampleD` verbatim (lines 16–33) and the expected arrays.

## Tests

- [ ] Port: `MinimapCharRenderer` › `letter d @ 2x`, `letter d @ 1x`
- [ ] New: `PreBaked_Scale1And2_HaveTheExpectedLength` (`CHAR_COUNT * charWidth * charHeight` bytes for each scale)
- [ ] New: `GetCharIndex_OutOfRange_SmallScaleWrapsLargeScaleUsesUnknown`
- [ ] New: `ClampToByte_RoundsHalfToEvenAndClamps` (0.5 → 0, 1.5 → 2, 300 → 255, -3 → 0)
- [ ] New: `BlockRenderChar_FillsTheCell`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: rows for the four minimap files and `rgba.ts`; tests table: `minimapCharRenderer.test.ts` 2 / 2
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Sampling a font with Win2D for scales above 2 (P6-45).

## Suggested commit message

`feat(view): port the minimap character renderer`

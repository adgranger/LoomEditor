# P6-45 — Minimap font sample data with Win2D (scales above 2)

- **Status:** todo
- **Depends on:** P6-44
- **Size:** S

## Goal

For minimap scales above 2 (high DPI or `scale: 3`), LoomEditor samples the editor font like Monaco does with a canvas: the
96 characters drawn bold at 16 px in 10-px cells, read back as RGBA bytes.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/minimap/minimapCharRendererFactory.ts` | 50–73 | `createSampleData(fontFamily)` |
| `src/vs/editor/browser/viewParts/minimap/minimapCharSheet.ts` | 6–25 | the sizes (`SAMPLED_CHAR_HEIGHT = 16`, `SAMPLED_CHAR_WIDTH = 10`, `CHAR_COUNT`) |

## Files to create or change

- `src/LoomEditor.Rendering/Win2D/Win2DMinimapFontSampler.cs` — create (header `minimapCharRendererFactory.ts (createSampleData)`):
  `internal static byte[] CreateSampleData(CanvasDevice device, string fontFamily)`
- the place where the renderer creates the minimap char renderer (P6-52) will pass `fontFamily => Win2DMinimapFontSampler.CreateSampleData(device, fontFamily)`;
  in this card add only the sampler and a test

## Design decisions (already made — do not change)

- Draw with a `CanvasRenderTarget` of `CHAR_COUNT * 10` × 16 pixels at **96 DPI** (1 DIP = 1 pixel), cleared to transparent, white
  text, `CanvasTextFormat { FontFamily, FontSize = 16, FontWeight = Bold }`, vertical alignment centered on y = 8 (Monaco: `textBaseline = 'middle'`
  at `y = SAMPLED_CHAR_HEIGHT / 2`), each character at `x = index * 10`. Read with `GetPixelBytes()` (BGRA premultiplied in Win2D) and
  convert to **RGBA non-premultiplied** (Monaco's `getImageData`): swap B and R, and un-premultiply (`c * 255 / a`, rounded, 0 when `a == 0`).
- The result must have exactly `SAMPLED_CHAR_HEIGHT * SAMPLED_CHAR_WIDTH * 4 * CHAR_COUNT` bytes (`CreateFromSampleData` checks it).
- Glyph shapes differ from Chromium's canvas rasterization: bit-identity with Monaco is **not** expected for scales above 2 (behavior
  difference to document in PORTING).

## Tests

- [ ] New (`tests/LoomEditor.Rendering.Tests`): `CreateSampleData_HasTheExpectedLength` and `…_LetterCellIsNotEmpty` — only if the
      Rendering tests can create a `CanvasDevice` (look for an existing test that does); otherwise **no automated test**: add a manual
      check to `docs/testing/phase-6-manual-tests.md` (create it as P6-26 describes if it does not exist yet) and say so in the card when closing it
- [ ] New (View, pure): `ConvertBgraPremultipliedToRgba_Unpremultiplies` — put the conversion in a small pure `static` helper in
      `LoomEditor.View/Minimap/` so it is testable without Win2D

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `minimapCharRendererFactory.ts` row ✅ with the Win2D note
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing the minimap (P6-52).

## Suggested commit message

`feat(rendering): sample the minimap font with Win2D`

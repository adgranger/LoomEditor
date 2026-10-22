# P6-50 — Rendering minimap lines into a pixel buffer

- **Status:** done
- **Depends on:** P6-49
- **Size:** M

## Goal

The minimap paints its lines (characters with token colors, or blocks) into an RGBA buffer, reusing the lines of the
previous frame that did not change, exactly like Monaco's `renderLines`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/viewParts/minimap/minimap.ts`.

| Lines | What to port |
|---|---|
| 416–433 | `class MinimapLine` |
| 435–511 | `class RenderData` (`linesEquals`, `scrollEquals`, `_get`, the line events) |
| 513–559 | `class MinimapBuffers` (two buffers alternated, background fill) |
| 1420–1435 | `InnerMinimap._getBuffer` |
| 1942–2037 | `renderLines` |
| 2039–2130 | `_renderUntouchedLines` |
| 2132–2200 | `_renderLine` |
| 2202–2237 | `class ContiguousLineMap` |

## Files to create or change

- `src/LoomEditor.View/Minimap/MinimapRenderData.cs` — create (`MinimapLine`, `RenderData`, `ContiguousLineMap<T>`)
- `src/LoomEditor.View/Minimap/MinimapBuffers.cs` — create
- `src/LoomEditor.View/Minimap/MinimapLinesRenderer.cs` — create (header `minimap.ts (InnerMinimap.renderLines)`): the state Monaco
  keeps on `InnerMinimap` for lines (`_lastRenderData`, `_buffers`) and the three methods
- `tests/LoomEditor.View.Tests/Minimap/MinimapLinesRendererTests.cs` — create

## Design decisions (already made — do not change)

- Buffers are `MinimapImageData` (P6-44); writes use `ClampToByte` (Uint8ClampedArray semantics, P6-44).
- `MinimapLinesRenderer` implements the line part of `IMinimapRenderTarget` (P6-49): `OnFlushed`, `OnLinesChanged`, `OnTokensChanged`
  forward to `RenderData` like Monaco's `InnerMinimap` (1450–1490).
- `RenderLines(MinimapLayout layout)` returns the `MinimapImageData` to draw (or `null` for a 0-size canvas) and keeps the
  `RenderData` for the next frame; the Win2D upload is P6-52.
- The canvas inner size comes from `MinimapOptions.CanvasInnerWidth/Height`.
- `renderMinimap === Text` → `charRenderer.RenderChar`, `Blocks` → `BlockRenderChar`, `useLighterFont` from
  `tracker.BackgroundIsLight()` — as upstream.

## Tests

No upstream unit tests. New, with a fake `IMinimapModel` and the scale-1 pre-baked renderer:

- [x] `RenderLines_OneLine_PaintsItsCharacters` (non-background pixels where the characters are)
- [x] `RenderLines_SameLayoutTwice_ReusesTheBuffer`
- [x] `RenderLines_AfterFlush_RepaintsEverything`
- [x] `RenderLines_ScrolledByOneLine_CopiesUntouchedLines` (check the copied rows equal the previous ones)
- [x] `RenderLines_Blocks_UsesBlockRendering`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `minimap.ts` row (+ line rendering)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Decorations and selections (P6-51), Win2D (P6-52).

## Suggested commit message

`feat(view): render the minimap lines`

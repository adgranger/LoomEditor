# P6-51 — Rendering minimap selections and decorations

- **Status:** done
- **Depends on:** P6-50
- **Size:** M

## Goal

The minimap paints, in a second RGBA buffer drawn above the lines, the line highlights and the column highlights of the
selections and of the decorations with minimap options (inline and gutter positions), like Monaco's `renderDecorations`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/viewParts/minimap/minimap.ts`.

| Lines | What to port |
|---|---|
| 1442–1449 | `onSelectionChanged`, `onDecorationsChanged` (set `_renderDecorations`) |
| 1540–1571 | `renderDecorations` (sort selections and decorations by z-index, line highlights first, then highlights; **not** `_renderSectionHeaders`) |
| 1573–1621 | `_renderSelectionLineHighlights` |
| 1623–1670 | `_renderDecorationsLineHighlights` |
| 1672–1697 | `_renderSelectionsHighlights` |
| 1699–1745 | `_renderDecorationsHighlights` (the `MinimapPosition.Inline` / `Gutter` cases, `GUTTER_DECORATION_WIDTH`) |
| 1747–1775 | `renderDecorationOnLine` |
| 1777–1829 | `getXOffsetForPosition` (tab and full-width aware, with the per-line offset cache) |
| 1831–1834 | `renderDecoration` |

Colors: `minimap.selectionHighlight` and the decoration's minimap color (resolved with `DecorationColorResolver`, P6-43).

## Files to create or change

- `src/LoomEditor.View/Minimap/MinimapDecorationsRenderer.cs` — create (header `minimap.ts (InnerMinimap.renderDecorations)`):
  implements the selection / decoration part of `IMinimapRenderTarget` (P6-49) and paints into its own `MinimapImageData`
- `tests/LoomEditor.View.Tests/Minimap/MinimapDecorationsRendererTests.cs` — create

## Design decisions (already made — do not change)

- Monaco draws on a 2D canvas with `fillRect` and a CSS color (alpha blended over transparent). LoomEditor fills rectangles in a
  `MinimapImageData` that is cleared to transparent each time: write a private `FillRect(x, y, w, h, Rgba8 color)` that does
  **source-over** blending on the buffer (non-premultiplied RGBA, `ClampToByte` on each channel) so overlapping fills look like the
  canvas. Coordinates are canvas inner pixels; truncate like the canvas does for integral rectangles (Monaco passes integers here).
- Section headers (`_renderSectionHeaders`, `_fitSectionHeader`, `_renderSectionLabel`) are **not** ported (P6-40): leave a
  comment where Monaco calls it.
- `decorations.sort` by z-index must be **stable** (JavaScript sort is stable): use `OrderBy`.
- The buffer is repainted only when `_renderDecorations` is set (selection or decoration change, flush, layout change), like Monaco.

## Tests

No upstream unit tests. New, with a fake `IMinimapModel`:

- [x] `Selection_PaintsALineHighlightAndAColumnHighlight`
- [x] `InlineDecoration_PaintsItsColumns`; `GutterDecoration_PaintsTheGutterStrip`
- [x] `HigherZIndexDecoration_WinsTheLineHighlight`; `Selection_HasPriorityOverDecorations`
- [x] `GetXOffsetForPosition_TabsAndFullWidthCharacters`
- [x] `NoChange_DoesNotRepaint`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `minimap.ts` row (+ decorations; section headers not ported)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Win2D drawing, slider and shadow (P6-52), mouse (P6-53), section headers.

## Suggested commit message

`feat(view): render the minimap selections and decorations`

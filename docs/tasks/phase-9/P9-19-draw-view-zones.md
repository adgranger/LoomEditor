# P9-19 — Drawing view zones: the diagonal fill

- **Status:** todo
- **Depends on:** P9-18, P9-16
- **Size:** S

## Goal

The renderer paints the zones that are in the viewport. `Empty` zones show the diff editor's diagonal fill (the hatching that tells "no line here"); the other kinds are left to P9-24 and P9-25 (the renderer skips them without error).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/style.css` | 445–454 | `.diagonal-fill`: `linear-gradient(-45deg, C 12.5%, transparent 12.5%, transparent 50%, C 50%, C 62.5%, transparent 62.5%, transparent 100%)` with `background-size: 8px 8px`, `C = diffEditor.diagonalFill` |
| `src/vs/editor/browser/widget/diffEditor/components/diffEditorViewZones/diffEditorViewZones.ts` | 125–135 | `createFakeLinesDiv` (the node of an alignment zone has class `diagonal-fill`) |

## Files to create or change

- `src/LoomEditor.Rendering/TextViewRenderer.ViewZones.cs` — create (the `partial` class; called from the main draw method between the line backgrounds and the lines: read `TextViewRenderer.cs` for the order and put it where whitespace areas must be under the text, in the same place that the current-line highlight is drawn)
- `src/LoomEditor.Rendering/DiagonalFillPattern.cs` — create (the RGBA tile or buffer, cached)
- `src/LoomEditor.View/Layout/ViewportData.cs` — change: `ViewZoneViewportData`: for each whitespace of `WhitespaceViewportData` that is a zone (match by id with `ViewModel.ViewZones.GetZone(id)`), the zone, its top and its height; empty when there are no zones
- `tests/LoomEditor.Rendering.Tests/ViewZonesRenderingTests.cs`, `tests/LoomEditor.View.Tests/Layout/ViewportDataZonesTests.cs` — create

## Design decisions (already made — do not change)

- Only `ViewZoneKind.Empty` is painted here. The area is the zone's full width **of the content area** (`contentLeft … contentLeft + contentWidth`, as the DOM node of a zone covers it; the margin keeps the gutter color) and its full height, at `top - scrollTop`.
- The pattern: 8×8 DIP tiles; with the surface API in `IRenderSurface` (no clipping, no brush), the hatching is one cached RGBA buffer per `(width, height, color)` drawn with a single `DrawRgbaImage` (`version` = a counter of the cache entry); the cache keeps at most 8 entries (LRU by last use) so scrolling a few zones does not allocate every frame. Pixel formula of the CSS gradient in tile coordinates `(x, y)` ∈ `[0,8)²`, `t = ((x + y) mod 8) / 8` for the `-45deg` direction: stripe color where `t < 0.125` or `0.5 <= t < 0.625`, transparent elsewhere (the diagonal runs from bottom-left to top-right: pick `x + y` or `x - y` so that it looks like the Monaco screenshot — a "/" hatch with two thin stripes per 8 px period — and write the chosen formula in a comment).
- Color: the theme color `diffEditor.diagonalFill` (P9-16 added it); when the theme has no value the zone is not painted.
- Heights below 1 DIP or hidden zones (`IsInHiddenArea`) are skipped.
- Behavior difference (list in `PORTING.md`): the hatching is rasterized at 1 pixel per DIP and stretched by the surface; at high DPI it is slightly softer than the CSS gradient.
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [ ] New (`tests/LoomEditor.Rendering.Tests`, recording surface): `Draw_EmptyZone_DrawsOneImageOverZoneRect`, `Draw_NoZones_NoImageCalls`, `Draw_ZoneOutsideViewport_NotDrawn`, `Draw_HiddenZone_NotDrawn`, `Draw_DeletedLinesZone_Skipped_NoException`, `Draw_ScrolledViewport_ZoneRectMovesWithScrollTop`, `Draw_NoDiagonalFillColor_NotDrawn`, `Pattern_Buffer_StripesAtExpectedPixels` (check four pixels of the 8×8 tile), `Pattern_Cache_ReusesBufferForSameSize`, `Pattern_Cache_EvictsBeyondEight`.
- [ ] New (`ViewportDataZonesTests`): `ZoneViewportData_ListsZonesInViewport`, `ZoneViewportData_NonZoneWhitespace_Ignored`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] **Manual check (maintainer, in P9-31's plan):** an empty zone shows the hatching and scrolls with the text
- [ ] `docs/PORTING.md`: `.diagonal-fill` row + the DPI difference
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`DeletedLines` (P9-24) and `Collapsed` (P9-25) painting, the margin part of zones.

## Suggested commit message

`feat(rendering): draw empty view zones with the diagonal fill`

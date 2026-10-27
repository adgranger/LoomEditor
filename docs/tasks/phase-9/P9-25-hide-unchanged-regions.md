# P9-25 — Hide unchanged regions: the collapsed bar

- **Status:** todo
- **Depends on:** P9-24
- **Size:** M

## Goal

With `HideUnchangedRegions = true` the diff editor hides the long stretches of unchanged lines (keeping 3 context lines): each hidden stretch shows a bar ("N hidden lines") in both editors; clicking the bar reveals the lines, clicking its top or bottom edge reveals 20 more lines on that side, dragging the edge reveals lines one by one, and moving the cursor into hidden lines reveals them.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/features/hideUnchangedRegionsFeature.ts` | 56–75 | revealing hidden lines when the cursor moves into them (`ensureOriginalLineIsVisible` / `ensureModifiedLineIsVisible`, `FromCloserSide`) |
| same | 95–189 | the `unchangedRegions` filter (a single region covering the whole file is dropped) and the zone creation: zone after `HiddenRange.StartLineNumber - 1`, height `24` (`PlaceholderViewZone(d, 24)`), skipped when `shouldHideControls`; the `compactMode` branches are **not** ported |
| same | 190–250 | decorations (`diff-unchanged-lines` over the whole unchanged range) and `setHiddenAreas` for both editors; the glyph margin fold icon (`fold-unchanged`) is **not** ported (the glyph margin is not drawn, phase 6 cut point) |
| same | 312–412 | `CollapsedCodeOverlayWidget`: structure (top strip, center, bottom strip), click on the center = `showAll`, `canMoveTop` / `canMoveBottom` states |
| same | 413–540 | the drag handling of the top and bottom strips (`mousedown` → `isDragged`, line delta = round(pixel delta / line height), `showMoreAbove` / `showMoreBelow` with `revealLineCount` on a click without move) |
| `src/vs/editor/browser/widget/diffEditor/style.css` | 55–70, 283–285 | `.diff-hidden-lines` strips, `.diff-unchanged-lines` background |

## Files to create or change

- `src/LoomEditor.View/DiffEditor/UnchangedRegionZones.cs` — create (pure: regions → zone specs, hidden ranges, decorations)
- `src/LoomEditor.View/Layout/ViewZone.cs` — change: `CollapsedZoneData(UnchangedRegion Region, bool IsOriginalSide)`
- `src/LoomEditor.Rendering/TextViewRenderer.ViewZones.cs` — change: paint `Collapsed` zones
- `src/LoomEditor.View/Layout/DecorationStyles.cs`, `src/LoomEditor.Themes/EditorColorDefaults.cs` — change only if missing: row `diff-unchanged-lines` (background `diffEditor.unchangedCodeBackground`; color defaults were added in P9-16)
- `src/LoomEditor.WinUI/Controls/DiffEditor/HideUnchangedRegionsController.cs` — create (subscribes to the view model, applies hidden areas, zones, decorations; handles the zone pointer events of both editors)
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs` — change: public `CollapseAllUnchangedRegions()` and `ShowAllUnchangedRegions()` (Monaco `diffEditor.collapseAllUnchangedRegions` / `showAllUnchangedRegions`, `commands.ts` 131–170) and the `HideUnchangedRegions` / `HideUnchangedRegionsRevealLineCount` / `HideUnchangedRegionsContextLineCount` / `HideUnchangedRegionsMinimumLineCount` dependency properties (the three counts default 20 / 3 / 3)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — regenerate
- `tests/LoomEditor.View.Tests/DiffEditor/UnchangedRegionZonesTests.cs`, `tests/LoomEditor.Rendering.Tests/CollapsedZoneRenderingTests.cs` — create

## Design decisions (already made — do not change)

- Pure part: `static UnchangedRegionLayout UnchangedRegionZones.Compute(IReadOnlyList<UnchangedRegion> regions, int modifiedLineCount)` → `record UnchangedRegionLayout(IReadOnlyList<ZoneSpec> OriginalZones, IReadOnlyList<ZoneSpec> ModifiedZones, IReadOnlyList<Range> OriginalHiddenAreas, IReadOnlyList<Range> ModifiedHiddenAreas, IReadOnlyList<ModelDeltaDecoration> OriginalDecorations, IReadOnlyList<ModelDeltaDecoration> ModifiedDecorations)`. Rules: a single region with `ModifiedLineNumber == 1 && LineCount == modifiedLineCount` → all empty; regions with `ShouldHideControls` get no zone **but still** the hidden areas (empty hidden range → skipped) and the decoration; zone `AfterLineNumber = GetHiddenXRange().StartLineNumber - 1`, `HeightInPx = 24` (a public constant `UnchangedRegionZones.BarHeight`), `Kind = Collapsed`, `ShowInHiddenAreas = true`, `SuppressMouseDown = true`, `Data = CollapsedZoneData`; hidden areas = `GetHiddenXRange().ToInclusiveRange()` of each region (non-null only); decorations = `diff-unchanged-lines` class, `IsWholeLine`, over `XUnchangedRange.ToInclusiveRange()`.
- The bar is painted by the renderer from `CollapsedZoneData`: background `diffEditor.unchangedRegionBackground`, a `⇕` glyph (Segoe UI Symbol, in the line number column area) and the text `"{n} hidden lines"` (singular `"1 hidden line"`) in `diffEditor.unchangedRegionForeground`, centered vertically; a 2 DIP line in the foreground color along the top edge when the region has visible lines at its top (`VisibleLineCountTop > 0`) and along the bottom edge when `VisibleLineCountBottom > 0` (Monaco's `canMoveTop` / `canMoveBottom` drawn as the strips). Behavior difference: Monaco's center also shows the outline breadcrumb of the modified code (`modifiedOutlineSource`): not ported (no outline provider; breadcrumbs are out of scope).
- Interaction (controller, on `ViewZonePointer` of **either** editor, left button only): `Pressed` in the top 6 DIPs of the bar → drag handle top; in the bottom 6 DIPs → drag handle bottom; elsewhere → on `Released` without move `ShowAll` (`UnchangedRegion.ShowAll`). A handle sets `region.IsDragged`, calls `CapturePointerForZone()`, `Moved` computes `lineDelta = round(deltaY / lineHeight)` and sets the visible count exactly as lines 413–540 (top: `cur + lineDelta` clamped to `[0, GetMaxVisibleLineCountTop()]`; bottom: `cur - lineDelta` clamped to `[0, GetMaxVisibleLineCountBottom()]`), `Released` with `|delta| <= 2 DIPs` (no move) → `ShowMoreAbove(revealLineCount)` / `ShowMoreBelow(revealLineCount)`, then clears `IsDragged`. The cursor shows `ResizeUpDown` over the strips (use `ProtectedCursor` as other controls of the repo do; if none does, skip the cursor and say so).
- **Scroll anchor** (no Monaco source: in Monaco the host passes optional `runWithModifiedEditorScrollAnchor` callbacks, `diffEditorWidget.ts` 53–60, and the standalone editor passes none): when a region changes because of a user action (bar click or drag), keep the screen position of the modified line just below the bar: remember `GetTopForLineNumber(anchorLine, true) - ScrollTop` before the change, restore `ScrollTop` after the layout has settled (the next tick after P9-23's recompute); `anchorLine` = the first modified line after the hidden range. Both editors scroll together through the P9-23 synchronization.
- Cursor moves: `SelectionChanged` of each editor → for each selection start / end line: `EnsureOriginalLineIsVisible` / `EnsureModifiedLineIsVisible(line, FromCloserSide)` (only when the selection change is not `ContentFlush` — a selection change caused by setting the text — nor caused by the controller itself).
- Applying: on `UnchangedRegionsChanged` and on side-by-side / inline mode changes: `SetHiddenAreas` on both editors (inline mode: the hidden original gets them too, so its view lines match), replace the controller's zones (own ids) and decorations; then the alignment of P9-23 recomputes (the zones/hidden areas trigger its inputs).
- `HideUnchangedRegions = false` → `UnchangedRegions` is empty (P9-14) and everything above is removed.
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [ ] New (`UnchangedRegionZonesTests`): `Compute_NoRegions_Empty`, `Compute_OneRegion_ZonesAfterLineBeforeHiddenRange`, `Compute_RegionWithTopVisibleLines_ZoneAfterTheVisibleOnes`, `Compute_ShouldHideControls_NoZoneButDecoration`, `Compute_SingleRegionCoveringWholeFile_Empty`, `Compute_HiddenAreas_AreTheHiddenRanges`, `Compute_Decorations_CoverWholeUnchangedRange`, `Compute_BarHeightIs24`.
- [ ] New (`CollapsedZoneRenderingTests`): `Draw_Collapsed_DrawsBackgroundGlyphAndLabel`, `Draw_Label_SingularAndPlural`, `Draw_TopStrip_OnlyWhenVisibleTopLines`, `Draw_BottomStrip_OnlyWhenVisibleBottomLines`.
- [ ] Manual (P9-31's plan): with a long file and two small changes the unchanged stretches collapse; clicking the bar reveals; clicking the top / bottom edge reveals 20 lines; dragging reveals line by line; Ctrl+Home / searching into hidden lines reveals them; both sides stay aligned in side-by-side and in inline mode.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI` run, diff checked
- [ ] `docs/PORTING.md`: `hideUnchangedRegionsFeature.ts` (subset: no compact mode, no breadcrumbs, no glyph fold icon)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Compact mode, `diffEditor.toggleCollapseUnchangedRegions` menu entry, the glyph margin fold icon, breadcrumbs.

## Suggested commit message

`feat: hide unchanged regions in the diff editor`

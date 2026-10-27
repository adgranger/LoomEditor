# P9-23 — Side by side: aligned view zones and synchronized scrolling

- **Status:** done
- **Depends on:** P9-22
- **Size:** M
- **Recommended model:** Opus (two controls, their view zones, a layout feedback loop and scroll synchronization that interact; a mistake shows up only at runtime as drifting lines or an endless re-layout. On failure — the sides drift, the layout never settles, or the zone re-creation flickers — stop, do not commit, report what you observed, the zone lists of both sides at that moment and what you tried.)

## Goal

In side-by-side mode both editors show the corresponding lines at the same height: for every aligned block the side with fewer (or shorter) lines gets an empty zone, the zones follow diff, option and word-wrap changes, and scrolling either side scrolls the other (vertically and horizontally).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/components/diffEditorViewZones/diffEditorViewZones.ts` | 43–100 | state (`_originalTopPadding`, ignored zone sets) |
| same | 100–140 | `alignments` derived value and `createFakeLinesDiv` |
| same | 140–372 | the `viewZones` derived value: for every alignment the delta `origHeight - modHeight`, an `Empty` zone on the shorter side (`afterLineNumber: range.endLineNumberExclusive - 1`, `heightInPx: delta`, `showInHiddenAreas: true`, `suppressMouseDown: true`); the **inline-diff branches** of this block are not ported (they are P9-24) |
| same | 374–392 | horizontal scroll sync |
| same | 394–430 | vertical scroll sync (`origScrollTop = modScrollTop` when no top padding; the padding / moved-text part is not ported) |
| same | 611–645 | `getAdditionalLineHeights` (existing zones of other owners and wrapped line extra rows → `AdditionalLineHeight` lists for P9-15) |
| `src/vs/editor/browser/widget/diffEditor/utils.ts` | 19–58 | `joinCombine` (merge two sorted lists by key, combining equal keys) |
| `src/vs/editor/browser/widget/diffEditor/utils.ts` | 321–386 | `applyViewZones` (what the helper does around `changeViewZones`: remove the old, add the new, the `setIsUpdating` guard) |
| `src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts` | 355–400 | the re-entrancy guard between the two editors' zone updates (`isUpdatingViewZones`, "update view zones until stable") |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs` — create (`internal sealed class`, takes the two `LoomEditor`s, the `DiffEditorViewModel`, the options)
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.Layout.cs` — change: create / dispose `DiffEditorViewZones` with the mode
- `src/LoomEditor.View/DiffEditor/AdditionalLineHeights.cs` — create (the pure part of `getAdditionalLineHeights`: takes whitespaces `(afterLineNumber, height, id)`, a set of ignored ids, a `Func<int,int>` giving the view line → model line, the wrapped-line counts per model line, the line height; returns `IReadOnlyList<AdditionalLineHeight>`; includes `JoinCombine`)
- `tests/LoomEditor.View.Tests/DiffEditor/AdditionalLineHeightsTests.cs` — create

## Design decisions (already made — do not change)

- Only in side-by-side mode. `DiffEditorViewZones` recomputes when: the diff changes, `RenderSideBySideEffective`, an editor's content size or layout changes (`HostLayoutChanged`, `HostScrollChanged` with `ScrollHeightChanged`), `ViewZones.ZonesChanged` of **either** editor that is not caused by this class, the line mapping changes (word wrap). It coalesces several triggers in one UI-thread tick with `DispatcherQueue.TryEnqueue` (one pending recompute at a time).
- Pipeline: `additionalOriginal = AdditionalLineHeights.Compute(original.GetWhitespaces() minus own zones, …)`, same for modified → `alignments = LineRangeAlignments.Compute(diff.Mappings, additionalOriginal, additionalModified, original.LineHeight, modified.LineHeight, line => originalModel.GetLineMaxColumn(line), originalModel.LineCount, innerHunkAlignment: true)` → for each alignment `delta = OriginalHeightInPx - ModifiedHeightInPx`: `delta > 0` → an `Empty` zone **in the modified editor** after `ModifiedRange.EndLineNumberExclusive - 1` with height `delta`; `delta < 0` → in the original editor after `OriginalRange.EndLineNumberExclusive - 1` with height `-delta` (read lines 140–372 for the exact branch conditions and `afterLineNumber` values, including the "range empty" cases and `showInHiddenAreas`). Zones are `ViewZone { Kind = Empty, ShowInHiddenAreas = true, SuppressMouseDown = true }`.
- Applying: one `Change` call per editor that removes **all of this class's previous zones** and adds the new ones (a list of ids is kept; ids of foreign zones are never touched: `ignored = own ids`). The recompute is skipped while it is itself updating zones (`_isUpdatingZones` flag), and runs again once after the second editor's `ZonesChanged` (the heights depend on the other side's zones), at most 3 passes; if still unstable, stop and `Debug.WriteLine` once.
- Scroll sync: `HostScrollChanged` of one editor → `SetScrollPositionImmediate(left, top)` on the other with a re-entrancy flag (`_syncing`); `ScrollTop` is copied only when the values differ by ≥ 0.5 DIP. Word wrap on one side only is not supported in phase 9 (the diff editor sets the same `WordWrap` on both: expose none; wrapping stays off; documented in the card result and PORTING).
- Hidden areas (`HideUnchangedRegions`, P9-25) use `SetHiddenAreas`; zones with `ShowInHiddenAreas = true` stay valid there.
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [x] New (`AdditionalLineHeightsTests`): `Compute_NoZones_NoHeights`, `Compute_ForeignZone_ReturnsItsHeightAtModelLine`, `Compute_OwnZone_Ignored`, `Compute_AfterLineZero_LineNumberZero`, `Compute_WrappedLine_ExtraRows` (a model line with 3 view lines gives `2 * lineHeight`), `Compute_ZoneAndWrap_SameLine_Combined` (`JoinCombine` sums), `JoinCombine_*` (interleaved keys, equal keys combined, one list empty), `Compute_ZoneInHiddenView_MapsToModelLine`.
- [ ] Manual (in P9-31's plan): two files with an inserted block, a deleted block and a replaced block: the lines on both sides stay level while scrolling with the wheel and the scrollbar; dragging the horizontal scrollbar of one side moves the other; editing the modified text re-aligns after about 0.2 s without flicker; resizing the window keeps the alignment.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — result: Native AOT build OK (the Gallery has no diff demo yet: run the existing one to prove the AOT build still works; the diff editor itself is exercised in P9-31)
- [x] `docs/PORTING.md`: `diffEditorViewZones.ts` (side by side part), `utils.ts` (`joinCombine`, `applyViewZones`); "Behavior differences": no word wrap difference between sides, no top padding
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The inline view (P9-24), moved-block padding, `diffWordWrap`.

## Suggested commit message

`feat(winui): align the two sides of the diff editor with view zones and sync scrolling`

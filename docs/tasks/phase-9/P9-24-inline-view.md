# P9-24 — Inline view: deleted lines drawn inside the modified editor

- **Status:** todo
- **Depends on:** P9-23
- **Size:** M
- **Recommended model:** Opus (the renderer must draw lines of a **different model** (the original) inside zones of the modified editor, which means extracting the line drawing path of `TextViewRenderer` without changing how the editor draws today. On failure — the normal editor rendering changes, the Rendering tests regress, or the zone text is drawn with wrong tokens or offsets — stop, do not commit, report what you changed in `TextViewRenderer`, the failing tests and what you tried.)

## Goal

With `RenderSideBySide = false` (or when the space is limited), the diff editor shows only the modified editor; every deleted or replaced block of the original appears above the new lines as a red zone with the old text (syntax coloured, the deleted characters highlighted, an original line number and a `-` sign).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/components/diffEditorViewZones/diffEditorViewZones.ts` | 207–296 | the inline branch of the `viewZones` derived value (`!renderSideBySide`, `a.diff` present): the deleted code zone (`afterLineNumber: a.modifiedRange.startLineNumber - 1`, height = number of view lines × line height), `delete-sign` margin elements when `renderIndicators`, the `InlineDecoration`s of the inner changes (`diffDeleteDecoration` class, ranges relative to the diff's first original line) |
| same | 297–330 | the `else` branch (alignments without a diff, extra heights): `delta < 0` → an `Empty` zone on the modified side; `delta > 0` zones are for the hidden original (**not needed**: the original editor is not shown) |
| `src/vs/editor/browser/widget/diffEditor/components/diffEditorViewZones/renderLines.ts` | 23–93, 94–200 | `renderLines`, `LineSource`, `RenderOptions` (what is rendered per line: tokens, decorations, whitespace options; the heights) |
| `src/vs/editor/browser/widget/diffEditor/style.css` | 347–430 | `.inline-deleted-margin-view-zone`, `.line-delete`, `.char-delete`, `.inline-deleted-text`, `.gutter-delete` |

## Files to create or change

- `src/LoomEditor.View/DiffEditor/InlineViewZones.cs` — create (pure: alignments → zone specs)
- `src/LoomEditor.View/Layout/ViewZone.cs` — change: `DeletedLinesZoneData` (a `sealed record DeletedLinesZoneData(LineRange OriginalLines, Func<int, ViewLineData> GetOriginalLineData, IReadOnlyList<Range> DeletedRanges, bool RenderIndicators)`; `DeletedRanges` are in **original model coordinates**, only the part that lies in `OriginalLines`)
- `src/LoomEditor.Rendering/TextViewRenderer.ViewZones.cs` — change: paint `DeletedLines` zones; `TextViewRenderer.cs` — change **only as much as needed to reuse the line drawing code** (extract the "draw one line from a `ViewLineData` at a top" part into a method both the normal path and the zone use; behavior of the normal path must stay byte for byte the same: the existing Rendering tests are the proof)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs`, `LoomDiffEditor.Layout.cs` — change: in inline mode collapse the original part, create the inline zones, no scroll sync needed (one visible editor)
- `tests/LoomEditor.View.Tests/DiffEditor/InlineViewZonesTests.cs`, `tests/LoomEditor.Rendering.Tests/DeletedLinesZoneRenderingTests.cs` — create

## Design decisions (already made — do not change)

- Pure part: `static IReadOnlyList<ZoneSpec> InlineViewZones.Compute(IReadOnlyList<LineRangeAlignment> alignments, int lineHeight, bool renderIndicators, Func<int, ViewLineData> getOriginalLineData)` returning `ZoneSpec(ViewZoneKind Kind, int AfterLineNumber, int HeightInPx, object? Data, bool SuppressMouseDown)`; a deleted lines spec for every alignment with `Diff != null && !OriginalRange.IsEmpty`: `AfterLineNumber = ModifiedRange.StartLineNumber - 1`, `HeightInPx = OriginalRange.Length * lineHeight` (word wrap is off in phase 9: one view line per model line; Monaco's `viewLineCounts` is not needed), `Data = DeletedLinesZoneData(OriginalRange, …, deleted ranges = the inner changes' `OriginalRange` clipped to `OriginalRange` lines)`. An alignment without a diff (extra heights): `delta = OriginalHeightInPx - ModifiedHeightInPx`; `delta < 0` → `Empty` zone on the modified side after `ModifiedRange.EndLineNumberExclusive - 1`, height `-delta`; `delta >= 0` → nothing.
  `SuppressMouseDown = true` for the deleted lines zone (Monaco: `false` — deleted text is selectable there; **behavior difference**, listed in `PORTING.md` and in the manual plan).
- Painting a `DeletedLines` zone, for each of its lines `i` (original line `OriginalLines.Start + i`): the full-width background `diffEditor.removedLineBackground` (the `line-delete` style row of P9-16), the line drawn with the same code as normal lines from `GetOriginalLineData(line)` (tokens, tab size, font from the host editor: the zone is drawn with the **modified** editor's measurer and theme), the deleted character ranges (`char-delete` style: `diffEditor.removedTextBackground`) as rectangles behind the text, and in the margin: the `gutter-delete` background, the **original line number** right aligned in the line number column with the editor's line number color, and a `−` sign (`GutterIcon.Minus`, P9-16) when `RenderIndicators`. Monaco's inline margin shows original line numbers as well (`inline-deleted-margin-view-zone`).
  A line that is not available (`GetOriginalLineData` throws or the line is beyond the model after an edit: stale diff, ADR 0012 decision 4) is skipped silently.
- `LoomDiffEditor` builds `GetOriginalLineData` from the hidden original editor: `Original` keeps its `TextModel` and syntax highlighting while collapsed. Use the model's tokens through the same path `ModelLineProjection.GetViewLineData` uses (read `ViewLineData`, `ModelLineProjection.cs`, `ViewModel`), **by model line**, ignoring the original editor's hidden areas (when `HideUnchangedRegions` hides lines of the original, deleted lines are never in them, but do not rely on view line numbers).
- Mode switch: `RenderSideBySideEffective` flips → remove the zones of the other mode, `Original` part `Visibility.Collapsed` (it keeps working: it must still tokenize), `Modified` takes the full width, the sash is hidden; `Original`'s overview / zones are untouched. Switching back re-creates the aligned zones (P9-23).
- `DeletedRanges` for stale diffs are clipped to the original model's line lengths when drawing (never throw).
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [ ] New (`InlineViewZonesTests`): `Compute_NoAlignments_Empty`, `Compute_Deletion_OneDeletedLinesZoneBeforeFollowingLine` (`[3,5)->[3,3)` → after line 2, height `2*lh`), `Compute_Replacement_DeletedZoneAboveModifiedRange`, `Compute_Insertion_NoZone` (original range empty), `Compute_InnerChanges_BecomeDeletedRangesClippedToTheBlock`, `Compute_NoDiffAlignment_NegativeDelta_EmptyZone`, `Compute_NoDiffAlignment_PositiveDelta_Nothing`, `Compute_ZoneSuppressesMouseDown`.
- [ ] New (`DeletedLinesZoneRenderingTests`, recording surface): `Draw_DeletedZone_DrawsBackgroundAndOneLineLayoutPerLine`, `Draw_DeletedRanges_DrawBackgroundsBehindText`, `Draw_OriginalLineNumbersInMargin`, `Draw_RenderIndicators_DrawsMinusSign`, `Draw_LineMissing_Skipped`, `Draw_NormalEditorRendering_Unchanged` (an existing text rendered before and after the extraction gives the same recorded calls: use the recording the neighbouring tests make).
- [ ] Manual (P9-31's plan): turn `RenderSideBySide` off; deleted blocks appear in red with old line numbers and the old text coloured like the original; shrinking the window below 900 DIPs switches to inline; growing it switches back.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (all earlier Rendering tests unchanged)
- [ ] `docs/PORTING.md`: `renderLines.ts`, `diffEditorViewZones.ts` (inline part); "Behavior differences": deleted text not selectable, no wrapping inside zones, original line numbers drawn by the renderer
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Selecting / copying deleted text, the margin menu of deleted code (`inlineDiffDeletedCodeMargin.ts`: revert / copy actions), true inline view, wrapping of deleted lines.

## Suggested commit message

`feat: add the inline view of the diff editor with deleted lines zones`

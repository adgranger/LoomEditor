# P9-15 — Line range alignment: how much empty space each side needs

- **Status:** done
- **Depends on:** P9-14
- **Size:** M

## Goal

A pure function turns a diff into a list of aligned blocks (`LineRangeAlignment`: an original range, a modified range and the height in pixels each needs), so that the side with fewer lines gets empty space and both sides stay level. This is the heart of the side-by-side view.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/components/diffEditorViewZones/diffEditorViewZones.ts` | 473–609 | `computeRangeAlignment` (the `handleAlignmentsOutsideOfDiffs` and `emitAlignment` closures, the `innerHunkAlignment` branch) |
| same | 611–645 | `getAdditionalLineHeights` (**not ported here**: the caller passes the lists; P9-23 builds them from the editors) |
| same | 647–659 | `allowsTrueInlineDiffRendering`, `rangeIsSingleLine` (the second is needed here; the first is not ported: no true inline view) |
| `src/vs/base/common/arrays.ts` | 734–end of `ArrayQueue` | `ArrayQueue<T>` (`peek`, `dequeue`, `takeWhile`, `takeFromEndWhile`, `removeFirst`...) |

## Files to create or change

- `src/LoomEditor.View/DiffEditor/LineRangeAlignment.cs` — create (the record and the static `LineRangeAlignments.Compute`)
- `src/LoomEditor.View/DiffEditor/ArrayQueue.cs` — create (internal generic helper; if a similar queue already exists in Core, reuse it: `grep -rn "class ArrayQueue" src`)
- `tests/LoomEditor.View.Tests/DiffEditor/LineRangeAlignmentTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed record LineRangeAlignment(LineRange OriginalRange, LineRange ModifiedRange, int OriginalHeightInPx, int ModifiedHeightInPx, LineRangeMapping? Diff)`.
- `internal readonly record struct AdditionalLineHeight(int LineNumber, int HeightInPx)` — "this model line has `HeightInPx` extra pixels on top of one line height" (an existing zone or the extra rows of a wrapped line).
- `static IReadOnlyList<LineRangeAlignment> Compute(IReadOnlyList<DiffMapping> diffs, IReadOnlyList<AdditionalLineHeight> originalExtra, IReadOnlyList<AdditionalLineHeight> modifiedExtra, int originalLineHeight, int modifiedLineHeight, Func<int, int> originalLineMaxColumn, int originalLineCount, bool innerHunkAlignment)`:
  the original line count and `originalLineMaxColumn(line)` replace `originalModel.getLineCount()` / `getLineMaxColumn` (the function stays free of models, ADR 0012). Heights are `int` pixels (the layout's whitespace heights are `int`).
- `Number.MAX_VALUE` becomes `int.MaxValue`; the comparison `distOrig < distNext` uses `long` or `double` where the sum could overflow (`origNext.lineNumber - lastOriginalLineNumber` cannot, but keep both sides `long`).
- `LineRange.OfLength(line, 1)` for the single-line alignments outside diffs, `LineRange(a, b)` otherwise (the constructor of P9-01 throws when start > end: the `emitAlignment` guard `origLineNumberExclusive < lastOrigLineNumber …` already returns before that).
- Port the closures as **local functions** capturing the mutable locals (`first`, `lastOrigLineNumber`, …) exactly like the TypeScript; do not restructure.
- `ArrayQueue<T>`: a class over an `IReadOnlyList<T>` with `Peek()`, `Dequeue()`, `TakeWhile(Func<T,bool>)` returning `List<T>?` (null when nothing taken: Monaco returns `undefined`), `TakeFromEndWhile`, `RemoveFirst`: read the TypeScript for the exact semantics (it advances indices; it does not copy).
- `<remarks>Threading: pure; stateless.</remarks>`

## Tests

- [x] New (expected alignments computed by hand): `Compute_NoDiffs_NoAlignments`, `Compute_NoDiffsButExtraHeights_AlignsTheTallerSide` (an original zone of 20 px after line 3 and none on the modified side → `[3,4)` vs `[3,4)` with heights `lh + 20` and `lh`), `Compute_Insertion_ModifiedLonger` (diff `[2,2)->[2,5)`: one alignment `[2,2)` vs `[2,5)`, heights `0` and `3*lh`), `Compute_Deletion`, `Compute_Replacement_SameLineCount`, `Compute_Replacement_DifferentLineCount_ExtraSpaceOnShorterSide` (heights `2*lh` vs `5*lh`), `Compute_InnerHunkAlignment_SplitsAtUnchangedTextOnTheLine` (a diff with an inner change that starts at column > 1 on both sides emits an extra alignment at that line), `Compute_InnerHunkAlignment_Off_SingleAlignmentPerDiff`, `Compute_ExtraHeightInsideDiff_AddedToTheRightBlock`, `Compute_ExtraHeightsOnBothSidesAtSameDistance_Paired`, `Compute_DiffsSortedAndNonOverlapping_RangesAreContiguous` (random diffs; the alignments' original ranges never overlap, seed in the message), `ArrayQueue_TakeWhile_Peek_Dequeue`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated: `computeRangeAlignment` (`diffEditorViewZones.ts`), `ArrayQueue`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Creating zones from the alignments (P9-23), the inline view (P9-24).

## Suggested commit message

`feat(view): add the diff line range alignment`

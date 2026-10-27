# P9-13 — `UnchangedRegion`: which unchanged lines are hidden

- **Status:** todo
- **Depends on:** P9-12
- **Size:** M

## Goal

The model of "hide unchanged regions" exists: the regions between changes that can be collapsed (computed from a diff with the minimum line count and the context lines), how many lines of each region are visible at its top and bottom, and the operations that show more.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/diffEditorViewModel.ts` | 458–493 | `UnchangedRegion.fromDiffs` |
| same | 494–530 | the constructor (clamps), `originalUnchangedRange`, `modifiedUnchangedRange`, `isDragged`, `shouldHideControls` |
| same | 531–567 | `setVisibleRanges` (uses `LineRangeSet.subtractFrom`, P9-01) |
| same | 568–643 | `getHiddenOriginalRange`, `getHiddenModifiedRange`, `setHiddenModifiedRange`, `getMaxVisibleLineCountTop`, `getMaxVisibleLineCountBottom`, `showMoreAbove`, `showMoreBelow`, `showAll`, `showModifiedLine`, `showOriginalLine`, `collapseAll`, `setState` |
| same | 645–649 | `RevealPreference` |

## Files to create or change

- `src/LoomEditor.View/DiffEditor/UnchangedRegion.cs`, `RevealPreference.cs` — create
- `tests/LoomEditor.View.Tests/DiffEditor/UnchangedRegionTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class UnchangedRegion` with `OriginalLineNumber`, `ModifiedLineNumber`, `LineCount` (readonly) and mutable `VisibleLineCountTop`, `VisibleLineCountBottom` (private setters); **no observables** (ADR 0012 decision 2): every mutating method ends with `RaiseChanged()` (`event EventHandler? Changed`, raised once per call and only when a value changed). Monaco's `ITransaction` parameters disappear. `IsDragged` is `UnchangedRegionDragSide? { Top, Bottom }` settable (raises `Changed`).
- `ShouldHideControls` = `VisibleLineCountTop + VisibleLineCountBottom == LineCount && IsDragged is null` (computed property).
- `static IReadOnlyList<UnchangedRegion> FromDiffs(IReadOnlyList<DetailedLineRangeMapping> changes, int originalLineCount, int modifiedLineCount, int minHiddenLineCount, int minContext)` uses `DetailedLineRangeMapping.Inverse` (P9-07).
- The constructor clamps as Monaco and does `Guard.Assert` (Monaco: `softAssert`) that the inputs were already in range.
- `SetVisibleRanges(IReadOnlyList<LineRangeMapping> visibleRanges)` returns `IReadOnlyList<UnchangedRegion>` (new regions for the hidden parts; `this` when nothing is hidden: it calls `ShowAll()` first, as Monaco).
- `RevealPreference { FromCloserSide, FromTop, FromBottom }`.
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [ ] New (expected values computed by hand from Monaco's formulas; each test names the member): `FromDiffs_NoChanges_WholeFileOneRegionHidden`, `FromDiffs_ChangeInTheMiddle_TwoRegionsWithContext` (30 lines, change at line 15, context 3, minimum 3 → regions `[1, 12)` minus context at the end only on one side: compute and assert both regions' original/modified start and count), `FromDiffs_SmallGap_NoRegion` (gap shorter than `2*context + min`), `FromDiffs_AtStartAtEnd_OneSidedContext`, `Constructor_ClampsVisibleCounts`, `GetHiddenRanges_AfterShowMoreAbove`, `ShowMoreAbove_Capped`, `ShowMoreBelow_Capped`, `ShowAll_NoHiddenLines_ControlsHidden`, `CollapseAll`, `ShowModifiedLine_FromCloserSide_Top`, `ShowModifiedLine_FromBottom`, `ShowOriginalLine_*`, `SetHiddenModifiedRange_*`, `SetVisibleRanges_SplitsInTwoRegions`, `SetVisibleRanges_AllVisible_ShowsAll`, `Changed_RaisedOncePerMutation`, `Changed_NotRaisedWhenNothingChanges`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated: `UnchangedRegion`, `RevealPreference`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Tracking regions through edits (P9-14), the widgets (P9-25).

## Suggested commit message

`feat(view): add UnchangedRegion for hiding unchanged lines`

# P6-57 — `StickyLineCandidateProvider`

- **Status:** todo
- **Depends on:** P6-56
- **Size:** S

## Goal

The candidate provider keeps the sticky model up to date (debounced on edits, immediately on hidden-area changes) and
answers which sticky lines intersect a range, skipping regions that are folded.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/contrib/stickyScroll/browser/stickyScrollProvider.ts`.

| Lines | What to port |
|---|---|
| 29–54 | `IStickyLineCandidateProvider` (informative) |
| 56–112 | the class, constructor (`_updateSoon`: 50 ms), `readConfiguration` (model change, hidden areas, content change listeners) |
| 117–159 | `getVersionId`, `updateStickyModelProvider`, `update`, `updateStickyModel` (the `isTooLargeForTokenization` guard) |
| 164–222 | `getCandidateStickyLinesIntersecting`, `getCandidateStickyLinesIntersectingFromStickyModel` |
| 227–250 | `filterHiddenRanges`, `updateIndex` |

## Files to create or change

- `src/LoomEditor.View/Features/StickyScroll/StickyLineCandidateProvider.cs` — create (header `stickyScrollProvider.ts`)
- `tests/LoomEditor.View.Tests/Features/StickyScroll/StickyLineCandidateProviderTests.cs` — create

## Design decisions (already made — do not change)

- Constructor `(TextModel model, ViewModel viewModel, IEditorScheduler scheduler, Func<EditorStickyScrollOptions> options)`; event
  `StickyScrollChanged` (Monaco `onDidChangeStickyScroll`).
- Listeners: `TextModel.RawContentChanged` → debounced update (`RunOnceScheduler` from `IEditorScheduler`, 50 ms); hidden areas changed
  (the outgoing `HiddenAreasChangedEvent` of `ViewModel.EventDispatcher`, P5-81) → immediate update. One model per editor: Monaco's
  `onDidChangeModel` path is the constructor. The document symbol listener is phase 8.
- `isTooLargeForTokenization` → the LoomEditor equivalent on `TextModel` (search `IsTooLargeForTokenization`); if it does not exist,
  skip the guard with a comment.
- `binarySearch` → `List<int>.BinarySearch` (same negative encoding, see P6-48); keep `updateIndex` as written.
- `getLineHeightForPosition` → `ViewModel.ViewLayout` line height for that line (custom heights are not ported: the line height).
- `textModel.isValidRange(new Range(start, 1, end, 1))` → `TextModel.IsValidRange`.
- `getHiddenAreas()` → `ViewModel.GetHiddenAreas()`.

## Tests

Port the **scenario** of `Testing the function getCandidateStickyLinesIntersecting`
(`src/vs/editor/contrib/stickyScroll/test/browser/stickyScroll.test.ts` 43–58 text, 132–159) with the indentation model instead of the
outline model — the expected candidates differ, so these are **new** tests, written after reading the upstream one:

- [ ] `GetCandidates_FirstBlock` (an indented text whose first block spans lines 1–3)
- [ ] `GetCandidates_NestedBlocks_HaveIncreasingTops` (top 0, then one line height)
- [ ] `GetCandidates_FoldedRegion_IsFilteredOut` (mirrors `issue #156268`)
- [ ] `ContentChange_UpdatesAfterTheDebounce` (fake scheduler)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `stickyScrollProvider.ts` row; tests table: `stickyScroll.test.ts` 0 / 4 (outline model, phase 8)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The controller and the feature (P6-58).

## Suggested commit message

`feat(view): provide the sticky line candidates`

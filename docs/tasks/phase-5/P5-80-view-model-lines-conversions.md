# P5-80 — `ViewModelLinesFromProjectedModel`: coordinate conversions and the real `CoordinatesConverter`

- **Status:** todo
- **Depends on:** P5-79
- **Size:** M

## Goal

`ViewModelLinesFromProjectedModel` converts positions and ranges between model and view lines (skipping hidden lines
as Monaco does), validates view positions, normalizes positions, reads decorations of a view range across hidden lines,
and creates Monaco's `CoordinatesConverter`, which replaces `IdentityCoordinatesConverter` in P5-81.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `common/viewModel/viewModelLines.ts`.

| Lines | What to port |
|---|---|
| 122–124 | `createCoordinatesConverter` |
| 811–932 | `validateViewPosition`, `validateViewRange`, `convertViewPositionToModelPosition`, `convertViewRangeToModelRange`, `convertModelPositionToViewPosition` (with `affinity`, `allowZeroLineNumber`, `belowHiddenRanges`), `convertModelRangeToViewRange`, `getViewLineNumberOfModelPosition` |
| 934–1000 | `getDecorationsInRange` (the hidden-lines path: one model query per visible run, sorted, duplicates removed) |
| 1007–1023 | `normalizePosition`, `getLineIndentColumn` |
| 1084–1130 | `class CoordinatesConverter` |

Tests: `test/browser/viewModel/modelLineProjection.test.ts` 100–135 (`withSplitLinesCollection(text, …)`), 136–208
(`'Invalid line numbers'`), 210–270 (`'issue #3662'`).

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.Conversions.cs` — create (partial)
- `src/LoomEditor.View/Layout/CoordinatesConverter.cs` — create
- `tests/LoomEditor.View.Tests/Layout/ViewModelLinesConversionTests.cs` — create

## Design decisions (already made — do not change)

- Monaco's two-number signatures stay (`ConvertViewPositionToModelPosition(int viewLineNumber, int viewColumn)`,
  `ConvertModelPositionToViewPosition(int modelLineNumber, int modelColumn, PositionAffinity affinity = PositionAffinity.None, bool allowZeroLineNumber = false, bool belowHiddenRanges = false)`);
  `CoordinatesConverter` implements the existing `ICoordinatesConverter` by forwarding, like Monaco 1093–1129.
  `ICoordinatesConverter.ConvertModelPositionToViewPosition` takes a nullable affinity today: `null` maps to
  `PositionAffinity.None`, and `ConvertModelRangeToViewRange`'s `null` maps to `PositionAffinity.Left` (Monaco's defaults).
- `GetDecorationsInRange(Range viewRange, int ownerId, …)` takes the same filter parameters as step A's
  `TextModel.GetDecorationsInRange` and returns the same decoration type. The fast path is kept. Sorting: by
  `Range.CompareRangesUsingStarts`, then by decoration id (ordinal), with a stable sort.
- `IdentityCoordinatesConverter` stays until P5-81 removes it; nothing uses the new converter yet.

## Tests

`withSplitLinesCollection(text, …)` builds the class on `TestCodeEditor.CreateTextModel(text)` with the model's tab size.

- [ ] Port: `Editor ViewModel - SplitLinesCollection` › `Invalid line numbers` — **without** the
      `getViewLinesIndentGuides` assertions (indent guides are phase 6: a `// Phase 6 cut point` comment in the test
      says so); the content, min / max column and `convertViewPositionToModelPosition` assertions are ported as they are
- [ ] Port: `Editor ViewModel - SplitLinesCollection` › `issue #3662`
- [ ] New: `ConvertModelPositionToViewPosition_InHiddenArea_GoesToTheLineAbove` (end of the previous visible line)
- [ ] New: `ConvertModelPositionToViewPosition_BelowHiddenRanges_GoesToTheLineBelow` (column 1)
- [ ] New: `ConvertModelPositionToViewPosition_FirstLinesHidden_AllowZeroLineNumber_ReturnsLineZero`
- [ ] New: `ValidateViewPosition_ViewPositionOfAnotherModelPosition_ConvertsTheExpectedPosition`
- [ ] New: `GetDecorationsInRange_AcrossHiddenLines_SkipsTheHiddenDecorationsOnce` (a decoration inside the hidden
      lines is not returned; one that spans two visible runs is returned once)
- [ ] New: `CoordinatesConverter_ForwardsToTheLines` (`ModelPositionIsVisible`, `GetModelLineViewLineCount`,
      `GetViewLineNumberOfModelPosition`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: rows `viewModelLines.ts` (conversions, P5-80) and `coordinatesConverter.ts`
      (`CoordinatesConverter`, P5-80); "Tests ported": `modelLineProjection.test.ts` so far 3 / 6
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model switch (P5-81). `'SplitLine'`, `'getViewLinesData - with wrapping'`, `'… with wrapping and injected
text'` (word wrap and injected text: phase 6).

## Suggested commit message

`feat(view): convert coordinates between model and view lines`

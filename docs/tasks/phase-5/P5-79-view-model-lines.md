# P5-79 — `ViewModelLinesFromProjectedModel`: view lines, hidden areas, model changes

- **Status:** done
- **Depends on:** P5-78
- **Size:** M

## Goal

`ViewModelLinesFromProjectedModel` exists without word wrap: it keeps one projection per model line, the prefix sum of
their view line counts, and the hidden areas (as model decorations, like Monaco). It answers the view line count and
the content, columns and data of a view line, and follows model edits. The coordinate conversions come in P5-80; the
view model uses the class in P5-81.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `common/viewModel/viewModelLines.ts`.

| Lines | What to port |
|---|---|
| 61–124 | fields and constructor: `model`, `tabSize`, `modelLineProjections`, `projectedModelLineLineCounts`, `hiddenAreasDecorationIds`, `_validModelVersionId`, `_editorId` (the wrapping fields are not ported); `dispose` |
| 126–170 | `_constructLines` (no line-breaks computer: every line gets `ModelLineProjection.Create(!isInHiddenArea)`) |
| 172–252 | `getHiddenAreas`, `setHiddenAreas` |
| 254–279 | `modelPositionIsVisible`, `getModelLineViewLineCount`, `setTabSize` |
| 331–446 | `onModelFlushed`, `onModelLinesDeleted`, `onModelLinesInserted`, `onModelLineChanged`, `acceptVersionId`, `_ensureAtLeastOneVisibleLine` |
| 448–461 | `getViewLineCount`, `_toValidViewLineNumber` |
| 484–506 | `getViewLineInfo`, `getMinColumnOfViewLine`, `getMaxColumnOfViewLine` |
| 743–809 | `getViewLineContent`, `getViewLineLength`, `getViewLineMinColumn`, `getViewLineMaxColumn`, `getViewLineContinuesWithWrappedLine`, `getViewLineData`, `getViewLinesData` |
| 1035–1074 | `normalizeLineRanges`, `ViewLineInfo` |

Tests: `test/browser/viewModel/modelLineProjection.test.ts` 274–441 (suite setup, the fixed-token tokenizer, the
`assert…RenderingData` helpers), 442–574 (`'getViewLinesData - no wrapping'`) and the suite's `withSplitLinesCollection` (948 and after).

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.cs` — create (partial; P5-80 adds `.Conversions.cs`)
- `src/LoomEditor.View/Layout/ViewLineInfo.cs` — create (`ViewLineInfo`, `normalizeLineRanges` as a static helper `LineRanges.Normalize`)
- `tests/LoomEditor.View.Tests/Layout/TestHelpers/FixedTokensTokenizationSupport.cs` — create (the suite's
  `tokenizeEncoded`: returns `_tokens[_lineIndex++]` with `value << MetadataConsts.FOREGROUND_OFFSET`)
- `tests/LoomEditor.View.Tests/Layout/ViewModelLinesTests.cs` — create

## Design decisions (already made — do not change)

- Monaco's class name is kept: `internal sealed partial class ViewModelLinesFromProjectedModel : IDisposable`.
  `IViewModelLines` and `ViewModelLinesFromModelAsIs` are not ported (one implementation).
- Constructor: `(int editorId, TextModel model, int tabSize)`. The wrapping parameters, `createLineBreaksComputer`,
  `setWrappingSettings`, `getViewLinesIndentGuides`, `getActiveIndentGuide`, `getViewLinesBracketGuides`,
  `getInjectedTextAt` are **phase 6 cut points** (one comment on the class lists them).
- The hidden areas are model decorations owned by `editorId`, with `ModelDecorationOptions.Empty` (step A:
  `TextModel.DeltaDecorations(ownerId, oldIds, newDecorations)` and `GetDecorationRange(id)`), exactly as Monaco.
  `Dispose` removes them. Ranges are sorted with `Range.CompareRangesUsingStarts` (stable sort, phase 3 trap 3).
- The model change methods take what the `ModelRawContentChangedEventArgs` gives (`int? versionId`, line numbers) and
  return **view** line ranges instead of view events: `OnModelLinesDeleted` → `ViewLineRange?`,
  `OnModelLinesInserted(int? versionId, int fromLineNumber, int toLineNumber, int count)` → `ViewLineRange?`,
  `OnModelLineChanged` → `(bool LineMappingChanged, ViewLineRange? Changed, ViewLineRange? Inserted, ViewLineRange? Deleted)`
  (`ViewLineRange` is the existing record of `ViewEvent.cs`). `!versionId` in Monaco means "null or 0".
- `projectedModelLineLineCounts` is the `ConstantTimePrefixSumComputer` of P5-78; `modelLineProjections` is a
  `List<IModelLineProjection>` (`splice` → `RemoveRange`, the `slice/concat` insert → `InsertRange`).
- `GetViewLinesData(int viewStartLineNumber, int viewEndLineNumber, bool[] needed)` returns `ViewLineData?[]`.
- `GetHiddenAreas()` returns `IReadOnlyList<Range>`; `SetHiddenAreas(IReadOnlyList<Range> ranges)` returns `bool`
  (line mapping changed) and validates the ranges with `model.ValidateRange` first.

## Tests

`ViewModelLinesTests` builds the class directly (Monaco's `withSplitLinesCollection` with `wordWrap: 'off'`).

- [x] Port: `SplitLinesCollection` › `getViewLinesData - no wrapping` (with `FixedTokensTokenizationSupport`
      registered for `modelModeTest1` and `ForceTokenization(lineCount)`; `assertAllMinimapLinesRenderingData` keeps
      Monaco's `break` after the first combination)
- [x] New: `SetHiddenAreas_SameRangesTwice_ReturnsFalse`
- [x] New: `SetHiddenAreas_EverythingHidden_RevealsEverything` (Monaco: "Cannot have everything be hidden")
- [x] New: `OnModelLinesInserted_AfterAHiddenLine_InsertsHiddenLines` (`isInHiddenArea` uses `fromLineNumber - 2`)
- [x] New: `OnModelLinesDeleted_StaleVersionId_IsIgnored`
- [x] New: `AcceptVersionId_AllLinesHidden_ShowsTheFirstLine`
- [x] New: `NormalizeLineRanges_MergesOverlappingAndTouchingRanges` (columns ignored, result at column 1)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: new row `common/viewModel/viewModelLines.ts` (🔶 no word wrap, no guides, P5-79)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Coordinate conversions and `getDecorationsInRange` (P5-80). Using the class in the view model (P5-81).

## Suggested commit message

`feat(view): port the view model lines with hidden areas (no word wrap)`

# P5-81 — The view model uses view lines: `SetHiddenAreas`, the real converter, the cursor in folded code

- **Status:** done
- **Depends on:** P5-80
- **Size:** M

## Goal

The `ViewModel` owns a `ViewModelLinesFromProjectedModel`: its line count, line contents, layout, viewport, tokens and
cursor work on **view lines**, and `SetHiddenAreas(ranges, source)` hides model lines, as in Monaco.
`IdentityCoordinatesConverter` is removed. The deferred `'Cursor move command - foldedLine unit'` tests pass.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `common/viewModel/viewModelImpl.ts`.

| Lines | What to port |
|---|---|
| 124 | `coordinatesConverter = this._lines.createCoordinatesConverter()` |
| 224–244 | `getModelVisibleRanges`, `visibleLinesStabilized`, `_handleVisibleLinesChanged` (the model visible ranges, not the view range) |
| 265–274, 1465–1479 | `_captureStableViewport`, `StableViewport` (see decisions) |
| 325–418 | `onDidChangeContentOrInjectedText`, the model-change part: `_lines.onModel…` first, then `viewLayout.onFlushed / onLinesDeleted / onLinesInserted` with the **view** lines they return, `acceptVersionId`, and the `hadModelLineChangeThatChangedLineMapping` branch (`cursor.onLineMappingChanged`, decorations `onLineMappingChanged`) |
| 513–527 | `onDidChangeTokens`: model line ranges → view line ranges |
| 541–556 | `onDidChangeOptions`: `_lines.setTabSize` → flush the layout and `cursor.onLineMappingChanged` |
| 571–572, 590–633 | `hiddenAreasModel`, `previousHiddenAreas`, `setHiddenAreas(ranges, source, forceUpdate)` |
| 649–701 | `getVisibleRanges`, `getHiddenAreas`, `_toModelVisibleRanges` |
| 792–829, 925–927 | `getLineCount`, `getLineContent`, `getLineLength`, `getLineMinColumn`, `getLineMaxColumn`, `getViewLineData` |
| 1288–1298 | `normalizePosition`, `getLineIndentColumn` through `_lines` |
| 1390–1460 | `HiddenAreasModel`, `mergeLineRangeArray`, `rangeArraysEqual` |
| `common/viewModelEventDispatcher.ts` 368 | the outgoing `HiddenAreasChangedEvent` |

Tests: `test/browser/viewModel/viewModelImpl.test.ts` (see below), `test/browser/viewModel/testViewModel.ts` 14–35,
`test/browser/controller/cursorMoveCommand.test.ts` 509–615 and its helpers `moveDownByFoldedLine` (675),
`moveUpByFoldedLine` (679).

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModel.cs` — change: create `_lines = new ViewModelLinesFromProjectedModel(EditorId, model, model.Options.TabSize)`
  before the layout (layout line count = `_lines.GetViewLineCount()`); `CoordinatesConverter = _lines.CreateCoordinatesConverter()`;
  `OnModelRawContentChanged` as Monaco 325–418; tab-size change in `OnModelOptionsChanged`; `Dispose` disposes `_lines`
- `src/LoomEditor.View/Layout/ViewModel.HiddenAreas.cs` — create (partial): `SetHiddenAreas`, `GetHiddenAreas`,
  `GetVisibleRanges`, `ToModelVisibleRanges`, `HiddenAreasModel`, `StableViewport`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: the `ICursorSimpleModel` members forward to `_lines`
  (`GetLineCount` → `GetViewLineCount`, `GetLineContent` → `GetViewLineContent`, …, `NormalizePosition`,
  `GetLineIndentColumn`); `GetHiddenAreas` is no longer `static`; new `GetViewSelections()` (cursor view states)
- `src/LoomEditor.View/Cursors/CursorsController.cs` — change: `GetViewSelections()` (Monaco: `getViewSelections`)
- `src/LoomEditor.View/Cursors/CursorMoveCommands.Simple.cs` — change: `viewModel.GetHiddenAreas()` (instance)
- `src/LoomEditor.View/Layout/ViewModel.Tokens.cs` — change: `GetViewLineTokens(viewLine)` = `_lines.GetViewLineData(viewLine).Tokens`;
  `SetVisibleLines` reports the **model** visible ranges (`getModelVisibleRanges`); `OnModelTokensChanged` converts ranges
- `src/LoomEditor.View/Layout/ViewportData.cs` — change: built from the view model (view line min / max columns,
  tokens through `ViewModel.GetViewLineTokens`), no direct `TextModel` access
- `src/LoomEditor.View/Events/OutgoingViewModelEvent.cs` — change: `HiddenAreasChangedEvent`
- `src/LoomEditor.View/Layout/ViewModelDecorations*.cs` (P5-07) — change: replace the `// Phase 5 cut point (hidden areas, step G)`
  identity query by `_lines.GetDecorationsInRange` (Monaco `viewModelDecorations.ts` 32); the view model calls
  `ViewModelDecorations.OnLineMappingChanged()` after every line mapping change
- `src/LoomEditor.View/Layout/IdentityCoordinatesConverter.cs`, `tests/LoomEditor.View.Tests/Layout/IdentityCoordinatesConverterTests.cs` — delete
- `tests/LoomEditor.View.Tests/Layout/ViewModelHiddenAreasTests.cs` — create
- `tests/LoomEditor.View.Tests/Commands/CursorMoveCommandTests.cs` — change: the foldedLine suite (or a new
  `CursorMoveCommandFoldedLineTests.cs` next to it); remove the "deferred" comment

## Design decisions (already made — do not change)

- **What is in view lines from now on:** every `ViewModel` line member of `ICursorSimpleModel`, `ViewLayout` (its
  line count and every `OnLines…` call), `ViewportData`, `GetCompletelyVisibleViewRange*`, `GetViewLineTokens`, the
  cursor view states. **What stays in model lines:** `ViewModel.Model`, cursor model states, `GetSelections()`,
  `IEditorContext.Selections`, the line-number digit count (Monaco: `setModelLineCount`), the attached view's visible
  ranges (model ranges, Monaco 224–244).
- `SetHiddenAreas(IReadOnlyList<Range> ranges, object? source = null, bool forceUpdate = false)`. Sources are compared
  with `ReferenceEquals` (`Dictionary<object, …>` with `ReferenceEqualityComparer`; `null` source → a private static
  key object). `GetMergedRanges` returns the same list instance when nothing changed, so the `===` test of Monaco 597
  is `ReferenceEquals`.
- `mergeLineRangeArray` is ported **with its known bug** (both indexes advance after a merge; Monaco's test
  `'hidden areas from multiple sources that overlap produce valid merged result'` documents it and
  `normalizeLineRanges` repairs the result). Do not fix it.
- **StableViewport** without Monaco's `_viewportStart` tracker (not ported): `_captureStableViewport` takes the first
  view line of `ViewLayout.GetLinesViewportData()` and `startLineDelta = scrollTop - GetVerticalOffsetForLineNumber(line)`,
  only when `scrollTop > 0`. Behavior difference documented in the XML doc and `docs/PORTING.md`.
- After a line mapping change, `SetHiddenAreas` raises (in this order, in one `EmitViewEvent` batch):
  `_cursor.OnLineMappingChanged(collector)`, the decorations' `OnLineMappingChanged`, `ViewLayout.OnFlushed(GetLineCount(), [])`,
  `ViewLayout.OnHeightMaybeChanged()`; then `HandleVisibleLinesChanged()` and the outgoing `HiddenAreasChangedEvent`.
  `ViewFlushedEvent`, `ViewLineMappingChangedEvent`, `ViewDecorationsChangedEvent` do not exist in LoomEditor: the
  control redraws on the outgoing event (P5-82). `_updateConfigurationViewLineCount` is not ported (no
  accessibility view line count).
- The hidden-area decorations use the editor id the view model already uses as decorations owner (step A,
  `ViewModel.EditorId`).
- `TestCodeEditor` is unchanged: hidden areas are set through `editor.ViewModel.SetHiddenAreas(...)`.

## Tests

`viewModelImpl.test.ts`, suite `ViewModel` (Monaco's `testViewModel` = `TestCodeEditor.WithTestCodeEditor`):

- [x] Port: `issue #21073: SplitLinesCollection: attempt to access a 'newer' model` (without `setViewport`: not ported, it is only a hint)
- [x] Port: `issue #44805: No visible lines via API call`
- [x] Port: `issue #44805: No visible lines via undoing`
- [x] Port: `issue #40926: Incorrect spacing when inserting new line after multiple folded blocks of code`
- [x] Port: suite `hidden areas must always leave at least one visible line` › `replacing the only visible line content does not make it hidden`,
      `deleting the only visible line when it is the last line`, `deleting the only visible line when it is in the middle`,
      `undo that removes the only visible line`, `deleting the only visible line between two hidden areas leaves all lines hidden`,
      `multiple visible lines deleted leaving only hidden lines`, `hidden areas from multiple sources that overlap produce valid merged result`,
      `tab size change with drifted hidden area decorations must not leave 0 visible lines`

`cursorMoveCommand.test.ts`, suite `Cursor move command - foldedLine unit`:

- [x] Port: `move down by foldedLine skips a fold below the cursor`, `move up by foldedLine skips a fold above the cursor`,
      `move down by foldedLine with count treats each fold as one step`, `move down by foldedLine skips a multi-line fold as one step`,
      `move down by foldedLine at last line stays at last line`, `move up by foldedLine at first line stays at first line`,
      `move down by foldedLine with count clamps to last visible line after fold`, `move up by foldedLine with count clamps to first visible line before fold`

New:

- [x] `SetHiddenAreas_UpdatesTheLayoutLineCount` (`ViewLayout` content height = 3 visible lines × line height)
- [x] `SetHiddenAreas_CursorInHiddenArea_MovesToTheLineAbove` (cursor view state; model selection unchanged)
- [x] `SetHiddenAreas_SameRanges_RaisesNoEvent`; `SetHiddenAreas_RaisesHiddenAreasChangedEvent`
- [x] `ModelTokensChanged_HiddenLines_RaisesViewLineRanges`
- [x] `VisibleLines_WithHiddenArea_ReportsModelRangesAroundIt`
- [x] Existing `ViewModelTests`, `ViewModelCursorTests`, `ViewModelTokensTests`, cursor tests: unchanged and green

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `viewModelImpl.ts` row (hidden areas, view lines, P5-81); `coordinatesConverter.ts` row
      (identity converter removed); "Tests ported": `cursorMoveCommand.test.ts` 55 / 55, `viewModelImpl.test.ts` 12 / 29
      (the rest: `getPlainTextToCopy` ×12 and `issue #22688` belong to the clipboard port of P3-36 and are counted
      there if ported; `issue #44805: …'newer' model` counts Monaco view events that LoomEditor does not emit;
      `view models react first to model changes` needs two editors with Monaco's listener order;
      `normalizePosition with multiple touching injected text` and `issue #193262` need injected text / word wrap:
      phase 6); behavior difference "StableViewport without `_viewportStart`"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The renderer, the hit testing, the control and the IME bounds read view lines in P5-82. Folding itself (P5-83…).

## Suggested commit message

`feat(view): hidden areas in the view model`

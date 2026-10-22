# P6-22 — Port the word wrap and injected text tests of the view model and the cursors

- **Status:** todo
- **Depends on:** P6-21
- **Size:** S

## Goal

The six upstream tests that phases 3 and 5 left for word wrap or injected text pass. Test card: production code changes
only to fix what a test reveals.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/test/browser/viewModel/viewModelImpl.test.ts` | 359–410 | `normalizePosition with multiple touching injected text` |
| same | 412–428 | `issue #193262: Incorrect implementation of modifyPosition` |
| `src/vs/editor/test/browser/viewModel/testViewModel.ts` | 15–35 | `testViewModel` (editor id 1, options) |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | 449–498 | `issue #144041: Cursor up/down works` |
| same | 500–560 (read to the end of the test) | `issue #140195: Cursor up/down makes progress` |
| `src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts` | 24–60 (read to the end of the test) | `issue #26393: Multiple cursors + Word wrap` |
| `src/vs/editor/test/browser/controller/columnSelectionPaste.test.ts` | 236–243 | `uses successive model lines when word wrap is enabled` |

## Files to create or change

- `tests/LoomEditor.View.Tests/TestHelpers/TestCodeEditor.cs` — change: `TestCodeEditorOptions` gets
  `EditorWrappingOptions? Wrapping = null` and an `int EditorId = 0`; the constructor passes
  `new EditorLayoutOptions { Wrapping = options.Wrapping ?? new() }` and the editor id to `ViewModel`
- the existing test classes that hold the other tests of each upstream file (find them with
  `grep -rln "Monaco: 'issue #40926" tests`, `… 'issue #144` …): `ViewModelHiddenAreasTests.cs` (or a new `ViewModelImplTests.cs`),
  the cursor test class with the `Editor Controller - Cursor` ports, `MultiCursorTests` (or the class with the `multicursor.test.ts`
  ports), `ColumnSelectionPasteTests.cs` — change: add the tests

## Design decisions (already made — do not change)

- `testViewModel(text, { wordWrap: 'wordWrapColumn', wordWrapColumn: 5 }, …)` → `TestCodeEditor.WithTestCodeEditor(lines, editor => …,
  new TestCodeEditorOptions(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 5 }, EditorId: 1))`.
- `wrappingIndent: 'indent'` → `WrappingIndent = WrappingIndent.Indent`.
- `viewModel.getCursorStates()[0].viewState.position.toString()` → the view position formatted `"(line,column)"`; reuse the
  formatting helper the cursor tests already use, or `Position.ToString()` if it has that format.
- `InsertCursorBelow` / `InsertCursorAbove` are the MultiCursor feature commands of P5 (`MultiCursorCommands`); call them the way
  the other ported `multicursor.test.ts` tests do.
- The column selection paste test uses the `AssertPaste` helper of `ColumnSelectionPasteTests` with the new wrapping options.

## Tests

- [ ] Port: `ViewModel` › `normalizePosition with multiple touching injected text`
- [ ] Port: `ViewModel` › `issue #193262: Incorrect implementation of modifyPosition`
- [ ] Port: `Editor Controller - Cursor` › `issue #144041: Cursor up/down works`
- [ ] Port: `Editor Controller - Cursor` › `issue #140195: Cursor up/down makes progress`
- [ ] Port: `Multicursor` › `issue #26393: Multiple cursors + Word wrap`
- [ ] Port: `Column selection paste` › `uses successive model lines when word wrap is enabled`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: update the rows of `viewModelImpl.test.ts` (14 / 29), `cursor.test.ts` (75 / 75), `multicursor.test.ts`
      (75 / 76), `columnSelectionPaste.test.ts` (45 / 57)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`getPlainTextToCopy` and the other clipboard tests (not ported, P3-36).

## Suggested commit message

`test(view): port the word wrap tests of the view model and cursors`

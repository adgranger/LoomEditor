# P5-68 — `FindModelBoundToEditorModel`: search, decorations, next / previous, select all

- **Status:** done
- **Depends on:** P5-67
- **Size:** M

## Goal

The find model reacts to the find state like Monaco's: it searches when the query or a toggle changes, decorates up to
19 999 matches, keeps "n of m" up to date, follows edits and cursor moves, moves to the next / previous match (with and
without loop, inside a search scope, with `^` / `$` line stops) and selects all matches. 27 tests of
`findModel.test.ts` pass, each on a plain buffer and on a 3-chunk buffer.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/find/browser/findModel.ts` | 84–139 | `FindModelBoundToEditorModel` constructor (cursor, content and state listeners; `_updateDecorationsScheduler`; `_startSearchingTimer`) |
| same | 141–265 | `dispose`, `_onStateChanged`, `_getSearchRange`, `research`, `_hasMatches`, `_cannotFind`, `_setCurrentFindMatch` |
| same | 267–472 | `_prevSearchPosition`, `_moveToPrevMatch`, `moveToPrevMatch`, `_nextSearchPosition`, `_moveToNextMatch`, `_getNextMatch`, `moveToNextMatch`, `_moveToMatch`, `moveToMatch` |
| same | 507–513 | `_findMatches` |
| same | 586–619 | `selectAllMatches`, `_executeEditorCommand` |
| `contrib/find/test/browser/findModel.test.ts` | 19–100 | `findTest`, `fromRange`, `_getFindState`, `assertFindState` |

## Files to create or change

- `src/LoomEditor.View/Features/Find/FindModelBoundToEditorModel.cs` — create (partial; this card: everything but replace)
- `src/LoomEditor.View/Features/Find/README.md` — update
- `tests/LoomEditor.View.Tests/Features/Find/FindModelNavigationTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/Find/TestHelpers/FindModelTestHelpers.cs` — create: `FindTest(bool chunked, Action<TestCodeEditor, ManualEditorScheduler>)`,
  `AssertFindState(viewModel, cursor, highlighted, findDecorations)`, `ChunkedSnapshot` (an `ITextSnapshot` returning the
  three chunks Monaco's `substr(0, 94)`, `substr(94, 101)`, `substr(195, 59)` — clamp the lengths to the text, `substr` does, given to `TextModel.FromSnapshot`)

## Design decisions (already made — do not change)

- `new FindModelBoundToEditorModel(ViewModel viewModel, FindReplaceState state, IEditorScheduler scheduler, EditorFindOptions? options = null)`,
  `internal sealed partial class : IDisposable`. Word separators: `viewModel.CursorConfig.WordSeparators`.
- Editor API → view model:
  - `onDidChangeCursorPosition` → `viewModel.EventDispatcher.OnEvent`, `CursorStateChangedEvent` whose `Reason` is
    `Explicit`, `Undo` or `Redo` → `_decorations.SetStartPosition(viewModel.GetPosition())`;
  - `onDidChangeModelContent` → `viewModel.Model.ContentChanged` (`IsFlush` → `_decorations.Reset()`);
  - `getSelection` / `setSelection(range)` → `viewModel.GetSelection()` / `viewModel.SetSelections("api", [new Selection(range.Start, range.End)])`;
    `setSelections` → `viewModel.SetSelections("api", …)`;
  - `revealRangeInCenterIfOutsideViewport(range, Smooth)` → `viewModel.RevealRange("api", true, viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(range), VerticalRevealType.CenterIfOutsideViewport, ScrollType.Smooth)`;
  - `pushUndoStop` → `viewModel.Model.PushStackElement()`; `executeCommand(source, cmd)` → `viewModel.ExecuteCommand(cmd, source)`;
  - `getOption(EditorOption.find).cursorMoveOnType` → `options.CursorMoveOnType`.
- **Timers** (ADR 0009 decision 10): `_updateDecorationsScheduler` is a `RunOnceScheduler` (step A, `Features/Scheduling/`)
  of 100 ms over `scheduler`. `_startSearchingTimer` (`TimeoutTimer`: `cancel()` then `setIfNotSet`) becomes a second
  `RunOnceScheduler` of `FindConstants.ResearchDelay` that is cancelled then scheduled; the event that triggered it is kept in
  a field and read when it runs. Both are disposed in `Dispose`.
- `isTooLargeForSyncing` → the existing `TextModel.IsTooLargeForSyncing`.
- `findFirstIdxMonotonousOrArrLen(ranges, pred)` (research, line 223) → a private binary search returning the first index
  where `Range.CompareUsingStarts(range, selection.Range) >= 0`, or the count.
- `newFindScope?: Range | Range[] | null` with `undefined` → a private overload: `Research(bool moveCursor)` reads
  `_decorations.GetFindScopes()`; `Research(bool moveCursor, IReadOnlyList<Range>? newFindScopes)` uses the argument.
- `isUsingLineStops` checks `searchString.Contains('^')` / `Contains('$')` like Monaco (lines 268–271, 362–365).
- `Position` with column 0 cannot exist (phase 3 README §3): if a port needs it, keep `int` locals.
- **Folded regions**: Monaco's find model does not unfold; the folding contribution unfolds when the selection lands in a
  folded range. That is the folding step's job (step G), not this card's.

## Tests

`findModel.test.ts`, suite `FindModel`. Each `findTest` becomes a `[Theory]` with `[InlineData(false)]` / `[InlineData(true)]`
(plain / chunked buffer), as Monaco runs the callback twice. `editor.setPosition` → `viewModel.SetSelections`;
`editor.trigger('mouse', CoreNavigationCommands.MoveTo.id, …)` → `CursorTestHelpers.MoveTo` (P3-27).
`_getFindState` reads `TextModel.GetDecorationsInRange(model.GetFullModelRange())` and keeps the class names
`currentFindMatch` / `findMatch`.

- [x] Port (27): `incremental find from beginning of file`, `find model removes its decorations`,
      `find model updates state matchesCount`, `find model reacts to position change`, `find model next`,
      `find model next stays in scope`, `multi-selection find model next stays in scope (overlap)`,
      `multi-selection find model next stays in scope`, `find model prev`, `find model prev stays in scope`,
      `find model next/prev with no matches`, `find model next/prev respects cursor position`, `find ^`, `find $`,
      `find next ^$`, `find .*`, `find next ^.*$`, `find prev ^.*$`, `find prev ^$`, `listens to model content changes`,
      `selectAllMatches`, `issue #14143 selectAllMatches should maintain primary cursor if feasible`,
      `issue #1914: NPE when there is only one find match`, `issue #27083. search scope works even if it is a single line`,
      `issue #3516: Control behavior of "Next" operations (not looping back to beginning)`,
      `issue #3516: Control behavior of "Next" operations (looping back to beginning)`,
      `issue #288515: Wrong current index in find widget if matches > 1000` (a plain `[Fact]`, as in Monaco)
- [x] New: `ContentChange_RunsResearchAfter100Ms` (ManualEditorScheduler), `Dispose_CancelsPendingResearch`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `contrib/find/browser/findModel.ts` 🔶 (navigation, P5-68); tests table `findModel.test.ts` 27 / 44 (replace: P5-69)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`replace`, `replaceAll` (P5-69); the controller (P5-70).

## Suggested commit message

`feat(view): port the find model (search and navigation)`

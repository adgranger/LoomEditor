# P3-20 — The view model owns the cursors (and the test editor)

- **Status:** todo
- **Depends on:** P3-19
- **Size:** M

## Goal

As in Monaco, the `ViewModel` creates and owns the `CursorsController`, is the cursor's "simple model", recreates the
`CursorConfiguration` when an input changes, forwards model changes to the cursor, and exposes the cursor API
(`GetSelections`, `SetSelections`, `SetCursorStates`, `ExecuteEdits`, `ExecuteCommand(s)`, `RevealPrimaryCursor`…)
wrapped in view-event collection. This card also creates the **test editor** (`TestCodeEditor`) that every ported
`cursor.test.ts` test uses from now on.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/viewModel/viewModelImpl.ts`. If a line is not exactly where given, search for the name.

| Lines | What to port |
|---|---|
| 88, 124–126 | create `cursorConfig`, `coordinatesConverter` (identity), `_cursor = new CursorsController(model, this, coordinatesConverter, cursorConfig)` |
| 244–250 | `setHasFocus` (cursor part; the focus view/outgoing events are not ported) |
| 313–316 | recreate `cursorConfig` and call `_cursor.updateConfiguration` when the configuration changes |
| 465–472 | `emitContentChangeEvent`: `_cursor.onModelContentChanged(eventsCollector, e)` inside `_emitViewEvent` |
| 1154–1262 | the cursor facade: `getPrimaryCursorState` … `revealRange` (see the list below) |
| 1269–1285 | `_withViewEventsCollector`, `_emitViewEvent`, `batchEvents` |
| 1288–1298 | `normalizePosition`, `getLineIndentColumn` (the view model is an `ICursorSimpleModel`) |

Test editor: `src/vs/editor/test/browser/testCodeEditor.ts` (`withTestCodeEditor`, 172–208) and
`src/vs/editor/test/browser/config/testConfiguration.ts` (outer size 100 × 100, the mock `FontInfo`, lines 20–51).

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — create (partial): the facade
- `src/LoomEditor.View/Layout/ViewModel.cs` — change: implements `ICursorSimpleModel`; creates the dispatcher, the
  converter, the configuration and the controller; forwards `RawContentChanged` to the cursor **after** the layout update
- `tests/LoomEditor.View.Tests/TestHelpers/TestCodeEditor.cs` — create
- `tests/LoomEditor.View.Tests/Layout/ViewModelCursorTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- New `ViewModel` members:
  - `ViewModelEventDispatcher EventDispatcher { get; }`, `ICoordinatesConverter CoordinatesConverter { get; }`,
    `CursorConfiguration CursorConfig { get; private set; }`, `EditorCursorOptions CursorOptions { get; }` +
    `void SetCursorOptions(EditorCursorOptions options)`.
  - `CursorConfig` is recreated (and passed to `_cursor.UpdateConfiguration`) whenever one of its inputs changes:
    `SetFontInfo`, `SetViewportSize` (layout height → page size), `SetCursorOptions`, and `Model.OptionsChanged` (P3-09).
  - Facade (Monaco names, PascalCase; the `source` parameters are `string?`): `GetPrimaryCursorState`, `GetLastAddedCursorIndex`,
    `GetCursorStates`, `SetCursorStates(string? source, CursorChangeReason reason, IReadOnlyList<PartialCursorState>? states)`,
    `GetCursorColumnSelectData`, `SetCursorColumnSelectData`, `GetPrevEditOperationType`, `SetPrevEditOperationType`,
    `GetSelection`, `GetSelections`, `GetPosition`, `SetSelections(string? source, IReadOnlyList<Selection> selections, CursorChangeReason reason = NotSet)`,
    `SaveCursorState`, `RestoreCursorState`, `ExecuteEdits`, `ExecuteCommand`, `ExecuteCommands`, `RevealAllCursors`,
    `RevealPrimaryCursor`, `RevealTopMostCursor`, `RevealBottomMostCursor`, `RevealRange`, `BatchEvents`, `SetHasFocus`.
    **Not in this card:** `StartComposition`, `EndComposition`, `Type`, `CompositionType`, `Paste`, `Cut`,
    `GetCursorAutoClosedCharacters` (added by P3-30, P3-33, P3-35, P3-36, P3-49).
  - `_executeCursorEdit` (lines 1202–1209): when `CursorConfig.ReadOnly`, emit a `ReadOnlyEditAttemptEvent` and return.
- `ViewModel` implements `ICursorSimpleModel` by forwarding to the model (identity mapping): `GetLineCount` → `Model.LineCount`, etc.
- Model changes: the existing `OnModelRawContentChanged` handler first updates `ViewLayout` (as today), then runs
  `_cursor.OnModelContentChanged(collector, e)` inside `_emitViewEvent` — this is Monaco's order
  (`onDidChangeContentOrInjectedText`, then `emitContentChangeEvent`).
- `Dispose` disposes the controller.
- `TestCodeEditor` (test helper; port of `withTestCodeEditor` for phase 3):
  ```csharp
  internal sealed class TestCodeEditor : IDisposable
  {
      public static void WithTestCodeEditor(string text, Action<TestCodeEditor> callback, TestCodeEditorOptions? options = null);
      public static void WithTestCodeEditor(string[] lines, Action<TestCodeEditor> callback, TestCodeEditorOptions? options = null); // joined with "\n"
      public static void WithTestCodeEditor(TextModel model, Action<TestCodeEditor> callback, TestCodeEditorOptions? options = null);
      public TextModel Model { get; }
      public ViewModel ViewModel { get; }
  }
  internal sealed record TestCodeEditorOptions(EditorCursorOptions? CursorOptions = null, TextModelOptions? ModelOptions = null, int OuterWidth = 100, int OuterHeight = 100, bool HasTextFocus = true);
  ```
  The view model is created with Monaco's test `FontInfo` (`new FontInfo(1, "mockFont", "normal", 14, 19, 1.5, true, 10, 20, true, 10, 10, 10, 10)`),
  `SetViewportSize(OuterWidth, OuterHeight)`, a scheduler that never runs (returns a no-op `IDisposable`) and
  `TimeProvider.System`. `HasTextFocus` (default true, like Monaco) calls `SetHasFocus(true)`.
  Monaco's `editor.executeEdits` / `editor.getSelection()` / `editor.setSelections()` are reached through
  `editor.ViewModel` in C#. Add `CreateTextModel(string text, TextModelOptions? options = null)` (Monaco's
  `createTextModel`, `detectIndentation: false`).

## Tests

- [ ] New: `WithTestCodeEditor("abc", e => …)`: the primary selection is `(1,1)`; `CursorConfig.PageSize` is 3
      (`max(1, floor(100 / 19) - 2)`)
- [ ] New: `SetSelections` with two selections → `GetSelections()` returns both; an outgoing `CursorStateChangedEvent`
      is raised once through `EventDispatcher.OnEvent`
- [ ] New: an edit through `Model.PushEditOperations` moves the cursor (markers) and the layout line count is updated first
- [ ] New: `Model.UpdateOptions(TabSize: 8)` → `CursorConfig.TabSize == 8`; `SetViewportSize(100, 400)` → page size 19
- [ ] New: with `ReadOnly = true` in the cursor options, `ExecuteCommands` raises `ReadOnlyEditAttemptEvent` and does nothing
- [ ] Existing `ViewModelTests` pass unchanged

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `viewModelImpl.ts` (cursor facade, P3-20)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`EditorContext`, the control and the renderer (P3-21).

## Suggested commit message

`feat(view): let the view model own the cursors`

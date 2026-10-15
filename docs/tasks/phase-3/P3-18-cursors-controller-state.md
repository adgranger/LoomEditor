# P3-18 — `CursorsController` (1/2): state, model changes, reveal, events

- **Status:** done
- **Depends on:** P3-17
- **Size:** M

## Goal

`CursorsController` is the cursor of one editor: it owns the `CursorCollection`, reacts to model changes (recover
from markers, flush, undo/redo selections), sets new cursor states, asks the view to reveal the cursors and emits the
cursor events. This card ports everything **except** editing (P3-19).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/cursor/cursor.ts`. If a function is not exactly at the given line, search for its name.

| Lines | What to port |
|---|---|
| 27–64 | fields, constructor, `dispose` |
| 66–84 | `updateConfiguration`, `onLineMappingChanged` |
| 86–120 | `setHasFocus`, `_validateAutoClosedActions` (inert in phase 3, see below), `getPrimaryCursorState`, `getLastAddedCursorIndex`, `getCursorStates` |
| 122–139 | `setStates` (including `multiCursorLimit` and `reachedMaxCursorCount`) |
| 141–163 | `setCursorColumnSelectData`, `revealAll`, `revealPrimary` |
| 165–228 | `saveState`, `restoreState` |
| 230–275 | `onModelContentChanged` |
| 277–324 | `getSelection`, `getTopMostViewPosition`, `getBottomMostViewPosition`, `getCursorColumnSelectData`, `getSelections`, `getPosition`, `setSelections`, `getPrevEditOperationType`, `setPrevEditOperationType` |
| 398–434 | `_interpretCommandResult`, `_emitStateChangedIfNecessary` |
| 652–680 | `class CursorModelState` |
| 682–748 | `class AutoClosedAction` — see below |
| `src/vs/editor/common/editorCommon.ts` 178–182 | `ICursorState` (for save/restore) |

## Files to create or change

- `src/LoomEditor.View/Cursors/CursorsController.cs` — create (`partial`; this card writes the state part)
- `src/LoomEditor.View/Cursors/CursorModelState.cs` — create
- `src/LoomEditor.View/Cursors/SavedCursorState.cs` — create (`ICursorState`: `InSelectionMode`, `SelectionStart`, `Position`)
- `tests/LoomEditor.View.Tests/Cursors/CursorsControllerStateTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `internal sealed partial class CursorsController : IDisposable`, constructor
  `(TextModel model, ICursorSimpleModel viewModel, ICoordinatesConverter coordinatesConverter, CursorConfiguration cursorConfig)`.
  Public `CursorContext Context { get; private set; }`.
- `source` parameters are `string?` (Monaco `string | null | undefined`). In `_emitStateChangedIfNecessary`,
  `source || 'keyboard'` becomes `string.IsNullOrEmpty(source) ? "keyboard" : source` (an empty string is falsy in JS).
- `onModelContentChanged` takes a `ModelRawContentChangedEventArgs` (C# has no `InternalModelContentChangeEvent`; there is
  no injected text, so the `ModelInjectedTextChangedEvent` branch is not ported). It uses `e.ResultingSelection` (P3-10),
  `e.IsUndoing`, `e.IsRedoing`, `e.ContainsEvent(RawContentChangedType.Flush)`, `e.VersionId`.
  **Undo/redo restore the selections only when the editor has the focus** (`_hasFocus`); otherwise the cursor reads its
  markers. Tests must call `SetHasFocus(true)` to check the undo selections.
- `AutoClosedAction` needs model decorations, which phase 3 does not have. Keep the `List<AutoClosedAction>` field
  and `_validateAutoClosedActions` with an empty `AutoClosedAction` class that has a `bool IsValid(...)` returning
  `false` and a comment `// Phase 4–5 cut point: auto-closed characters (cursor.ts 682–748)`. Nothing adds to the list
  in phase 3.
- `restoreState` uses JavaScript truthiness for missing fields (lines 200–215): a `null` or 0 line/column falls back to
  the defaults. Use `int?` in `SavedCursorState` and port the fallbacks explicitly (conventions §4.5, and §3 for 0).
- `revealAll` / `revealPrimary` emit a `ViewRevealRangeRequestEvent` (P3-17) on the collector. Nothing listens yet.
- `setStates`: when there are more states than `CursorConfig.MultiCursorLimit`, keep the first `MultiCursorLimit` and
  set `reachedMaxCursorCount`.
- Every method that takes a `ViewModelEventsCollector` in Monaco takes it in C# too.

## Tests

The Monaco tests that cover this file (`cursor.test.ts`: `cursor initialized`, `saveState & restoreState`,
`issue #4905…`) are ported later through the view model (P3-27). New tests here, with the `CursorTestContext` of P3-16:

- [x] New: `SetStates` with 2 states → `GetSelections()` returns them; one `CursorStateChangedEvent` and one
      `ViewCursorStateChangedEvent` are collected; setting the same states again emits nothing
- [x] New: `SetStates` with `MultiCursorLimit + 1` states keeps `MultiCursorLimit` cursors and the event has `ReachedMaxCursorCount == true`
      (use a configuration with `MultiCursorLimit = 3`)
- [x] New: model `SetValue` → one cursor at `(1,1)`, reason `ContentFlush`, `OldSelections == null`
- [x] New: an edit made by someone else (`TextModel.PushEditOperations`) before the cursor moves it (reason `RecoverFromMarkers`)
- [x] New: with focus, `Model.Undo()` sets the selections from `ResultingSelection` (reason `Undo`) and emits a reveal request;
      without focus, it recovers from markers
- [x] New: `SaveState` then `RestoreState` gives back the same selections

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `cursor.ts` (🔶 `CursorsController` state part, P3-18); behavior difference "auto-closed
      actions are not tracked in phase 3"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`executeEdits`, `executeCommands`, `type`, `paste`, `cut`, composition (P3-19).

## Suggested commit message

`feat(view): add CursorsController state and event handling`

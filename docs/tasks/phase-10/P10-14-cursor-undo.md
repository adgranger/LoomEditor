# P10-14 — Cursor undo and redo

- **Status:** done
- **Depends on:** P10-13
- **Size:** S

## Goal

`cursorUndo` (`Ctrl+U`) and `cursorRedo` restore the previous / next cursor positions (and scroll position) without touching the text.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/cursorUndo/browser/cursorUndo.ts` | 16–126 | `CursorState` (with `equals`), `StackElement`, `CursorUndoRedoController` (the two stacks, the 50 element bound, the reset on model change and on **content** change, `_isCursorUndoRedo` guard, `cursorUndo`, `cursorRedo`, `_applyState`) |
| `src/vs/editor/contrib/cursorUndo/browser/cursorUndo.ts` | 128–160 | the two actions: `cursorUndo` (`Ctrl+U`, when `textInputFocus`), `cursorRedo` (no key) |
| `src/vs/editor/contrib/cursorUndo/test/browser/cursorUndo.test.ts` | whole file | the tests |

## Files to create or change

- `src/LoomEditor.View/Features/CursorUndo/CursorUndoFeature.cs`, `CursorUndoCommands.cs`, `README.md` — create (header: ported from `cursorUndo.ts`); add to `EditorFeatures.Default` (**first** in the list: Monaco registers it `Eager` "because it needs to see every cursor event")
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: `Ctrl+U` → `cursorUndo` (when `TextInputFocus`)
- `tests/LoomEditor.View.Tests/Features/CursorUndo/CursorUndoFeatureTests.cs` — create

## Design decisions (already made — do not change)

- The feature listens to the editor's cursor selection change event (`EditorContext.SelectionChanged` or the `CursorStateChangedEvent` of the view model: use the one that carries the **old selections** and the **model version ids**; if the existing event lacks `OldSelections` / `OldModelVersionId`, add them to the event args in the code that raises it — a few lines — and say so in the commit body). It ignores the event when `OldSelections` is null, when the model version changed (`oldModelVersionId != modelVersionId`: a selection change caused by an edit) and while it is itself applying a state.
- Scroll: `StackElement` keeps `ScrollTop` and `ScrollLeft` as `double` from `ViewModel.ViewLayout` (`GetCurrentScrollTop` / `GetCurrentScrollLeft`) and `_applyState` sets selections then the scroll position (immediate, no animation).
- The stacks are `List<StackElement>` bounded at 50 (the oldest is dropped). Both are cleared on any content change and when the model is replaced (`context.ModelChanged` if it exists; otherwise only the content change: say so in the README).
- `CursorState.Equals` compares the selections one by one (`Selection.EqualsSelection` equivalent: start, end **and** direction).
- Commands: `cursorUndo`, `cursorRedo` registered by the feature; no-ops when the stack is empty.

## Tests

- [x] Port: `cursorUndo.test.ts`
- [x] New: `Undo_AfterTwoMoves_RestoresFirstPosition`, `Redo_AfterUndo_RestoresSecond`, `NewMove_AfterUndo_ClearsRedoStack`, `TypingText_ClearsBothStacks`, `SameSelectionTwice_NotPushedTwice`, `Stack_BoundedTo50`, `Undo_RestoresScrollPosition`, `Undo_WithEmptyStack_DoesNothing`, `SelectionChangeCausedByEdit_IsNotRecorded`, `Ctrl_U_ResolvesToCursorUndo`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `cursorUndo.ts` row (+ the event args change if any)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Undoing text, persisting the stacks across models.

## Suggested commit message

`feat(view): add cursor undo and redo`

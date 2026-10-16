# P3-37 — Undo and redo commands, read-only editor

- **Status:** done
- **Depends on:** P3-36
- **Size:** S

## Goal

Register the `undo` and `redo` commands (Ctrl+Z, Ctrl+Y come in P3-39). They undo the model and the cursor goes back
to where it was (through `ResultingSelection`, P3-10/P3-18). In a read-only editor they do nothing, and every edit
attempt raises `ReadOnlyEditAttemptEvent`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/coreCommands.ts` | 2096–2124 | `CoreEditingCommands.Undo`, `Redo` (refused when `readOnly`) |
| `src/vs/editor/browser/editorExtensions.ts` | 646–700 | the ids `undo`, `redo` (the keybindings are used in P3-39) |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | 2803 | `issue #44805: Should not be able to undo in readonly editor` |

## Files to create or change

- `src/LoomEditor.View/Commands/CoreEditingCommands.cs`, `CoreCommandRegistration.cs` — change: `undo`, `redo`
- `tests/LoomEditor.View.Tests/Commands/UndoRedoCommandTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `Undo.Run(ViewModel viewModel)`: if `viewModel.CursorConfig.ReadOnly`, return; otherwise `viewModel.Model.Undo()`.
  The cursor restores the selections from the raw event's `ResultingSelection` **when the editor has the focus**
  (P3-18); do not set the selections from the return value of `Undo()` (Monaco does not either). Same for `Redo`.
- The ids are Monaco's: `undo`, `redo`.

## Tests

- [x] Port `cursor.test.ts` › `issue #44805: Should not be able to undo in readonly editor` (2803)
- [x] New: type `"abc"` (source `"keyboard"`), move the cursor to `(1,1)`, `Commands.Execute("undo")` → the text is empty
      and the cursor is at `(1,1)`; `Commands.Execute("redo")` → `"abc"` and the cursor at `(1,4)`
- [x] New: with `ReadOnly = true`, `Type` and `deleteLeft` change nothing and raise `ReadOnlyEditAttemptEvent` once each

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `coreCommands.ts` (+ undo/redo, P3-37); tests table `cursor.test.ts` (+1)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Keybindings (P3-39); the control's `IsReadOnly` property (P3-41).

## Suggested commit message

`feat(view): add the undo and redo commands`

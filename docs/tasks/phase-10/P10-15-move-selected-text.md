# P10-15 — Move selected text one character left or right

- **Status:** todo
- **Depends on:** P9-33
- **Size:** S

## Goal

`editor.action.moveCarretLeftAction` and `editor.action.moveCarretRightAction` move each non-empty single-line selection one character to the left / right, swapping it with the neighbouring character. They have no default key (as in Monaco).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/caretOperations/browser/moveCaretCommand.ts` | 11–55 | `MoveCaretCommand` (both directions, the guards, `computeCursorState`) |
| `src/vs/editor/contrib/caretOperations/browser/caretOperations.ts` | 13–62 | `MoveCaretAction`, ids `editor.action.moveCarretLeftAction` / `editor.action.moveCarretRightAction` (Monaco's spelling "Carret" is the id: keep it), `pushUndoStop` before and after |
| `src/vs/editor/contrib/caretOperations/test/browser/moveCarretCommand.test.ts` | whole file | the tests |

## Files to create or change

- `src/LoomEditor.View/Features/CaretOperations/MoveCaretCommand.cs`, `CaretOperationsFeature.cs`, `CaretOperationsCommands.cs`, `README.md` — create (header: ported from the two files); add to `EditorFeatures.Default`
- `tests/LoomEditor.View.Tests/Features/CaretOperations/MoveCaretCommandTests.cs` — create

## Design decisions (already made — do not change)

- `MoveCaretCommand` is an `IEditCommand` of the existing command executor (look at `SortLinesCommand` / `CopyLinesCommand` in `Features/LinesOperations` for the interface and how an action runs a list of commands: `CommandExecutor` / `EditorContext.ExecuteCommands` or the equivalent; do the same, with a stack element pushed before and after).
- `"Carret"` is Monaco's spelling in the **ids**; everything else in C# uses `Caret` (class `MoveCaretCommand`).
- Read-only editors: the commands do nothing (follow the other editing features).
- The `transposeLetters` action of the same Monaco folder is **already** ported (`LinesOperationsActions.Transpose.cs`).

## Tests

- [ ] Port: `moveCarretCommand.test.ts` (names in `// Monaco:` comments)
- [ ] New: `Left_AtColumn1_DoesNothing`, `Right_AtLineEnd_DoesNothing`, `EmptySelection_DoesNothing`, `MultiLineSelection_DoesNothing`, `Left_MovesSelectionAndKeepsItSelected`, `MultiCursor_EachMovesIndependently`, `Undo_RestoresInOneStep`, `ReadOnly_DoesNothing`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `caretOperations.ts`, `moveCaretCommand.ts` rows
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

A default key, moving a selection across lines (that is `moveLinesDown`), drag and drop (P10-17).

## Suggested commit message

`feat(view): add the move caret left / right commands`

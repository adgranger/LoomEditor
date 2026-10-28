# CaretOperations

**Role:** adds `editor.action.moveCarretLeftAction` and `editor.action.moveCarretRightAction` ("Carret" is Monaco's spelling of the ids):
each non-empty single-line selection moves one character left / right, swapping with the neighbouring character. No default key.

**Monaco sources:** `src/vs/editor/contrib/caretOperations/browser/` (`moveCaretCommand.ts`, `caretOperations.ts`). `transposeLetters` is `LinesOperations`.

**Files:**
- `MoveCaretCommand.cs` — the edit command.
- `CaretOperationsFeature.cs` — registers the two commands (writable editors only, one undo step).
- `CaretOperationsCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/CaretOperations/`.

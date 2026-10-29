# CursorUndo

**Role:** adds `cursorUndo` (`Ctrl+U`) and `cursorRedo` (no key): restore the previous / next cursor positions and
scroll position without touching the text. Each selection change that is not caused by an edit pushes the old
selections on an undo stack (bounded at 50).

**Monaco source:** `src/vs/editor/contrib/cursorUndo/browser/cursorUndo.ts`.

**Files:**
- `CursorUndoFeature.cs` — the two stacks and the commands; listens to `CursorStateChangedEvent` of the view model.
- `CursorUndoCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/CursorUndo/`.

**Known limits:** the model of an editor never changes, so only a content change clears the stacks. The stacks are not
persisted.

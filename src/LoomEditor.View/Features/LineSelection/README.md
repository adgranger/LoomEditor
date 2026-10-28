# LineSelection

**Role:** adds `expandLineSelection` (`Ctrl+L`): selects the line of each cursor, then one more line each time it runs.

**Monaco source:** `src/vs/editor/contrib/lineSelection/browser/lineSelection.ts`; the move is `CursorMoveCommands.ExpandLineSelection`.

**Files:**
- `LineSelectionFeature.cs` — registers the command.
- `LineSelectionCommands.cs` — command id.

**Tests:** `tests/LoomEditor.View.Tests/Features/LineSelection/`.

**Known limits:** none.

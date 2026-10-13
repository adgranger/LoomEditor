# SelectAll

**Role:** adds the `editor.action.selectAll` command (Ctrl+A), which selects the whole document.

**Monaco source:** `src/vs/editor/common/cursor/cursorMoveCommands.ts` (`CursorMoveCommands.selectAll`),
registered in `src/vs/editor/browser/coreCommands.ts`.

**Files:**
- `SelectAllFeature.cs` — registers the command; `ComputeSelection` is the pure logic.
- `SelectAllCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/SelectAll/`.

**Known limits:** single cursor only (multi-cursor arrives in phase 3).

This feature is intentionally tiny: it is the reference example of docs/how-to/add-a-feature.md.

# MultiCursor

**Role:** adds the multi-cursor commands that do not need the Find feature: add cursor above/below, cursors at the end
of each selected line, cursors to the top/bottom, focus next/previous cursor (7 ids).

**Monaco sources:** `src/vs/editor/contrib/multicursor/browser/multicursor.ts`.

**Files:**
- `MultiCursorFeature.cs` — registers the commands.
- `MultiCursorCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/MultiCursor/`.

**Known limits:** the Find-based commands (`addSelectionToNextFindMatch` Ctrl+D, `moveSelectionToNextFindMatch`,
`selectHighlights`, `changeAll`, `MultiCursorSession`, `SelectionHighlighter`) are phase 5. Keybindings are P3-39;
screen reader announcements are not ported.

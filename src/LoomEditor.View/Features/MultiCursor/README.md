# MultiCursor

**Role:** the multi-cursor commands: add cursor above/below, cursors at the end of each selected line, cursors to the
top/bottom, focus next/previous cursor, and, when the Find feature is attached, the occurrence commands (Ctrl+D,
Ctrl+K Ctrl+D, Ctrl+Shift+L, Ctrl+F2 and their "previous" siblings).

**Monaco sources:** `src/vs/editor/contrib/multicursor/browser/multicursor.ts`.

**Files:**
- `MultiCursorFeature.cs` — registers the commands; creates the controller when `FindFeature` is attached.
- `MultiCursorCommands.cs` — command ids.
- `MultiCursorSession.cs` — what a run of Ctrl+D presses searches for and how (`MultiCursorSession`).
- `MultiCursorSelectionController.cs` — the session life cycle and the six Find-based actions.
- `MultiCursorOptions.cs` — `selectedTextMatchMode`.

**Tests:** `tests/LoomEditor.View.Tests/Features/MultiCursor/`.

**Known limits:** `SelectionHighlighter` is P5-77; menus and screen reader announcements are not ported.

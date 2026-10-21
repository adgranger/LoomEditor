# LinesOperations

**Role:** line commands: copy, move, sort, trim trailing whitespace, delete, join, transpose and case transforms.

**Monaco sources:** `src/vs/editor/contrib/linesOperations/browser/` (`copyLinesCommand.ts`, `sortLinesCommand.ts`, later `moveLinesCommand.ts`, `linesOperations.ts`)
and `src/vs/editor/common/commands/trimTrailingWhitespaceCommand.ts`.

**Files:**
- `MoveLinesCommand.cs`, `MoveLinesCommand.AutoIndent.cs` — move lines up or down with auto-indent.
- `CopyLinesCommand.cs`, `SortLinesCommand.cs` — the edit commands (the trim command is `Cursors/Commands/TrimTrailingWhitespaceCommand.cs`).
- `LinesOperationsActions.CopyMove.cs`, `LinesOperationsActions.Sort.cs` — what the actions do to the selections (undo stops, whole document for a single-line selection).
- `LinesOperationsFeature.cs` — registers the first eleven commands (copy, duplicate, move, sort, remove duplicates, reverse, trim); the others come in P5-58 to P5-60.
- `LinesOperationsCommands.cs` — all the command ids, already.

**Tests:** `tests/LoomEditor.View.Tests/Features/LinesOperations/`.

**Keys (Windows):** `Shift+Alt+↑/↓` copy lines, `Alt+↑/↓` move lines, `Ctrl+K Ctrl+X` trim trailing whitespace; the others have no key.

**Known limits:** no menus; delete, indent, insert, join, transpose and case commands are not registered yet. `SortLinesCommand` compares with the invariant culture, not the locale of the user.

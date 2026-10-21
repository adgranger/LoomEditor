# LinesOperations

**Role:** line commands: copy, move, sort, trim trailing whitespace, delete, join, transpose and case transforms.

**Monaco sources:** `src/vs/editor/contrib/linesOperations/browser/` (`copyLinesCommand.ts`, `sortLinesCommand.ts`, later `moveLinesCommand.ts`, `linesOperations.ts`)
and `src/vs/editor/common/commands/trimTrailingWhitespaceCommand.ts`.

**Files:**
- `MoveLinesCommand.cs`, `MoveLinesCommand.AutoIndent.cs` — move lines up or down with auto-indent.
- `CopyLinesCommand.cs`, `SortLinesCommand.cs` — the edit commands (the trim command is `Cursors/Commands/TrimTrailingWhitespaceCommand.cs`).
- `LinesOperationsActions.CopyMove.cs`, `.Sort.cs`, `.Lines.cs`, `.DeleteAll.cs`, `.Join.cs` — what the actions do to the selections (undo stops, whole document for a single-line selection).
- `LinesOperationsActions.Transpose.cs`, `LinesOperationsActions.Case.cs`, `CaseTransforms.cs` — transpose and the seven case transforms (invariant culture).
- `LinesOperationsFeature.cs` — registers all the commands (copy, duplicate, move, sort, remove duplicates, reverse, trim, delete, indent, outdent, insert line, delete all left / right, join, transpose, case transforms).
- `LinesOperationsCommands.cs` — all the command ids, already.

**Tests:** `tests/LoomEditor.View.Tests/Features/LinesOperations/`.

**Keys (Windows):** `Shift+Alt+↑/↓` copy lines, `Alt+↑/↓` move lines, `Ctrl+K Ctrl+X` trim trailing whitespace, `Ctrl+Shift+K` delete lines, `Ctrl+]` / `Ctrl+[` indent / outdent, `Ctrl+Enter` / `Ctrl+Shift+Enter` insert line below / above; the others have no key.

**Known limits:** no menus; Delete all left / right, join lines and transpose have no Windows key (the keys exist only on macOS). `SortLinesCommand` compares with the invariant culture, not the locale of the user.

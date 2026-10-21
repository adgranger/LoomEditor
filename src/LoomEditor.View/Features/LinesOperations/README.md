# LinesOperations

**Role:** line commands: copy, move, sort, trim trailing whitespace, delete, join, transpose and case transforms.

**Monaco sources:** `src/vs/editor/contrib/linesOperations/browser/` (`copyLinesCommand.ts`, `sortLinesCommand.ts`, later `moveLinesCommand.ts`, `linesOperations.ts`)
and `src/vs/editor/common/commands/trimTrailingWhitespaceCommand.ts`.

**Files:**
- `MoveLinesCommand.cs`, `MoveLinesCommand.AutoIndent.cs` — move lines up or down with auto-indent.
- `CopyLinesCommand.cs`, `SortLinesCommand.cs` — the edit commands (the trim command is `Cursors/Commands/TrimTrailingWhitespaceCommand.cs`).
- `LinesOperationsFeature.cs` — the feature (it registers nothing yet: the actions come in P5-57 and later).
- `LinesOperationsCommands.cs` — all the command ids, already.

**Tests:** `tests/LoomEditor.View.Tests/Features/LinesOperations/`.

**Known limits:** no actions yet. `SortLinesCommand` compares with the invariant culture, not the locale of the user.

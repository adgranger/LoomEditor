# WordOperations

**Role:** adds the word move commands (`cursorWordLeft`, `cursorWordEndRight`, the `…Select` and `…Accessibility…`
variants: 16 ids). Word deletes and word parts follow in P3-30.

**Monaco sources:** `src/vs/editor/contrib/wordOperations/browser/wordOperations.ts` (the commands) and
`src/vs/editor/common/cursor/cursorWordOperations.ts` (the logic, in `Cursors/WordOperations*.cs`).

**Files:**
- `WordOperationsFeature.cs` — registers the commands.
- `WordOperationsCommands.cs` — command ids.
- `MoveWordCommand.cs` — the command classes.

**Tests:** `tests/LoomEditor.View.Tests/Features/WordOperations/`.

**Known limits:** `Intl.Segmenter` word recognition (`wordSegmenterLocales`) is not available; keybindings are P3-39.

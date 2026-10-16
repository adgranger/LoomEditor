# WordPartOperations

**Role:** adds the word part commands: move (`cursorWordPartLeft`, `cursorWordPartRight`, and their `Select`
variants) and delete (`deleteWordPartLeft`, `deleteWordPartRight`) inside `camelCase`, `snake_case` and `kebab-case`
identifiers. Also registers the legacy ids `cursorWordPartStartLeft` and `cursorWordPartStartLeftSelect`.

**Monaco sources:** `src/vs/editor/contrib/wordPartOperations/browser/wordPartOperations.ts` (the commands) and
`src/vs/editor/common/cursor/cursorWordOperations.ts` (`WordPartOperations`, in `Cursors/WordPartOperations.cs`).
The commands reuse `MoveWordCommand` and `DeleteWordCommand` of the `WordOperations` feature.

**Files:**
- `WordPartOperationsFeature.cs` — registers the commands; the command classes.
- `WordPartOperationsCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/WordPartOperations/`.

**Known limits:** keybindings are P3-39.

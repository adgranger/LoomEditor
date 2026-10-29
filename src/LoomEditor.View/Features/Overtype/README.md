# Overtype

**Role:** adds `editor.action.toggleOvertype`: flips the input mode of the editor between insert and overtype.
In overtype mode a typed character replaces the character after the caret, a composition end overtypes what follows
it, and a paste overtypes when `OvertypeOnPaste` is true.

**Monaco source:** `src/vs/editor/common/inputMode.ts`, `commands/replaceCommand.ts`, `cursor/cursorTypeEditOperations.ts`.

**Files:**
- `OvertypeFeature.cs` — registers the command; the state is `EditorContext.InputMode` (`EditorInputMode`).
- `OvertypeCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Cursors/OvertypeModeTests.cs`.

**Known limits:** the mode is per editor (Monaco: global); no key and no caret shape yet (P10-12).

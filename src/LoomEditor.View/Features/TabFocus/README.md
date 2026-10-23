# TabFocus

**Role:** adds `editor.action.toggleTabFocusMode` (Ctrl+M): switches Tab / Shift+Tab between "insert a tab" and
"move the keyboard focus to the next control", and announces the change. Without it a keyboard-only user cannot leave the editor.

**Monaco source:** `src/vs/editor/contrib/toggleTabFocusMode/browser/toggleTabFocusMode.ts`, `src/vs/editor/browser/config/tabFocus.ts`.

**Files:**
- `TabFocusFeature.cs` — registers the command; the state is `EditorContext.TabMovesFocus`.
- `TabFocusCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/TabFocus/`.

**Known limits:** the state is per editor (Monaco: global); not persisted; no `editor.tabFocusMode` option.

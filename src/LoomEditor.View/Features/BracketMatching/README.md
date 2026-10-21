# BracketMatching

**Role:** highlights the bracket pair at (or, with `MatchBracketsMode.Always`, around) each empty selection 50 ms after
the cursor moves, and adds `editor.action.jumpToBracket` (Ctrl+Shift+\), `editor.action.selectToBracket` and
`editor.action.removeBrackets` (Ctrl+Alt+Backspace).

**Monaco sources:** `src/vs/editor/contrib/bracketMatching/browser/bracketMatching.ts` and `bracketMatching.css`.

**Files:**
- `BracketMatchingFeature.cs` — the controller: commands, decorations, scheduling.
- `BracketMatchingCommands.cs` — command ids.
- `MatchBracketsMode.cs` — the `matchBrackets` option (`MatchBrackets` on the feature; no public option on the control).

**Tests:** `tests/LoomEditor.View.Tests/Features/BracketMatching/`.

**Known limits:** the overview ruler marker and the Go menu entry are not ported. `Ctrl+Shift+\` uses the OEM_5 virtual
key, so on layouts where `\` is elsewhere the shortcut follows the key position of a US keyboard (VS Code dispatches on
the scan code). `onDidChangeModel` is not ported: the model of an editor never changes.

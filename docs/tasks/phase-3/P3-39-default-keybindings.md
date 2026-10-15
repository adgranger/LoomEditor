# P3-39 — Default keybindings and the keybinding resolver

- **Status:** todo
- **Depends on:** P3-38
- **Size:** M

## Goal

Create `LoomEditor.View/Commands/DefaultKeybindings.cs`: VS Code's default **Windows** shortcuts for every command
ported so far, with their `when` conditions and weights, and a small resolver that turns a key press plus the editor
state into a command id. It is UI-independent and fully testable; the control uses it in P3-41.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

Every keybinding comes from the `kbOpts` of a command registration. For Windows, use `win` when present, otherwise
`primary` and `secondary` (ignore `mac` and `linux`). The `when` of a keybinding is **`precondition AND kbExpr`**
(`src/vs/editor/browser/editorExtensions.ts`, search `registerKeybindingRule` / `kbOpts`).

| File | Lines | What |
|---|---|---|
| `src/vs/editor/browser/coreCommands.ts` | 36 | `CORE_WEIGHT = KeybindingWeight.EditorCore` |
| same | 475–588, 690–893, 985–1306, 1429–1557, 1775–1830, 1981–2094 | the core commands' `kbOpts` |
| same | 1940–1964 | `registerColumnSelection` (Shift+arrows when `columnSelection` is on, weight `CORE_WEIGHT + 1`) |
| `src/vs/editor/browser/editorExtensions.ts` | 646–708 | `undo`, `redo`, `editor.action.selectAll` (weight `EditorCore`) |
| `src/vs/editor/contrib/wordOperations/browser/wordOperations.ts` | 101–469 | word moves and deletes |
| `src/vs/editor/contrib/multicursor/browser/multicursor.ts` | 46–202 | insert cursor above/below/at end of lines |
| `src/vs/editor/contrib/clipboard/browser/clipboard.ts` | 41–150 | cut / copy / paste keys (the commands are registered in P3-47) |
| `src/vs/platform/keybinding/common/keybindingsRegistry.ts` | search `enum KeybindingWeight` | `EditorCore = 0`, `EditorContrib = 100`, `WorkbenchContrib = 200`… |
| `src/vs/platform/keybinding/common/keybindingResolver.ts` | search `_findCommand` | resolution: the **last** matching rule after sorting by weight wins |

## The table to write (check every row against the source before writing it)

| Keys (Windows) | Command id | when (precondition AND kbExpr) | Weight |
|---|---|---|---|
| ← / Shift+← | `cursorLeft` / `cursorLeftSelect` | textInputFocus | Core |
| → / Shift+→ | `cursorRight` / `cursorRightSelect` | textInputFocus | Core |
| ↑ / Shift+↑ | `cursorUp` / `cursorUpSelect` | textInputFocus | Core |
| ↓ / Shift+↓ | `cursorDown` / `cursorDownSelect` | textInputFocus | Core |
| PgUp / Shift+PgUp | `cursorPageUp` / `cursorPageUpSelect` | textInputFocus | Core |
| PgDn / Shift+PgDn | `cursorPageDown` / `cursorPageDownSelect` | textInputFocus | Core |
| Home / Shift+Home | `cursorHome` / `cursorHomeSelect` | textInputFocus | Core |
| End / Shift+End | `cursorEnd` / `cursorEndSelect` | textInputFocus | Core |
| Ctrl+Home / Ctrl+Shift+Home | `cursorTop` / `cursorTopSelect` | textInputFocus | Core |
| Ctrl+End / Ctrl+Shift+End | `cursorBottom` / `cursorBottomSelect` | textInputFocus | Core |
| Ctrl+Shift+Alt+←/→/↑/↓/PgUp/PgDn | `cursorColumnSelectLeft/Right/Up/Down/PageUp/PageDown` | textInputFocus | Core |
| Shift+←/→/↑/↓/PgUp/PgDn | the same six column-select ids | textInputFocus AND columnSelection | Core + 1 |
| Ctrl+↑ / Ctrl+↓ | `scrollLineUp` / `scrollLineDown` | textInputFocus | Core |
| Alt+PgUp / Alt+PgDn | `scrollPageUp` / `scrollPageDown` | textInputFocus | Core |
| Esc, Shift+Esc | `cancelSelection` | hasNonEmptySelection AND textInputFocus | Core |
| Esc, Shift+Esc | `removeSecondaryCursors` | hasMultipleSelections AND textInputFocus | Core + 1 |
| Ctrl+A | `editor.action.selectAll` | (none) | Core |
| Tab | `tab` | writable AND editorTextFocus AND tabDoesNotMoveFocus | Core |
| Shift+Tab | `outdent` | writable AND editorTextFocus AND tabDoesNotMoveFocus | Core |
| Backspace, Shift+Backspace | `deleteLeft` | textInputFocus | Core |
| Delete | `deleteRight` | textInputFocus | Core |
| Ctrl+Z | `undo` | (none) | Core |
| Ctrl+Y, Ctrl+Shift+Z | `redo` | (none) | Core |
| Ctrl+← / Ctrl+Shift+← | `cursorWordLeft` / `cursorWordLeftSelect` | textInputFocus AND NOT (accessibilityModeEnabled AND isWindows) | Contrib |
| Ctrl+→ / Ctrl+Shift+→ | `cursorWordEndRight` / `cursorWordEndRightSelect` | same | Contrib |
| Ctrl+Backspace | `deleteWordLeft` | writable AND textInputFocus | Contrib |
| Ctrl+Delete | `deleteWordRight` | writable AND textInputFocus | Contrib |
| Ctrl+Alt+↑ / Ctrl+Alt+↓ | `editor.action.insertCursorAbove` / `…Below` | editorTextFocus | Contrib |
| Shift+Alt+I | `editor.action.insertCursorAtEndOfEachLineSelected` | editorTextFocus | Contrib |
| Ctrl+X, Shift+Delete | `editor.action.clipboardCutAction` | see clipboard.ts | Contrib |
| Ctrl+C, Ctrl+Insert | `editor.action.clipboardCopyAction` | see clipboard.ts | Contrib |
| Ctrl+V, Shift+Insert | `editor.action.clipboardPasteAction` | see clipboard.ts | Contrib |

If a row of this table disagrees with the source, **the source wins**: fix the row and say so in the commit body.

## Files to create or change

- `src/LoomEditor.View/Commands/KeyCode.cs` — create: the keys used above (letters A–Z, arrows, Home, End, PageUp,
  PageDown, Escape, Tab, Backspace, Delete, Insert, Enter, F1–F12)
- `src/LoomEditor.View/Commands/KeyModifiers.cs` — create: `[Flags] Ctrl = 1, Shift = 2, Alt = 4, Meta = 8`
- `src/LoomEditor.View/Commands/Keybinding.cs` — create: `readonly record struct Keybinding(KeyModifiers Modifiers, KeyCode Key)`
- `src/LoomEditor.View/Commands/KeybindingContext.cs` — create: the context keys
- `src/LoomEditor.View/Commands/KeybindingRule.cs`, `KeybindingWeight.cs` — create
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — create: `static IReadOnlyList<KeybindingRule> Windows { get; }`
- `src/LoomEditor.View/Commands/KeybindingResolver.cs` — create
- `src/LoomEditor.View/Commands/README.md` — update
- `tests/LoomEditor.View.Tests/Commands/KeybindingResolverTests.cs`, `DefaultKeybindingsTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first. Everything is `internal` (ADR 0006: no public keybinding API in phase 3).
- `KeybindingContext` is an `internal readonly record struct` with `bool TextInputFocus, EditorTextFocus,
  HasNonEmptySelection, HasMultipleSelections, ColumnSelection, Writable, TabDoesNotMoveFocus, AccessibilityModeEnabled`.
  The control fills it (P3-41): `TextInputFocus = EditorTextFocus = HasFocus`, `Writable = !ReadOnly`,
  `TabDoesNotMoveFocus = true`, `AccessibilityModeEnabled = false`, `ColumnSelection` = the `columnSelection` editor
  option (default `false`, `editorOptions.ts` line 6239; add `bool ColumnSelection` to `EditorCursorOptions`).
- `when` is a C# predicate `Func<KeybindingContext, bool>?` (`null` = always). There is **no** expression parser
  (ADR 0006, decision 8). Name the predicates after Monaco's context keys in a small static class `WhenClauses`.
- `KeybindingRule(Keybinding Keybinding, string CommandId, Func<KeybindingContext, bool>? When, int Weight)`.
  `KeybindingWeight.EditorCore = 0`, `EditorContrib = 100` (Monaco's values).
- `KeybindingResolver.Resolve(Keybinding key, KeybindingContext context)` returns the command id or `null`: among the
  rules with this key whose `When` is true, the one with the highest weight; for equal weights, the one registered
  **last** (Monaco sorts by weight and searches from the end).
- Chords (`Ctrl+K Ctrl+D`…) are not needed in phase 3 (the only Windows chord of these commands is Find-based):
  `Keybinding` has one part. Write it in the README.
- The ids of the clipboard commands are declared here as constants (`editor.action.clipboardCutAction`…) even if the
  commands arrive in P3-47; `CommandRegistry.Execute` returns `false` for an unknown id until then.

## Tests

- [ ] New: Esc with two cursors → `removeSecondaryCursors`; Esc with one cursor and a selection → `cancelSelection`;
      Esc with one empty cursor → `null`
- [ ] New: Shift+← → `cursorLeftSelect`; with `ColumnSelection = true` → `cursorColumnSelectLeft`
- [ ] New: Tab when not writable → `null`; Ctrl+Backspace when not writable → `null`
- [ ] New: Ctrl+← → `cursorWordLeft`; Ctrl+→ → `cursorWordEndRight` (not `cursorWordRight`)
- [ ] New: every command id of `DefaultKeybindings.Windows` except the three clipboard ids is registered in a new
      `EditorContext` (`Commands.Contains(id)`)
- [ ] New: no two rules have the same key, the same weight and conditions that can both be true (check the pairs Esc,
      Shift+arrows by hand in the test: list the expected conflicts and assert there are no others)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row "keybindings" (`coreCommands.ts`/`editorExtensions.ts` `kbOpts`, `keybindingResolver.ts`
      subset → `Commands/DefaultKeybindings.cs`, `KeybindingResolver.cs`); behavior difference "`when` clauses are C#
      predicates; no chords; Windows table only"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The WinUI key handling (P3-41), user-defined keybindings, macOS and Linux tables.

## Suggested commit message

`feat(view): add VS Code's default Windows keybindings and a resolver`

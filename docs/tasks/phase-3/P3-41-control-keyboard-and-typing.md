# P3-41 — The control: keyboard commands, typing and `IsReadOnly`

- **Status:** done
- **Depends on:** P3-40
- **Size:** M

## Goal

The editor becomes editable from the keyboard: key presses go through the keybinding resolver (P3-39) to the
commands, and characters typed on the keyboard (including dead keys and AltGr) are inserted at every cursor. Add the
public `IsReadOnly` property. ADR 0006, decision 11 (the plain typing path).

## Monaco sources

None to port (WinUI wiring). For reference: Monaco's keyboard path is `src/vs/editor/browser/controller/editContext/native/nativeEditContext.ts`
(`onKeyDown`, lines ~385–410) and the `beforeinput` handler for Enter (lines 157–162: `insertParagraph` /
`insertLineBreak` type `"\n"`).

## Files to create or change

- `src/LoomEditor.WinUI/Input/KeyboardInput.cs` — rewrite: `VirtualKey` + modifiers → `Keybinding` → `KeybindingResolver` → `CommandRegistry.Execute`
- `src/LoomEditor.WinUI/Input/KeyMapping.cs` — create: `static KeyCode? ToKeyCode(VirtualKey key)`
- `src/LoomEditor.View/Controller/TextInputBuffer.cs` — create: the `CharacterReceived` rules (pure, testable without UI — see below)
- `tests/LoomEditor.View.Tests/Controller/TextInputBufferTests.cs` — create
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: `OnKeyDown`, `OnCharacterReceived`, `IsReadOnly` dependency property
- `src/LoomEditor.WinUI/Input/README.md` — update
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — update (`IsReadOnly`, `IsReadOnlyProperty`)

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `KeyboardInput.TryHandle(VirtualKey key, KeybindingContext context)`: reads Ctrl/Shift/Alt/Windows with
  `InputKeyboardSource.GetKeyStateForCurrentThread` (as today), maps the key with `KeyMapping.ToKeyCode` (unknown key →
  not handled), resolves it, and returns `true` only when `CommandRegistry.Execute(id)` returns `true`. The control
  sets `e.Handled = true` only in that case.
- `KeybindingContext` is built by the control: `TextInputFocus = EditorTextFocus = HasFocus`,
  `HasNonEmptySelection` / `HasMultipleSelections` from `ViewModel.GetSelections()`, `ColumnSelection` from the cursor
  options, `Writable = !IsReadOnly`, `TabDoesNotMoveFocus = true`, `AccessibilityModeEnabled = false`.
- Typing: override `OnCharacterReceived(CharacterReceivedRoutedEventArgs e)`. Rules (ADR 0006, decision 11), in a pure
  `internal sealed class TextInputBuffer` with `string? Accept(char ch)`:
  - `'\r'` → returns `"\n"` (Enter; Monaco types `"\n"`, the model converts to its EOL);
  - other characters `< U+0020` and `U+007F` → `null` (ignored: Tab, Backspace and Ctrl+letter arrive here too, they
    are handled as keys);
  - a high surrogate is kept and `null` is returned; the next low surrogate returns the pair; a lone low surrogate or a
    high surrogate followed by something else: drop the lone surrogate (write it in the XML doc);
  - anything else → the character as a string.
  The control calls `ViewModel.Type(text, "keyboard")` with a non-null result and marks the event handled.
  Put `TextInputBuffer` in `LoomEditor.View/Controller/` so that it is tested in `LoomEditor.View.Tests` (no Windows types).
- `IsReadOnly`: public `bool` dependency property, default `false`; its callback calls
  `ViewModel.SetCursorOptions(ViewModel.CursorOptions with { ReadOnly = value })`. Read-only still allows moving the
  cursor and selecting (Monaco: `readOnly` blocks edits only).
- Remove the phase 0 table `CtrlShortcuts` and the `SelectAllCommands` import from `KeyboardInput` (Ctrl+A now comes
  from `DefaultKeybindings`).
- Tab: when `KeyDown` handles Tab (command `tab` ran), focus must not move to the next control. Check it by hand; if
  WinUI still moves the focus, also handle `PreviewKeyDown` for Tab only, and write why in a comment.

## Tests

- [x] New (View.Tests): `TextInputBuffer` — `'a'` → `"a"`; `'\r'` → `"\n"`; `'\t'`, `'\b'`, `'\u0001'`, `'\u007F'` → `null`;
      `'\uD83D'` then `'\uDE00'` → `"😀"`; a lone `'\uDE00'` → `null`
- [ ] Manual (Gallery): type text, Enter keeps the indentation, Backspace/Delete, arrows, Home/End, Ctrl+←/→, Shift+arrows
      select, Ctrl+Z / Ctrl+Y, Tab / Shift+Tab on several lines, Ctrl+Alt+↓ adds a cursor and typing goes to all cursors,
      Esc removes them; a French keyboard: `é`, `^` then `e` (dead key) gives `ê`, AltGr+`e` gives `€`
- [ ] Manual: with `IsReadOnly = true` (temporary code or a Gallery toggle added in P3-54), typing does nothing, arrows work

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the manual checks pass in the AOT app _(publish OK; manual checks left to the maintainer)_
- [x] `PublicAPI.Unshipped.txt` of `LoomEditor.WinUI`: only `IsReadOnly` / `IsReadOnlyProperty`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

IME (P3-48 and later), mouse (P3-46), clipboard keys (P3-47), drawing several cursors (P3-42: until then only the
primary cursor is drawn).

## Suggested commit message

`feat(winui): type text and run commands from the keyboard`

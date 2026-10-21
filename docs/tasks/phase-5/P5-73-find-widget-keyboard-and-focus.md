# P5-73 — The find widget: keyboard and focus routing

- **Status:** done
- **Depends on:** P5-72
- **Size:** M

## Goal

Keys work like in VS Code whether the focus is in the text or in the find widget: Enter / Shift+Enter in the find box
go to the next / previous match, Esc closes the widget and gives the focus back to the text, F3, Alt+C/W/R/L/P,
Ctrl+Shift+1, Ctrl+Alt+Enter, Alt+Enter and Ctrl+F / Ctrl+H work from the widget, and nothing typed in the widget
reaches the text. The controller knows which input has the focus (`findInputFocussed`, `replaceInputFocussed`).

## Sources

`contrib/find/browser/findWidget.ts` at `0c9c0a4295a9222e12a605537738719455ae3056`:

| Lines | What to reproduce |
|---|---|
| 244–275 | focus trackers: `findInputFocussed`, `replaceInputFocussed`, `lastFocusedInputWasReplace`, widget focus |
| 324–352 | `lastFocusedInputWasReplace`, `focusLastElement` |
| 840–850 | `focusFindInput`, `focusReplaceInput` (select all, then focus) |
| 893–973 | `_onFindInputKeyDown`, `_onReplaceInputKeyDown`: Ctrl+Enter inserts a line break, Tab order, Ctrl+Down focuses the editor (Up/Down multi-line handling: not ported, the `TextBox` moves the caret itself) |

Read `src/LoomEditor.WinUI/Input/KeyboardInput.cs`, `Controls/LoomEditor.Keyboard.cs`, `LoomEditor.Ime.cs`,
`LoomEditor.Caret.cs`, `LoomEditor.Pointer.cs` (`IsFromScrollBar`).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Find/FindWidget.Keyboard.cs` — create (partial: `PreviewKeyDown`, focus tracking, `IFindWidget` focus members)
- `src/LoomEditor.WinUI/Input/KeyboardInput.cs` — change: `TryHandle(VirtualKey key, KeybindingContext context, Func<string, bool>? filter = null)`;
  the modifier reading moves to a `static KeyModifiers CurrentModifiers()`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Keyboard.cs` — change: `CreateKeybindingContext` fills `EditorFocus`,
  `FindInputFocused`, `ReplaceInputFocused` from the widget; `OnCharacterReceived` ignores characters when `!HasFocus`
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: `OnKeyDown` returns early (after `base.OnKeyDown`) when `!HasFocus`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Ime.cs` — change: `OnImeGotFocus` does nothing when `!HasFocus`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: pointer press / move / release coming from the widget
  (`IsFromFindWidget(e.OriginalSource)`, same walk as `IsFromScrollBar`) are ignored
- `src/LoomEditor.WinUI/Controls/Find/README.md`, `src/LoomEditor.WinUI/Input/README.md` — update

## Design decisions (already made — do not change)

- **Where the focus is.** The control's `HasFocus` (`FocusState` of the control itself) is Monaco's text focus
  (`editorTextFocus`, `textInputFocus`); a focused element inside the widget gives `EditorFocus = true`,
  `TextInputFocus = EditorTextFocus = false`. `GotFocus` / `LostFocus` bubble from the widget's `TextBox`es: every handler
  of the control that reacts to focus must check `HasFocus` (caret: already does; IME: this card; the `ViewModel.SetHasFocus`
  call already uses `HasFocus`, so the cursor stops blinking while the find box has the focus, as in VS Code).
- **Keys in the widget.** `FindWidget` handles `PreviewKeyDown` on its root (tunneling: it runs before the `TextBox`):
  1. Ctrl+Enter in a box inserts a line break (`\r`, P5-72) at the caret and is handled (Monaco 894–903).
  2. Ctrl+Down focuses the editor (Monaco 916–920). Tab / Shift+Tab: `TabIndex` order find box (1), replace box (2), then
     the toggles and buttons — the XAML order, close to Monaco's 905–914 / 943–953.
  3. Otherwise resolve the key with the control's `KeybindingContext` (widget focus as above) and run the command **only if
     its id is in `FindWidget.RoutedCommandIds`**: every `FindCommands` id (P5-75 adds the Find-based multi-cursor ids).
     Undo, redo, select all and the clipboard ids are therefore left to the `TextBox`, which handles them for its own text.
     A routed command marks the event handled; any other key goes on to the `TextBox`.
- A key that the `TextBox` does not handle bubbles to `LoomEditor.OnKeyDown`: it returns without resolving when `!HasFocus`
  (the widget already routed what belongs to the editor). Characters typed in the widget bubble as `CharacterReceived`:
  ignored by the control when `!HasFocus`.
- **Focus members of `IFindWidget`.** `ApplyStartFocus(FocusFindInput)` → `FindTextBox.SelectAll()` then
  `Focus(FocusState.Programmatic)`; `FocusReplaceInput` → the same on `ReplaceTextBox`; `NoFocusChange` → nothing. A
  widget that just became visible may not take the focus in the same tick: if `Focus` returns `false`, retry once with
  `DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, …)`. `IsFindInputFocused` / `ReplaceInputFocused` read the
  boxes' `FocusState`; `WasReplaceInputLastFocused` is updated in their `GotFocus` (Monaco 246–263);
  `FocusLastElement` focuses the last focused element of the widget, else the replace or find box (Monaco 341–352);
  `FocusEditor` calls the `focusEditor` callback (`LoomEditor.Focus(FocusState.Programmatic)`).
- Esc in the widget: `closeFindWidget` hides the widget, and `_hide(true)` puts the focus back in the text (P5-72).

## Tests

No WinUI test project; the routing table is tested in P5-71. **Manual check by the maintainer** (Gallery, Debug and AOT):

- [ ] Ctrl+F focuses the find box with its text selected; Enter / Shift+Enter move to the next / previous match and the
      text never receives the key; F3 / Shift+F3 work from the find box and from the text
- [ ] Esc in the find box closes the widget and the caret blinks again in the text; Esc in the text with the widget open
      closes it too; with the widget closed, Esc still removes secondary cursors
- [ ] Typing, Backspace, Ctrl+A, Ctrl+Z, Ctrl+C / Ctrl+V in the find box act on the find box only
- [ ] Alt+C / Alt+W / Alt+R from the find box toggle the options; Ctrl+Enter in the find box inserts a line break and the
      search becomes multi-line
- [ ] Clicking the widget's background or the "n of m" text does not move the cursor in the text
- [ ] With the Japanese IME composing in the find box, the editor's IME state is untouched (no text appears in the editor)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings
- [x] `docs/PORTING.md`: map row `findWidget.ts` (+ keyboard and focus, P5-73); behavior difference "Up/Down in a
      multi-line find box move the caret only (no history)"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Replace row, "n of m", find in selection (P5-74).

## Suggested commit message

`feat(winui): route keys and focus between the editor and the find widget`

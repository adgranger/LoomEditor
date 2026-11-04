# P15-11 — Keyboard, macOS modifiers, clipboard

- **Status:** done
- **Depends on:** P15-10
- **Size:** M

**Recommended model:** Sonnet

## Goal

Typing and every keybinding work in the Avalonia `LoomEditor`. On macOS the keybindings use Cmd where Windows uses Ctrl. Copy, cut and paste use the system clipboard.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Input/KeyboardInput.cs`, `KeyMapping.cs`, `WindowsClipboardService.cs`, `Controls/LoomEditor.Keyboard.cs`. Also read
`src/LoomEditor.View/Commands/KeyModifiers.cs`, `src/LoomEditor.View/Controller/IClipboardService.cs`, `ClipboardCommands.cs`, [README](README.md) decision 7.

## Files to create or change

- `src/LoomEditor.Avalonia/Input/KeyboardInput.cs`, `KeyMapping.cs`, `Controls/LoomEditor.Keyboard.cs` — create (twins)
- `src/LoomEditor.Avalonia/Input/AvaloniaClipboardService.cs` — create (twin of `WindowsClipboardService`)
- `tests/LoomEditor.Avalonia.Tests/Input/KeyMappingTests.cs`, `Controls/LoomEditorKeyboardTests.cs` — create
- `docs/PORTING.md` — "Behavior differences": macOS modifier swap, no `mac:` keybindings
- `docs/testing/phase-15-manual-tests.md` — section "P15-11"

## Design decisions (already made — do not change)

- Key down: `KeyDown` (`e.Key`, `e.KeyModifiers`) → `KeyMapping` → the View key code. That is the same path as `KeyRoutedEventArgs` in WinUI. Characters: `TextInput`
  (`e.Text`), the WinUI character path. When IME is active (P15-13) the IME client owns text.
- `KeyMapping.ToModifiers(KeyModifiers avalonia, bool isMacOS)`: on macOS `Meta → Ctrl`, `Control → Meta`; elsewhere `Control → Ctrl`, `Meta → Meta`. `Alt` and `Shift` map
  directly. `isMacOS` comes from `OperatingSystem.IsMacOS()` at the call site, a parameter so that tests cover both OSes.
- `Key` → View key code table: every `VirtualKey` case of the WinUI `KeyMapping`, translated. A `Key` that is not in the table is ignored, as in WinUI.
- Clipboard: `TopLevel.GetTopLevel(this)!.Clipboard` — `WriteText` calls `SetTextAsync` without awaiting (fire-and-forget, exceptions logged through the same path as
  WinUI). `ReadTextAsync` → `GetTextAsync`. No clipboard (headless without one) → `null`, no exception.
- Tab key: the editor keeps focus on Tab (`KeyboardNavigation.TabNavigation` / handled `KeyDown`), as WinUI `IsTabStop` + handled Tab. Ctrl+Tab/Ctrl+Shift+Tab move focus out of the
  editor, as WinUI.

## Tests

- [x] New: `ToModifiers_Windows_CtrlIsCtrl`, `ToModifiers_MacOS_CmdIsCtrl`, `ToModifiers_MacOS_ControlIsMeta`; `Typing_Hello_InsertsText` (`KeyTextInput("hello")`);
  `CtrlA_SelectsAll` (pass `Control` on Windows/Linux runs, `Meta` on macOS: pick with `OperatingSystem.IsMacOS()`); `Enter_InsertsNewLineWithIndent`;
  `CopyPaste_RoundTrips` (headless clipboard); `Tab_InsertsIndent_KeepsFocus`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

IME (P15-13), pointer (P15-12).

## Suggested commit message

`feat(avalonia): keyboard input, macOS modifiers and the clipboard`

## Result

- Done as written. No public API changes (`PublicAPI.Unshipped.txt` and `AvaloniaParityPending.txt` are unchanged).
- Enter: typed from `KeyDown` (None or Shift), and `OnTextInput` drops CR and LF, so that a platform that also raises text input for Enter does not insert two line breaks.
- `KeybindingContext`: `TabDoesNotMoveFocus` is `true`, the find widget keys are `false` (P15-14 and P15-17 set them), `IsComposing` is not read yet (P15-13).
- `InsertSnippet` now reads the clipboard through `AvaloniaClipboardService`.
- Manual checks "P15-11" written in `docs/testing/phase-15-manual-tests.md`, not run.

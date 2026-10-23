# P7-16 — Tab focus mode (`Ctrl+M`): the keyboard trap exit

- **Status:** todo
- **Depends on:** P7-15
- **Size:** M

## Goal

`Ctrl+M` toggles whether `Tab` / `Shift+Tab` insert a tab (indent) or move the keyboard focus out of the editor, the change is announced, and the control exposes it as `TabMovesFocus`.
Without it a keyboard-only or screen reader user cannot leave the editor with the keyboard.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/toggleTabFocusMode/browser/toggleTabFocusMode.ts` | 1–50 | `ToggleTabFocusModeAction` (id `editor.action.toggleTabFocusMode`, `Ctrl+M`, the two alerts) |
| `src/vs/editor/browser/config/tabFocus.ts` | 1–30 | `TabFocus`: the flag and its change event (per editor here, not global) |
| `src/vs/editor/browser/config/editorConfiguration.ts` | 131 | `tabFocusMode: option \|\| TabFocus.getTabFocusMode()` → the `tabDoesNotMoveFocus` context |

## Files to create or change

- `src/LoomEditor.View/Features/TabFocus/TabFocusFeature.cs`, `TabFocusCommands.cs`, `README.md` — create (feature folder rules, `docs/how-to/add-a-feature.md`)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: `() => new TabFocusFeature(),` at the end of the list
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal bool TabMovesFocus { get; set; }` and `internal event EventHandler? TabMovesFocusChanged` (raised when the value really changes)
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: `Add(KeyModifiers.Ctrl, KeyCode.KeyM, TabFocusCommands.ToggleTabFocusMode, null, KeybindingWeight.EditorContrib);` next to the other contrib keybindings (comment: `toggleTabFocusMode.ts: no kbExpr`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Accessibility.cs` — change: the `TabMovesFocus` dependency property and the wiring
- `src/LoomEditor.WinUI/Controls/LoomEditor.Keyboard.cs` — change: `TabDoesNotMoveFocus: !TabMovesFocus` in `CreateKeybindingContext`; `OnCharacterReceived`: `if (TabMovesFocus && e.Character == '\t') { return; }` first
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `docs/testing/phase-7-manual-tests.md` — change: section 2 below
- `tests/LoomEditor.View.Tests/Features/TabFocus/TabFocusFeatureTests.cs` — create; `tests/LoomEditor.View.Tests/Commands/` — change: keybinding tests (find the file that tests `DefaultKeybindings` / `KeybindingResolver`: `grep -rln "DefaultKeybindings" tests`)

## Design decisions (already made — do not change)

- `TabFocusCommands.ToggleTabFocusMode = "editor.action.toggleTabFocusMode"` (const). The command is **always enabled** (no precondition, like Monaco), registered by the feature with `context.Commands.Register(new EditorCommand(id, run))`.
- Run: `EditorContext editorContext = (EditorContext)context` (the feature, like `FindFeature`, does nothing and returns when `context` is not an `EditorContext`); `bool value = !editorContext.TabMovesFocus; editorContext.TabMovesFocus = value;` then
  `context.GetAnnouncer().Announce(value ? AccessibilityMessages.TabMovesFocusOn : AccessibilityMessages.TabMovesFocusOff)`.
- The state is **per editor** (Monaco's is a process-wide singleton: a documented behavior difference; add the row to `docs/PORTING.md`).
- The control's `TabMovesFocus` is a dependency property (`TabMovesFocusProperty`, `bool`, default `false`) that mirrors `EditorContext.TabMovesFocus`: setting the property sets the context value; the context event sets the property
  (guard against re-entrancy with the equality check: `SetValue` only when different). It is **not** the same as `IsTabStop`.
- `Tab` is handled by the existing `tab` / `outdent` keybindings whose `when` is `WhenClauses.Tab` (`Writable && EditorTextFocus && TabDoesNotMoveFocus`): with `TabDoesNotMoveFocus == false` no keybinding matches, `TryHandle` returns `false`, `OnKeyDown` leaves the event unhandled and
  WinUI moves the focus to the next element (`IsTabStop` is already `true` in `Generic.xaml`). No code in the control moves the focus by hand.
- A read-only editor already lets `Tab` through (`Writable` is part of the clause): unchanged.
- The `AccessibilityModeEnabled` flag of the keybinding context stays `false` (see ADR 0010 decision 8: Monaco disables the standard word-move keys in screen reader mode because the browser then handles them; LoomEditor has no such fallback).
- Public API: `LoomEditor.TabMovesFocus` and `LoomEditor.TabMovesFocusProperty` only.

## Tests

- [ ] New (`TabFocusFeatureTests`): `Toggle_FlipsTheContextFlag` (run the command twice: `true`, `false`), `Toggle_On_AnnouncesTheMessage`, `Toggle_Off_AnnouncesTheMessage`, `TabMovesFocusChanged_RaisedOncePerChange`,
      `Command_IsRegisteredAndEnabledWithoutAnyContext`, `Attach_WithANonEditorContext_DoesNothing`, `Dispose_UnregistersTheCommand`
- [ ] New (keybindings test file): `CtrlM_ResolvesToToggleTabFocusMode_WhateverTheContext`, `Tab_WithTabDoesNotMoveFocusFalse_ResolvesToNothing` (`KeybindingContext(Writable: true, EditorTextFocus: true, TabDoesNotMoveFocus: false)`),
      `Tab_WithTabDoesNotMoveFocusTrue_ResolvesToTab`, `ShiftTab_WithTabDoesNotMoveFocusFalse_ResolvesToNothing`
- [ ] Architecture tests (`ConventionTests`) pass with the new folder (`TabFocusFeature.cs`, `TabFocusCommands.cs`, `README.md`, registered in `EditorFeatures.cs`)
- [ ] Manual (`docs/testing/phase-7-manual-tests.md` section 2 "Tab focus mode", a Result column):
      2.1 click in the editor, press `Tab` → a tab is inserted; 2.2 press `Ctrl+M`, then `Tab` → the focus leaves the editor (to the next control of the Gallery) and nothing is inserted; 2.3 `Shift+Tab` comes back; 2.4 with Narrator on, `Ctrl+M` → Narrator says
      "Pressing Tab will now move focus to the next focusable element"; 2.5 `Ctrl+M` again → "Pressing Tab will now insert the tab character"; 2.6 `TabMovesFocus = true` set from code behaves like 2.2

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (architecture tests included)
- [ ] `docs/PORTING.md` map and behavior differences updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Persisting the setting, a menu entry, `editor.tabFocusMode` as an option, `Escape` handling.

## Suggested commit message

`feat(tab-focus): add the tab focus mode and the TabMovesFocus property`

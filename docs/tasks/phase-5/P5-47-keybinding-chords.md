# P5-47 — Two-key chords in keybindings (`Ctrl+K Ctrl+C`)

- **Status:** done
- **Depends on:** P5-46
- **Size:** S

## Goal

Comment, LinesOperations and later Folding use VS Code's two-key chords (`Ctrl+K Ctrl+C`, `Ctrl+K Ctrl+U`,
`Ctrl+K Ctrl+X`). Phase 3 has no chords (P3-39). After this card a `KeybindingRule` can have a second key press, the
resolver answers "more chords needed", and the control waits for the second key. The keys `/`, `[` and `]` exist.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/platform/keybinding/common/keybindingResolver.ts` | 11–33 | `ResultKind` (`NoMatchingKb`, `MoreChordsNeeded`, `KbFound`), `ResolutionResult` |
| same | 320–371 | `resolve(context, currentChords, keypress)` — two chords at most |
| `src/vs/platform/keybinding/common/abstractKeybindingService.ts` | 306–410 | `_doDispatch`: the three `ResultKind` cases and chord mode (not the status messages, telemetry, single-modifier chords) |
| same | 193–224 | `_expectAnotherChord`, `_leaveChordMode` (not the IME calls, not the status message) |
| `src/vs/base/common/keyCodes.ts` | search `Slash`, `BracketLeft`, `BracketRight` | the three key codes (US layout: `VK_OEM_2`, `VK_OEM_4`, `VK_OEM_6`) |

## Files to create or change

- `src/LoomEditor.View/Commands/KeyCode.cs` — change: add `Slash`, `BracketLeft`, `BracketRight` (skip any an earlier card already added)
- `src/LoomEditor.View/Commands/KeybindingRule.cs` — change: optional last parameter `Keybinding? Chord = null`
- `src/LoomEditor.View/Commands/KeybindingResolution.cs` — create: `KeybindingResolutionKind` + `KeybindingResolution`
- `src/LoomEditor.View/Commands/KeybindingResolver.cs` — change: chord-aware `Resolve`
- `src/LoomEditor.View/Commands/KeybindingDispatcher.cs` — create: chord mode (port of `_doDispatch`)
- `src/LoomEditor.View/Commands/README.md` — change: replace "There are no chords" with the chord rules below
- `src/LoomEditor.WinUI/Input/KeyMapping.cs` — change: `(VirtualKey)191 → Slash`, `219 → BracketLeft`, `221 → BracketRight`
- `src/LoomEditor.WinUI/Input/KeyboardInput.cs` — change: use `KeybindingDispatcher`; add `LeaveChordMode()`
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` (where `LostFocus` is handled) — change: call `KeyboardInput.LeaveChordMode()`
- `tests/LoomEditor.View.Tests/Commands/KeybindingResolverTests.cs` — change; `KeybindingDispatcherTests.cs` — create

## Design decisions (already made — do not change)

- `Keybinding` stays one key press. A chord rule is `new KeybindingRule(first, id, when, weight, Chord: second)`.
  Only two chords: no rule has three key presses in VS Code's editor defaults.
- `internal enum KeybindingResolutionKind { NoMatchingKb, MoreChordsNeeded, KbFound }` and
  `internal readonly record struct KeybindingResolution(KeybindingResolutionKind Kind, string? CommandId = null)`.
- `KeybindingResolver.Resolve(KeybindingContext context, Keybinding? currentChord, Keybinding keypress)`: Monaco's
  `resolve`. Without `currentChord`, the candidates are the rules whose first key is `keypress`; with it, the rules whose
  first key is `currentChord` and whose `Chord` is `keypress`. The winner is chosen as today (heaviest weight whose
  `When` is true, last registered among equal weights). Winner with a `Chord` and no `currentChord` →
  `MoreChordsNeeded`; otherwise `KbFound`. The existing `string? Resolve(Keybinding, KeybindingContext)` stays and
  returns `CommandId` only for `KbFound` (existing tests keep working).
- `KeybindingDispatcher` (View, pure): `bool Dispatch(Keybinding keypress, KeybindingContext context, out string? commandId)`
  returns "the key was handled" (Monaco's `shouldPreventDefault`). `NoMatchingKb` in chord mode → leave chord mode,
  handled; `MoreChordsNeeded` → remember the chord, handled; `KbFound` → leave chord mode, `commandId` set, handled.
  `LeaveChordMode()` and `bool InChordMode` are public members of the internal class.
- **Behavior differences** (write them in the XML doc and in `docs/PORTING.md`): no status bar message while waiting
  for the second key; no 5-second timeout (`_scheduleLeaveChordMode`): chord mode ends on the next key press or when the
  editor loses focus; keys are mapped from Windows virtual keys of the US layout, as Monaco's browser key codes are.
- `KeyboardInput.TryHandle` calls the dispatcher, then `commands.Execute(commandId)` when `commandId` is set. It
  returns `true` whenever the dispatcher handled the key (even when the command is unknown), as Monaco does.

## Tests

- [x] New: `Resolve_FirstKeyOfAChord_ReturnsMoreChordsNeeded` (a rule `Ctrl+K` then `Ctrl+C`)
- [x] New: `Resolve_SecondKeyOfAChord_ReturnsKbFoundWithTheCommand`
- [x] New: `Resolve_SecondKeyWithoutRule_ReturnsNoMatchingKb`
- [x] New: `Resolve_ChordWhoseWhenIsFalse_ReturnsNoMatchingKb`
- [x] New: `Dispatch_ChordThenUnknownKey_LeavesChordModeAndHandlesTheKey`
- [x] New: `Dispatch_ChordThenSecondKey_ReturnsTheCommandAndLeavesChordMode`
- [x] New: `Dispatch_SingleKeyRule_StillReturnsTheCommandOutsideChordMode`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row "keybindings" updated (chords: `keybindingResolver.ts` `resolve`,
      `abstractKeybindingService.ts` `_doDispatch` subset); the two behavior differences above
- [ ] Manual check (maintainer): in the Gallery, `Ctrl+K` then `Escape` does nothing and the next key works normally
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The rules that use chords (P5-51, P5-56), user keybindings, the status bar message.

## Suggested commit message

`feat(view): support two-key chords in keybindings`

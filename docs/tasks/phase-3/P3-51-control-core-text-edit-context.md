# P3-51 — The control: IME through `CoreTextEditContext`

- **Status:** todo
- **Depends on:** P3-50
- **Size:** M

## Goal

Japanese, Chinese and Korean input methods, the emoji panel and other Windows text services can type into the editor.
The control creates a `CoreTextEditContext` (Windows 11; on Windows 10 the editor works without IME), answers its text
and selection requests from the `EditContextModel` of P3-50, and turns its updates into cursor calls. Plain typing
stays on `CharacterReceived` (P3-41); during a composition, keys and characters go to the IME only.
ADR 0006, decision 11, and the findings of the IME spike (P3-48) — **read that section of the ADR first**.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/browser/controller/editContext/native/nativeEditContext.ts` — for reference (the logic is in P3-50):
composition start/end handlers (223–238), focus (106–110, 372–381), `onCursorStateChanged` (306–311) and the model
content handler (253–265) that re-sync the edit context, key handling during composition (401–409).

## Mapping (Windows → Monaco → LoomEditor)

| `CoreTextEditContext` | Monaco | LoomEditor |
|---|---|---|
| `TextRequested` | `_getNewEditContextState` | `args.Request.Text = model.Text` (the range asked, clamped) |
| `SelectionRequested` | same | `args.Request.Selection = new CoreTextRange { StartCaretPosition, EndCaretPosition }` |
| `TextUpdating` | `textupdate` → `_emitTypeEvent` → `_onType` | `EditContextModel.OnTextUpdate` → `EditContextModel.Apply`; `args.Result = Succeeded` |
| `SelectionUpdating` | (folded into `textupdate`) | store the new selection in the model; with an empty selection, `CompositionType("", 0, 0, delta)` |
| `FormatUpdating` | `textformatupdate` | P3-53 (until then: `Result = Succeeded`, ignore) |
| `LayoutRequested` | `_updateSelectionAndControlBounds` | P3-52 (until then: the control's bounds) |
| `CompositionStarted` | `compositionstart` | `ViewModel.StartComposition()` |
| `CompositionCompleted` | `compositionend` | `ViewModel.EndComposition("keyboard")` |
| `FocusRemoved` | focus tracker | nothing (the control keeps its XAML focus) |
| `NotifyFocusEnter` / `NotifyFocusLeave` | focus tracker | on `GotFocus` / `LostFocus` |
| `NotifyTextChanged` / `NotifySelectionChanged` | `_updateEditContext` | after a cursor or model change, when `EditContextModel.Update` reports a change |

## Files to create or change

- `src/LoomEditor.WinUI/Input/ImeInput.cs` — create: owns the `CoreTextEditContext`, the `EditContextModel`, the event handlers
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: create/dispose `ImeInput`; focus; while composing, `OnKeyDown`
  and `OnCharacterReceived` do nothing
- `src/LoomEditor.WinUI/Input/README.md` — update

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Creation: `CoreTextServicesManager.GetForCurrentView()` in `try/catch`. On failure (Windows 10): `Debug.WriteLine`
  once, `ImeInput` stays disabled, everything else works. Never let the exception escape.
- `InputScope = CoreTextInputScope.Text`; `InputPaneDisplayPolicy = CoreTextInputPaneDisplayPolicy.Manual` (no touch
  keyboard pop-up in phase 3).
- **Never call `Notify*` from inside a `CoreTextEditContext` handler** (re-entrancy into the text services): queue the
  call with `DispatcherQueue.TryEnqueue` and run it after the handler (Windows-universal-samples `CustomEditControl`
  does the same).
- Re-sync (`EditContextModel.Update` + `Notify*`) after: a `CursorStateChangedEvent` (P3-17), a model content change
  that touches the lines of the primary selection, the start and the end of a composition.
- While a composition is active (`CompositionStarted` … `CompositionCompleted`): `OnKeyDown` returns without running
  keybindings and `OnCharacterReceived` ignores the character (Monaco stops `KEY_IN_COMPOSITION`). Also ignore
  `VirtualKey.ProcessKey` (229) in `OnKeyDown` (the IME owns that key) — keep it only if P3-48 confirmed WinUI reports it.
- Some services (emoji panel) send `TextUpdating` without `CompositionStarted`: handle every `TextUpdating` the same way.
- Unloaded: unsubscribe **every** event and call `NotifyFocusLeave` before dropping the context (Windows Terminal had
  crashes when it did not revoke `LayoutRequested`, `TextUpdating`, `CompositionCompleted`).

## Tests

The logic is tested in P3-49 and P3-50. Manual checks (Windows 11, Gallery, Debug and the AOT build):

- [ ] Microsoft IME Japanese: type `nihongo`, convert with Space, commit with Enter → `日本語` once, caret after it, one
      undo step removes it; Esc during the composition cancels it and leaves no text
- [ ] Microsoft Pinyin: type `nihao`, choose `你好` with Space → inserted once
- [ ] Korean IME: type `안녕` → syllables commit correctly (a composition can start right after the previous one ends)
- [ ] Emoji panel (Win + .) inserts the emoji at every cursor
- [ ] With two cursors, an IME composition types at both
- [ ] Plain typing (English, French with dead keys and AltGr) still works and is not doubled
- [ ] Windows 10 (if available): the editor starts, plain typing works, a single debug line says that IME is unavailable

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the manual checks pass in the AOT app
- [ ] `docs/PORTING.md`: map row `nativeEditContext.ts` (+ WinUI `CoreTextEditContext`, P3-51); behavior differences
      "IME needs Windows 11", "plain typing comes from `CharacterReceived`, composition from `CoreTextEditContext`"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Candidate window position (P3-52; until then the candidate window may appear at the top-left of the control),
composition underline (P3-53), the touch keyboard.

## Suggested commit message

`feat(winui): type with an IME through CoreTextEditContext`

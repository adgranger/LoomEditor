# P3-48 — IME spike: check `CoreTextEditContext` in the WinUI 3 desktop control

- **Status:** todo
- **Depends on:** P3-47
- **Size:** S (throw-away probe + a findings section in ADR 0006)

## Goal

Before writing the IME cards (P3-49 … P3-53), check on a real machine the assumptions of ADR 0006, decision 11.
**This card needs the maintainer at the keyboard** with the Japanese (Microsoft IME) and Chinese (Microsoft Pinyin)
input methods installed. The agent writes a temporary probe, the maintainer runs it and reports what happens, the agent
writes the findings into the ADR and deletes the probe. Only the ADR change is committed.

## Monaco sources

None. References (read them, do not port them):

- MS Learn, "Custom text input": https://learn.microsoft.com/en-us/windows/apps/develop/input/custom-text-input
- Windows-universal-samples, `Samples/CustomEditControl` (the reference use of `CoreTextEditContext`)
- microsoft-ui-xaml issue #4239 (`GetForCurrentView` in desktop apps, fixed in Windows 11), #4851 (no `TextUpdating` for
  plain English typing)
- Windows Terminal, `src/cascadia/TerminalControl/TSFInputControl.cpp` before PR #17067 (a Win32 app that used
  `CoreTextServicesManager.GetForCurrentView()`; how it computed `LayoutBounds` in screen pixels)

## Files to create or change

- `samples/LoomEditor.Gallery/ImeProbe.cs` — create, **temporary, delete before committing**
- `docs/adr/0006-editing-and-text-input.md` — change: add a section "Findings of the IME spike (P3-48)"

## What the probe does

A static class called once from `MainWindow` after the editor is loaded (temporary line, deleted afterwards):

1. `CoreTextServicesManager.GetForCurrentView()` inside `try/catch`; write the result or the exception (type and
   HRESULT) with `Debug.WriteLine`.
2. `CreateEditContext()`, `InputScope = CoreTextInputScope.Text`, subscribe to **every** event (`TextRequested`,
   `SelectionRequested`, `TextUpdating`, `SelectionUpdating`, `FormatUpdating`, `LayoutRequested`, `CompositionStarted`,
   `CompositionCompleted`, `FocusRemoved`, `NotifyFocusLeaveCompleted`) and log each call with its arguments.
   Answer `TextRequested` with an empty string and `SelectionRequested` with `(0,0)`; accept every `TextUpdating`
   (`Result = Succeeded`); answer `LayoutRequested` with the control's bounds converted to screen pixels
   (`TransformToVisual(null)`, × `XamlRoot.RasterizationScale`, + the window client origin from
   `Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId)` and `ClientToScreen`).
3. `NotifyFocusEnter()` on `GotFocus`, `NotifyFocusLeave()` on `LostFocus`.
4. Log `KeyDown` (`VirtualKey`, including `ProcessKey` = 229) and `CharacterReceived` (code unit) of the editor.

## Questions to answer (write each answer in the ADR)

- [ ] Windows 11: does `GetForCurrentView()` work? Windows 10 (if a machine or VM is available): does it throw, and what?
- [ ] Japanese IME (type `sennsei`, convert, commit with Enter): sequence of events; does `CharacterReceived` also fire
      for the composed text? Is `KeyDown` reported with `ProcessKey` during the composition?
- [ ] Chinese Pinyin (type `nihao`, choose a candidate with Space): same questions
- [ ] English keyboard without IME: confirm that `TextUpdating` does **not** fire (only `CharacterReceived`)
- [ ] Emoji panel (Win + .): which events arrive (with or without `CompositionStarted`)?
- [ ] Candidate window: is it placed at the bounds given in `LayoutRequested` with **screen pixels**? Try
      `LayoutBoundsVisualPixels` too (DIPs relative to the viewport) and write which one works at 100 % and 150 % scale
- [ ] Native AOT: `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` with the probe — 0 warnings, and the
      probe behaves the same in the AOT app

## Acceptance criteria

- [ ] The ADR section lists every answer, the Windows build numbers tested, and any correction to decision 11
- [ ] If an answer contradicts ADR 0006 (for example `CharacterReceived` also delivers composed text), **stop** and
      propose the change of P3-49 … P3-53 to the maintainer before going on
- [ ] The probe file and the temporary call are deleted; `git status` shows only the ADR change
- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings; `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any production code (P3-49 and later).

## Suggested commit message

`docs(adr): record the findings of the IME spike`

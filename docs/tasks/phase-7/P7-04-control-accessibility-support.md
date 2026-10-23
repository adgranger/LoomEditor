# P7-04 — The control: `AccessibilitySupport` and screen reader detection

- **Status:** todo
- **Depends on:** P7-03
- **Size:** M

## Goal

The `LoomEditor` control has an `AccessibilitySupport` property (`Auto`, `On`, `Off`); in `Auto` it detects a running screen reader, and the editor
wraps accordingly (P7-01, P7-03). The phase 7 manual test list exists.

## Sources

Monaco: `src/vs/editor/browser/config/editorConfiguration.ts` lines 140–156 (`accessibilitySupport` read from the accessibility service; the
service is the workbench's, so the detection is LoomEditor's own: `SystemParametersInfo(SPI_GETSCREENREADER)`).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/EditorAccessibilitySupport.cs` — create (public enum: `Auto = 0`, `On = 1`, `Off = 2`, XML doc, like `EditorWordWrap.cs`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Accessibility.cs` — create (partial class: the dependency property and the wiring)
- `src/LoomEditor.WinUI/Accessibility/ScreenReaderDetector.cs` — create
- `src/LoomEditor.WinUI/Interop/NativeMethods.cs` — change: `SystemParametersInfo`
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: call `InitializeAccessibility()` in the constructor; `OnTextChanged` feeds `IsDominatedByLongLines` (below); `OnLoaded` and a `GotFocus` handler call `RefreshScreenReader()`
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (run `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `docs/testing/phase-7-manual-tests.md` — create (title, the Windows build / date / tester header like `phase-6-manual-tests.md`, and section 1 below)
- `src/LoomEditor.WinUI/Accessibility/README.md` — change: list the files of the folder

## Design decisions (already made — do not change)

- `ScreenReaderDetector.IsScreenReaderRunning()` is `static`, returns `SystemParametersInfo(SPI_GETSCREENREADER = 0x0046, 0, ref int value, 0)` with `value != 0`, `false` when the call fails.
  `NativeMethods.SystemParametersInfo` is a `[LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")] internal static partial bool SystemParametersInfo(uint action, uint param, ref int value, uint winIni)`
  with `[return: MarshalAs(UnmanagedType.Bool)]`. Narrator and NVDA both set this flag; JAWS too.
- The dependency property is `AccessibilitySupportProperty` (`nameof(AccessibilitySupport)`, type `EditorAccessibilitySupport`, default `Auto`, callback `OnAccessibilitySupportChanged`), like `WordWrapProperty`.
- `RefreshScreenReader()` builds `new EditorAccessibilityOptions { Setting = <mapped>, Detected = ScreenReaderDetector.IsScreenReaderRunning() ? Enabled : Disabled }` (`Auto` → `Unknown`, `On` → `Enabled`, `Off` → `Disabled`),
  and calls `_viewModel.SetOptions(_viewModel.LayoutOptions with { Accessibility = options }, _viewModel.ViewLayoutOptions)` only if the options differ from the current ones (record equality).
  The detection is repeated on `Loaded` and on every `GotFocus`: Windows tells a window about a screen reader starting only through `WM_SETTINGCHANGE`, which a WinUI control cannot see
  without subclassing the window; focus is the pragmatic trigger (a screen reader user always focuses the editor). Document this in the remarks of the property.
- `IsDominatedByLongLines`: `OnTextChanged` calls `editor._viewModel.SetIsDominatedByLongLines(editor._context.Model.IsDominatedByLongLines())` right after `SetText`. Edits typed later do not recompute it
  (Monaco computes it once when the model is attached); edits made through the `Model` property neither. Document this in the XML doc of `AccessibilitySupport`.
- `_viewModel` already exists when the property changes before the control is loaded (it is created in the constructor): no null checks beyond what `OnWrappingChanged` does.
- Public API: only `EditorAccessibilitySupport` and the property (`AccessibilitySupport`, `AccessibilitySupportProperty`). `ScreenReaderDetector` and the native method are `internal`.

## Tests

No unit test is possible (WinUI control, ADR 0010 decision 2); the logic it feeds is tested by P7-01 and P7-03. Manual checks, in `docs/testing/phase-7-manual-tests.md` section 1 "Screen reader mode"
(the Gallery's "Long-line sample" button comes in P7-24; until then set `Editor.Text` from code to one line of 12 000 characters of `"lorem "` repeated, plus a few short lines):

- [ ] 1.1 `AccessibilitySupport = On`, a long line, word wrap off → the line wraps at the window width; with `Off` it does not
- [ ] 1.2 `AccessibilitySupport = On`, `WrappingIndent = Indent`, `WordWrap = On` → continuation lines are not indented; with `Off` they are
- [ ] 1.3 `Auto` and Narrator started (Win+Ctrl+Enter), then click into the editor → behaves like `On`; after stopping Narrator and clicking out and in again → like `Off`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` (see phase 7 README §6) — result reported in the commit body
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

UIA providers (P7-17 onwards), announcements, a Gallery switch (P7-24).

## Suggested commit message

`feat(winui): add the AccessibilitySupport property and screen reader detection`

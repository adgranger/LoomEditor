# P15-14 — Accessibility and high contrast

- **Status:** todo
- **Depends on:** P15-13
- **Size:** M

**Recommended model:** Opus (mapping UI Automation onto Avalonia's automation API needs judgment within the decisions below)

## Goal

Screen readers (Narrator / NVDA on Windows, VoiceOver on macOS, Orca on Linux, as far as Avalonia supports each) find the editor, read its value and the current line, and
hear the announcements the WinUI control makes. High contrast on Windows and "increase contrast" on macOS pick the high-contrast theme.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Accessibility/*.cs`, `Controls/LoomEditor.Accessibility.cs`, `Controls/LoomEditor.HighContrast.cs`. Also read
[ADR 0010](../../adr/0010-accessibility.md), `src/LoomEditor.View/Accessibility/` (`IAutomationLineMetrics`, the announcement source).

## Files to create or change

- `src/LoomEditor.Avalonia/Accessibility/LoomEditorAutomationPeer.cs`, `AutomationAnnouncer.cs`, `AutomationEventSink.cs`, `RendererLineMetrics.cs`, `SystemHighContrast.cs`,
  `ScreenReaderDetector.cs` — create (twins)
- `src/LoomEditor.Avalonia/Accessibility/LoomEditorTextRangeProvider.cs` — create **only** if the pinned Avalonia version has a public text-range provider interface
- `src/LoomEditor.Avalonia/Controls/LoomEditor.Accessibility.cs`, `LoomEditor.HighContrast.cs` — create (twins)
- `tests/LoomEditor.Avalonia.Tests/Accessibility/LoomEditorAutomationPeerTests.cs` — create
- `docs/adr/0010-accessibility.md` — "Consequences": one paragraph on Avalonia (what is supported, what is a gap)
- `docs/PORTING.md` — "Behavior differences": each gap
- `docs/testing/phase-15-manual-tests.md` — section "P15-14"

## Design decisions (already made — do not change)

- Peer: `LoomEditorAutomationPeer : ControlAutomationPeer`, `GetAutomationControlTypeCore() = AutomationControlType.Edit`, name from `AutomationProperties.Name` or the WinUI
  default name. It implements **`IValueProvider`** (value = the model text, `IsReadOnly` from the editor, `SetValue` only when editable). It implements the text-range provider only if
  Avalonia exposes one.
- **Announcements:** the WinUI `AutomationAnnouncer` raises a UIA notification. On Avalonia, an off-screen `TextBlock` with `AutomationProperties.LiveSetting = Assertive` whose text
  is set to the announcement (the documented Avalonia way). Same strings, same triggers.
- `ScreenReaderDetector`: Windows → the same P/Invoke as WinUI (`SystemParametersInfo(SPI_GETSCREENREADER)`), behind `OperatingSystem.IsWindows()`, in an `Interop/` file of the
  Avalonia project (this is the only Windows API allowed, through `[LibraryImport]`; add the file to the exceptions of `PlatformNeutralSourceTests` with that reason).
  macOS / Linux → `false` (Monaco's `accessibilitySupport: "auto"` then stays off; the host can set `AccessibilitySupport = On`).
- High contrast: `Application.Current.PlatformSettings.GetColorValues().ContrastPreference == ColorContrastPreference.High` and its `ColorValuesChanged` event replace the WinUI
  `AccessibilitySettings`. The theme is built by `SystemHighContrastThemeFactory` from `ColorValues` (accent) plus the same fixed fallback colors as WinUI where the platform gives
  none.

## Tests

- [ ] New: `Peer_ControlType_IsEdit`; `Peer_Value_IsModelText`; `Peer_SetValue_ReadOnly_Throws`; `Announcement_SetsLiveRegionText` (go to line 5 → the live region holds the WinUI
  string); `HighContrast_On_UsesHighContrastTheme` (fake `IPlatformSettings` in the test app)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated; Result lists the gaps

## Out of scope

The accessible diff viewer (P15-28), automation peers of widgets (with each widget card).

## Suggested commit message

`feat(avalonia): automation peer, announcements and high contrast`

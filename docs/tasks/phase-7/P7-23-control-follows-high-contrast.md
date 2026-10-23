# P7-23 — The control follows Windows high contrast

- **Status:** todo
- **Depends on:** P7-22
- **Size:** M

## Goal

When Windows high contrast is on, the editor (text area, gutter, find widget) uses the colors of the user's scheme (P7-22) instead of the `Theme` property, and returns to the `Theme` when high contrast is switched off, live, without restarting.
`UseSystemHighContrast` turns this off.

## Sources

None to port (ADR 0010 decision 10). Windows API: `Windows.UI.ViewManagement.AccessibilitySettings` (`HighContrast`, `HighContrastChanged`), Win32 `GetSysColor` (`COLOR_WINDOW = 5`, `COLOR_WINDOWTEXT = 8`, `COLOR_HIGHLIGHT = 13`, `COLOR_HIGHLIGHTTEXT = 14`,
`COLOR_BTNFACE = 15`, `COLOR_GRAYTEXT = 17`, `COLOR_BTNTEXT = 18`, `COLOR_HOTLIGHT = 26`; the result is a `COLORREF`: `0x00BBGGRR`).

## Files to create or change

- `src/LoomEditor.WinUI/Interop/NativeMethods.cs` — change: `GetSysColor`
- `src/LoomEditor.WinUI/Accessibility/SystemHighContrast.cs` — create (reads the colors, wraps `AccessibilitySettings`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.HighContrast.cs` — create (the property and the wiring)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Highlighting.cs` — change: `Theme` setter and `ApplyTheme` use the effective theme
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: `OnLoaded` / `OnUnloaded` call `StartHighContrast()` / `StopHighContrast()`
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `docs/testing/phase-7-manual-tests.md` — change: section 6 below

## Design decisions (already made — do not change)

- `NativeMethods.GetSysColor`: `[LibraryImport("user32.dll", EntryPoint = "GetSysColor")] internal static partial uint GetSysColor(int index);`.
- `SystemHighContrast` (`internal sealed`): `static SystemHighContrastColors ReadColors()` builds `ThemeColor.FromRgb(r, g, b)` from each `GetSysColor` result (`r = value & 0xFF`, `g = (value >> 8) & 0xFF`, `b = (value >> 16) & 0xFF`);
  an instance wraps one `AccessibilitySettings`: `bool IsActive => _settings.HighContrast`, `event Action? Changed` raised from `HighContrastChanged` **on the UI thread** (the WinRT event may arrive on another thread: marshal with `DispatcherQueue.TryEnqueue`
  using the control's `DispatcherQueue`, passed to the constructor), `Dispose()` unsubscribes.
- The property: `UseSystemHighContrastProperty` (`bool`, default `true`, callback `RefreshHighContrast`), public `UseSystemHighContrast`.
- Effective theme: a field `EditorTheme? _systemHighContrastTheme` (`null` = not in system high contrast). `RefreshHighContrast()`: if `UseSystemHighContrast && _highContrast.IsActive` then
  `_systemHighContrastTheme = SystemHighContrastThemeFactory.Create(SystemHighContrast.ReadColors())`, else `null`; then `ApplyTheme(EffectiveTheme)`. `EffectiveTheme => _systemHighContrastTheme ?? _theme`.
  The `Theme` setter stores `_theme` and applies `EffectiveTheme` (so setting `Theme` while high contrast is on does not override the user's scheme, but is remembered for when it goes off). The `Theme` getter still returns `_theme` (what the application set): document it in the XML doc of `Theme`.
- `StartHighContrast()` (`OnLoaded`): creates `_highContrast ??= new SystemHighContrast(DispatcherQueue)`, subscribes `Changed += RefreshHighContrast`, calls `RefreshHighContrast()`. `StopHighContrast()` (`OnUnloaded`): unsubscribes and disposes it, `_highContrast = null`.
  Before `OnLoaded` the property callback does nothing (no `_highContrast` yet): `OnLoaded` then applies.
- `ApplyTheme` already updates the renderer, the highlighter (token colors), the find widget and invalidates the canvas: reuse it, add nothing for the widgets.
- The scrollbars and the rest of the XAML template follow WinUI's own high contrast resources: not touched here. If something in `Generic.xaml` hard-codes a color (`grep -n "#[0-9A-Fa-f]\{6\}" src/LoomEditor.WinUI/Themes/Generic.xaml src/LoomEditor.WinUI/Controls/Find/FindWidget.xaml`),
  replace it with a theme resource only if the manual check 6.6 shows it unreadable; otherwise list it in the commit body.
- Public API: only the property pair.

## Tests

No unit test (WinUI + Windows settings); the theme building is tested by P7-22. Manual checks, `docs/testing/phase-7-manual-tests.md` section 6 "High contrast" (Windows 11: Settings → Accessibility → Contrast themes; `Left Alt + Left Shift + Print Screen` toggles the last high contrast theme):

- [ ] 6.1 Gallery open with a C# sample and a selection; turn on **Aquatic** → within a second the editor background and text become the scheme's, the selection uses the scheme's highlight colors with readable text, the Gallery window does not need a restart
- [ ] 6.2 Repeat with **Desert**, **Dusk**, **Night sky** → text readable in each, selection readable, caret visible, line numbers readable, the active line is outlined
- [ ] 6.3 Turn high contrast off → the editor returns to its `Theme` (Dark Modern by default)
- [ ] 6.4 `UseSystemHighContrast = false` (set from code) with high contrast on → the editor keeps its `Theme`; back to `true` → the scheme's colors
- [ ] 6.5 Open the find widget (`Ctrl+F`) in a high contrast scheme → input, buttons, toggles and the result count are readable; a toggled option (match case) is distinguishable from an untoggled one
- [ ] 6.6 Scrollbars, minimap, sticky scroll lines and the folding chevrons are visible in each scheme (note any that vanish)
- [ ] 6.7 Set `Theme = EditorTheme.BuiltIn[8]` ("Dark High Contrast") with system high contrast **off** → black background, white text, black text on the white selection (P7-21)
- [ ] 6.8 Native AOT build (README §6): 6.1 and 6.3

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` (README §6) — result reported in the commit body
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Reduced transparency, dark / light **app** theme following (the `Theme` property stays application-controlled), a Gallery switch (P7-24).

## Suggested commit message

`feat(winui): follow the Windows high contrast colors`

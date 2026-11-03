# P15-17 — Find and replace widget

- **Status:** todo
- **Depends on:** P15-16
- **Size:** M (~600 lines with AXAML; code ~400)

**Recommended model:** Sonnet

## Goal

Ctrl+F / Ctrl+H (Cmd on macOS) open the find widget in the Avalonia `LoomEditor`, with the same options, keyboard, match count, replace and theme as WinUI.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/Find/FindWidget.xaml`, `FindWidget.xaml.cs`, `FindWidget.Keyboard.cs`, `FindWidget.Replace.cs`, `FindWidget.Theme.cs`,
`Controls/LoomEditor.Find.cs`, and the `PART_FindWidget` element in `Themes/Generic.xaml`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/Find/FindWidget.axaml`, `FindWidget.axaml.cs`, `FindWidget.Keyboard.cs`, `FindWidget.Replace.cs`, `FindWidget.Theme.cs`,
  `Controls/LoomEditor.Find.cs` — create (twins)
- `src/LoomEditor.Avalonia/Themes/Generic.axaml` — `PART_FindWidget` in the `LoomEditor` template (right, top, margin `0,0,14,0`, `IsVisible=False`)
- `src/LoomEditor.Avalonia/Themes/Icons.axaml` — the icons the widget uses ([README](README.md) decision 6): chevron right/down (toggle replace), arrow up/down, close, replace,
  replace all, and the case / whole word / regex / preserve case toggles (simple paths, or `Aa`, `ab`, `.*` text in a `TextBlock` when WinUI also uses text)
- `tests/LoomEditor.Avalonia.Tests/Controls/FindWidgetTests.cs` — create
- `docs/testing/phase-15-manual-tests.md` — section "P15-17"

## Design decisions (already made — do not change)

- AXAML with `x:DataType` and compiled bindings only where WinUI uses `x:Bind`; event handlers in code-behind as WinUI. `TextBox` is Avalonia's.
- Toggles: `ToggleButton` with the same automation names and tooltips (`ToolTip.Tip`).
- The widget is an overlay child of the template (not a `Popup`), as WinUI.

## Tests

- [ ] New: `CtrlF_OpensWidget_FocusesFindBox`; `TypingInFindBox_HighlightsMatches_ShowsCount` (`"1 of 3"`, same string as WinUI); `Enter_GoesToNextMatch`;
  `Escape_ClosesAndFocusesEditor`; `ReplaceAll_ReplacesEveryMatch`; `RegexToggle_InvalidPattern_ShowsNoResults`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Find in the diff editor beyond what each side editor already gives.

## Suggested commit message

`feat(avalonia): find and replace widget`

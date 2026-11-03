# P16-14 — Share the find widget logic

- **Status:** todo
- **Depends on:** P16-03
- **Size:** M

**Recommended model:** Sonnet

## Goal

The find widget's state handling (connect, refresh, buttons, matches count, find-in-selection, keys, theme colors) exists once; the XAML and framework calls stay per host.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/Find/FindWidget.xaml.cs`, `FindWidget.Replace.cs`, `FindWidget.Keyboard.cs`, `FindWidget.Theme.cs` and the Avalonia twins (`FindWidget.axaml.cs`, …).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/Find/FindWidget.axaml.cs` | 34+123 | `Connect`, `Disconnect`, `OnStateChanged`, `Refresh` | WinUI `FindWidget.xaml.cs` 29+123 |
| `src/LoomEditor.Avalonia/Controls/Find/FindWidget.Replace.cs` | 12+23, 40+30, 137+33 | `UpdateButtons`, `UpdateFindInSelectionToggle`, `OnReadOnlyChanged`, `UpdateMatchesCount`, body of `OnFindInSelectionClick` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Find/FindWidget.Keyboard.cs` | 14+29 | the handled-key set | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Find/FindWidget.Theme.cs` | 7+25 | `ApplyTheme`, `Set` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/Find/FindWidget.Logic.cs` — new: the members of the first two rows (named parts used through `Text`, `IsEnabled`, `IsChecked` only; visibility, focus, tool tips
  through host members in `FindWidget.Host.cs`).
- `src/LoomEditor.HostShared/Controls/Find/FindWidget.Keyboard.cs` — the key set as View `KeyCode`s or the shared part of the handler; the framework key mapping stays per host.
- `src/LoomEditor.HostShared/Controls/Find/FindWidget.Theme.cs` — `ApplyTheme`; `Set(string brushKey, …)` becomes the host member `SetBrushColor(string key, byte a, byte r, byte g, byte b)`.
- The code-behind files (`FindWidget.xaml.cs` / `FindWidget.axaml.cs`) keep the constructor and `InitializeComponent`.

## Design decisions (already made — do not change)

- XAML files are not touched. Named parts keep their names.
- The WinUI XAML compiler generates the named fields in the code-behind's partial: shared files can use them by name.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).
- [ ] `FindWidgetTests` pass unchanged.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The `FindController` in View (already shared). Suggest and hover theme files (P16-17).

## Suggested commit message

`refactor(hosts): share the find widget logic`

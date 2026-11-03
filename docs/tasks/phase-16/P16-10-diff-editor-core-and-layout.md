# P16-10 — Share the duplicated members of `LoomDiffEditor.cs` and `LoomDiffEditor.Layout.cs`

- **Status:** todo
- **Depends on:** P16-08
- **Size:** M

**Recommended model:** Sonnet

## Goal

The diff state API, decoration updates, text and language application and the layout reactions of `LoomDiffEditor` exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs`, `LoomDiffEditor.Layout.cs`, `LoomDiffEditor.Options.cs` (WinUI's `ApplyText` lives there) and the Avalonia twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.cs` | 149+38 | `IsDiffUpToDate`, `ChangeCount`, `Changes`, `CollapseAllUnchangedRegions`, `ShowAllUnchangedRegions`, `WaitForDiffAsync` | WinUI `LoomDiffEditor.cs` 84+38 |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.cs` | 276+73 | `OnViewModelDiffChanged`, `UpdateDecorations`, `OnOriginalContentChanged`, `OnModifiedContentChanged`, `UpdateDecorationsIfLineCountChanged` | WinUI `LoomDiffEditor.cs` 211+73 |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.cs` | 367+34 | `ApplyText`, `ApplyLanguageAndTheme` | WinUI `LoomDiffEditor.Options.cs` 267+34 |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.Layout.cs` | 17+54 | `OnOptionsChanged`, `OnSashMoved`, `OnSashReset`, `UpdateViewZones` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/DiffEditor/LoomDiffEditor.State.cs` — new: the members of the first two rows.
- `src/LoomEditor.HostShared/Controls/DiffEditor/LoomDiffEditor.Text.cs` — new: `ApplyText`, `ApplyLanguageAndTheme` (moved out of WinUI `LoomDiffEditor.Options.cs` and Avalonia `LoomDiffEditor.cs`).
- `src/LoomEditor.HostShared/Controls/DiffEditor/LoomDiffEditor.Layout.cs` — from the WinUI file; framework layout calls (column widths, `InvalidateArrange`/`InvalidateMeasure`) → `LoomDiffEditor.Layout.Host.cs`.

## Design decisions (already made — do not change)

- The host files `LoomDiffEditor.cs` and `LoomDiffEditor.Options.cs` keep their names and the rest of their members (constructor, properties, `OnLoaded`/`OnUnloaded`).

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Option properties (`DependencyProperty` / `StyledProperty` declarations stay per host).

## Suggested commit message

`refactor(hosts): share the diff state, decoration and layout members of LoomDiffEditor`

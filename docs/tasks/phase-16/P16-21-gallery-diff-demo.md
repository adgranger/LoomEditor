# P16-21 — Gallery: share the diff demo window logic

- **Status:** done
- **Depends on:** P16-20
- **Size:** S

**Recommended model:** Sonnet

## Goal

The diff demo window's staging actions, comment sample and diff-changed logging exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `samples/LoomEditor.Gallery/DiffDemoWindow.Actions.cs`, `.Comments.cs`, `.xaml.cs` and the Avalonia twins (the `sonar-fix` work may have touched the Avalonia `DiffDemoWindow.Actions.cs`: start from the current file).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `samples/LoomEditor.Gallery.Avalonia/DiffDemoWindow.Actions.cs` | 6+34, 87+90 | `InitializeActions`, `ApplyWordWrap`, `LogStaging`, `Format`, `CreateLargeStagingPair`, … | WinUI same path |
| `samples/LoomEditor.Gallery.Avalonia/DiffDemoWindow.Comments.cs` | 5+36, 68+22 | `InitializeComments`, `SideOrRemoved`, `Log` | WinUI same path |
| `samples/LoomEditor.Gallery.Avalonia/DiffDemoWindow.axaml.cs` | 34+17 | `OnDiffChanged` | `DiffDemoWindow.xaml.cs` 28+17 |

## Files to create or change

- Shared `samples/LoomEditor.Gallery/DiffDemoWindow.Actions.cs`, `samples/LoomEditor.Gallery/DiffDemoWindow.Comments.cs` (linked into Avalonia); `DiffDemoWindow.Actions.Host.cs`, `DiffDemoWindow.Comments.Host.cs`
  per Gallery (event handlers with `RoutedEventArgs`, glyph strings — Avalonia's `Glyph(…, fallback)` stays there).
- `OnDiffChanged` → shared `samples/LoomEditor.Gallery/DiffDemoWindow.Log.cs`.

## Design decisions (already made — do not change)

- Where the two Galleries behave differently (`Math.Max(0, SelectedIndex)`, glyph fallbacks), the difference stays in the host part.

## Tests

- [x] No new test. `--diff-selfcheck` PASS on the Avalonia Gallery.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The XAML/AXAML files.

## Suggested commit message

`refactor(gallery): share the diff demo window logic`

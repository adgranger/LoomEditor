# P16-11 — Share the accessible diff viewer logic

- **Status:** todo
- **Depends on:** P16-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

Opening, closing and feeding the accessible diff viewer, and the row building of `AccessibleDiffViewer`, exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.AccessibleViewer.cs`, `src/LoomEditor.WinUI/Controls/DiffEditor/AccessibleDiffViewer.cs` and their twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.AccessibleViewer.cs` | 34+37, 74+20, 96+38 | `OpenAccessibleDiffViewer`, `CloseAccessibleDiffViewer`, `RefreshScreenReader`, `UpdateViewerDiffs`, `DestroyAccessibleViewer`, the viewer host adapter (`OriginalLineCount` … `ModifiedReveal`) | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/AccessibleDiffViewer.cs` | 60+14, 128+37 | constructor part, `RebuildRows` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/DiffEditor/LoomDiffEditor.AccessibleViewer.cs` — from the WinUI file; host parts for focus and placement of the viewer.
- `src/LoomEditor.HostShared/Controls/DiffEditor/AccessibleDiffViewer.cs` — shared part: `RebuildRows` and the row model loop; each host keeps `AccessibleDiffViewer.Host.cs` with the base class,
  `CreateRow` (framework controls) and automation properties.

## Design decisions (already made — do not change)

- The rows' automation names and order stay as in WinUI.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).
- [ ] `AccessibleDiffViewerTests` pass unchanged.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The View model `AccessibleDiffViewerModel` (already shared).

## Suggested commit message

`refactor(hosts): share the accessible diff viewer logic`

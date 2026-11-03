# P16-22 — Gallery: share the host API demo window logic

- **Status:** todo
- **Depends on:** P16-20
- **Size:** S

**Recommended model:** Sonnet

## Goal

The host API demo's decoration, gutter, view zone and blame logic exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `samples/LoomEditor.Gallery/HostApiDemoWindow.Diff.cs`, `.Blame.cs` and the Avalonia twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `samples/LoomEditor.Gallery.Avalonia/HostApiDemoWindow.Diff.cs` | 8+41, 51+24, 87+11, 106+39 | `RegisterStyles`, `InitializeGutter`, conflict colors, conflict header, zone removal, `OnGutterClicked` | WinUI same path |
| `samples/LoomEditor.Gallery.Avalonia/HostApiDemoWindow.Blame.cs` | 26+55 | body of `RedrawBlame` (text and layout arithmetic) | WinUI same path |

## Files to create or change

- Shared `samples/LoomEditor.Gallery/HostApiDemoWindow.Diff.cs` and `samples/LoomEditor.Gallery/HostApiDemoWindow.Blame.cs` (linked); `.Host.cs` per Gallery: handlers, `Canvas` children creation, colors
  (`Windows.UI.Color` / `Avalonia.Media.Color`), clip.

## Design decisions (already made — do not change)

- The blame layout arithmetic is shared; creating `TextBlock`s and setting `Canvas.Left/Top` stays per host.

## Tests

- [ ] No new test. `--hostapi-selfcheck` PASS on the Avalonia Gallery.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Other windows.

## Suggested commit message

`refactor(gallery): share the host API demo window logic`

# P16-12 — Share the diff block actions controller

- **Status:** todo
- **Depends on:** P16-08
- **Size:** M

**Recommended model:** Sonnet

## Goal

Placement, hover and hit logic of the block action buttons exist once; only the buttons themselves (creation, glyph, tool tip, theme brushes) stay per host.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/DiffEditor/DiffBlockActionsController.cs`, `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.BlockActions.cs` and their twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/DiffEditor/DiffBlockActionsController.cs` | 22+22, 75+172, 296+25 | fields; `SetBlocks`, `Configure`, `OnPointerMoved`, `OnPointerLeft`, `Arrange`, `Dispose`, `LineOf`, `Anchor`, `LineCount`, `FirstBlockFrom`, `BlockAt`, `SetHoveredLine`, `Place`; the `Slot` record | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.BlockActions.cs` | 99+54 | `AttachBlockActions`, `DetachBlockActions`, `CreateDiffGutter`, `UpdateGutterLanes`, `GetGutterLanes`, `ConfigureGutter` | WinUI 76+54 |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/DiffEditor/DiffBlockActionsController.cs` — from the WinUI file, `partial`; each host → `DiffBlockActionsController.Host.cs` with: `Button CreateButton(…)`,
  `SetGlyph`, `SetToolTip`, `SetVisible`, `ApplyTheme`'s brush code, `OnClick(object sender, <RoutedEventArgs>)`. `Place` keeps the arithmetic shared and calls a host
  member for `Canvas.SetLeft/SetTop` only if the two spellings differ.
- `src/LoomEditor.HostShared/Controls/DiffEditor/LoomDiffEditor.BlockActions.cs` — the six members above; the rest of each host file → `LoomDiffEditor.BlockActions.Host.cs`.

## Design decisions (already made — do not change)

- Pointer handlers already take plain coordinates from `DiffGutterControllers` (shared in P16-09); keep that.
- The automation name of each button stays as in WinUI.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).
- [ ] `DiffBlockActionsTests` pass unchanged.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Line check boxes (P16-13).

## Suggested commit message

`refactor(hosts): share the diff block actions controller`

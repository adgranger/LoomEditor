# P16-13 — Share the diff line check boxes and line selection

- **Status:** done
- **Depends on:** P16-12
- **Size:** M

**Recommended model:** Sonnet

## Goal

Line check box placement, hover, check state and the line selection API of `LoomDiffEditor` exist once; the check box controls stay per host.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/DiffEditor/DiffLineCheckBoxesController.cs`, `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.LineSelection.cs`, their twins, and P16-12's split as the model.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/DiffEditor/DiffLineCheckBoxesController.cs` | 25+23, 94+30, 125+82, 208+22, 241+35 | fields; `Configure`, `OnPointerMoved`, `OnPointerLeft`, `SyncChecks`, `Arrange`, `ApplyTheme`, `Dispose`, `LineOf`, `SetChecked`, `SetHoveredLine`, `PlaceBox` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.LineSelection.cs` | 77+29, 123+23, 157+18, 189+30 | `SetSelection`, `ClearSelection`, `ToggleLine`, `ToggleLinePair`, `ExtendLineSelection`, the body of `OnLineActionKeyDown`, `ToDiffLine`, `UpdateLineSelectionBlocks`, `CreateLineCheckBoxes`, `OnLineActionClicked`, `ArrangeLineCheckBoxes`, `RaiseSelectionChanged` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/DiffEditor/DiffLineCheckBoxesController.cs` — shared part (same split as P16-12); `DiffLineCheckBoxesController.Host.cs` per host: `CreateBox`, check box events,
  brushes, automation.
- `src/LoomEditor.HostShared/Controls/DiffEditor/LoomDiffEditor.LineSelection.cs` — the members above; the key handler keeps the framework key test in `LoomDiffEditor.LineSelection.Host.cs` and calls
  a shared `bool HandleLineActionKey(…)` with View key values.

## Design decisions (already made — do not change)

- Shift-click range extension and the selection-changed event order stay as in WinUI.

## Tests

- [x] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).
- [x] `DiffLineSelectionTests` pass unchanged.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

New keyboard shortcuts.

## Suggested commit message

`refactor(hosts): share the diff line check boxes and line selection`

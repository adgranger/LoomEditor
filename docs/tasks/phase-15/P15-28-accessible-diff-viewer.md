# P15-28 — The accessible diff viewer

- **Status:** todo
- **Depends on:** P15-27
- **Size:** M

**Recommended model:** Sonnet

## Goal

F7 / Shift+F7 open the accessible diff viewer on Avalonia. It is a list of changed rows that a screen reader reads one by one, with the same row text and navigation as WinUI.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/DiffEditor/AccessibleDiffViewer.cs`, `LoomDiffEditor.AccessibleViewer.cs`. Also read
`src/LoomEditor.View/DiffEditor/DiffViewerRowItem.cs` (moved in P15-02) and `IAccessibleDiffViewerHost`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/DiffEditor/AccessibleDiffViewer.cs`, `LoomDiffEditor.AccessibleViewer.cs` — create (twins)
- `tests/LoomEditor.Avalonia.Tests/Controls/DiffEditor/AccessibleDiffViewerTests.cs` — create
- `docs/testing/phase-15-manual-tests.md` — section "P15-28" (Narrator, VoiceOver, Orca)

## Design decisions (already made — do not change)

- `ListView` → `ListBox` (virtualized). Rows are built in code from `DiffViewerRowItem`, and `AutomationProperties.Name` is set from `AutomationName`.

## Tests

- [ ] New: `F7_OpensViewer_FocusesFirstChange`; `Down_MovesToNextRow`; `Row_AutomationName_IsRowItemName`; `Escape_ClosesAndFocusesModifiedEditor`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

The phase 14 actions (P15-29, P15-30).

## Suggested commit message

`feat(avalonia): the accessible diff viewer`

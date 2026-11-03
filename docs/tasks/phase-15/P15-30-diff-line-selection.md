# P15-30 — Diff line checkboxes, selection and the line action

- **Status:** todo
- **Depends on:** P15-29
- **Size:** M (~550 lines; mechanical, allowed above 400)

**Recommended model:** Sonnet

## Goal

The phase 14 line selection on Avalonia: `LineSelectionEnabled`, the checkboxes, Ctrl/Cmd+click for a pair, Shift+click ranges, `SelectedLines`, `SetSelection`,
`ClearSelection`, `SelectionChanged`, the line action button and key, `LineActionInvoked`. `LoomDiffEditor` is complete on Avalonia.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/DiffEditor/DiffLineCheckBoxesController.cs`, `LoomDiffEditor.LineSelection.cs`. Also read
`docs/tasks/phase-14/P14-04-line-checkboxes-and-line-action.md` and `src/LoomEditor.View/DiffEditor/DiffLineSelection.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/DiffEditor/DiffLineCheckBoxesController.cs`, `LoomDiffEditor.LineSelection.cs` — create (twins)
- `src/LoomEditor.Avalonia/Controls/DiffEditor/DiffGutterControllers.cs` — remove the P15-29 `null`
- `tests/LoomEditor.Avalonia.Tests/Controls/DiffEditor/DiffLineSelectionTests.cs` — create
- `docs/testing/phase-15-manual-tests.md` — section "P15-30"
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`

## Design decisions (already made — do not change)

- `LineActionKey` is `Avalonia.Input.Key`, default `Key.S` ([README](README.md) decision 3). The pair gesture is Ctrl+click, which is Cmd+click on macOS through
  `KeyMapping.ToModifiers`.

## Tests

- [ ] New: `Click_TogglesOneLine`; `CtrlClick_TogglesPair`; `ShiftClick_ExtendsFromAnchor`; `NewDiff_SameVersions_KeepsSelection`; `LineActionKey_RaisesLineActionInvoked`
- [ ] Parity: no `LoomDiffEditor` key left in `AvaloniaParityPending.txt`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

The Gallery (P15-31).

## Suggested commit message

`feat(avalonia): diff line checkboxes and the line action`

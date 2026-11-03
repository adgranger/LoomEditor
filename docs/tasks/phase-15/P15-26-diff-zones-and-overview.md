# P15-26 — Diff alignment zones and the diff overview ruler

- **Status:** done
- **Depends on:** P15-25
- **Size:** M

**Recommended model:** Sonnet

## Goal

The two sides stay aligned (padding zones for inserted and deleted lines, scroll sync), and the diff overview ruler shows and navigates the changes on Avalonia.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs`, `DiffOverviewRuler.cs`, `LoomDiffEditor.Overview.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/DiffEditor/` — the same 3 files (twins)
- `src/LoomEditor.Avalonia/Themes/Generic.axaml` — `PART_Overview` in the `LoomDiffEditor` template (replaces the P15-24 placeholder)
- `tests/LoomEditor.Avalonia.Tests/Controls/DiffEditor/DiffAlignmentTests.cs` — create

## Design decisions (already made — do not change)

- `DiffOverviewRuler` draws in `Render(DrawingContext)` with the same painter (`OverviewRulerPainter` from Rendering) through an `AvaloniaRenderSurface`, as WinUI does through Win2D.

## Tests

- [x] New: `InsertedBlock_OriginalSideGetsPaddingZone` (line after the block has the same top on both sides); `Scroll_OneSide_ScrollsTheOther`; `OverviewRuler_DrawsChangeMarks`;
  `OverviewRuler_Click_ScrollsToChange`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Hidden unchanged regions (P15-27).

## Suggested commit message

`feat(avalonia): diff alignment zones and overview ruler`

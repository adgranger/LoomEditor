# P15-29 — Diff block action buttons

- **Status:** todo
- **Depends on:** P15-28
- **Size:** M

**Recommended model:** Sonnet

## Goal

The phase 14 block actions on Avalonia: glyph lanes, the per-block primary and secondary buttons (`BlockActionsVisibility`, glyphs, tooltips) and `BlockActionInvoked`.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/DiffEditor/DiffBlockActionsController.cs`, `LoomDiffEditor.BlockActions.cs`, `DiffGutterControllers.cs`. Also read
[phase-14/README.md](../phase-14/README.md) (decisions 1–4), `docs/tasks/phase-14/P14-03-glyph-lanes-and-block-buttons.md`, and [README](README.md) decision 6 (glyph font).

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/DiffEditor/DiffBlockActionsController.cs`, `LoomDiffEditor.BlockActions.cs`, `DiffGutterControllers.cs` — create (twins; `DiffGutterControllers`
  takes a `null` line-checkbox controller until P15-30, behind a `// P15-30` comment)
- `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.BlockActions.cs` — also the Avalonia-only `GlyphFontFamily` styled property (decision 6), default
  `"Segoe Fluent Icons, Segoe MDL2 Assets"`
- `tests/LoomEditor.Avalonia.Tests/Controls/DiffEditor/DiffBlockActionsTests.cs` — create
- `docs/PORTING.md` — "Behavior differences": host glyphs need `GlyphFontFamily` outside Windows
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`

## Design decisions (already made — do not change)

- WinUI `FontIcon { Glyph, FontSize = 12 }` → `TextBlock { Text = glyph, FontFamily = GlyphFontFamily, FontSize = 12 }` inside the `Button`. Everything else (pool, visible
  blocks only, anchor side, hover rule, automation name) is as in the twin.

## Tests

- [ ] New: `Always_FiveBlocks_FiveButtons`; `Click_Third_RaisesBlockActionInvoked_WithChanges2`; `PureDeletion_ButtonOnOriginalSide`; `OnHover_OnlyHoveredBlock`;
  `NotSideBySide_NoButtons`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Line checkboxes (P15-30).

## Suggested commit message

`feat(avalonia): per-block action buttons in the LoomDiffEditor gutter`

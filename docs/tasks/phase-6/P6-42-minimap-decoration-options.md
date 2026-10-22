# P6-42 — Minimap options of decorations

- **Status:** todo
- **Depends on:** P6-36
- **Size:** S

## Goal

A decoration can ask to be painted in the minimap (`minimap: { color, darkColor, position }`), and the decorations-changed
event reports `AffectsMinimap` precisely.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model.ts` | 76–90 | `MinimapPosition` (`Inline = 1`, `Gutter = 2`), `MinimapSectionHeaderStyle` (`Normal = 1`, `Underlined = 2`) |
| same | 128–144 | `IModelDecorationMinimapOptions` (`position`, `sectionHeaderStyle`, `sectionHeaderText`) |
| `src/vs/editor/common/model/textModel.ts` | 2417–2452 | `ModelDecorationMinimapOptions` (`getColor` → the view, P6-43) |
| same | 2509, 2549 | `ModelDecorationOptions.minimap` |
| same | 2658–2664 | `checkAffectedAndFire`: `affectsMinimap ||= !!options.minimap?.position` |

## Files to create or change

- `src/LoomEditor.Core/Decorations/MinimapPosition.cs` — create (with `MinimapSectionHeaderStyle`)
- `src/LoomEditor.Core/Decorations/ModelDecorationMinimapOptions.cs` — create (header `textModel.ts`)
- `src/LoomEditor.Core/Decorations/ModelDecorationOptionsData.cs`, `ModelDecorationOptions.cs` — change: `Minimap` (update the
  "Not ported" remark: `overviewRuler` and `minimap` are ported)
- `src/LoomEditor.Core/Model/TextModel.DecorationsEmitter.cs` — change: the minimap flag in `CheckAffectedAndFireDecorations`;
  update the file comment ("only the glyph margin of the four flags can be set" is no longer true)
- `tests/LoomEditor.Core.Tests/Decorations/ModelDecorationMinimapOptionsTests.cs` — create

## Design decisions (already made — do not change)

- `ModelDecorationMinimapOptions(DecorationColor? Color, DecorationColor? DarkColor, MinimapPosition Position, MinimapSectionHeaderStyle? SectionHeaderStyle = null, string? SectionHeaderText = null)`
  as an `internal sealed record` (the raw data uses the same record), with `DecorationColor` from P6-36.
- The section header members are kept (they are plain data) even though section headers are not drawn (P6-40).
- `MinimapPosition` has no `0` value in Monaco; a missing `position` is a programming error in C# (it is required in the record).

## Tests

- [ ] New: `MinimapOptions_KeepTheirValues`
- [ ] New: `DeltaDecorations_WithMinimapPosition_SetsAffectsMinimap`; `…_WithoutMinimap_DoesNotSetIt`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: decorations rows; remove the `ModelDecorationOptions` row of "Cut points for phase 6"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Reading minimap decorations from the view (P6-43).

## Suggested commit message

`feat(core): add minimap options to decorations`

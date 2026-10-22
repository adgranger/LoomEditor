# P6-43 — The view model feeds the minimap: lines data and minimap decorations

- **Status:** todo
- **Depends on:** P6-42, P6-41, P6-37
- **Size:** S

## Goal

The view model answers what the minimap needs: the rendering data of a range of view lines (with the tab size) and the
decorations of a view range that have minimap options (bracket colorization excluded), plus the resolution of a minimap
decoration color against the theme.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel.ts` | 238–249 | `MinimapLinesRenderingData` |
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 847–849 | `getMinimapDecorationsInRange` |
| same | 933–939 | `getMinimapLinesRenderingData` |
| `src/vs/editor/common/viewModel/viewModelDecorations.ts` | 32, 65–67 | the `onlyMinimapDecorations` argument and `getMinimapDecorationsInRange` |
| `src/vs/editor/common/viewModel/inlineDecorations.ts` | 60–90 | the `onlyMinimapDecorations` parameter of the context and of `getDecorations` |
| `src/vs/editor/common/viewModel/viewModelLines.ts` | 934–1000 | `getDecorationsInRange(… onlyMinimapDecorations …)` |
| `src/vs/editor/common/model/textModel.ts` | 1826–1833 | `getDecorationsInRange(… onlyMinimapDecorations …)`: passed to the decoration providers only |
| `src/vs/editor/common/model/bracketPairsTextModelPart/colorizedBracketPairsDecorationProvider.ts` | 45–49 | bracket colors are never minimap decorations |
| `src/vs/editor/common/model/textModel.ts` | 2417–2452 | `ModelDecorationMinimapOptions.getColor` (theme resolution, dark color) |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.Decorations.cs`, `src/LoomEditor.Core/Decorations/IDecorationProvider.cs`,
  `src/LoomEditor.Core/Model/BracketPairs/ColorizedBracketPairsDecorationProvider.cs` — change: the `onlyMinimapDecorations`
  parameter (default `false`), exactly where Monaco has it
- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.Conversions.cs`, `InlineDecorations.cs`, `ViewModelDecorations.cs` — change:
  plumb `onlyMinimapDecorations`; add `GetMinimapDecorationsInRange`; update the "Behavior difference" remark of `GetDecorationsInRange`
- `src/LoomEditor.View/Layout/MinimapLinesRenderingData.cs` — create (header `viewModel.ts`)
- `src/LoomEditor.View/Layout/ViewModel.Decorations.cs` — change: `GetMinimapDecorationsInRange`, `GetMinimapLinesRenderingData`
- `src/LoomEditor.View/Layout/DecorationColorResolver.cs` — change: a `ResolveMinimap` overload if the rule differs (Monaco's minimap
  `getColor` returns `undefined` for an unknown color and parses hex with `Color.fromHex`: same as the ruler rule; reuse `Resolve`)
- `tests/LoomEditor.View.Tests/Layout/ViewModelMinimapDataTests.cs` — create

## Design decisions (already made — do not change)

- `MinimapLinesRenderingData(int TabSize, ViewLineData?[] Data)` record.
- `GetMinimapLinesRenderingData(int startLineNumber, int endLineNumber, bool[] needed)` returns the view lines' data (wrapped and
  injected text included, like Monaco).
- `onlyMarginDecorations` stays not ported (glyph margin lanes).

## Tests

- [ ] New: `GetMinimapDecorationsInRange_DoesNotReturnBracketColors`
- [ ] New: `GetMinimapDecorationsInRange_ReturnsDecorationsWithMinimapOptions`
- [ ] New: `GetMinimapLinesRenderingData_HasTheTabSizeAndTheNeededLines`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `viewModelDecorations.ts` row (no longer "not `getMinimapDecorationsInRange`"); remove the
      `ViewModelDecorations.cs` minimap remark
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The minimap view part (P6-46 … P6-52).

## Suggested commit message

`feat(view): provide the minimap lines and decorations`

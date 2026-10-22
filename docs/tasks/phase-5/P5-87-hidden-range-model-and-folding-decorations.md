# P5-87 — Hidden range model and the folding decoration provider

- **Status:** done
- **Depends on:** P5-86
- **Size:** M

## Goal

`HiddenRangeModel` turns the collapsed regions of a `FoldingModel` into the hidden areas that the view model hides,
moves selections out of hidden lines, and tells whether a line is hidden. `FoldingDecorationProvider` gives each region
its decoration options, with Monaco's names (`inline-folded` after content, `folded-background`, the `codicon-folding-*`
first-line class names), according to `showFoldingControls` and `foldingHighlight`. `inline-folded` and
`folded-background` are already drawn (`DecorationStyles` rows of P5-08); the gutter icons are drawn in P5-90.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/folding/browser/hiddenRangeModel.ts` | 16–150 | `HiddenRangeModel`, `isInside`, `findRange` |
| `src/vs/base/common/arraysFind.ts` | search `findFirstIdxMonotonousOrArrLen` | the binary search `findRange` uses (inline as a private helper) |
| `contrib/folding/browser/foldingDecorations.ts` | 17–19 | the colors `editor.foldBackground`, `editor.foldPlaceholderForeground`, `editorGutter.foldingControlForeground` |
| same | 21–24, 28–29 | icon class names (`codicon-folding-expanded`, `-collapsed`, `-manual-collapsed`, `-manual-expanded`); tooltips (not ported) |
| same | 31–169 | `FoldingDecorationProvider`: the 12 registered options, `showFoldingControls`, `showFoldingHighlights`, `getDecorationOption`, `changeDecorations`, `removeDecorations` |
| `contrib/folding/test/browser/hiddenRangeModel.test.ts` | all | `hasRanges` |

## Files to create or change

- `src/LoomEditor.View/Features/Folding/HiddenRangeModel.cs` — create
- `src/LoomEditor.View/Features/Folding/FoldingDecorationProvider.cs` — create
- `src/LoomEditor.View/Features/Folding/ShowFoldingControls.cs` — create: `enum ShowFoldingControls { Always, Never, Mouseover }`
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the three color ids (see decisions) that P5-08 did not add
- `tests/LoomEditor.View.Tests/Features/Folding/HiddenRangeModelTests.cs`, `FoldingDecorationProviderTests.cs` — create

## Design decisions (already made — do not change)

- `HiddenRangeModel(FoldingModel model)`: `event EventHandler<IReadOnlyList<Range>>? Changed` (Monaco `onDidChange`),
  `HiddenRanges`, `NotifyChangeModelContent(ModelContentChangedEventArgs e)`, `HasRanges()`, `IsHidden(int line)`,
  `AdjustSelections(Selection[] selections) → bool` (changes the array in place, like Monaco), `Dispose()`.
- `countEOL(change.text)[0] !== 0` → `change.Text.AsSpan().IndexOfAny('\r', '\n') >= 0`.
- `if (adjustedStartLine)` is JavaScript truthiness (phase 3 trap 5): a result of **0** (hidden range starting at line 1)
  means "no change". Write `if (adjusted is int line && line != 0)`.
- Hidden ranges are `Range(start, 1, end, 1)` as Monaco.
- `FoldingDecorationProvider(ViewModel viewModel)` implements `IDecorationProvider` (P5-85). Monaco's
  `editor.changeDecorations` / `removeDecorations` become `viewModel.Model.ChangeDecorations(viewModel.EditorId, …)`
  (step A: the editor's decorations owner id). Properties `ShowFoldingControls ShowFoldingControls = Mouseover` and
  `bool ShowFoldingHighlights = true`.
- The 12 options are `static readonly` fields registered with `ModelDecorationOptions.Register`, Monaco's descriptions,
  stickiness, `AfterContentClassName = "inline-folded"`, `ClassName = "folded-background"`, `IsWholeLine = true`, and
  `FirstLineDecorationClassName` = `"codicon codicon-folding-collapsed"`, `"codicon codicon-folding-manual-collapsed"`,
  `"alwaysShowFoldIcons codicon codicon-folding-expanded"`, `"codicon codicon-folding-expanded"`,
  `"alwaysShowFoldIcons codicon codicon-folding-manual-expanded"`, `"codicon codicon-folding-manual-expanded"`
  (Monaco's `ThemeIcon.asClassName`). Not ported: `linesDecorationsTooltip` (no hover in phase 5), `minimap` (phase 6).
- Colors (`EditorColorDefaults`, with the upstream line in a comment):
  `editor.foldBackground` = `transparent(editor.selectionBackground, 0.3)` for dark and light, `null` for high contrast;
  `editor.foldPlaceholderForeground` = `#808080` (dark, light), `null` (hc);
  `editorGutter.foldingControlForeground` = `icon.foreground` (= `#C5C5C5`, `#424242`, `#FFFFFF`, `#292929`,
  `src/vs/platform/theme/common/colors/baseColors.ts` 33–35). If the table cannot express `transparent(id, factor)`
  yet, this card adds it (`colorUtils.ts`: `transparent` multiplies the alpha of the resolved color).

## Tests

- [x] Port: `Hidden Range Model` › `hasRanges`
- [x] New: `AdjustSelections_SelectionInHiddenRange_MovesToTheEndOfTheHeaderLine`
- [x] New: `AdjustSelections_HiddenRangeFromLineOne_LeavesTheSelection` (the truthiness case) — not written: no folding region can start at line 0, so a hidden range from line 1 cannot be built; the `!= 0` guard is kept as in Monaco
- [x] New: `NotifyChangeModelContent_LineInserted_RaisesChangedAgain`
- [x] New: `GetDecorationOption_Matrix` (Theory over `isCollapsed` × `isHidden` × `isManual` × `ShowFoldingControls` ×
      `ShowFoldingHighlights`: the expected option field, as Monaco 141–164)
- [x] New (Themes): `FoldBackground_IsTheSelectionBackgroundWithThirtyPercentAlpha` (Dark Modern) — already covered by `EditorColorDefaultsTests` (`#264F784D`); the three colors were already in `EditorColorDefaults`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: rows `contrib/folding/browser/hiddenRangeModel.ts` (✅ P5-87), `foldingDecorations.ts`
      (🔶 no tooltips, no minimap, P5-87); "Tests ported": `hiddenRangeModel.test.ts` 1 / 1
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing the class names (P5-90). The controller that connects the models to the view model (P5-88).

## Suggested commit message

`feat(folding): hidden range model and folding decorations`

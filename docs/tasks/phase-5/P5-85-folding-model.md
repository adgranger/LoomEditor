# P5-85 — `FoldingModel`: regions, collapse state and their decorations

- **Status:** todo
- **Depends on:** P5-84
- **Size:** M

## Goal

`FoldingModel` holds the current `FoldingRegions` of a text model, keeps one model decoration per region (through an
`IDecorationProvider`), toggles collapse states, merges new provider results with the folded and manual ranges
(`update`), removes manual ranges, and answers "which regions are at / inside this line". It raises a change event
that the hidden range model (P5-87) listens to.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/folding/browser/foldingModel.ts` | 14–23 | `IDecorationProvider`, `FoldingModelChangeEvent` |
| same | 33–160 | `FoldingModel`: fields, constructor, `toggleCollapseState`, `removeManualRanges`, `update`, `updatePost` |
| same | 162–188 | `_currentFoldedOrManualRanges` |
| same | 247–316 | `dispose`, `getAllRegionsAtLine`, `getRegionAtLine`, `getRegionsInside`, `RegionFilter`, `RegionFilterWithLevel` |
| `contrib/folding/test/browser/foldingModel.test.ts` | 1–137 | `TestDecorationProvider` and the assert helpers |
| same | 139–433, 779–838, 945–1092 | the 13 tests listed below |

## Files to create or change

- `src/LoomEditor.View/Features/Folding/FoldingModel.cs` — create
- `src/LoomEditor.View/Features/Folding/IDecorationProvider.cs` — create (`IDecorationProvider`, `FoldingModelChangeEvent`)
- `tests/LoomEditor.View.Tests/Features/Folding/TestHelpers/TestDecorationProvider.cs` — create (port of `TestDecorationProvider`)
- `tests/LoomEditor.View.Tests/Features/Folding/FoldingModelTests.cs` — create

## Design decisions (already made — do not change)

- `IDecorationProvider`: `ModelDecorationOptions GetDecorationOption(bool isCollapsed, bool isHidden, bool isManual)`,
  `T ChangeDecorations<T>(Func<IModelDecorationsChangeAccessor, T> callback)`, `void RemoveDecorations(IReadOnlyList<string> decorationIds)`
  (step A types: `ModelDecorationOptions`, the change accessor with `DeltaDecorations` and `ChangeDecorationOptions`,
  decoration ids as `string`). Monaco's `T | null` return becomes `T`.
- `FoldingModel(TextModel textModel, IDecorationProvider decorationProvider)`; properties `Regions`, `TextModel`,
  `DecorationProvider`; `event EventHandler<FoldingModelChangeEvent>? Changed` (Monaco `onDidChange`);
  `FoldingModelChangeEvent(FoldingModel Model, IReadOnlyList<FoldingRegion>? CollapseStateChanged)`.
- `toggleCollapseState` sorts with a **stable** sort on `RegionIndex` (phase 3 trap 3); `processed` is a
  `HashSet<string>` of decoration ids.
- `GetRegionsInside` has two overloads (Monaco tests `filter.length === 2`, the arity of the function):
  `(FoldingRegion? region, Func<FoldingRegion, bool>? filter = null)` and `(FoldingRegion? region, Func<FoldingRegion, int, bool> filter)`.
  `Number.MAX_VALUE` → `int.MaxValue`.
- The decoration of region *i* spans `(start, maxColumn(start))` to `(end, maxColumn(end) + 1)` as Monaco; the model
  validates it.
- **Not ported:** `getMemento`, `applyMemento`, `_getLinesChecksum`, `CollapseMemento` (view-state persistence; no
  `saveViewState` in LoomEditor; needs `base/common/hash.ts`). A `// Cut point:` comment says so.
- `TestDecorationProvider` registers its three option objects with `ModelDecorationOptions.Register` (description
  `test`, `NeverGrowsWhenTypingAtEdges`, `LinesDecorationsClassName = "folding"`) and lists the decorations with
  `model.GetAllDecorations()` (P5-03), comparing options by reference.

## Tests

- [ ] Port: `Folding Model` › `getRegionAtLine`, `collapse`, `update`, `delete`, `getRegionsInside`,
      `getRegionsInsideWithLevel`, `getRegionAtLine2`, `folding decoration`,
      `removeManualRanges - cursor on manual range removes innermost only`,
      `removeManualRanges - cursor skips provider ranges to remove nearest manual range`,
      `removeManualRanges - cursor not on manual range removes all manual ranges`,
      `removeManualRanges - single-line selection removes all intersecting manual ranges`,
      `removeManualRanges - selection range removes intersecting manual ranges`
- [ ] New: `ToggleCollapseState_RaisesChangedWithTheToggledRegions`
- [ ] New: `Dispose_RemovesTheDecorations`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: new row `contrib/folding/browser/foldingModel.ts` (🔶 class, no memento, P5-85);
      "Tests ported": `foldingModel.test.ts` 13 / 22 so far
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The free functions of `foldingModel.ts` (`setCollapseState…`, fold jumping: P5-86). The real decoration provider (P5-87).

## Suggested commit message

`feat(folding): port the folding model`

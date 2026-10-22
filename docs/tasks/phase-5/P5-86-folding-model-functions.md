# P5-86 — Folding model functions: fold levels, recursive folding, fold jumping

- **Status:** done
- **Depends on:** P5-85
- **Size:** M

## Goal

The free functions of `foldingModel.ts` exist: toggle and set the collapse state by levels, up / down, at a level, for
the rest, for matching lines and for a type, and find the parent / previous / next fold line. The folding commands of
P5-89 are thin wrappers around them.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/folding/browser/foldingModel.ts` | 320–347 | `toggleCollapseState(foldingModel, levels, lineNumbers)` |
| same | 349–428 | `setCollapseStateLevelsDown`, `setCollapseStateLevelsUp`, `setCollapseStateUp`, `setCollapseStateAtLevel`, `setCollapseStateForRest` |
| same | 430–466 | `setCollapseStateForMatchingLines`, `setCollapseStateForType` |
| same | 468–603 | `getParentFoldLine`, `getPreviousFoldLine`, `getNextFoldLine` |
| `contrib/folding/test/browser/foldingModel.test.ts` | 434–778, 840–944 | the 9 tests listed below |

## Files to create or change

- `src/LoomEditor.View/Features/Folding/FoldingModelFunctions.cs` — create: `internal static class FoldingModelFunctions`
- `tests/LoomEditor.View.Tests/Features/Folding/FoldingModelFunctionsTests.cs` — create (reuses `TestDecorationProvider` of P5-85)

## Design decisions (already made — do not change)

- Monaco's names in PascalCase, `FoldingModel` first: `ToggleCollapseState(FoldingModel, int levels, IReadOnlyList<int> lineNumbers)`,
  `SetCollapseStateLevelsDown(FoldingModel, bool doCollapse, int levels = int.MaxValue, IReadOnlyList<int>? lineNumbers = null)`, …
  `Number.MAX_VALUE` → `int.MaxValue`. `number | null` returns → `int?`.
- `SetCollapseStateForMatchingLines(FoldingModel, Regex regExp, bool doCollapse)` uses `regExp.IsMatch(line)` (no
  `lastIndex`, phase-5 README trap 1). The test's `new RegExp('^\\s*' + escapeRegExpCharacters('/*'))` is
  `new Regex("^\\s*" + Strings.EscapeRegExpCharacters("/*"))`.
- `SetCollapseStateForType` is ported even though the indentation provider gives no types (phase 8 providers will).
- Monaco's `toggleCollapseState` free function and the `FoldingModel.toggleCollapseState` method keep their two names
  (`FoldingModelFunctions.ToggleCollapseState` vs `FoldingModel.ToggleCollapseState`).

## Tests

- [x] Port: `Folding Model` › `setCollapseStateRecursivly`, `setCollapseStateAtLevel`, `setCollapseStateLevelsDown`,
      `setCollapseStateLevelsUp`, `setCollapseStateUp`, `setCollapseStateForMatchingLines`, `setCollapseStateForRest`,
      `fold jumping`, `fold jumping issue #129503`
- [x] New: `SetCollapseStateForType_CollapsesOnlyThatType` (regions built with `FoldingRegions.FromFoldRanges` with types)
- [x] New: `ToggleCollapseState_LevelsOne_OnlyTheRegionAtTheLine`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `foldingModel.ts` row (functions, P5-86); "Tests ported": `foldingModel.test.ts` 22 / 22
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The commands that call these functions (P5-89).

## Suggested commit message

`feat(folding): port the folding level and fold jumping functions`

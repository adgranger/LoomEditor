# P5-78 — `ConstantTimePrefixSumComputer` and the identity / hidden line projections

- **Status:** todo
- **Depends on:** P5-77
- **Size:** M

## Goal

The two building blocks of `ViewModelLinesFromProjectedModel` exist: Monaco's `ConstantTimePrefixSumComputer` (the
sum of the view line counts of the model lines) and the **identity** and **hidden** model line projections (a visible
model line is one view line, a hidden one is zero view lines). The projection with line breaks (word wrap) is phase 6
(ADR 0009, decision 8).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/prefixSumComputer.ts` | 189–296 | `ConstantTimePrefixSumComputer` (all members) |
| `test/common/viewModel/prefixSumComputer.test.ts` | 32–58 | `createBoth`, `forBoth`: every test runs on both computers |
| `common/viewModel/modelLineProjection.ts` | 16–40 | `IModelLineProjection` (subset, see below) |
| same | 53–64 | `createModelLineProjection`: the `lineBreakData === null` branch only |
| same | 263–349 | `IdentityModelLineProjection` |
| same | 353–424 | `HiddenModelLineProjection` |
| `common/viewModel.ts` | 251–301 | `ViewLineData` (without `inlineDecorations`) |

## Files to create or change

- `src/LoomEditor.Core/Model/ConstantTimePrefixSumComputer.cs` — create
- `src/LoomEditor.Core/Model/PrefixSumComputer.cs` — change: remove "Not ported: `ConstantTimePrefixSumComputer`" from the summary
- `src/LoomEditor.View/Layout/ModelLineProjection.cs` — create: `IModelLineProjection`, `ModelLineProjection.Create`,
  `IdentityModelLineProjection`, `HiddenModelLineProjection`
- `src/LoomEditor.View/Layout/ViewLineData.cs` — create
- `tests/LoomEditor.Core.Tests/Model/PrefixSumComputerTests.cs` — change: every test runs on both computers
- `tests/LoomEditor.View.Tests/Layout/ModelLineProjectionTests.cs` — create

## Design decisions (already made — do not change)

- `ConstantTimePrefixSumComputer` keeps Monaco's `number[]` as `List<int>` (`_values`, `_prefixSum`, `_indexBySum`);
  `arrayInsert` becomes `List<int>.InsertRange`. It returns the existing `PrefixSumIndexOfResult`. Public members
  (Monaco names): `GetTotalSum()`, `GetPrefixSum(int count)` (sum of the **first `count`** values, unlike
  `PrefixSumComputer.GetPrefixSum(index)`), `GetIndexOf(int sum)`, `RemoveValues(int start, int deleteCount)`,
  `InsertValues(int insertIndex, IReadOnlyList<int> insertArr)`, `SetValue(int index, int value)`.
- `PrefixSumComputerTests`: the existing private `Psc` wrapper becomes an abstract wrapper with two implementations
  (`PrefixSumComputer`: `GetPrefixSum(count) => count == 0 ? 0 : GetPrefixSum(count - 1)` as today; constant time:
  direct). Every `[Fact]` becomes a `[Theory]` with `[InlineData(false)]` / `[InlineData(true)]` (`constantTime`), so
  the 48 tests run twice, like Monaco's `forBoth`.
- `IModelLineProjection` members (Monaco names, PascalCase; `model` is the `TextModel`, there is no `ISimpleModel`):
  `IsVisible`, `SetVisible`, `GetViewLineCount`, `GetViewLineContent`, `GetViewLineLength`, `GetViewLineMinColumn`,
  `GetViewLineMaxColumn`, `GetViewLineContinuesWithWrappedLine`, `GetViewLineData`, `GetViewLinesData`,
  `GetModelColumnOfViewPosition`, `GetViewPositionOfModelPosition`, `GetViewLineNumberOfModelPosition`,
  `NormalizePosition`. **Not ported** (phase 6 cut point, written as a comment on the interface):
  `getProjectionData`, `getInjectedTextAt`.
- `ModelLineProjection.Create(bool isVisible)` returns the two singletons (`IdentityModelLineProjection.Instance`,
  `HiddenModelLineProjection.Instance`). Phase 6 adds the overload with line-break data.
- The hidden projection throws `InvalidOperationException("Not supported")` where Monaco throws `Error('Not supported')`.
- `ViewLineData` = `internal sealed record ViewLineData(string Content, bool ContinuesWithWrappedLine, int MinColumn,
  int MaxColumn, int StartVisibleColumn, IViewLineTokens Tokens)`. The identity projection builds it with
  `model.Tokenization.GetLineTokens(line).Inflate()` (as `ViewModel.GetViewLineTokens` does today).
- `GetViewLinesData(model, modelLineNumber, outputLineIdx, lineCount, globalStartIndex, bool[] needed, ViewLineData?[] result)`:
  `result` is an array the caller sizes, as Monaco's `result` array.

## Tests

- [ ] Port: `Editor ViewModel - PrefixSumComputer` › all 48 tests now run on both computers (96 runs)
- [ ] New: `IdentityProjection_ReturnsModelLine` — content, length, min/max column, `GetViewLineCount() == 1`,
      `GetViewLineData` tokens equal the model's inflated tokens
- [ ] New: `HiddenProjection_HasNoViewLine` — `GetViewLineCount() == 0`, `GetViewLineContent` throws `InvalidOperationException`
- [ ] New: `SetVisible_SwitchesBetweenTheTwoSingletons`
- [ ] New: `IdentityProjection_GetViewPositionOfModelPosition_KeepsTheColumn` (`deltaLineNumber` is the view line)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: the `prefixSumComputer.ts` row includes `ConstantTimePrefixSumComputer`; new row
      `common/viewModel/modelLineProjection.ts` (🔶 identity and hidden projections, P5-78); the
      `prefixSumComputer.test.ts` row says the tests run on both computers
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`ViewModelLinesFromProjectedModel` (P5-79, P5-80). The projection with line breaks, injected text (phase 6).

## Suggested commit message

`feat(view): port the constant-time prefix sum and the identity and hidden line projections`

# P6-16 — Wrapped projection: `GetViewLinesData` with tokens and injected text

- **Status:** todo
- **Depends on:** P6-15
- **Size:** S

## Goal

The wrapped projection returns `ViewLineData` per output line: the sliced tokens (injected text gets the default token),
the wrapped indent, the start visible column and the inline decorations of the injected text.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/modelLineProjection.ts` | 156–223 | `getViewLineData`, `getViewLinesData`, `_getViewLineData` |
| same | 16–40 | the `baseViewLineNumber` parameter of `getViewLineData` / `getViewLinesData` (LoomEditor dropped it in P5-78) |
| `src/vs/editor/common/model/textModel.ts` | 2135–2163 | `getLineTokensWithInjections` |

## Files to create or change

- `src/LoomEditor.View/Layout/ModelLineProjection.cs` — change: add `int baseViewLineNumber` to `GetViewLineData` and
  `GetViewLinesData` of the interface and of the identity / hidden projections (they ignore it), and remove the remark that says it
  was dropped
- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.cs` — change: pass the base view line number exactly as
  Monaco `viewModelLines.ts` does: `getViewLineData` (768–772) passes
  `projectedModelLineLineCounts.getPrefixSum(info.modelLineNumber - 1) + 1`, `getViewLinesData` (774–809) passes
  `projectedModelLineLineCounts.getPrefixSum(modelLineIndex) + 1` (copy both expressions as they are, even if they look
  asymmetric: the C# `ConstantTimePrefixSumComputer.GetPrefixSum` is a faithful port)
- `src/LoomEditor.View/Layout/WrappedModelLineProjection.Data.cs` — create (partial; same header)
- `src/LoomEditor.Core/Tokens/LineTokensWithInjections.cs` — create (header `textModel.ts`): `internal static class` with
  `GetLineTokensWithInjections(LineTokens tokens, IReadOnlyList<ModelDecorationInjectedTextOptions>? injectionOptions, IReadOnlyList<int>? injectionOffsets)`
- `tests/LoomEditor.Core.Tests/Tokens/LineTokensWithInjectionsTests.cs` — create
- `tests/LoomEditor.View.Tests/Layout/ModelLineProjectionTests.cs` — change: new tests

## Design decisions (already made — do not change)

- `getLineTokensWithInjections`: the `tokens` branch of Monaco (pre-tokenized injected text) is not ported (P6-03 dropped
  `tokens`): every injection is inserted with `LineTokens.DefaultTokenMetadata`, through the existing `LineTokens.WithInserted`.
- The inline decorations come from `new InjectedTextInlineDecorationsComputer(context)` (P6-14) with a private nested context
  class wrapping the projection data and `baseViewLineNumber` (Monaco builds an object literal).
- `lineWithInjections.sliceAndInflate(start, end, deltaStartIndex)` → `LineTokens.SliceAndInflate(start, end, delta)` (exists).
- `ViewLineData.StartVisibleColumn` is an `int` in LoomEditor; the break visible columns are `double` (P6-07):
  convert with `(int)` and say so in a comment (`// integral for monospace fonts with an integer full-width ratio`).
- `GetViewLineData` builds a one-element array and calls `GetViewLinesData`, like Monaco (the doc comment "Try using
  GetViewLinesData instead" is kept).
- `minColumn = GetMinOutputOffset + 1`, `maxColumn = lineContent.Length + 1` exactly as Monaco.

## Tests

- [ ] New (Core): `GetLineTokensWithInjections_NoInjections_ReturnsTheSameTokens`; `…_InsertsDefaultTokens`
- [ ] New (View): `GetViewLinesData_WrappedLine_SlicesTokensAndAddsIndent` (a TextModel line of 3 tokens, breaks inside the
      second token, indent 2: check content, `MinColumn`, `MaxColumn`, `ContinuesWithWrappedLine`, `StartVisibleColumn` and the
      token end offsets / metadata of each view line)
- [ ] New (View): `GetViewLinesData_WithInjectedText_HasInlineDecorations` (an injection with `InlineClassName`)
- [ ] New (View): `GetViewLinesData_NotNeeded_IsNull`

The upstream tests that exercise this (`getViewLinesData - with wrapping`, `… and injected text`) are ported in P6-19.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `modelLineProjection.ts` row ✅ (all three projections); new row for `getLineTokensWithInjections`
      (in the `textModel.ts` row or a new one) noting that `tokens` of injected text is not ported
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model lines building wrapped projections (P6-17).

## Suggested commit message

`feat(view): build the view line data of wrapped lines`

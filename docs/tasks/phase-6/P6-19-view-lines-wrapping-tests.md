# P6-19 — Port the wrapping tests of `modelLineProjection.test.ts`

- **Status:** todo
- **Depends on:** P6-18
- **Size:** S

## Goal

`getViewLinesData - with wrapping` and `getViewLinesData - with wrapping and injected text` pass against the wrapped view
lines, which closes `modelLineProjection.test.ts` except the guide assertions of `Invalid line numbers` (P6-31). Test card:
fix production code only where a test reveals a porting bug (P6-07 … P6-18).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/test/browser/viewModel/modelLineProjection.test.ts`.

| Lines | What to port |
|---|---|
| 274–441 | suite `SplitLinesCollection` setup and helpers — **already ported** in P5-79 for `getViewLinesData - no wrapping` (find it: `grep -rn "getViewLinesData - no wrapping" tests/`); reuse it |
| 576–747 | `getViewLinesData - with wrapping` |
| 749–946 | `getViewLinesData - with wrapping and injected text` |
| 948–983 | `withSplitLinesCollection(model, wordWrap, wordWrapColumn, wrapOnEscapedLineFeeds, callback)` |

## Files to create or change

- the test file that holds `getViewLinesData - no wrapping` (P5-79) — change: add the two tests and a
  `WithSplitLinesCollection(model, WordWrap wordWrap, int wordWrapColumn, bool wrapOnEscapedLineFeeds, Action<ViewModelLinesFromProjectedModel>)`
  helper
- production files of P6-07 … P6-18 — only to fix bugs the tests reveal (list them in the commit body)

## Design decisions (already made — do not change)

- Monaco builds a `TestConfiguration({ wordWrap, wordWrapColumn, wrappingIndent: 'indent' })` and reads the computed options.
  LoomEditor has no options service: the helper computes the wrapping column with `EditorLayoutInfoComputer.ComputeLayout`
  (P6-02) when `wordWrap` is `On` / `Bounded`, and uses `wordWrapColumn` directly for `WordWrapColumn` (both tests use
  `'wordWrapColumn'`, so `ComputeLayout` is not needed for them: write the helper for `WordWrapColumn` and `Off` only and
  `throw new NotSupportedException()` otherwise). Font: `FontInfo.Default` (it is Monaco's test font). Factory:
  `MonospaceLineBreaksComputerFactory` with the default break characters. Editor id `1`, `WrappingIndent.Indent`,
  `WordBreak.Normal`, `useTwoCellFullwidthCharacters: false`.
- `ITestMinimapLineRenderingData` / the `assert*` helpers exist from P5-79; extend them if `inlineDecorations` must be
  compared (Monaco's helper compares only content, columns and tokens: check lines 395–441 and keep the same coverage).

## Tests

- [ ] Port: `SplitLinesCollection` › `getViewLinesData - with wrapping`
- [ ] Port: `SplitLinesCollection` › `getViewLinesData - with wrapping and injected text`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `modelLineProjection.test.ts` is 6 / 6 (the guide assertions of `Invalid line numbers` stay open
      until P6-31); remove its row from "Cut points for phase 6" when P6-31 is done, not now
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model (P6-20).

## Suggested commit message

`test(view): port the wrapping tests of the split lines collection`

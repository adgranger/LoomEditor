# P6-23 — Port the word wrap tests of `viewModelDecorations.test.ts`

- **Status:** done
- **Depends on:** P6-22
- **Size:** S

## Goal

`getDecorationsViewportData` and `issue #17208` pass: view decorations and inline decorations of wrapped lines are cut per
view line correctly. Test card: production code changes only to fix what a test reveals (most likely in
`InlineModelDecorationsComputer` or `ViewModelLinesFromProjectedModel.GetDecorationsInRange`).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/test/browser/viewModel/viewModelDecorations.test.ts` | 17–173 | `getDecorationsViewportData` |
| same | 175–217 | `issue #17208: Problem scrolling in 1.8.0` |

## Files to create or change

- `tests/LoomEditor.View.Tests/Layout/ViewModelDecorationsTests.cs` — change: add the two tests (the class already holds the
  P5-07 port of `issue #37401`; reuse its way of calling `getViewportViewLineRenderingData` / `getDecorationsInViewport`)
- production code — only to fix a bug a test reveals

## Design decisions (already made — do not change)

- Use `TestCodeEditor` with `Wrapping = new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 13 }`
  and `EditorId: 1` (P6-22 added both options).
- `viewModel.getLineContent(n)` → the view model's view-line content getter (`GetLineContent`).
- Monaco compares `ViewModelDecoration` objects with `deepStrictEqual`; compare range and options' class names (the same
  fields Monaco's expected literals list) with a small local helper.
- If the expected inline decorations have `InlineDecorationType.Before` / `After` with column ranges that LoomEditor computes
  differently, the bug is in LoomEditor (Monaco is the reference).

## Tests

- [x] Port: `ViewModelDecorations` › `getDecorationsViewportData`
- [x] Port: `ViewModelDecorations` › `issue #17208: Problem scrolling in 1.8.0`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `viewModelDecorations.test.ts` is 3 / 3; remove the `Layout/ViewModel*.cs` row of "Cut points for
      phase 6" (its tests are all ported after P6-22 and this card)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`lineDecorations.test.ts` (the `LineDecorationsNormalizer` is not used by the Win2D renderer and stays unported).

## Suggested commit message

`test(view): port the word wrap tests of the view model decorations`

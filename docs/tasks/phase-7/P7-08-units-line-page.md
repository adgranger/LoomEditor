# P7-08 — Text units: line (view line) and page

- **Status:** done
- **Depends on:** P7-07
- **Size:** M

## Goal

The `Line` unit follows the **view lines** (a wrapped model line is several lines, like on screen) and the `Page` unit follows the visible window, so that Narrator reads
"line by line" and "page by page" as the user sees them.

## Sources

None (specified in `docs/tasks/phase-7/README.md` §4). Uses `ViewModel.CoordinatesConverter` (`ConvertViewPositionToModelPosition`, `ConvertModelPositionToViewPosition`),
`ViewModel.GetLineCount()` (the number of **view** lines), `ViewModel.GetLineMinColumn(viewLine)` and `ViewModel.GetCompletelyVisibleViewRange()` (`src/LoomEditor.View/Layout/ViewModel.Cursors.cs`).

## Files to create or change

- `src/LoomEditor.View/Accessibility/LineUnitStarts.cs`, `PageUnitStarts.cs` — create
- `src/LoomEditor.View/Accessibility/AutomationUnitStarts.cs` — change: `Create` returns them (no unit throws any more)
- `tests/LoomEditor.View.Tests/Accessibility/LineUnitStartsTests.cs`, `PageUnitStartsTests.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/AutomationUnitStartsTests.cs` — change: add `Create_EveryUnit_ReturnsStarts`

## Design decisions (already made — do not change)

- **Line starts.** View line `v` (1-based, `1 … viewModel.GetLineCount()`) *starts* at the model position `ViewStart(v) = CoordinatesConverter.ConvertViewPositionToModelPosition(new Position(v, viewModel.GetLineMinColumn(v)))`.
  The starts are ascending in `v`. Text hidden by folding lies between two consecutive starts, so it belongs to the line before it (README §4). Injected text is not in the model, so it never shifts a start.
- `StartAtOrBefore(p)`: binary search over `v ∈ [1, viewLineCount]` for the greatest `v` with `ViewStart(v) <= p` (`ViewStart(1) == DocumentStart` always holds, so one exists). `PreviousStart(p)`: the greatest `ViewStart(v) < p`, else `null`.
  `NextStartStrict(p)`: the smallest `ViewStart(v) > p`, else `null`. All three are one binary search (`O(log n)` converter calls), never a scan over all lines.
  Two consecutive view lines never have the same `ViewStart` (a break is never at a line's start); no special case is needed, comparisons are always on positions, never on view line indices.
- **Page starts.** `N = Math.Max(1, range.EndLineNumber - range.StartLineNumber + 1)` where `range = viewModel.GetCompletelyVisibleViewRange()`; the starts are `ViewStart(k * N + 1)` for `k = 0, 1, 2, …` while `k * N + 1 <= viewLineCount`.
  `N` is read at each call (the viewport may have been resized between calls). `StartAtOrBefore(p)`: `v` = the view line found as for `Line`; `k = (v - 1) / N`; the result is `ViewStart(k * N + 1)`.
  `PreviousStart(p)`: `s = StartAtOrBefore(p)`; if `s < p` → `s`; else if `k > 0` → `ViewStart((k - 1) * N + 1)`; else `null`. `NextStartStrict(p)`: `k = (v - 1) / N`; `(k + 1) * N + 1 <= viewLineCount ? ViewStart((k + 1) * N + 1) : null`.
- When the viewport has no size (a view model that never had `SetViewportSize`), `GetCompletelyVisibleViewRange()` may be empty or one line: `N` is then 1 (the `Math.Max`).
- `Create` now throws only `ArgumentOutOfRangeException` for an undefined enum value.
- `internal sealed`, XML doc + `Threading` remark.

## Tests

(`WithDocument` with `TestCodeEditorOptions(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 })` for the wrapped cases; first assert in each test that the view lines are the ones the
test relies on — `viewModel.GetLineContent(1) == "aaaa bbbb "`, `GetLineContent(2) == "cccc"` for the text `"aaaa bbbb cccc"` — and if the break differs by a column, change the text, never the production code)

- [x] New (`LineUnitStartsTests`): `S_NoWrap_IsTheModelLineStart`, `N_NoWrap_IsTheNextModelLineStart`, `S_SecondViewLine_IsTheBreakPosition` (`"aaaa bbbb cccc"`: `S((1,12)) = (1,11)`),
      `N_FirstViewLine_IsTheBreakPosition` (`N((1,3)) = (1,11)`), `P_SecondViewLine_Positions` (`P((1,12)) = (1,11)`, `P((1,11)) = (1,1)`),
      `N_LastViewLine_IsNull`, `P_AtDocumentStart_IsNull`, `NS_LastViewLine_IsDocumentEnd`,
      `WrappedLineThenNextModelLine` (`"aaaa bbbb cccc\nx"`: `N((1,11)) = (2,1)`),
      `FoldedLines_BelongToThePreviousViewLine` (3 model lines, lines 2–3 hidden with `viewModel.SetHiddenAreas([new Range(2, 1, 3, 1)])` — use the helper the folding tests use, `grep -rn "SetHiddenAreas" tests`:
      `S((3,1)) = (1,1)` and `N((1,1)) = null` when nothing follows; with a 4th visible line `N((1,1)) = (4,1)`),
      `EmptyDocument_StartIsDocumentStart_NextIsNull`, `LargeDocument_BinarySearchIsCorrect` (10 000 lines: `S((5000,1)) = (5000,1)`, `N((5000,1)) = (5001,1)`, `P((5000,1)) = (4999,1)`)
- [x] New (`PageUnitStartsTests`; `OuterHeight` chosen so that `GetCompletelyVisibleViewRange()` is lines 1–3, asserted first: start from `3 * 19` and adjust the height, never the code): `S_FirstPage_IsDocumentStart`,
      `S_FifthLine_IsTheStartOfPageTwo` (`S((5,1)) = (4,1)`), `N_FirstPage_IsLineFour`, `N_LastPage_IsNull`, `P_PageTwo_IsPageOneStart`, `P_AtDocumentStart_IsNull`, `NoViewport_PagesAreOneLine`
      (`OuterHeight: 0`), `PagesFollowViewLinesNotModelLines` (wrapped text)
- [x] New (`AutomationUnitStartsTests`): `Create_EveryUnit_ReturnsStarts` (theory over the 7 units: `StartAtOrBefore(DocumentStart) == DocumentStart`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Expanding and moving ranges (P7-09), screen coordinates (P7-11).

## Suggested commit message

`feat(view): add the line and page text units`

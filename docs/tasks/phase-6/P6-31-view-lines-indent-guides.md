# P6-31 — View lines: indent guides and the active indent guide

- **Status:** todo
- **Depends on:** P6-30, P6-20
- **Size:** S

## Goal

`ViewModelLinesFromProjectedModel` answers the indent guides per **view** line (a wrapped continuation repeats the guides of
its model line, hidden lines are skipped) and the active indent guide in view line numbers; the guide assertions of
`Invalid line numbers` pass.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/viewModel/viewModelLines.ts`.

| Lines | What to port |
|---|---|
| 463–482 | `getActiveIndentGuide` |
| 508–534 | `getModelStartPositionOfViewLine`, `getModelEndPositionOfViewLine` |
| 536–584 | `getViewLineInfosGroupedByModelRanges` |
| 670–741 | `getViewLinesIndentGuides` (with `IndentGuideRepeatOption`) |
| 1079–1082 | `class ViewLineInfoGroupedByModelRange` |
| 1132–1136 | `const enum IndentGuideRepeatOption` |

Test: `src/vs/editor/test/browser/viewModel/modelLineProjection.test.ts` 150–161 (the `getOutputIndentGuide` assertions of
`Invalid line numbers`; the rest of that test was ported in P5-80).

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.Guides.cs` — create (partial; header `viewModelLines.ts`)
- `src/LoomEditor.View/Layout/ViewLineInfo.cs` — change: add `ViewLineInfoGroupedByModelRange` (`internal sealed record (Range ModelRange, IReadOnlyList<ViewLineInfo> ViewLines)`)
- the test file with the P5-80 port of `Invalid line numbers` (`grep -rln "Monaco: 'Invalid line numbers'" tests`) — change: add
  the guide assertions (lines 150–161) where Monaco has them
- `tests/LoomEditor.View.Tests/Layout/ViewModelLinesGuidesTests.cs` — create

## Design decisions (already made — do not change)

- `GetViewLinesIndentGuides(int viewStartLineNumber, int viewEndLineNumber)` → `int[]`; `GetActiveIndentGuide(int viewLineNumber, int minLineNumber, int maxLineNumber)`
  → `ActiveIndentGuideInfo` in view line numbers; both call `_model.Guides` (P6-28, P6-29).
- `IndentGuideRepeatOption` is a private nested `enum` (`BlockNone`, `BlockSubsequent`, `BlockAll`).
- `getViewLineInfosGroupedByModelRanges` is `private` and is shared with P6-32.
- Remove "the indent and bracket guides" from the "Phase 6 cut points" remark of `ViewModelLinesFromProjectedModel` in P6-32
  (when both are done), not here.

## Tests

- [ ] Port: `Editor ViewModel - SplitLinesCollection` › `Invalid line numbers` — the 11 `getViewLinesIndentGuides` assertions
- [ ] New: `GetViewLinesIndentGuides_WrappedLine_RepeatsTheGuidesOnContinuations` (an indented line that wraps)
- [ ] New: `GetViewLinesIndentGuides_HiddenLines_AreSkipped`
- [ ] New: `GetActiveIndentGuide_ReturnsViewLineNumbers` (a wrapped line above the block shifts the view line numbers)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: the `modelLineProjection.test.ts` gap ("without the indent guide assertions") is closed
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Bracket guides in view lines (P6-32), drawing (P6-34).

## Suggested commit message

`feat(view): compute the indent guides of view lines`

# P6-18 — `ViewModelLinesFromProjectedModel`: model changes with line breaks, `GetInjectedTextAt`

- **Status:** todo
- **Depends on:** P6-17
- **Size:** S

## Goal

Inserted and changed model lines get their projection from line-break data computed by the caller (two-pass, like
Monaco), and the view lines answer which injected text is at a view position.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/viewModel/viewModelLines.ts`.

| Lines | What to port |
|---|---|
| 351–385 | `onModelLinesInserted(versionId, fromLineNumber, _toLineNumber, lineBreaks)` |
| 387–434 | `onModelLineChanged(versionId, lineNumber, lineBreakData)` |
| 1002–1005 | `getInjectedTextAt` |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.cs` — change: `OnModelLinesInserted` takes
  `IReadOnlyList<ModelLineProjectionData?> lineBreaks` instead of `int count`; `OnModelLineChanged` takes
  `ModelLineProjectionData? lineBreakData`
- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.Conversions.cs` — change: add `GetInjectedTextAt(Position)`
- `src/LoomEditor.View/Layout/ViewModel.cs` — change: the **minimal** call-site update (see decisions)
- `tests/LoomEditor.View.Tests/Layout/ViewModelLinesTests.cs` (and any other test calling the two methods) — change: pass
  `null` breaks
- `tests/LoomEditor.View.Tests/Layout/ViewModelLinesWrappingTests.cs` — change: new tests

## Design decisions (already made — do not change)

- `OnModelLinesInserted(int? versionId, int fromLineNumber, int toLineNumber, IReadOnlyList<ModelLineProjectionData?> lineBreaks)`:
  the line count is `lineBreaks.Count`; keep the existing `Guard.Assert` but against `lineBreaks.Count`.
- The two-pass computation in `ViewModel.OnModelRawContentChanged` (Monaco `viewModelImpl.ts` 322–352: a first loop that adds
  requests to one computer for every inserted / changed line, then `finalize`, then a queue) is P6-20. In this card the view model
  passes `new ModelLineProjectionData?[count]` (all `null`) and `null`: behavior is unchanged until P6-20.
- `GetInjectedTextAt(Position viewPosition)` → `InjectedText?`.

## Tests

- [ ] New: `OnModelLinesInserted_WithLineBreaks_InsertsTheWrappedViewLines` (insert a long line with data from the real factory;
      the returned view range covers all its view lines)
- [ ] New: `OnModelLineChanged_LineNowWraps_ReportsInsertedViewLines`; `…_LineNoLongerWraps_ReportsDeletedViewLines`;
      `…_SameCount_ReportsChangedOnly`
- [ ] New: `GetInjectedTextAt_OnInjectedText_ReturnsIt`; `…_OnText_ReturnsNull`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model two-pass computation and events (P6-20).

## Suggested commit message

`feat(view): apply line breaks to changed and inserted model lines`

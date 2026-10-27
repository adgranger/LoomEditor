# P9-11 — `DocumentDiffProvider`: diffing two models off the UI thread

- **Status:** todo
- **Depends on:** P9-10
- **Size:** S

## Goal

Given two `TextModel`s, `DocumentDiffProvider.ComputeDiffAsync` returns a `DocumentDiff` (changes, moves, `Identical`, `QuitEarly`) computed on a thread-pool thread and cancellable, as Monaco's web worker does.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/diff/documentDiffProvider.ts` | 16–85 | `IDocumentDiffProvider` (`computeDiff`), `IDocumentDiffProviderOptions`, `IDocumentDiff`, `nullDocumentDiff` |
| `src/vs/editor/common/services/editorWebWorker.ts` | 136–177 | `EditorWorker.computeDiff` (the `identical` rule), `_modelsAreIdentical` |
| `src/vs/editor/browser/widget/diffEditor/diffProviderFactoryService.ts` | 64–112 | the behavior of `WorkerBasedDocumentDiffProvider.computeDiff`: the "original file is empty" shortcut (both empty → identical; else one change `[1,2) -> [1, modifiedLineCount+1)` with one inner change over the full model ranges) and the cancelled result (`identical: false`, `quitEarly: true`, no changes). **Not ported:** the worker call, the telemetry, the `diffCache` (max 10 entries), the `diffAlgorithm` option (always advanced) and `onDidChange` |
| `src/vs/editor/browser/widget/diffEditor/diffEditorViewModel.ts` | 381–414 | `normalizeDocumentDiff`, `normalizeRangeMapping` (an inner change that starts at column 1 and ends at the end of a line, on both sides, is extended to the start of the next line; used by the view model, ported here as `DocumentDiff.Normalize`) |

## Files to create or change

- `src/LoomEditor.Core/Diff/DocumentDiff.cs`, `IDocumentDiffProvider.cs`, `DocumentDiffProvider.cs` — create
- `tests/LoomEditor.Core.Tests/Diff/DocumentDiffProviderTests.cs` — create

## Design decisions (already made — do not change)

- `sealed record DocumentDiff(bool Identical, bool QuitEarly, IReadOnlyList<DetailedLineRangeMapping> Changes, IReadOnlyList<MovedText> Moves)`; `static readonly DocumentDiff Null` (identical, not quit early, empty lists).
- `interface IDocumentDiffProvider { Task<DocumentDiff> ComputeDiffAsync(TextModel original, TextModel modified, LinesDiffComputerOptions options, CancellationToken token); }`.
- `DocumentDiffProvider(ILinesDiffComputer? computer = null)` defaults to `LinesDiffComputers.GetDefault()`. `ComputeDiffAsync` does, **on the calling (UI) thread**: `original.GetLinesContent()` and `modified.GetLinesContent()` (arrays are private copies: the model may change while the worker runs), an already cancelled token → `Task.FromCanceled`; then `Task.Run(() => compute, token)` where `compute` calls `ComputeDiff`, then `Identical = changes.Count == 0 && AllLinesEqual(...)` (Monaco `_modelsAreIdentical` compares the line arrays: do that on the arrays already taken, so it does not touch the models from the worker) and `QuitEarly = result.HitTimeout`.
- Before any worker call, on the UI thread: the "original file is empty" shortcut of lines 74–99 (it reads the line count, the max column of line 1 and the full model range of both models: use the existing `TextModel` members, find their names with `grep -n "GetFullModelRange\|GetLineMaxColumn" src/LoomEditor.Core/Model/TextModel*.cs`).
  A token cancelled **after** the computation returns the result of lines 123–130 (`Identical = false`, `QuitEarly = true`, empty lists) instead of throwing; cancelled **before** it starts → `Task.FromCanceled`.
- No result cache and no `IEditorWorkerService` (no visible behavior difference: the view model skips unchanged versions itself).
- `DocumentDiff.Normalize(int originalLineCount, Func<int, int> originalLineMaxColumn, int modifiedLineCount, Func<int, int> modifiedLineMaxColumn)` is a port of `normalizeDocumentDiff` / `normalizeRangeMapping`, written over line counts and max columns instead of models so it is testable and touches no model. `Range.WithEnd` (existing) replaces `setEndPosition`.
- Exceptions of `ILinesDiffComputer` propagate through the task (the view model handles them, P9-14).
- `<remarks>Threading: ComputeDiffAsync must be called on the UI thread (it reads the models); the diff itself runs on a pool thread and touches no model.</remarks>`
- **No Native AOT risk:** `Task.Run` with a lambda over arrays only.

## Tests

- [ ] New: `ComputeDiffAsync_IdenticalModels_IdenticalTrue`, `ComputeDiffAsync_DifferentModels_ReturnsChanges`, `ComputeDiffAsync_OnlyEolDiffers_Identical` (the models have different EOL but equal lines: `Identical == true`, as in Monaco), `ComputeDiffAsync_CancelledBefore_Cancelled`, `ComputeDiffAsync_ModelEditedWhileRunning_UsesSnapshot` (a computer fake that blocks on a `ManualResetEventSlim`; edit the model; release; the result still describes the old text), `ComputeDiffAsync_QuitEarly_WhenComputerHitTimeout` (fake computer returns `HitTimeout = true`), `ComputeDiffAsync_FakeComputerThrows_TaskFaulted`, `ComputeDiffAsync_OriginalEmpty_ModifiedNot_SingleChange`, `ComputeDiffAsync_BothEmpty_Identical`, `ComputeDiffAsync_CancelledDuringCompute_QuitEarlyResult`, `Normalize_FullLineInnerChange_ExtendedToNextLineStart`, `Normalize_NotAtLineEnd_Unchanged`, `Normalize_LastLine_Unchanged`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `documentDiffProvider.ts`, `editorWebWorker.ts` (`computeDiff`), `diffProviderFactoryService.ts` (early-outs only), `normalizeDocumentDiff`; "Behavior differences": no worker, no cache
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model (P9-14), diff algorithm selection.

## Suggested commit message

`feat(core): add DocumentDiffProvider to diff two models off the UI thread`

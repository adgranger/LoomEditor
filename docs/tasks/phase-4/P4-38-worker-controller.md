# P4-38 — Background tokenization (3/3): the UI side (`TextMateWorkerTokenizerController`) and `IUiDispatcher`

- **Status:** done
- **Depends on:** P4-37
- **Size:** L — split: `TextMateWorkerTokenizerController.cs` (+ `.Rebase.cs`), `ThreadedBackgroundTokenizerFactory.cs`

## Goal

A model with a TextMate language gets its tokens from the worker: the UI side mirrors changes to the worker, receives
the batches on the UI thread, drops stale ones, rebases the others on the edits made since, stores states before
tokens, and marks background tokenization as finished.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/workbench/services/textMate/browser/backgroundTokenization/textMateWorkerTokenizerController.ts` | 28–251 | `TextMateWorkerTokenizerController` (constructor subscriptions, `dispose`, `requestTokens`, `setTokensAndStates` with pending changes, `MonotonousIndexTransformer`, `ContiguousMultilineTokens.applyEdit`, states before tokens, `backgroundTokenizationFinished`), `_stringEditFromChanges`, `changesToString` |
| vscode: `…/backgroundTokenization/threadedBackgroundTokenizerFactory.ts` | 30–212 | `ThreadedBackgroundTokenizerFactory`: `createBackgroundTokenizer` (no worker when the model is too large for syncing), `setGrammarDefinitions`, `acceptTheme`, routing results by controller id, `_disposeWorker`, `keepAliveWhenAttached` — **not** the WASM / web-worker proxy plumbing |
| vscode: `src/vs/editor/common/model/textModel.ts` | 188, 368, 633 | `_MODEL_SYNC_LIMIT` (50 MB), `isTooLargeForSyncing` |

## Files to create or change

- `src/LoomEditor.Core/Threading/IUiDispatcher.cs` — create (`bool Post(Action action)`; ADR 0008 decision 5)
- `src/LoomEditor.TextMate/Tokenization/TextMateWorkerTokenizerController.cs` (+ `.Rebase.cs`), `ThreadedBackgroundTokenizerFactory.cs` — create
- `src/LoomEditor.TextMate/Tokenization/TextMateTokenizationSupport.cs` — change: `CreateBackgroundTokenizer` uses the factory
- `src/LoomEditor.Core/Model/TextModel.Tokenization.cs` — change: `IsTooLargeForSyncing`
- `tests/LoomEditor.TextMate.Tests/TestHelpers/ManualUiDispatcher.cs` — create (queues actions; `RunAll()`)
- `tests/LoomEditor.TextMate.Tests/Tokenization/WorkerControllerTests.cs` — create

## Design decisions (already made — do not change)

- The worker host of P4-37 is implemented here: it captures the batch and calls `IUiDispatcher.Post(() => controller.SetTokensAndStates(…))`.
- Results for a disposed controller or another controller id are dropped (upstream 116–119, 154–162).
- Pending changes: the controller keeps the `MirrorModelChangedEvent`s sent to the worker with their version ids;
  changes with `VersionId <= received` are applied to the controller's own `TokenizationStateStore` mirror and removed
  (144–150); newer ones rebase the batch (158–185).
- `keepAliveWhenAttached`: the controller exists while the model is attached to at least one view (P4-30 attached views).
- Tests use a `TokenizationWorker` stand-in that runs `TextMateWorkerTokenizer` synchronously, and `ManualUiDispatcher`
  to choose when results arrive (between edits).

## Tests

No upstream unit tests. New:

- [x] `Results_ForCurrentVersion_AreStored`
- [x] `Results_AfterAnEdit_AreRebased` (edit lines above the batch before running the dispatcher)
- [x] `Results_ForEditedLines_AreDropped`
- [x] `Results_ForDisposedController_AreIgnored`
- [x] `LastLineReceived_MarksBackgroundTokenizationFinished`
- [x] `TooLargeForSyncing_UsesNoWorker`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Creating the worker for an editor and the WinUI dispatcher (P4-42, P4-45).

## Suggested commit message

`feat(textmate): receive background tokens on the UI thread`

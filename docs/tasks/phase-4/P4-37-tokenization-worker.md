# P4-37 — Background tokenization (2/3): the worker (`textMateWorkerTokenizer.ts`) on its own thread

- **Status:** todo
- **Depends on:** P4-36
- **Size:** L — split: `TextMateWorkerTokenizer.cs` (the logic, no thread), `TokenizationWorker.cs` (thread + channel), `StateDeltas.cs`

## Goal

A background thread keeps a mirror of the document, tokenizes it from the first invalid line in batches of at most
200 lines and slices of 20 ms, and hands each batch (tokens + end states) to a host, while reading the next messages
(changes, retokenize, theme, language, dispose) in order.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/workbench/services/textMate/browser/backgroundTokenization/worker/textMateWorkerTokenizer.ts` | 28–233 | `TextMateModelTokenizerHost`, `TextMateWorkerTokenizer` (constructor, `dispose`, `onLanguageId`, `onEvents`, `acceptMaxTokenizationLineLength`, `retokenize`, `_resetTokenization`, `_tokenize`), `StateDeltaBuilder` — **not** `_getFontTokensUpdate` |
| vscode: `…/worker/textMateTokenizationWorker.worker.ts` | 40–162 | `StateDeltas`, `TextMateTokenizationWorker` (`$acceptNewModel` with the per-language grammar cache, `$acceptModelChanged`, `$retokenize`, `$acceptModelLanguageChanged`, `$acceptRemovedModel`, `$acceptTheme`, `$acceptMaxTokenizationLineLength`), `IRawModelData` — **not** `$init` / WASM loading |
| vscode: `…/worker/textMateWorkerHost.ts` | 12–24 | `TextMateWorkerHost` (`$setTokensAndStates`; not `$readFile`, not `$reportTokenizationTime`) |

## Files to create or change

- `src/LoomEditor.TextMate/Tokenization/Worker/TextMateWorkerTokenizer.cs`, `StateDeltas.cs`, `ITextMateWorkerHost.cs` — create
- `src/LoomEditor.TextMate/Tokenization/Worker/TokenizationWorker.cs`, `WorkerMessage.cs` — create
- `tests/LoomEditor.TextMate.Tests/Tokenization/Worker/TextMateWorkerTokenizerTests.cs`, `TokenizationWorkerTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0008 decision 5. One `TokenizationWorker` per editor: a `Thread` (`IsBackground = true`,
  `Priority = BelowNormal`, name `"LoomEditor tokenizer"`) that owns **its own** `TMGrammarFactory` (P4-34), built from
  the shared `RawGrammarCache`. It reads a `Channel<WorkerMessage>` (unbounded, single reader) synchronously.
- Monaco's scheduling is replaced like this, with the same numbers: wait up to **10 ms** after a message for more
  messages before tokenizing (the `RunOnceScheduler`); tokenize until **200 lines** or **20 ms**, send the batch; then
  read pending messages (`TryRead`) before the next slice (the `setTimeout0` yield).
- `StateDeltas` carries `StartLineNumber` and `IReadOnlyList<StateStack?>` (references, `null` = unchanged), not
  `StackDiff`s. Tokens travel as `ContiguousMultilineTokens` (P4-26). After sending, the worker never touches them.
- `TextMateWorkerTokenizer` has no thread: tests call `AcceptEvents`, `Retokenize` and `TokenizeSlice(maxLines, maxMilliseconds)` directly.
- `ITextMateWorkerHost.SetTokensAndStates(int controllerId, int versionId, IReadOnlyList<ContiguousMultilineTokens>, IReadOnlyList<StateDeltas>)`
  is called **on the worker thread**; the UI side (P4-38) posts it to the UI thread.
- Dispose: a `Dispose` message, then `Thread.Join` with a 1 s timeout; the worker disposes its grammars (native
  scanners) on its own thread (ADR 0008 decision 5). Exceptions inside the loop are caught per message, written with
  `Debug.WriteLine`, and the loop continues.

## Tests

No upstream unit tests. New:

- [ ] `TokenizeSlice_StopsAfter200Lines_AndSendsOneBatch`
- [ ] `TokenizeSlice_StateUnchanged_SendsNullDelta`
- [ ] `AcceptEvents_EditInvalidatesFromEditedLine`
- [ ] `Retokenize_InvalidatesTheRange`
- [ ] `Worker_ProcessesMessagesInOrder_AndStopsOnDispose` (real thread, `ManualResetEventSlim` + 5 s timeout)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green, and stable when run 10 times in a row
- [ ] `docs/PORTING.md` map updated; "Behavior differences": thread + channel instead of a web worker, references instead of serialization
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The UI side (P4-38).

## Suggested commit message

`feat(textmate): tokenize documents on a background thread`

# P8-33 — `LspCompletionProvider`: completion and resolve over LSP

- **Status:** todo
- **Depends on:** P8-32
- **Size:** S

## Goal

An `ICompletionItemProvider` that answers the editor's completion requests with `textDocument/completion` (trigger characters from the server's capabilities) and resolves items with `completionItem/resolve`.

## Monaco sources

Sources: none (specified in this card; LSP 3.17 *Completion Request*, *Completion Item Resolve Request*). The contract it implements is `ICompletionItemProvider` of P8-01.

## Files to create or change

- `src/LoomEditor.Lsp/Editor/LspCompletionProvider.cs` — create (`internal sealed class : ICompletionItemProvider`)
- `tests/LoomEditor.Lsp.Tests/Editor/LspCompletionProviderTests.cs` — create

## Design decisions (already made — do not change)

- **`LspCompletionProvider(LspClient client, string documentUri)`**. `TriggerCharacters` = `client.CompletionTriggerCharacters` copied at construction (the session constructs it after `Running`); `CanResolve` = `client.SupportsCompletionResolve` read at construction.
- **`async Task<CompletionList?> ProvideCompletionItemsAsync(TextModel model, Position position, CompletionContext context, CancellationToken token)`** (this is allowed to use `async` / `await` with `ConfigureAwait(false)`, ADR 0011 decision 4; the part **before the first `await` runs on the caller's UI thread**, the rest on a pool thread, so the method must not touch `model` after the first `await` — it never touches it at all):
  `client.State != Running` → return `null`; build `CompletionParams { TextDocument = { Uri = documentUri }, Position = LspConvert.ToLsp(position), Context = LspConvert.ToLspContext(context) }`; `result = await client.RequestRawAsync(LspMethods.Completion, JsonSerializer.SerializeToElement(@params, LspJsonContext.Default.CompletionParams), token).ConfigureAwait(false)`;
  return `LspConvert.ToCompletionList(result)`. **Error handling**: a `JsonRpcException` with code `ContentModified` (-32801), `RequestCancelled` (-32800) or `ServerCancelled` (-32802) → return `null` (the document changed under the server: the next keystroke asks again); an `LspClientException` (not running) → `null`; any other exception propagates (the consumer ignores faults); an `OperationCanceledException` propagates.
- **`async Task<CompletionItem?> ResolveCompletionItemAsync(CompletionItem item, CancellationToken token)`**: `item.ProviderData is not LspCompletionItem raw` → `null`; (read on the UI thread, before the first await) `client.State != Running` → `null`; `resolved = await client.RequestAsync(LspMethods.CompletionResolve, raw, LspJsonContext.Default.LspCompletionItem, LspJsonContext.Default.LspCompletionItem, token).ConfigureAwait(false)`;
  `resolved is null` → `null`; else `LspConvert.ApplyResolved(resolved)`. The same error handling as above.
- No caching, no debouncing, no `isIncomplete` logic (the suggest model of P8-16 does the retrigger).
- `<remarks>Threading: the methods are called on the UI thread and the tasks complete on a pool thread (ADR 0011 decision 4).</remarks>`.

## Tests

- [ ] New (`LspCompletionProviderTests`; in process with `FakeServerHost`, an initialized `LspClient`, an `LspDocumentSync` opened on `new TextModel("foo foobar bar\nbaz")`, and `FakeServerHost.BarrierAsync()` after edits): `TriggerCharacters_FromServer` (`["."]`), `CanResolve_FromServer` (true for the fake; false for a `ScriptedServer` without `resolveProvider`),
      `Complete_Words_ReturnsDistinctWordsAndKeywords` (labels as the fake server rules say; kinds mapped: `Text`, `Keyword`), `Complete_PositionIsSentZeroBased` (a word list that depends on the position: the word ending at the cursor is excluded: put the cursor at the end of `foo` and see `foo` missing), `Complete_TriggerDot_MemberItemsWithInsertReplaceRanges` (`CompletionContext(TriggerCharacter, ".")`: the first items are `Length`, `Count`, …; `Range` has insert `[cursor, cursor]` and a replace range ending at the identifier end; ranges are one-based),
      `Complete_Preselect_Keywords` (`if` has `Preselect`), `Complete_Incomplete_Flag` (`--incomplete`), `Complete_ItemsKeepRawItemInProviderData`, `Complete_ContextSent` (the fake records nothing about the context: use a `ScriptedServer` and assert `context.triggerKind == 2` and `triggerCharacter == "."`), `Complete_ClientNotRunning_Null`,
      `Complete_ContentModified_Null` (a `ScriptedServer` answering error -32801), `Complete_OtherError_Faults`, `Complete_Cancelled_TaskCanceled_AndCancelRequestSent` (`--delay=500`, cancel after 50 ms: the task is canceled and the fake server's `ReceivedMethods` contains `$/cancelRequest`), `Complete_DoesNotTouchTheModel_AfterFirstAwait` (run the provider and edit the model from the test thread meanwhile: no exception, no data race: the test only needs to pass; document the intent),
      `Resolve_FillsDetailAndDocumentation` (`Detail == "detail of foo"`, documentation markdown `**foo** documentation`), `Resolve_Length_HasAdditionalTextEdit` (the `// resolved\n` insert at `(1,1)-(1,1)`), `Resolve_NoProviderData_Null`, `Resolve_ContentModified_Null`, `Resolve_RoundTripsData` (the fake echoes `data`; the converted item's `ProviderData` is the resolved raw item with equal `data`)
(The integration with the editor's suggest code is tested by P8-36 and the Gallery: `LoomEditor.Lsp` does not reference `LoomEditor.View`, so its tests cannot use `SuggestItems`.)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green; the Lsp test project run three times, none failed
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Snippets, `completion/triggerKind` retries, word based fallbacks, `itemDefaults`, `labelDetails` rendering rules.

## Suggested commit message

`feat(lsp): add the completion provider`

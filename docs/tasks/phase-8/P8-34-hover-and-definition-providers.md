# P8-34 — `LspHoverProvider` and `LspDefinitionProvider`

- **Status:** todo
- **Depends on:** P8-33
- **Size:** S

## Goal

Two providers that answer the editor's hover and go-to-definition requests with `textDocument/hover` and `textDocument/definition`.

## Monaco sources

Sources: none (specified in this card; LSP 3.17 *Hover Request*, *Goto Definition Request*). The contracts are `IHoverProvider` and `IDefinitionProvider` of P8-01.

## Files to create or change

- `src/LoomEditor.Lsp/Editor/LspHoverProvider.cs`, `LspDefinitionProvider.cs`, `LspRequestErrors.cs` — create (`internal`)
- `src/LoomEditor.Lsp/Editor/LspCompletionProvider.cs` — change: use `LspRequestErrors.IsBenign(Exception)` for its error filter (the same codes as P8-33; a pure refactor, its tests must still pass)
- `tests/LoomEditor.Lsp.Tests/Editor/LspHoverProviderTests.cs`, `LspDefinitionProviderTests.cs`, `LspRequestErrorsTests.cs` — create

## Design decisions (already made — do not change)

- **`LspRequestErrors.IsBenign(Exception e)`**: `true` for a `JsonRpcException` with code `ContentModified` (-32801), `RequestCancelled` (-32800) or `ServerCancelled` (-32802) and for an `LspClientException` (the client is not running); `false` otherwise. Providers return `null` for a benign error and let every other exception propagate.
- **`LspHoverProvider(LspClient client, string documentUri) : IHoverProvider`**: `async Task<Hover?> ProvideHoverAsync(TextModel model, Position position, CancellationToken token)`: not `Running` → `null`; `TextDocumentPositionParams { TextDocument = { Uri = documentUri }, Position = LspConvert.ToLsp(position) }`;
  `result = await client.RequestRawAsync(LspMethods.Hover, JsonSerializer.SerializeToElement(@params, LspJsonContext.Default.TextDocumentPositionParams), token).ConfigureAwait(false)`; return `LspConvert.ToHover(result)`; benign errors → `null`. Never touches `model` (ADR 0011 decision 4).
- **`LspDefinitionProvider(LspClient client, string documentUri) : IDefinitionProvider`**: `async Task<IReadOnlyList<LocationLink>?> ProvideDefinitionAsync(TextModel model, Position position, CancellationToken token)`: the same with `LspMethods.Definition` and `LspConvert.ToLocationLinks(result)`; an empty list is returned as an **empty list** (not `null`); benign errors → `null`.
- `<remarks>Threading: called on the UI thread, completes on a pool thread.</remarks>`.

## Tests

- [ ] New (`LspRequestErrorsTests`): `IsBenign_ContentModified_RequestCancelled_ServerCancelled`, `IsBenign_ClientException`, `IsBenign_OtherJsonRpcError_False`, `IsBenign_OtherException_False`
- [ ] New (`LspHoverProviderTests`; in process, `FakeServerHost` + initialized `LspClient` + opened `LspDocumentSync` on `new TextModel("let value = 1;\nsecond value")`): `Hover_OnWord_ReturnsMarkdownAndRange` (position of `value` on line 1: contents one `MarkdownString` whose `Value` is the fake's hover markdown, `Range` one-based `(1,5)-(1,10)`), `Hover_PositionAtWordEnd_StillInsideTheWord`,
      `Hover_OnWhitespace_Null`, `Hover_SecondLine_PositionIsZeroBased` (a word on line 2: the hover word is that word), `Hover_AfterEdit_UsesTheNewText` (edit, `BarrierAsync()`, hover: the new word), `Hover_ClientNotRunning_Null`, `Hover_ContentModified_Null` (`ScriptedServer`), `Hover_OtherError_Faults`, `Hover_Cancelled_TaskCanceled`, `Hover_MarkedStringArray_Converted` (`ScriptedServer` returning an array of marked strings), `Hover_NullResult_Null`
- [ ] New (`LspDefinitionProviderTests`; document text `"def alpha\nclass Beta\nalpha Beta external"`): `Definition_Def_SingleLocation` (position on `alpha` line 3: one link with `Uri == documentUri`, `Range` = the word `alpha` on line 1, `TargetSelectionRange` equal to it), `Definition_Class_LocationLink` (on `Beta`: the link has `OriginSelectionRange` = the word at the request, `Range` = the whole line 2, `TargetSelectionRange` = `Beta`),
      `Definition_External_OtherUri` (`file:///other/file.txt`), `Definition_NotFound_Null_Or_Empty` (the fake answers `null`: the provider returns an empty list), `Definition_ClientNotRunning_Null`, `Definition_ContentModified_Null`, `Definition_Cancelled_TaskCanceled`, `Definition_PositionIsSentZeroBased`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Hover verbosity, `textDocument/typeDefinition`, `implementation`, `declaration`, `references`.

## Suggested commit message

`feat(lsp): add the hover and definition providers`

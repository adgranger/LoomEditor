# P8-30 — Text document synchronization: `didOpen`, incremental `didChange`, `didClose`

- **Status:** todo
- **Depends on:** P8-29
- **Size:** M

## Goal

`LspDocumentSync` keeps the server's copy of the document equal to the editor's model: it opens the document with its full text, then sends every model change as an incremental `didChange` (or the full text when the server wants it, or after a flush or an EOL change), and closes the document on dispose. The server's text equals the model's text after every kind of edit, including undo, redo and multi-cursor edits.

## Monaco sources

Sources: none (specified in this card; LSP 3.17 *Text Document Synchronization*). The editor side is `ModelContentChangedEventArgs` (`LoomEditor.Core.Model`, ported from `IModelContentChangedEvent`): its `Changes` are **ordered from the end of the document to the beginning**, so applying them in sequence is valid, which is exactly what LSP requires for `contentChanges` (each change is relative to the document after the previous one).

## Files to create or change

- `src/LoomEditor.Lsp/Editor/LspDocumentSync.cs` — create (`internal sealed class : IDisposable`)
- `src/LoomEditor.Lsp/Editor/LspConvert.cs` — create (**only** the position / range helpers listed here; P8-31 adds the rest)
- `tests/LoomEditor.Lsp.Tests/Editor/LspDocumentSyncTests.cs`, `LspDocumentSyncRandomTests.cs`, `LspConvertPositionTests.cs` — create
- `tests/LoomEditor.Lsp.Tests/TestHelpers/FakeServerHost.cs` — change: `Task BarrierAsync()` (see below)

## Design decisions (already made — do not change)

- **`LspConvert`** (`internal static class`, pure; ADR 0011 decision 5; this card creates it with exactly these members, P8-31 extends it):
  `static LspPosition ToLsp(Position position)` → `new LspPosition { Line = position.LineNumber - 1, Character = position.Column - 1 }`; `static LspRange ToLsp(Range range)`;
  `static Position ToPosition(LspPosition position, TextModel model)` → `model.ValidatePosition(new Position(Math.Max(position.Line, 0) + 1, Math.Max(position.Character, 0) + 1))` (a negative value from a bad server counts as 0; **an out-of-range character is clamped to the line end and an out-of-range line to the end of the document by `ValidatePosition`**: the LSP rule);
  `static Range ToRange(LspRange range, TextModel model)` → `model.ValidateRange(new Range(start…, end…))` built from the two validated positions, **swapped when the end is before the start**. Use only `TextModel` members that are **public** (`ValidatePosition`, `ValidateRange` are; if one is not public, stop and report: do not add `InternalsVisibleTo` for the Lsp assembly).
- **`LspDocumentSync(LspClient client, TextModel model, string documentUri, string languageId)`**; `Open()` and `Close()`; `Dispose()` = `Close()`.
  - `Open()`: no-op when already open; requires `client.State == Running` (else `InvalidOperationException`); reads the sync options once: `(openClose, change) = client.TextDocumentSync`; sends `textDocument/didOpen` with `TextDocumentItem { Uri = documentUri, LanguageId = languageId, Version = model.VersionId, Text = model.GetValue() }` (the model's text with its own EOL: `TextDefined`),
    **subscribes to `model.ContentChanged`**, remembers `_sentVersion = model.VersionId`. When `openClose` is false (the server did not declare sync at all) it sends nothing and stays closed; the session treats that as "no features that need sync".
  - `OnContentChanged(object?, ModelContentChangedEventArgs e)` (UI thread; the model raises it **synchronously inside the edit**):
    1. `change == None (0)` → nothing is sent (the server does not want changes; `_sentVersion` is still updated);
    2. `e.IsFlush || e.IsEolChange || change == Full (1)` → `didChange` with `contentChanges = [ { Text = model.GetValue() } ]` (no range) and `Version = e.VersionId`;
    3. otherwise (`Incremental`): one `TextDocumentContentChangeEvent` per entry of `e.Changes`, **in the given order**: `Range = LspConvert.ToLsp(change.Range)`, `RangeLength = change.RangeLength`, `Text = change.Text`; `Version = e.VersionId`.
    The notification is sent with `client.Notify(LspMethods.DidChange, params, LspJsonContext.Default.DidChangeTextDocumentParams)` **inside the handler**, on the UI thread: it serializes and enqueues, so the server receives it **before** any request the UI thread sends afterwards (ADR 0011 decision 4).
    A `LspClientException` from `Notify` (client faulted or stopped) is caught: the sync marks itself `IsFaulted = true` and stops sending (the session reports the fault). Any other exception is **not** caught.
  - `Close()`: unsubscribes, and if the document was opened and the client is `Running`, sends `textDocument/didClose` (`TextDocumentIdentifier { Uri }`) (exceptions from `Notify` ignored). Idempotent.
  - Properties: `bool IsOpen`, `bool IsFaulted`, `string DocumentUri`, `int SentVersion`.
  - Text of a change in `change.Text` uses the model's EOL when the edit was typed, `\n` normalized to the model EOL by the model: the server's document must use the same line breaks as the model: the **tests below check this with a CRLF model**.
- The language id is fixed for the life of a sync object (changing the editor's language needs a new session: documented, the session of P8-35 does not support it).
- `<remarks>Threading: UI thread only (it subscribes to the model); the notifications it sends are queued and written by the connection's writer loop.</remarks>`.
- **Test barrier**: `FakeServerHost.BarrierAsync()` sends a `textDocument/hover` request at position (1,1) and awaits its answer: because the fake server handles messages sequentially (P8-28), everything sent before is processed once it returns. All tests call it before reading `server.DocumentText`.

## Tests

- [ ] New (`LspConvertPositionTests`): `ToLsp_Position_IsZeroBased`, `ToLsp_Range`, `ToPosition_RoundTrips`, `ToPosition_CharacterBeyondLine_ClampedToLineEnd`, `ToPosition_LineBeyondDocument_ClampedToEnd`, `ToPosition_Negative_ClampedToStart`, `ToRange_Swapped_Normalized`, `ToPosition_Utf16Surrogates_CharacterIsUtf16Index` (a line `a😀b`: LSP character 3 is the position before `b`, column 4)
- [ ] New (`LspDocumentSyncTests`; in process: a `FakeServerHost`, an `LspClient` over its connection initialized with the helper, a `TextModel` from `new TextModel("…")`): `Open_SendsFullTextAndVersion` (`server.DocumentText`, `DocumentUri`, `DocumentVersion == model.VersionId`), `Open_WithCrLfModel_ServerTextHasCrLf`, `Open_TwiceIsNoOp`, `Open_BeforeRunning_Throws`, `Open_ServerWithoutSync_SendsNothing` (a tiny server without `textDocumentSync`),
      `Edit_Insert_Incremental`, `Edit_Delete_Incremental`, `Edit_Replace_Incremental`, `Edit_MultiLine_Incremental`, `Edit_MultipleRangesInOneEdit_ChangesInOrder` (`model.ApplyEdits` with three edits: the server text equals the model text), `Edit_Undo_Redo_Synced`, `Edit_AfterSetValue_FullText` (`IsFlush`), `Edit_AfterSetEol_FullText` (`IsEolChange`, LF → CRLF),
      `Edit_ServerWantsFull_SendsFullText` (`--sync=full`), `Edit_ServerWantsNone_NothingSent_ButOpenClose` (`--sync=none`: the server text stays the opening text), `Edit_MultiByteCharacters_Utf16Positions` (`é€😀` before and after the edit position), `Edit_VersionsIncrease_AndMatchTheModel`, `Edit_BeforeOpen_Ignored`, `Edit_AfterClose_Ignored`,
      `Close_SendsDidClose` (`server.DocumentUri` null afterwards), `Dispose_Closes`, `Dispose_Twice_Ok`, `ClientFaulted_StopsSending_IsFaulted` (`--exit-on=textDocument/didChange`: after the crash the next edit does not throw and `IsFaulted` becomes true), `Edit_OrderBeforeRequest` (an edit followed immediately by a hover request: the hover answer already reflects the edit: the hover word is the new one)
- [ ] New (`LspDocumentSyncRandomTests`; `new Random(seed)` with the seed in every assertion message; several seeds: 1, 2, 3, 12345): `RandomEdits_ServerTextMatchesModel` (300 steps per seed, each step one of: insert at a random position, delete a random range, replace, multi-range `ApplyEdits` with 1–4 non-overlapping edits, `Undo`, `Redo`, a text containing `\r\n`, `é€😀`; a `BarrierAsync()` and the comparison `server.DocumentText == model.GetValue()` after **every** step; a `SetValue` and a `SetEol` every ~50 steps),
      `RandomEdits_CrLfModel`, `RandomEdits_ServerWantsFull`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green; run `dotnet test --project tests/LoomEditor.Lsp.Tests -c Release` three times, none failed
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`willSave`, `didSave`, several documents, language id changes, the mapping of other types (P8-31), `workspace/didChangeConfiguration`.

## Suggested commit message

`feat(lsp): add incremental text document synchronization`

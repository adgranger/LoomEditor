# P8-35 — `LspEditorSession`: one call connects an editor to a language server

- **Status:** done
- **Depends on:** P8-34
- **Size:** M

## Goal

`LspEditorSession` is the public entry point of the LSP project: given an editor's text model, its language features service and marker service, a document URI, a language id, a server command and a `post` function that runs code on the UI thread, `StartAsync` starts the server, opens the document, registers the completion / hover / definition providers
that the server supports and shows its diagnostics; `StopAsync` undoes all of it. A crashed server turns into a `Faulted` state and removes everything it added.

## Monaco sources

Sources: none (specified in this card; ADR 0011 decisions 2, 4 and 6). Monaco has no equivalent (the workbench's language client is not in the repository).

## Files to create or change

- `src/LoomEditor.Lsp/Editor/LspEditorSession.cs`, `LspEditorSession.Lifecycle.cs`, `LspSessionOptions.cs`, `LspSessionState.cs` — create (**public**: the session, the options and the state enum)
- `src/LoomEditor.Lsp/PublicAPI.Unshipped.txt` — regenerate (`./eng/Update-PublicApi.ps1 src/LoomEditor.Lsp`): `LspEditorSession`, `LspSessionOptions`, `LspSessionState`, `LspServerCommand`
- `src/LoomEditor.Lsp/LoomEditor.Lsp.csproj` — change: `<InternalsVisibleTo Include="LoomEditor.View.Tests" />` (P8-36 links the Lsp test helpers into the View tests)
- `tests/LoomEditor.Lsp.Tests/Editor/LspEditorSessionTests.cs`, `LspEditorSessionFaultTests.cs` — create
- `src/LoomEditor.Lsp/README.md` — change (the `Editor/` files and the "How to use" snippet below)

## Design decisions (already made — do not change)

- **Public API** (XML doc on every member, `<remarks>Threading</remarks>` on the class):
  - `public enum LspSessionState { NotStarted, Starting, Running, Stopping, Stopped, Faulted }`.
  - `public sealed record LspSessionOptions { string? RootUri; TimeSpan InitializeTimeout = 60 s; string? ClientVersion; string DiagnosticsOwner = "lsp" }` (init-only properties with these defaults).
  - `public sealed class LspEditorSession : IAsyncDisposable` with the constructor `LspEditorSession(TextModel model, LanguageFeaturesService features, MarkerService markers, string documentUri, string languageId, LspServerCommand command, Func<Action, bool> post, LspSessionOptions? options = null)` and the members
    `LspSessionState State { get; }`, `string? FaultMessage { get; }`, `event Action<LspSessionState>? StateChanged;`, `event Action<string>? ServerOutput;` (log messages, show messages and stderr lines of the server, as text lines for the host's log), `Task StartAsync(CancellationToken token = default)`, `Task StopAsync()`, `ValueTask DisposeAsync()` (= `StopAsync`).
  - `internal LspEditorSession(…same first parameters…, Func<LspClient> clientFactory, …)` — the constructor the tests use to give the session a client over in-memory streams; the public constructor passes `() => LspClient.StartProcess(command, clientOptions)`.
- **Thread rules** (ADR 0011 decision 4): the constructor, `StartAsync`, `StopAsync` and `DisposeAsync` are called on the **UI thread**. Everything the session does to the editor (registering providers, opening the document, markers, state changes, events) happens on the UI thread: from a pool thread it goes through `post`; a `post` that returns `false` means the UI is gone: the step is skipped and the session just shuts the server down.
  `State` is a volatile field; `StateChanged` and `ServerOutput` are raised **on the UI thread** (through `post`).
- **`StartAsync`** (only from `NotStarted`, else `InvalidOperationException`): state `Starting` (raised synchronously, UI thread); `client = clientFactory()` (a `LspServerStartException` → `Fault(ex)` and the returned task faults with it); subscribes to `client.LogMessageReceived`, `ServerErrorLine`, `StateChanged`;
  `await client.InitializeAsync(token).ConfigureAwait(false)` (pool thread afterwards); then `post(FinishStart)` and completes the returned task when `FinishStart` has run (a `TaskCompletionSource` with `RunContinuationsAsynchronously`); an initialize failure → `Fault(ex)` (the client already killed the process), the task faults with the same exception.
  **`FinishStart`** (UI thread): if the session was stopped / disposed meanwhile → dispose the client and return; `sync = new LspDocumentSync(client, model, documentUri, languageId)`; `sync.Open()` (sends the **current** text: whatever was typed during initialization is included); if the server declared no sync (`!client.TextDocumentSync.OpenClose`) skip every feature that needs the document: register nothing and go to `Running` anyway (the host sees a running server that does nothing; `ServerOutput` gets one line `"The server did not declare text document synchronization: language features are off."`);
  otherwise create `LspDiagnosticsBinder(client, model, markers, documentUri, post, options.DiagnosticsOwner)`, then register on `features`: `CompletionProviders.Register(languageId, new LspCompletionProvider(client, documentUri))` if `client.SupportsCompletion`; `HoverProviders.Register(languageId, new LspHoverProvider(...))` if `client.SupportsHover`; `DefinitionProviders.Register(languageId, new LspDefinitionProvider(...))` if `client.SupportsDefinition`;
  keep the `IDisposable` registrations; state `Running`.
- **Faults**: `client.StateChanged` with `Faulted` (a crash, a closed pipe, a failed initialize) → `post(() => Fault(client.FaultReason))`. **`Fault(Exception?)`** (UI thread, idempotent): unregister the three providers (dispose the registrations), `binder.Clear()` and dispose the binder, dispose the sync (stops sending), `FaultMessage` = the exception message (or "The language server stopped unexpectedly"), state `Faulted`, a `ServerOutput` line with the message; the client is disposed in the background (`_ = client.DisposeAsync()`). No automatic restart.
- **`StopAsync`** (from `Running`, `Starting` (cancels the start) or `Faulted`; `Stopped` / `NotStarted` → completed task): state `Stopping`; on the UI thread: unregister the providers, `sync.Close()` (sends `didClose`), dispose the binder (clears the markers); then `await client.ShutdownAsync()` and `await client.DisposeAsync()` (pool thread), then `post(() => state = Stopped)`; the returned task completes after that. Exceptions of the shutdown are swallowed. Idempotent; concurrent calls share one task.
- **Server output lines**: `LogMessageReceived` → `"[<type>] <message>"` with `<type>` = `error`, `warning`, `info`, `log` for 1–4 (show messages use the same text prefixed `"(notification) "`); stderr lines as `"[stderr] <line>"`; each is posted to the UI thread and raised through `ServerOutput`. Lines are never dropped when nobody listens (no buffering: if there is no handler the line is lost).
- **How to use** (goes into `src/LoomEditor.Lsp/README.md`):

  ```csharp
  var session = new LspEditorSession(editor.Model, editor.LanguageFeatures, editor.Markers, editor.DocumentUri, "csharp",
      new LspServerCommand("csharp-ls", [], workspaceFolder), action => editor.DispatcherQueue.TryEnqueue(() => action()),
      new LspSessionOptions { RootUri = workspaceUri });
  await session.StartAsync();
  ```
- `<remarks>Threading: see ADR 0011 decision 4. UI thread only for the public members; the events are raised on the UI thread.</remarks>`.

## Tests

- [x] New (`LspEditorSessionTests`; in process through the internal constructor with a client over a `FakeServerHost`'s streams (`FakeServerHost` gets `CreateSessionClientFactory()`: returns the `Func<LspClient>` that builds the client on the host's existing `JsonRpcConnection`), a `ManualPost`, a real `LanguageFeaturesService`, `MarkerService` and `new TextModel(…)`; every `await` has a 10 s timeout; after each await of the session the test calls `ManualPost.RunAll()` where the card says so):
      `Start_RegistersAllThreeProviders_ForTheLanguage` (`Ordered("csharp")` of each registry has one provider; another language has none), `Start_OpensTheDocument` (`server.DocumentText == model.GetValue()`, `DocumentUri` as given), `Start_StateSequence` (`Starting`, `Running` raised on the thread that calls `RunAll()`), `Start_TypedDuringInitialize_OpensWithTheLatestText` (a `ScriptedServer` that delays its `initialize` answer; the test edits the model meanwhile),
      `Start_OnlyHoverCapability_OnlyHoverRegistered` (`ScriptedServer`), `Start_NoSyncCapability_RegistersNothing_StillRunning` (and the `ServerOutput` line), `Start_Twice_Throws`, `Start_PostRefused_ShutsTheServerDown`, `Providers_Work_EndToEnd` (call the registered completion, hover and definition providers directly and check the fake's answers),
      `Diagnostics_ReachTheMarkerService` (the text `"ERROR"`: one marker after `BarrierAsync()` and `RunAll()`), `Edits_AreSynced` (type into the model, `BarrierAsync()`, server text equal), `Stop_UnregistersClearsAndShutsDown` (providers gone, markers gone, the server saw `didClose`, `shutdown`, `exit`; state `Stopped`), `Stop_Twice_Ok`, `Stop_WhileStarting_Cancels`, `Dispose_IsStop`,
      `ServerOutput_LogAndShowMessages` (`Chatty`: lines `[info] fake server ready`, `(notification) [warning] fake warning`), `ServerOutput_Stderr` (process mode, `--stderr=hello`: `[stderr] hello`)
- [x] New (`LspEditorSessionFaultTests`): `InitializeFails_Faulted_StartTaskFaults` (`ScriptedServer` answering an error), `InitializeTimeout_Faulted` (`InitializeTimeout = 300 ms`), `ServerCrashesWhileRunning_FaultedAndEverythingRemoved` (process mode, `--exit-on=textDocument/didChange`, then edit: providers unregistered, markers cleared, `FaultMessage` set, state `Faulted`, `ServerOutput` line), `Fault_DoesNotRestart`, `StartProcess_FileNotFound_PublicConstructor_Faulted_AndTaskFaults` (a `LspServerCommand("this-does-not-exist-xyz", [])` through the **public** constructor),
      `Stop_AfterFault_Ok`, `Edits_AfterFault_DoNotThrow`, `ModelEditedAfterStop_NoNotifications` (the server does not receive anything after `Stopped`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green; the Lsp test project three times in a row, none failed, no `LoomEditor.Lsp.FakeServer` process left
- [x] `./eng/Update-PublicApi.ps1 src/LoomEditor.Lsp` run; the public surface is exactly the four types listed (`LspServerCommand` from P8-29 plus the three of this card)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Restart on crash, several documents, changing the language id, the end-to-end tests with the real editor code (P8-36), the Gallery (P8-37).

## Suggested commit message

`feat(lsp): add the editor session that wires a language server to an editor`

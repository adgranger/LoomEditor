# P8-32 — Diagnostics: server `publishDiagnostics` become markers

- **Status:** done
- **Depends on:** P8-31
- **Size:** S

## Goal

`LspDiagnosticsBinder` turns the diagnostics the server publishes for the open document into the editor's markers (and so into squiggles and ruler marks), on the UI thread, dropping the ones computed for an older text, and clears them when the session ends.

## Monaco sources

Sources: none (specified in this card; LSP 3.17 *PublishDiagnostics*; `vscode-languageclient` drops diagnostics whose `version` is not the document's current version when `versionSupport` is on).

## Files to create or change

- `src/LoomEditor.Lsp/Editor/LspDiagnosticsBinder.cs` — create (`internal sealed class : IDisposable`)
- `tests/LoomEditor.Lsp.Tests/Editor/LspDiagnosticsBinderTests.cs` — create; `tests/LoomEditor.Lsp.Tests/TestHelpers/ManualPost.cs` — create (README §6)

## Design decisions (already made — do not change)

- **`LspDiagnosticsBinder(LspClient client, TextModel model, MarkerService markers, string documentUri, Func<Action, bool> post, string owner = "lsp")`**; it subscribes to `client.DiagnosticsReceived` in the constructor and unsubscribes in `Dispose`.
- **Handler** (runs on the read loop thread): `if (!string.Equals(p.Uri, _documentUri, StringComparison.Ordinal)) return;` then `post(() => Apply(p))`; a `post` that returns `false` (the UI is shutting down) drops it. The handler does nothing else (no model access).
- **`Apply(PublishDiagnosticsParams p)`** (UI thread, through `post`): returns when disposed; **stale** — `p.Version is { } version && version != model.VersionId` → return without touching the markers (the text has changed since the server computed them; the server republishes for the new version);
  else `markers.Change(owner, [.. p.Diagnostics.Select(d => LspConvert.ToMarker(d, model, p.Version))])` (an empty array clears the owner's markers: `MarkerService.Change` removes them).
- `Clear()`: `markers.Remove(owner)` (UI thread). `Dispose()`: unsubscribe, set the disposed flag (pending posted actions then do nothing), `Clear()`. Idempotent. A session calls `Clear()` also when the client faults (stale squiggles are worse than none).
- `<remarks>Threading: the constructor and Dispose on the UI thread; the event handler on the read loop thread; Apply on the UI thread.</remarks>`.
- **`ManualPost`** (test helper): `Func<Action, bool> Post` that queues actions and returns `true`; `RunAll()` runs them on the calling thread in order (actions queued while running run too); `bool Accepting = true` (when false `Post` returns `false` and queues nothing); `int PendingCount`.

## Tests

- [x] New (`LspDiagnosticsBinderTests`; in process: `FakeServerHost`, an initialized `LspClient`, an `LspDocumentSync` (P8-30) opened on a `TextModel`, a real `MarkerService`, `ManualPost`; the test waits for the server's diagnostics with `FakeServerHost.BarrierAsync()` then `ManualPost.RunAll()`): `Diagnostics_BecomeMarkers` (the text `"ERROR and WARN"`: two markers, severities `Error` and `Warning`, the code `"FAKE001"` and `"2"`, source `fake`, positions one-based), `Diagnostics_NotAppliedBeforeRunAll` (the markers appear only after `RunAll()`: the handler posts),
      `Diagnostics_OtherUri_Ignored` (a `ScriptedServer` publishing for another uri), `Diagnostics_StaleVersion_Dropped` (a `ScriptedServer` publishing `version = model.VersionId - 1`), `Diagnostics_NoVersion_Applied` (a `ScriptedServer` publishing without `version`), `Diagnostics_Replaced_OnNextPublish` (edit the text to remove `ERROR`: one marker left),
      `Diagnostics_EmptyList_ClearsMarkers`, `Diagnostics_Tags_Mapped` (`UNUSED`, `OLD`), `Diagnostics_RangeBeyondText_Clamped` (a `ScriptedServer`), `PostRefused_Dropped` (`Accepting = false`), `Handler_DoesNotTouchTheModel` (the handler thread is not the test thread; `Apply` runs on the thread that calls `RunAll()`: record `Environment.CurrentManagedThreadId` in both places),
      `Dispose_ClearsMarkers_AndUnsubscribes`, `Dispose_PendingPostedAction_DoesNothing`, `Dispose_Twice_Ok`, `Clear_RemovesTheOwnersMarkers_OnlyTheOwners` (another owner's markers stay), `CustomOwner_Used`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Related information, code descriptions, pull diagnostics, the diagnostics list.

## Suggested commit message

`feat(lsp): turn published diagnostics into markers`

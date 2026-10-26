# Phase 8 — read this before any card

Phase 8 brings a **language server client** to the roadmap ([phase-8-lsp.md](../../roadmap/phase-8-lsp.md)): diagnostics drawn as squiggles, hover, the completion
list and go to definition, fed by a real language server over JSON-RPC on stdio (the Gallery shows `csharp-ls`). The design is in
[ADR 0011](../../adr/0011-language-server-client.md): **read it first**. This page lists the conventions that **every** phase 8 card relies on; a card never
repeats them. The conventions of [phase-3/README.md](../phase-3/README.md) (sections 1, 3, 4, 5 and 6), [phase-5/README.md](../phase-5/README.md) (sections 1–5),
[phase-6/README.md](../phase-6/README.md) (sections 1 and 2) and [phase-7/README.md](../phase-7/README.md) (sections 5–7) still apply.

## Order of the cards

Take the cards in table order of [../README.md](../README.md) (each card's "Depends on" is satisfied by the cards above it). Steps:

- **A — Language features in Core** (P8-01 … P8-05): provider contracts, registry, markers, marker decorations, the context / control wiring and `UiContinuation`.
- **B — Diagnostics on screen** (P8-06): squiggles and ruler marks.
- **C — Hover** (P8-07 … P8-11): Markdown subset, mouse move events, hover computation, the controller, the WinUI overlay and popup.
- **D — Completion** (P8-12 … P8-22): fuzzy filter, suggest items, completion model, suggest model (state machine) and its automatic triggers, list state, controller, late edits and commit characters, the feature (commands, keys), the WinUI list.
- **E — Go to definition** (P8-23).
- **F — The LSP client** (P8-24 … P8-36): the `LoomEditor.Lsp` project, framing, JSON-RPC connection, JSON types, the fake server, client lifecycle, document sync, conversions, diagnostics, completion, hover and definition providers, the editor session, end-to-end tests of the whole stack.
- **G — Prove it and close** (P8-37 … P8-39): the Gallery shows a language server (and a self-check), the manual test plan, close the phase.

Steps A to E need no server and no LSP code; step F needs A (the types it fills) but not B to E, and P8-36 needs all of them; the cards are in the one linear order so `/next-task` never has to choose.
The cards flagged **Recommended model: Opus** (P8-11, P8-16, P8-26, P8-27, P8-29, P8-37) have a header line saying why; on failure they say what to report.
P8-06, P8-08, P8-11, P8-22 and P8-37 have manual checks for the maintainer (`docs/testing/phase-8-manual-tests.md`, written in full by P8-38).

## Out of scope for phase 8

Snippets (`insertText` is plain text), signature help, references, rename, code actions and quick fixes, formatting, semantic tokens, inlay hints, document highlights and
folding ranges from the server, document links, workspace symbols, pull diagnostics, several documents or workspace folders, server-initiated edits, hover verbosity,
the completion details side pane and `suggestMemory`, word based completions (`wordDistance`, `wordBasedSuggestions`), Ctrl+hover link underlines, peek definition,
the diagnostics list / "next problem" navigation (`F8`), localization of messages, bundling a server. Anything the manual plan finds is a follow-up card, not a change
to a closed one. (The complete list with the reasons is ADR 0011 decision 7.)

## 1. Upstream sources

Everything comes from `microsoft/vscode` at `0c9c0a4295a9222e12a605537738719455ae3056`
(`https://raw.githubusercontent.com/microsoft/vscode/0c9c0a4295a9222e12a605537738719455ae3056/<path>`; paths in the cards are full, `src/vs/…`). Line numbers are hints; the
function or class **name** is authoritative. A card that has no Monaco counterpart says **"Sources: none (specified in this card)"**: the card is the spec and its tests are
`New:` tests. The header of a ported or inspired file is the two-line header of `CLAUDE.md` (checked by an architecture test); a file with no Monaco counterpart has none.
Monaco has **no LSP**: every card of step F is specified by the LSP 3.17 specification (<https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/>);
the card gives the exact member names it needs, read the matching spec section when a card says so.

## 2. Where things go

The table is in ADR 0011, decision 1. Namespaces follow the folders; tests mirror `src/` (`tests/LoomEditor.Core.Tests/Languages/Features/…`, `tests/LoomEditor.View.Tests/Suggest/…`,
`tests/LoomEditor.Lsp.Tests/…`). Everything is `internal` unless the card says **public**; public members of `LoomEditor.Core` go in `PublicAPI.Unshipped.txt` with
`./eng/Update-PublicApi.ps1 src/LoomEditor.Core` (same for `LoomEditor.WinUI`, `LoomEditor.Lsp`).

## 3. Monaco concepts → LoomEditor (new in phase 8)

| Monaco | LoomEditor |
|---|---|
| `ILanguageFeaturesService` (`completionProvider`, `hoverProvider`, `definitionProvider`) | `LanguageFeaturesService` (Core, **public**; one per editor; `CompletionProviders`, `HoverProviders`, `DefinitionProviders`) |
| `LanguageFeatureRegistry<T>.register(selector, provider)` | `LanguageFeatureRegistry<T>.Register(string selector, T provider)`; the selector is a language id or `"*"` |
| `IMarkerService` + `IMarkerDecorationsService` | `MarkerService` (Core, **public**; one per editor, owner strings) + `MarkerDecorations` (internal, created by `EditorContext`) |
| `IMarkerData` / `IMarker` | `MarkerData` / `Marker` (records; `Marker` adds the owner) |
| `IMarkdownString` | `MarkdownString(string Value, bool IsTrusted)`; the content is always Markdown source |
| `ProviderResult<T>` (`T \| undefined \| null \| Thenable`), `CancellationToken` | `Task<T?>` and `System.Threading.CancellationToken` |
| `async` / `await`, `Promise.then` in a contrib | no `await` in View: `UiContinuation.Then(…)` (ADR 0011 decision 4) |
| `AsyncIterableProducer`, `HoverOperation` first / second wait | `HoverOperation` over `IEditorScheduler`, results delivered by callbacks (P8-09) |
| `IContextKey` (`suggestWidgetVisible`, `editorHoverVisible`) | a `bool` of `KeybindingContext` read by a `WhenClauses` predicate (phase 3) |
| `ISymbolNavigationService`, `ICodeEditorService.openCodeEditor` | `ILocationOpener` (public, given by the host) |
| `URI` | a `string` (the document URI the host and the server agree on, `file:///…`); no `System.Uri` parsing in the editor |

## 4. Asynchrony and threading in phase 8

Binding rules of ADR 0011 decision 4, as a checklist for the cards:

1. Providers are **called on the UI thread** and return a `Task` that **may complete on any thread**.
2. View code consumes a provider `Task` only through `UiContinuation.Then` (P8-05). It never calls `await`, `.Result`, `.Wait()` or `Task.Run`.
3. Every request has a `CancellationTokenSource` owned by the object that started it; starting a new request, hiding the widget or disposing the feature cancels
   and disposes the old one; a result that arrives for a cancelled or outdated request (the token is cancelled, or the model version changed) is dropped silently.
4. `LoomEditor.Lsp` code may use `async` / `await` with `ConfigureAwait(false)`; it never touches `TextModel`, `MarkerService` or `LanguageFeaturesService` off the UI
   thread: the session (P8-34) posts through its `post` delegate. The conversions (`LspConvert`) are pure and do not touch the model except through the model values the caller passes.
5. Exceptions of a provider are caught where the result is consumed and ignored (Monaco: `onUnexpectedExternalError`), never rethrown on the UI thread.

## 5. LSP wire conventions (steps F and G)

- JSON property names are camelCase (`LspJsonContext`); the framing is `Content-Length: <bytes>\r\n\r\n<utf-8 json>` (`Content-Type` is accepted and ignored).
- Positions: ADR 0011 decision 5 (UTF-16, zero based; `LspConvert` is the only place that converts).
- Document URIs are strings, passed through unchanged between the host, the server and the editor (never normalized, never parsed).
- Request ids the client sends are positive integers starting at 1; ids from the server may be integers or strings and are echoed back as they came.
- The client never sends a request before `initialize` has answered, never sends `initialized` before the `initialize` response, and sends nothing after `shutdown` except `exit`.

## 6. Test helpers shared by many cards

| Helper | Created by | What |
|---|---|---|
| `tests/LoomEditor.Core.Tests/Languages/Features/TestHelpers/TestProviders.cs` | P8-02 | `FakeCompletionProvider`, `FakeHoverProvider`, `FakeDefinitionProvider` (scripted results, record calls) |
| `ManualUiDispatcher` (existing: `tests/LoomEditor.View.Tests/Highlighting/TestHelpers/ManualUiDispatcher.cs`) | — | an `IUiDispatcher` that queues actions until `RunAll()` |
| `tests/LoomEditor.View.Tests/TestHelpers/TestProviders.cs` (its own copy of the Core one, same names, same members) | P8-09 | the fakes above for View tests |
| `tests/LoomEditor.View.Tests/Suggest/TestHelpers/SuggestTestEditor.cs` | P8-16 | an `EditorContext` with a `ManualEditorScheduler`, a `ManualUiDispatcher`, a language, a text and a caret helper (`Type("x")`, `MoveCaretTo(line, column)`) |
| `tests/LoomEditor.Lsp.Tests/TestHelpers/TestPeer.cs` | P8-26 | the other end of a `MemoryPipe` with a message reader and writer, for tests of the JSON-RPC connection |
| `tests/LoomEditor.Lsp.Tests/TestHelpers/MemoryPipe.cs` | P8-24 | two connected in-memory `Stream`s (a duplex pair) for in-process client ⇄ server tests |
| `tests/LoomEditor.Lsp.Tests/TestHelpers/ScriptedServer.cs` | P8-29 | a hand-written server (handlers by method; can stay silent or answer errors) for tests that need a minimal or misbehaving peer |
| `tests/LoomEditor.Lsp.Tests/TestHelpers/ManualPost.cs` | P8-32 | a `Func<Action, bool>` that queues actions until `RunAll()` |
| `tests/LoomEditor.Lsp.Tests/TestHelpers/FakeServerHost.cs` (+ `FakeServerLocator.cs`) | P8-28 | starts the fake server in process over a `MemoryPipe`, or as a child process |

A card that needs a helper missing from this table creates it in the `TestHelpers/` folder next to its tests and adds a row here.

## 7. Manual checks

WinUI cards add their manual checks to `docs/testing/phase-8-manual-tests.md` (created by P8-05, written in full by P8-38). The Native AOT publish of the Gallery is
attempted by the cards that say so with `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` (the csproj already sets
`UseOptimizedPublish=false`, see ADR 0008), followed by the **start check**: run the published `LoomEditor.Gallery.exe`, wait 10 seconds, the process must still be running
(`Get-Process LoomEditor.Gallery`), then kill it (`Stop-Process -Name LoomEditor.Gallery -Force`). Report the result honestly in the card, never tick it if it was not run.
Anything only a human with the real server can verify stays **pending** in the manual plan and in the roadmap, never claimed.

## 8. Commands and commit

Same as every card (see `CLAUDE.md`). Public API: `./eng/Update-PublicApi.ps1 src/<Project>` after adding public members, and check the diff of
`PublicAPI.Unshipped.txt`. `docs/PORTING.md` map: one row per ported Monaco file, with what is not ported.

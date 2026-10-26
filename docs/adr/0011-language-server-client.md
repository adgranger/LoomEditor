# ADR 0011 — Language server client: language features, markers, hover, completion, go to definition, LSP over stdio

- **Status:** accepted
- **Date:** 2026-10-23

## Context

Phase 8 lets the editor talk to a real language server (Language Server Protocol, JSON-RPC over stdio) and shows what it
returns: diagnostics (squiggles), hover, the completion list and go to definition. Monaco has no LSP: it has
**language features** (`languages.ts`: `CompletionItemProvider`, `HoverProvider`, `DefinitionProvider`…, registered in a
`LanguageFeatureRegistry`) and **markers** (`IMarkerData`, turned into decorations by `markerDecorationsService.ts`), and the
workbench (`vscode-languageclient`, not in this repository) is what speaks LSP. So the work has three independent layers:

1. the **provider contracts and the pure editor logic** (registry, markers, fuzzy filter, completion model, suggest state
   machine, hover computation, go to definition): a port of Monaco, with ported tests;
2. the **widgets** (squiggles drawn by the renderer, a hover popup, a completion list): WinUI / Rendering;
3. the **LSP client** (framing, JSON-RPC, JSON types, lifecycle, document sync, mapping LSP ⇄ provider types): new code with
   no Monaco counterpart, specified here and in the cards.

Four problems need a decision: where the code goes (the dependency table of `ARCHITECTURE.md`), how JSON is read and
written under Native AOT, which thread runs what (the server answers off the UI thread, the model changes on it), and which
real server the Gallery shows.

## Decisions

1. **Provider contracts, registry and markers are public and live in `LoomEditor.Core`.** They are the extension API of the
   control (an application can register its own completion provider without LSP) and the LSP project must reach them, so
   they cannot be `internal` in `View`.

   | What | Where | Namespace |
   |---|---|---|
   | `languages.ts` subset: `CompletionItem`, `CompletionList`, `CompletionContext`, `Hover`, `Location`, `LocationLink`, `MarkdownString`, provider interfaces (**public**) | `LoomEditor.Core/Languages/Features/` | `LoomEditor.Core.Languages.Features` |
   | `languageFeatureRegistry.ts`, `languageSelector.ts` (`score`), `services/languageFeatures.ts` (**public**) | same folder | same |
   | `platform/markers/common/markers.ts` subset, `services/markerDecorationsService.ts` (service **public**, decorations updater `internal`) | `LoomEditor.Core/Markers/` | `LoomEditor.Core.Markers` |
   | `base/common/filters.ts` (fuzzy part) | `LoomEditor.Core/Filters/` | `LoomEditor.Core.Filters` |
   | a small Markdown subset parser (no Monaco counterpart) | `LoomEditor.View/Markdown/` | `LoomEditor.View.Markdown` |
   | `contrib/suggest/browser/*` (model and logic) | `LoomEditor.View/Suggest/` (logic), `LoomEditor.View/Features/Suggest/` (glue: commands, keybindings, feature) | `LoomEditor.View.Suggest`, `LoomEditor.View.Features.Suggest` |
   | `contrib/hover/browser/*` | `LoomEditor.View/Hover/`, `LoomEditor.View/Features/Hover/` | likewise |
   | `contrib/gotoSymbol/browser/*` | `LoomEditor.View/Features/GotoDefinition/` | `LoomEditor.View.Features.GotoDefinition` |
   | hover popup, completion list, overlay host | `LoomEditor.WinUI/Controls/Overlay/`, `Hover/`, `Suggest/` | `LoomEditor.WinUI.Controls.…` |
   | squiggles, ruler marks | `LoomEditor.View/Layout/DecorationStyles.cs`, `LoomEditor.Rendering/TextViewRenderer.Decorations.cs` | existing |
   | the LSP client | **new project** `src/LoomEditor.Lsp/` | `LoomEditor.Lsp.JsonRpc`, `.Protocol`, `.Client`, `.Editor` |
   | the in-repo test double of a language server | `tests/LoomEditor.Lsp.FakeServer/` (console app, test only) | `LoomEditor.Lsp.FakeServer` |
   | LSP tests | `tests/LoomEditor.Lsp.Tests/` | mirrors `src/LoomEditor.Lsp` |

   The logic of a contrib that is not a feature by itself (`Suggest/`, `Hover/`, `Markdown/`) sits in a plain folder of
   `LoomEditor.View`, like `Accessibility/` and `Minimap/`; the **feature folder** (`Features/Suggest/` …) holds only the glue
   (`XxxFeature.cs`, `XxxCommands.cs`, `README.md`), so `ConventionTests` (every feature folder has the three files and is
   listed in `EditorFeatures.cs`) holds from the card that creates the folder.

2. **`LoomEditor.Lsp` references only `LoomEditor.Core`.** It is platform neutral (no Windows API: `System.Diagnostics.Process`,
   `System.Text.Json`, `System.Threading.Channels` are portable), so it joins `UiIndependentProjects` and `PortableProjects`
   in the architecture tests and gets the row `LoomEditor.Lsp → LoomEditor.Core` in `DependencyRulesTests.AllowedReferences`
   and in `ARCHITECTURE.md`. It does **not** reference `View` (it must not know features, widgets or `EditorContext`): it talks to the editor only
   through public Core types (`TextModel`, `LanguageFeaturesService`, `MarkerService`). The Gallery (a sample, outside the table) references
   both `LoomEditor.WinUI` and `LoomEditor.Lsp` and wires them. The dependency table gains one row; nothing existing changes.

3. **JSON: `System.Text.Json` source generation, no reflection.** One `LspJsonContext : JsonSerializerContext`
   (`[JsonSerializable]` per message / parameter / result type, camelCase, `DefaultIgnoreCondition = WhenWritingNull`) is the only way types are
   (de)serialized: `JsonSerializer.Serialize(value, LspJsonContext.Default.X)`, never the overloads with `JsonSerializerOptions` (banned by
   `BannedSymbols.txt`). The LSP unions (`Hover.contents`, completion result `CompletionItem[] | CompletionList`, `definition` result
   `Location | Location[] | LocationLink[]`, `documentation: string | MarkupContent`, `textEdit: TextEdit | InsertReplaceEdit`, request ids
   `integer | string`) are typed as `JsonElement` in the wire types and decoded by hand-written, AOT-safe helpers (`LspUnions`), not by
   polymorphic converters. A JSON-RPC message is read into one flat `JsonRpcMessage` (`id`, `method`, `params`, `result`, `error` as `JsonElement?`) and
   the `params` / `result` are then deserialized with the type info of the method that the client knows. Unknown properties are ignored (servers send
   many). **Proof under Native AOT:** the fake server is built with `PublishAot` and the card that adds the types (P8-27) publishes it and runs the
   `initialize` handshake against it; the Gallery AOT publish (P8-37) proves the whole chain.

4. **Threading.** Everything that touches the editor (`TextModel`, decorations, `MarkerService`, `LanguageFeaturesService`, the features, the
   widgets) is **UI thread only**, as before. The LSP client is thread-safe and never touches them directly:
   - the client has **one background read loop** (a long-running task reading `Content-Length` frames from the server's stdout) and **one ordered
     write queue** (`Channel<byte[]>`, single reader): sending never blocks the caller and messages keep their order, so a `didChange` sent on the UI
     thread always reaches the server before a `completion` request sent after it;
   - requests return `Task<T>` completed by the read loop (`TaskCreationOptions.RunContinuationsAsynchronously`: the read loop never runs foreign code inline);
     cancellation sends `$/cancelRequest`;
   - server notifications (`publishDiagnostics`, `window/logMessage`, `window/showMessage`) are handed to the **session** (`LspEditorSession`), which
     posts them with a `Func<Action, bool> post` given at construction (the Gallery passes `DispatcherQueue.TryEnqueue`; tests pass a manual queue); a handler
     never runs on the read loop;
   - **providers** (`ICompletionItemProvider`…) are called on the UI thread and return a `Task` that **may complete on any thread**. The View code
     that consumes a provider result never uses `await` (no `SynchronizationContext` in tests, and one in WinUI is an implicit dependency): it uses
     `UiContinuation.Then(task, dispatcher, onSuccess, onError, token)` (P8-05), which runs the callback through `IEditorContext`'s `IUiDispatcher`
     (the control passes its `DispatcherQueue` dispatcher, as for tokenization, ADR 0008 decision 5; with none, the callback runs inline: tests, the
     parameterless `EditorContext`). After the callback is posted the code re-checks that its request is still the current one (version id, request number).
   - **conversions that run in a provider continuation** (a thread-pool thread: LSP → completion items, hovers, locations; hover part building) **never touch the `TextModel`**: they convert positions numerically, and the UI-thread code that consumes the result validates ranges against the model. Only diagnostics are converted on the UI thread (they need the model to clamp ranges);
   - timers (quick suggest delay, hover delay) are `IEditorScheduler` timers as in ADR 0009 decision 10. No `Task.Delay`, no `Thread`, no `DateTime` in View.
   `async` / `await` is allowed in `LoomEditor.Lsp` (always `ConfigureAwait(false)`); the `Task`s it exposes complete on a thread-pool thread.

5. **Positions.** LSP positions are zero-based `line` and `character` counted in **UTF-16 code units** (the default; the client advertises
   `general.positionEncodings: ["utf-16"]`), exactly Monaco's `column - 1` (a Monaco column is a UTF-16 offset + 1). So `Position(line, column) ⇄ (line - 1, column - 1)`,
   nothing else: no grapheme or code point conversion. Lines are split like the model splits them (`\n`, `\r\n`, `\r`), so line numbers agree.
   A position from a server that is beyond the line end is clamped to the line end (LSP rule), one beyond the last line to the end of the document
   (`TextModel.ValidatePosition`). A range from a server is validated the same way before it is used. All of it is in one class, `LspConvert` (P8-30).

6. **Document sync.** One editor = one text document (the editor has one model): `textDocument/didOpen` with the full text, then **incremental**
   `didChange` built from `ModelContentChangedEventArgs.Changes` (already ordered from the end of the document to the beginning, so they apply in sequence,
   which is what LSP requires), a full-text `didChange` after a flush (`SetValue`) or an EOL change, `didClose` on dispose; the version is the model's
   `VersionId`. If the server asks for `Full` (`textDocumentSync.change = 1`) the client sends the full text. No `willSave` / `didSave`.

7. **What the client asks for, and what it does not.** Client capabilities: incremental sync; `completion` (no snippets: `snippetSupport = false`, so
   `insertText` is plain text; markdown and plaintext documentation; `insertReplaceSupport`; `resolveSupport` for `documentation`, `detail`, `additionalTextEdits`;
   `contextSupport`; the 25 LSP kinds); `hover` (markdown, plaintext); `definition` (`linkSupport`); `publishDiagnostics` (`versionSupport`, `tagSupport` 1 and 2);
   nothing else, and no `workspace.*`, no `window.workDoneProgress`. **Not in phase 8:** snippets, signature help, references, rename, code actions, formatting,
   semantic tokens, inlay hints, folding ranges and document highlights from the server (ADR 0009 decision 9 named LSP folding; phase 8 does not deliver it, the
   roadmap does not list it), document links, workspace symbols, pull diagnostics, several documents / workspace folders (the client sends one `rootUri`),
   server-initiated edits (`workspace/applyEdit` is answered `applied: false`), and Ctrl+hover link underlines.

8. **Features in the editor.** Squiggles and the overview ruler marks come from **marker decorations** (the port of `markerDecorationsService.ts`, owner = the
   editor id); `DecorationStyles` gets Monaco's `squiggly-*` rows and the renderer draws an underline (zigzag, dotted). Hover is Monaco's content hover reduced
   to two participants (markers, providers) over a `HoverOperation` state machine; the popup is a WinUI element of an **overlay layer in the control template**
   (no `Popup`, so no `XamlRoot` or window-level z-order issues: it is clipped to the editor, as Monaco's `fixedOverflowWidgets: false`). The completion list is
   a WinUI element of the same layer, **drawn with a fixed number of reusable row elements and its own scrolling** (no `ListView`, no `ItemsSource`, no data template:
   nothing that needs binding under Native AOT); the editor keeps the keyboard focus and keys go through the keybinding resolver with new `when` conditions
   (`suggestWidgetVisible`, `hoverVisible`). Go to definition is `F12` and `Ctrl+click`; a target in another document goes to an `ILocationOpener` given by the
   host (default: not handled), a target in this document moves the cursor and reveals it.

9. **The real language server of the Gallery is `csharp-ls`.** Reasons: MIT license; **not bundled** (nothing to ship, no license issue): a developer installs
   it with `dotnet tool install --global csharp-ls`, and a developer of this repository already has the .NET SDK; it speaks plain stdio LSP; it supports
   completion, hover, definition and diagnostics; the Gallery already ships a C# sample. The Gallery finds it, in order: the environment variable
   `LOOMEDITOR_LSP_COMMAND` (a full command line, any server that speaks stdio; `LOOMEDITOR_LSP_LANGUAGE` gives the language id, default `csharp`), then
   `%USERPROFILE%\.dotnet\tools\csharp-ls.exe`, then `csharp-ls` on `PATH`. csharp-ls needs a project to analyze: the Gallery writes a tiny workspace
   (`Sample.csproj` + `Program.cs`) under `%LOCALAPPDATA%\LoomEditor.Gallery\lsp-workspace\`, starts the server with that folder as working directory and
   `rootUri`, and opens `Program.cs` as the document. **When the server is absent** the Gallery shows "Language server not found" and the install command in its
   status line; the editor works as before and nothing else changes. Whether csharp-ls works with the installed .NET SDK (11 preview) and how long it takes
   to start **was not verified when this ADR was written**: the card P8-37 says what to do on failure, and a human verifies the result (manual plan, P8-38).
   CI never needs a real server: every automated test uses the **fake server** (decision 10).

10. **The fake language server** (`tests/LoomEditor.Lsp.FakeServer`) is a console app, `PublishAot`-able, built only for tests. It holds the text of the open
    document and answers `initialize` (capabilities: incremental sync, completion with trigger character `.`, hover, definition), `completion`,
    `completionItem/resolve`, `hover`, `definition`, `shutdown`, `exit`, and pushes `publishDiagnostics` after `didOpen` / `didChange`; its rules are
    in P8-28 and deterministic. The loop is a class over two `Stream`s, so most LSP tests run it **in process** over in-memory pipes; a few tests start the
    real executable as a child process to test the process lifecycle and the stdio framing. Test projects reference it with a `ProjectReference`
    (`ReferenceOutputAssembly` stays true to copy it; the child process is started as `dotnet <path>/LoomEditor.Lsp.FakeServer.dll`).

## Consequences

- The View code of phase 8 is testable without a UI and without a server: providers are test doubles, the dispatcher is manual.
- `LoomEditor.Core`'s public API grows (provider contracts, registry service, marker service); `EditorContext` and the control expose
  `LanguageFeatures`, `Markers`, `DocumentUri` (P8-05). Applications can register their own providers and markers without LSP.
- One more project to build and one more AOT-checked assembly. The architecture tests and `ARCHITECTURE.md` get the `LoomEditor.Lsp` row (P8-24).
- Real-server behavior (start time, SDK compatibility, what csharp-ls returns) is only verified by a human (manual plan P8-38); the roadmap says so.
- Deferred on purpose: see decision 7.

## Changes during implementation

- **Snippets are flattened, not refused (decision 7).** `snippetSupport = false` is advertised, but a server may still send snippet text; `LspSnippet.ToPlainText` reduces it to plain text (tab stops vanish, placeholders keep their default, choices keep the first option). Card P8-31.
- **The Gallery window close waits for the session (decision 9).** The `Closed` handler cancels the close, waits at most 3 s for the session to stop and closes again, because the session needs the UI thread to finish stopping. Card P8-37.

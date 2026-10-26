# Phase 8 — LSP client (completion, hover, diagnostics, go-to-definition)

**Done when:** the Gallery demonstrates a real language server. — **Not ticked** (see "Status of the done-when line").

Task cards: [docs/tasks/phase-8/](../tasks/phase-8/README.md). Design: [ADR 0011](../adr/0011-language-server-client.md). The real server of the Gallery is `csharp-ls` (a `dotnet` tool, installed by the developer, not bundled); every automated test uses an in-repo fake language server instead.

## Checklist (ticked 2026-10-26 = built and covered by automated tests; the UI parts are also published with Native AOT, but only a human ran none of the manual plan)

- [x] JSON-RPC over stdio (source-generated JSON, Native AOT safe) — P8-24 (the `LoomEditor.Lsp` project), P8-25 (`Content-Length` framing), P8-26 (the JSON-RPC connection), P8-27 (LSP types and union decoders), P8-28 (the fake language server), P8-29 (client lifecycle and server process), P8-30 (incremental document sync)
- [x] Diagnostics → decorations — P8-03 … P8-06 (markers, marker decorations, squiggles and ruler marks), P8-31 / P8-32 (LSP diagnostics → markers)
- [x] Hover — P8-07 … P8-11 (Markdown subset, mouse events, hover computation and controller, the WinUI popup), P8-34 (the LSP hover provider)
- [x] Completion widget — P8-12 … P8-22 (fuzzy filter, suggest model and triggers, list state, accept, late edits, commit characters, commands and keys, the WinUI list), P8-33 (the LSP completion provider)
- [x] Go to definition — P8-23 (`F12`, `Ctrl+click`), P8-34 (the LSP definition provider)
- [x] Language features API for applications (providers, registry, markers) — P8-01, P8-02, P8-03, P8-05
- [x] The editor session that wires a server to an editor — P8-35; end-to-end tests of the whole stack against the fake server — P8-36
- [x] The Gallery connects to a language server (and has in-process demo providers) and a self-check — P8-37; manual test plan written — P8-38; close — P8-39
- [ ] The manual test plan ([testing/phase-8-manual-tests.md](../testing/phase-8-manual-tests.md)) run by the maintainer: **pending**

## Status of the done-when line

- **Demonstrated against the fake language server**, in the published Native AOT Gallery (P8-37 self-check, exit code 0): `PASS server found`, `session running`, `diagnostics`, `completion`, `hover`, `definition`, `stop`.
- **Not verified against `csharp-ls`.** The one run against `csharp-ls` 0.26.0.0 (P8-37, no environment variable) exited 1: `PASS` for server found, session running, diagnostics, hover, definition and stop, but **`FAIL completion: no item`** at line 14 column 17 (right after `Console.`).
  The document has a syntax error there and the server may still have been loading the workspace; the cause was not investigated. Manual plan section 4 is pending the maintainer.
- Native AOT: the Gallery publish (`-p:PublishAot=true -warnaserror`) succeeded in P8-11, P8-22 (with `-p:UseOptimizedPublish=false`) and P8-37, the exe still running after 10 s each time. P8-39 changed no code and did not re-run it.

## Moved / not delivered (ADR 0011 decision 7)

Snippets, signature help, references, rename, code actions, formatting, semantic tokens, inlay hints, folding ranges and document highlights from the server, document links, workspace symbols, pull diagnostics, several documents or workspace folders, server-initiated edits (`workspace/applyEdit` is answered `applied: false`), peek definition, Ctrl+hover link underlines, the diagnostics list.
Also not delivered, from earlier phases' cut points: outline-based sticky scroll, the sticky scroll click-link gesture, `hoverMessage` decorations in the popup.

## Open risks

- `csharp-ls` with the installed .NET SDK (11 preview) and its start time: the completion row failed once (above).
- UI-thread assumptions: the providers' continuations go through `IUiDispatcher`; a provider that completes synchronously runs inline.
- The `didChange` order guarantee relies on `ModelContentChangedEventArgs.Changes` being end-to-start and on one writer queue.
- The overlay widgets are not announced by screen readers; the hover and completion widgets are clipped to the editor.
- One cursor only for completion insertion; plain-text insertion only (servers that send snippets get them flattened).
- Markdown subset: no tables, images; links are colored but not clickable.
- `Ctrl+click` also fires on a double click (the click count is not available).

# Phase 8 — LSP client (completion, hover, diagnostics, go-to-definition)

**Done when:** the Gallery demonstrates a real language server.

Task cards: [docs/tasks/phase-8/](../tasks/phase-8/README.md). Design: [ADR 0011](../adr/0011-language-server-client.md). The real server of the Gallery is `csharp-ls` (a `dotnet` tool, installed by the developer, not bundled); every automated test uses an in-repo fake language server instead.

- [ ] JSON-RPC over stdio (source-generated JSON, Native AOT safe) — P8-24 (the `LoomEditor.Lsp` project), P8-25 (`Content-Length` framing), P8-26 (the JSON-RPC connection), P8-27 (LSP types and union decoders), P8-28 (the fake language server), P8-29 (client lifecycle and server process), P8-30 (incremental document sync)
- [ ] Diagnostics → decorations — P8-03 … P8-06 (markers, marker decorations, squiggles and ruler marks), P8-31 / P8-32 (LSP diagnostics → markers)
- [ ] Hover — P8-07 … P8-11 (Markdown subset, mouse events, hover computation and controller, the WinUI popup), P8-34 (the LSP hover provider)
- [ ] Completion widget — P8-12 … P8-22 (fuzzy filter, suggest model and triggers, list state, accept, late edits, commit characters, commands and keys, the WinUI list), P8-33 (the LSP completion provider)
- [ ] Go to definition — P8-23 (`F12`, `Ctrl+click`), P8-34 (the LSP definition provider)
- [ ] Language features API for applications (providers, registry, markers) — P8-01, P8-02, P8-03, P8-05
- [ ] The editor session that wires a server to an editor — P8-35; end-to-end tests of the whole stack — P8-36
- [ ] The Gallery demonstrates a language server (and the in-process demo providers) — P8-37; manual test plan — P8-38; close — P8-39

Not in phase 8 (ADR 0011 decision 7, listed in the task index): snippets, signature help, references, rename, code actions, formatting, semantic tokens, inlay hints, folding ranges and document highlights from the server, document links, workspace symbols, pull diagnostics, several documents or workspace folders, server-initiated edits, peek definition, the diagnostics list.

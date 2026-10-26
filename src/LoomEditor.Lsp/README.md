# LoomEditor.Lsp

Language Server Protocol client: JSON-RPC over stdio, document synchronization, and the mapping from LSP to the
editor's language features (diagnostics → markers, completion, hover, definition). See ADR 0011.

## Folder plan

- `JsonRpc/` — message framing and the JSON-RPC connection.
- `Protocol/` — LSP protocol types and source-generated JSON.
- `Client/` — `LspClient`: process start, lifecycle, document sync.
- `Editor/` — providers and `LspEditorSession`, the bridge to the editor.

## Rules

- References **Core only**; no UI framework (checked by `LoomEditor.Architecture.Tests`).
- UI-thread rule of ADR 0011 applies to everything that touches the editor.
- No `await` outside this project's own code.
- No reflection-based JSON (Native AOT): source-generated `System.Text.Json` only.

## Files

- `LspProtocolVersion.cs` — the specification version (3.17).
- `Editor/LspEditorSession.cs`, `LspEditorSession.Lifecycle.cs` — the public entry point: start, stop and fault handling.
- `Editor/LspSessionOptions.cs`, `LspSessionState.cs` — its options and its state enum.

## How to use

```csharp
var session = new LspEditorSession(editor.Model, editor.LanguageFeatures, editor.Markers, editor.DocumentUri, "csharp",
    new LspServerCommand("csharp-ls", [], workspaceFolder), action => editor.DispatcherQueue.TryEnqueue(() => action()),
    new LspSessionOptions { RootUri = workspaceUri });
await session.StartAsync();
```

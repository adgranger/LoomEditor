# How to connect an editor to a language server

Phase 8 gives the control completion, hover, go to definition and diagnostics. They are driven by **providers** and **markers** that your application supplies:
from a language server over LSP (`LoomEditor.Lsp`), or from your own code. Design: [ADR 0011](../adr/0011-language-server-client.md).

## With a language server (LSP)

Reference `LoomEditor.Lsp`, start the server as a child process and bind it to one editor (one editor = one document):

```csharp
var session = new LspEditorSession(editor.Model, editor.LanguageFeatures, editor.Markers, editor.DocumentUri, "csharp",
    new LspServerCommand("csharp-ls", [], workspaceFolder),
    action => editor.DispatcherQueue.TryEnqueue(() => action()),   // runs server notifications on the UI thread
    new LspSessionOptions { RootUri = workspaceUri });
await session.StartAsync();   // initialize, didOpen, then providers and diagnostics are registered
// ...
await session.StopAsync();    // unregisters, didClose, shutdown, exit
```

`LspEditorSession.StateChanged` and `ServerOutput` tell you what the server is doing. Nothing is bundled: install the server yourself
(`dotnet tool install --global csharp-ls`). The Gallery shows the whole thing in `samples/LoomEditor.Gallery/Lsp/`.

## Without LSP: your own providers and markers

```csharp
IDisposable reg = editor.LanguageFeatures.CompletionProviders.Register("mylang", new MyCompletionProvider());
// HoverProviders and DefinitionProviders work the same way; dispose the registration to remove it.

editor.Markers.Change("my-linter", [new MarkerData(MarkerSeverity.Warning, "message", 1, 1, 1, 5)]);   // squiggles + ruler marks
editor.Markers.Remove("my-linter");
```

A provider is called on the UI thread and returns a `Task` that may complete on any thread. Markers are per owner: `Change` replaces all the markers of that owner.

## Go to definition across documents

A definition in the current document moves the cursor. For another document set `editor.LocationOpener` to your `ILocationOpener`
(`bool Open(LocationLink target)`: open the document and select the range, return `false` when you cannot). Without one the jump is not handled.

## Testing

Do not depend on a real server. `tests/LoomEditor.Lsp.FakeServer` is a deterministic server; run it in process over in-memory pipes
(`tests/LoomEditor.Lsp.Tests/TestHelpers/`) or as a child process. Its rules are in its [README](../../tests/LoomEditor.Lsp.FakeServer/README.md);
`tests/LoomEditor.View.Tests/Lsp/` shows the real editor features driven against it.

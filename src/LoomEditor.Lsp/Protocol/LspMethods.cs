namespace LoomEditor.Lsp.Protocol;

/// <summary>The LSP method names the client sends or receives.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspMethods
{
    public const string Initialize = "initialize";
    public const string Initialized = "initialized";
    public const string Shutdown = "shutdown";
    public const string Exit = "exit";
    public const string DidOpen = "textDocument/didOpen";
    public const string DidChange = "textDocument/didChange";
    public const string DidClose = "textDocument/didClose";
    public const string PublishDiagnostics = "textDocument/publishDiagnostics";
    public const string Completion = "textDocument/completion";
    public const string CompletionResolve = "completionItem/resolve";
    public const string Hover = "textDocument/hover";
    public const string Definition = "textDocument/definition";
    public const string CancelRequest = "$/cancelRequest";
    public const string LogMessage = "window/logMessage";
    public const string ShowMessage = "window/showMessage";
    public const string ShowMessageRequest = "window/showMessageRequest";
    public const string WorkspaceConfiguration = "workspace/configuration";
    public const string RegisterCapability = "client/registerCapability";
    public const string UnregisterCapability = "client/unregisterCapability";
    public const string WorkDoneProgressCreate = "window/workDoneProgress/create";
    public const string ApplyEdit = "workspace/applyEdit";
}

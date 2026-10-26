namespace LoomEditor.Lsp.Client;

/// <summary>The lifecycle state of an <see cref="LspClient"/>.</summary>
internal enum LspClientState
{
    NotStarted,
    Initializing,
    Running,
    ShuttingDown,
    Stopped,
    Faulted,
}

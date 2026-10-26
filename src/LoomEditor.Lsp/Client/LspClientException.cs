namespace LoomEditor.Lsp.Client;

/// <summary>The client cannot do what was asked: the server is not running, or it failed.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed class LspClientException(string message, Exception? inner = null) : Exception(message, inner);

namespace LoomEditor.Lsp.Client;

/// <summary>The language server process could not be started.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed class LspServerStartException(string message, Exception? inner = null) : Exception(message, inner);

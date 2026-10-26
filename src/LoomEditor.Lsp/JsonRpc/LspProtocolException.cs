namespace LoomEditor.Lsp.JsonRpc;

/// <summary>The peer broke the protocol: malformed header, end of stream inside a message, message too large.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed class LspProtocolException(string message) : Exception(message);

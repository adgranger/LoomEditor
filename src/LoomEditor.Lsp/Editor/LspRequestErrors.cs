using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.JsonRpc;

namespace LoomEditor.Lsp.Editor;

/// <summary>Classifies the errors of an LSP request that the providers answer with <c>null</c> instead of faulting.</summary>
/// <remarks>Threading: pure, any thread.</remarks>
internal static class LspRequestErrors
{
    /// <summary>True when the document changed under the server (the next keystroke asks again), the request was cancelled, or the client stopped.</summary>
    public static bool IsBenign(Exception e) => e is LspClientException
        || e is JsonRpcException { Code: JsonRpcErrorCodes.ContentModified or JsonRpcErrorCodes.RequestCancelled or JsonRpcErrorCodes.ServerCancelled };
}

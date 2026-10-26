using System.Text.Json;

namespace LoomEditor.Lsp.JsonRpc;

/// <summary>A JSON-RPC error response: thrown to the caller of a request, or by a request handler to answer with an error.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed class JsonRpcException(int code, string message, JsonElement? data = null) : Exception(message)
{
    /// <summary>The error code (see <see cref="JsonRpcErrorCodes"/>).</summary>
    public int Code { get; } = code;

    /// <summary>The optional <c>data</c> member of the error (named to not hide <see cref="Exception.Data"/>).</summary>
    public JsonElement? ErrorData { get; } = data;
}

/// <summary>The connection is closed or disposed: a send was refused or a pending request lost its answer.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed class JsonRpcConnectionClosedException(string message, Exception? inner = null) : Exception(message, inner);

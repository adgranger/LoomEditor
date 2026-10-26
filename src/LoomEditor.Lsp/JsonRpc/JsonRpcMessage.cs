using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoomEditor.Lsp.JsonRpc;

/// <summary>The JSON-RPC 2.0 envelope: a request, a notification or a response, told apart by which members are present.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class JsonRpcMessage
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>An integer or a string; kept as an element so a response echoes it unchanged.</summary>
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }

    [JsonPropertyName("result")]
    public JsonElement? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

/// <summary>The <c>error</c> member of a response.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}

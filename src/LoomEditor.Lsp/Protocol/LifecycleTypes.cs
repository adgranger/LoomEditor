using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>InitializeParams</c>; <c>processId</c> and <c>rootUri</c> are required keys that may be null.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class InitializeParams
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public int? ProcessId { get; set; }

    [JsonPropertyName("clientInfo")]
    public ClientInfoDto? ClientInfo { get; set; }

    public string? Locale { get; set; }

    public string? RootPath { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? RootUri { get; set; }

    public required ClientCapabilities Capabilities { get; set; }

    public JsonElement? InitializationOptions { get; set; }

    public string? Trace { get; set; }
}

/// <summary>The <c>clientInfo</c> member of <c>InitializeParams</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class ClientInfoDto
{
    public required string Name { get; set; }

    public string? Version { get; set; }
}

/// <summary>LSP <c>InitializedParams</c>: writes <c>{}</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class InitializedParams;

/// <summary>LSP <c>InitializeResult</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class InitializeResult
{
    public required ServerCapabilities Capabilities { get; set; }

    public ServerInfoDto? ServerInfo { get; set; }
}

/// <summary>The <c>serverInfo</c> member of <c>InitializeResult</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class ServerInfoDto
{
    public required string Name { get; set; }

    public string? Version { get; set; }
}

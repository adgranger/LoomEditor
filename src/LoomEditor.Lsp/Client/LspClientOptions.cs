using System.Text.Json;

namespace LoomEditor.Lsp.Client;

/// <summary>What the client tells the server about itself, and its time limits.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record LspClientOptions
{
    public string ClientName { get; init; } = "LoomEditor";

    public string? ClientVersion { get; init; }

    public string? RootUri { get; init; }

    public JsonElement? InitializationOptions { get; init; }

    /// <summary>A real server can be slow to start.</summary>
    public TimeSpan InitializeTimeout { get; init; } = TimeSpan.FromSeconds(60);

    public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(3);
}

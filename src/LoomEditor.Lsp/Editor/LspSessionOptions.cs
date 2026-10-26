namespace LoomEditor.Lsp.Editor;

/// <summary>Settings of an <see cref="LspEditorSession"/>.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
public sealed record LspSessionOptions
{
    /// <summary>The workspace root sent to the server as <c>rootUri</c>.</summary>
    public string? RootUri { get; init; }

    /// <summary>How long the server may take to answer <c>initialize</c>; a real server can be slow to start.</summary>
    public TimeSpan InitializeTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>The client version sent to the server in <c>clientInfo</c>.</summary>
    public string? ClientVersion { get; init; }

    /// <summary>The marker owner of the diagnostics of the server.</summary>
    public string DiagnosticsOwner { get; init; } = "lsp";
}

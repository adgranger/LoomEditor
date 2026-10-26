using System.Text.Json;

namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>PublishDiagnosticsParams</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class PublishDiagnosticsParams
{
    public required string Uri { get; set; }

    public int? Version { get; set; }

    public required LspDiagnostic[] Diagnostics { get; set; }
}

/// <summary>LSP <c>Diagnostic</c>; <c>code</c> is an integer or a string, kept as an element.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspDiagnostic
{
    public required LspRange Range { get; set; }

    public int? Severity { get; set; }

    public JsonElement? Code { get; set; }

    public string? Source { get; set; }

    public required string Message { get; set; }

    public int[]? Tags { get; set; }
}

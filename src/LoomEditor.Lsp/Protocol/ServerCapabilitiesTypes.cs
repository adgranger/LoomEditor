using System.Text.Json;

namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>ServerCapabilities</c> (only the parts the client reads); union members are decoded by <see cref="LspUnions"/>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class ServerCapabilities
{
    public string? PositionEncoding { get; set; }

    public JsonElement? TextDocumentSync { get; set; }

    public CompletionOptions? CompletionProvider { get; set; }

    public JsonElement? HoverProvider { get; set; }

    public JsonElement? DefinitionProvider { get; set; }
}

/// <summary>LSP <c>CompletionOptions</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class CompletionOptions
{
    public string[]? TriggerCharacters { get; set; }

    public string[]? AllCommitCharacters { get; set; }

    public bool? ResolveProvider { get; set; }
}

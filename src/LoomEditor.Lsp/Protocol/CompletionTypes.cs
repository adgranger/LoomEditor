using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>CompletionParams</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class CompletionParams
{
    public required TextDocumentIdentifier TextDocument { get; set; }

    public required LspPosition Position { get; set; }

    [JsonPropertyName("context")]
    public CompletionContextDto? Context { get; set; }
}

/// <summary>LSP <c>CompletionContext</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class CompletionContextDto
{
    public required int TriggerKind { get; set; }

    public string? TriggerCharacter { get; set; }
}

/// <summary>LSP <c>CompletionList</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspCompletionList
{
    public required bool IsIncomplete { get; set; }

    public required LspCompletionItem[] Items { get; set; }
}

/// <summary>LSP <c>CompletionItem</c>; <c>documentation</c> and <c>textEdit</c> are unions decoded by <see cref="LspUnions"/>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspCompletionItem
{
    public required string Label { get; set; }

    [JsonPropertyName("labelDetails")]
    public LabelDetailsDto? LabelDetails { get; set; }

    public int? Kind { get; set; }

    public int[]? Tags { get; set; }

    public string? Detail { get; set; }

    public JsonElement? Documentation { get; set; }

    public bool? Deprecated { get; set; }

    public bool? Preselect { get; set; }

    public string? SortText { get; set; }

    public string? FilterText { get; set; }

    public string? InsertText { get; set; }

    public int? InsertTextFormat { get; set; }

    public JsonElement? TextEdit { get; set; }

    public LspTextEdit[]? AdditionalTextEdits { get; set; }

    public string[]? CommitCharacters { get; set; }

    /// <summary>Opaque server data; returned unchanged by <c>completionItem/resolve</c>.</summary>
    public JsonElement? Data { get; set; }
}

/// <summary>LSP <c>CompletionItemLabelDetails</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LabelDetailsDto
{
    public string? Detail { get; set; }

    public string? Description { get; set; }
}

namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>ClientCapabilities</c> (only the parts the client declares).</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class ClientCapabilities
{
    public GeneralClientCapabilities? General { get; set; }

    public TextDocumentClientCapabilities? TextDocument { get; set; }
}

/// <summary>LSP <c>GeneralClientCapabilities</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class GeneralClientCapabilities
{
    public string[]? PositionEncodings { get; set; }
}

/// <summary>LSP <c>TextDocumentClientCapabilities</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class TextDocumentClientCapabilities
{
    public TextDocumentSyncClientCapabilities? Synchronization { get; set; }

    public CompletionClientCapabilities? Completion { get; set; }

    public HoverClientCapabilities? Hover { get; set; }

    public DefinitionClientCapabilities? Definition { get; set; }

    public PublishDiagnosticsClientCapabilities? PublishDiagnostics { get; set; }
}

/// <summary>LSP <c>TextDocumentSyncClientCapabilities</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class TextDocumentSyncClientCapabilities
{
    public bool? DynamicRegistration { get; set; }

    public bool? WillSave { get; set; }

    public bool? WillSaveWaitUntil { get; set; }

    public bool? DidSave { get; set; }
}

/// <summary>LSP <c>CompletionClientCapabilities</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class CompletionClientCapabilities
{
    public bool? DynamicRegistration { get; set; }

    public CompletionItemCapabilities? CompletionItem { get; set; }

    public CompletionItemKindCapabilities? CompletionItemKind { get; set; }

    public bool? ContextSupport { get; set; }
}

/// <summary>The <c>completionItem</c> member of <c>CompletionClientCapabilities</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class CompletionItemCapabilities
{
    public bool? SnippetSupport { get; set; }

    public string[]? DocumentationFormat { get; set; }

    public bool? DeprecatedSupport { get; set; }

    public bool? PreselectSupport { get; set; }

    public TagSupport? TagSupport { get; set; }

    public bool? InsertReplaceSupport { get; set; }

    public ResolveSupport? ResolveSupport { get; set; }

    public bool? LabelDetailsSupport { get; set; }

    public bool? CommitCharactersSupport { get; set; }
}

/// <summary>The <c>tagSupport</c> capability shape.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class TagSupport
{
    public required int[] ValueSet { get; set; }
}

/// <summary>The <c>resolveSupport</c> capability shape.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class ResolveSupport
{
    public required string[] Properties { get; set; }
}

/// <summary>The <c>completionItemKind</c> member of <c>CompletionClientCapabilities</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class CompletionItemKindCapabilities
{
    public int[]? ValueSet { get; set; }
}

/// <summary>LSP <c>HoverClientCapabilities</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class HoverClientCapabilities
{
    public bool? DynamicRegistration { get; set; }

    public string[]? ContentFormat { get; set; }
}

/// <summary>LSP <c>DefinitionClientCapabilities</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class DefinitionClientCapabilities
{
    public bool? DynamicRegistration { get; set; }

    public bool? LinkSupport { get; set; }
}

/// <summary>LSP <c>PublishDiagnosticsClientCapabilities</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class PublishDiagnosticsClientCapabilities
{
    public bool? RelatedInformation { get; set; }

    public TagSupport? TagSupport { get; set; }

    public bool? VersionSupport { get; set; }

    public bool? CodeDescriptionSupport { get; set; }

    public bool? DataSupport { get; set; }
}

/// <summary>Builds the capabilities the client sends in <c>initialize</c>.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class LspClientCapabilities
{
    /// <summary>The client capabilities of ADR 0011: UTF-16, plain-text insertion, no dynamic registration.</summary>
    public static ClientCapabilities Create()
    {
        int[] kinds = new int[25];
        for (int i = 0; i < kinds.Length; i++)
        {
            kinds[i] = i + 1;
        }

        string[] formats = [LspMarkupKind.Markdown, LspMarkupKind.PlainText];
        return new ClientCapabilities
        {
            General = new GeneralClientCapabilities { PositionEncodings = [LspPositionEncoding.Utf16] },
            TextDocument = new TextDocumentClientCapabilities
            {
                Synchronization = new TextDocumentSyncClientCapabilities
                {
                    DynamicRegistration = false,
                    WillSave = false,
                    WillSaveWaitUntil = false,
                    DidSave = false,
                },
                Completion = new CompletionClientCapabilities
                {
                    DynamicRegistration = false,
                    ContextSupport = true,
                    CompletionItem = new CompletionItemCapabilities
                    {
                        SnippetSupport = true,
                        DocumentationFormat = formats,
                        DeprecatedSupport = true,
                        PreselectSupport = true,
                        TagSupport = new TagSupport { ValueSet = [LspCompletionItemTag.Deprecated] },
                        InsertReplaceSupport = true,
                        ResolveSupport = new ResolveSupport { Properties = ["documentation", "detail", "additionalTextEdits"] },
                        LabelDetailsSupport = true,
                        CommitCharactersSupport = true,
                    },
                    CompletionItemKind = new CompletionItemKindCapabilities { ValueSet = kinds },
                },
                Hover = new HoverClientCapabilities { DynamicRegistration = false, ContentFormat = formats },
                Definition = new DefinitionClientCapabilities { DynamicRegistration = false, LinkSupport = true },
                PublishDiagnostics = new PublishDiagnosticsClientCapabilities
                {
                    RelatedInformation = false,
                    TagSupport = new TagSupport { ValueSet = [LspDiagnosticTag.Unnecessary, LspDiagnosticTag.Deprecated] },
                    VersionSupport = true,
                    CodeDescriptionSupport = false,
                    DataSupport = false,
                },
            },
        };
    }
}

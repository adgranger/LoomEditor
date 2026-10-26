namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>DidOpenTextDocumentParams</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class DidOpenTextDocumentParams
{
    public required TextDocumentItem TextDocument { get; set; }
}

/// <summary>LSP <c>DidChangeTextDocumentParams</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class DidChangeTextDocumentParams
{
    public required VersionedTextDocumentIdentifier TextDocument { get; set; }

    public required TextDocumentContentChangeEvent[] ContentChanges { get; set; }
}

/// <summary>LSP <c>TextDocumentContentChangeEvent</c>; a full-text change has no <c>range</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class TextDocumentContentChangeEvent
{
    public LspRange? Range { get; set; }

    public int? RangeLength { get; set; }

    public required string Text { get; set; }
}

/// <summary>LSP <c>DidCloseTextDocumentParams</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class DidCloseTextDocumentParams
{
    public required TextDocumentIdentifier TextDocument { get; set; }
}

using System.Text.Json;

namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>Position</c>: zero based line and UTF-16 character offset.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspPosition
{
    public required int Line { get; set; }

    public required int Character { get; set; }
}

/// <summary>LSP <c>Range</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspRange
{
    public required LspPosition Start { get; set; }

    public required LspPosition End { get; set; }
}

/// <summary>LSP <c>Location</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspLocation
{
    public required string Uri { get; set; }

    public required LspRange Range { get; set; }
}

/// <summary>LSP <c>LocationLink</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspLocationLink
{
    public LspRange? OriginSelectionRange { get; set; }

    public required string TargetUri { get; set; }

    public required LspRange TargetRange { get; set; }

    public required LspRange TargetSelectionRange { get; set; }
}

/// <summary>LSP <c>TextDocumentIdentifier</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class TextDocumentIdentifier
{
    public required string Uri { get; set; }
}

/// <summary>LSP <c>VersionedTextDocumentIdentifier</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class VersionedTextDocumentIdentifier
{
    public required string Uri { get; set; }

    public required int Version { get; set; }
}

/// <summary>LSP <c>TextDocumentItem</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class TextDocumentItem
{
    public required string Uri { get; set; }

    public required string LanguageId { get; set; }

    public required int Version { get; set; }

    public required string Text { get; set; }
}

/// <summary>LSP <c>TextDocumentPositionParams</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class TextDocumentPositionParams
{
    public required TextDocumentIdentifier TextDocument { get; set; }

    public required LspPosition Position { get; set; }
}

/// <summary>LSP <c>TextEdit</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspTextEdit
{
    public required LspRange Range { get; set; }

    public required string NewText { get; set; }
}

/// <summary>LSP <c>InsertReplaceEdit</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspInsertReplaceEdit
{
    public required string NewText { get; set; }

    public required LspRange Insert { get; set; }

    public required LspRange Replace { get; set; }
}

/// <summary>LSP <c>MarkupContent</c>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspMarkupContent
{
    public required string Kind { get; set; }

    public required string Value { get; set; }
}

/// <summary>LSP <c>CancelParams</c> (<c>$/cancelRequest</c>); the id is an integer or a string.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class CancelParams
{
    public required JsonElement Id { get; set; }
}

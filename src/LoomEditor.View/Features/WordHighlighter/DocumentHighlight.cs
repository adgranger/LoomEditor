// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (DocumentHighlightKind, DocumentHighlight)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.WordHighlighter;

/// <summary>A document highlight kind (Monaco: <c>DocumentHighlightKind</c>).</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
internal enum DocumentHighlightKind
{
    /// <summary>A textual occurrence.</summary>
    Text = 0,

    /// <summary>Read-access of a symbol, like reading a variable.</summary>
    Read = 1,

    /// <summary>Write-access of a symbol, like writing to a variable.</summary>
    Write = 2,
}

/// <summary>A document highlight is a range inside a text document which deserves special attention (Monaco: <c>DocumentHighlight</c>).</summary>
/// <param name="Range">The range this highlight applies to.</param>
/// <param name="Kind">The highlight kind; <see langword="null"/> is drawn as a regular highlight.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct DocumentHighlight(Range Range, DocumentHighlightKind? Kind = null);

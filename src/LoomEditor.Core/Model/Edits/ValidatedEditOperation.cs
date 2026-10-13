// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.Edits;

/// <summary>An edit with the data <c>ApplyEdits</c> needs: line break statistics and offsets.</summary>
/// <remarks>
/// <para>Monaco: <c>IValidatedEditOperation</c>. Mutable like Monaco: <see cref="SortIndex"/> is assigned after creation.</para>
/// <para>Threading: not thread-safe.</para>
/// </remarks>
internal sealed class ValidatedEditOperation
{
    /// <summary>Position of the edit in the original list, used to keep sorting stable.</summary>
    public int SortIndex { get; set; }

    /// <summary>An identifier associated with this edit, or <c>null</c>.</summary>
    public EditOperationIdentifier? Identifier { get; init; }

    /// <summary>The range to replace.</summary>
    public Range Range { get; init; }

    /// <summary>Offset of the start of <see cref="Range"/> in the text.</summary>
    public int RangeOffset { get; init; }

    /// <summary>Length of <see cref="Range"/> in the text.</summary>
    public int RangeLength { get; init; }

    /// <summary>The replacement text; empty to delete.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Number of line breaks in <see cref="Text"/>.</summary>
    public int EolCount { get; init; }

    /// <summary>Length of the first line of <see cref="Text"/>.</summary>
    public int FirstLineLength { get; init; }

    /// <summary>Length of the last line of <see cref="Text"/>.</summary>
    public int LastLineLength { get; init; }

    /// <summary>True when markers at the edit position must move.</summary>
    public bool ForceMoveMarkers { get; init; }

    /// <summary>True when the edit was produced by auto-whitespace.</summary>
    public bool IsAutoWhitespaceEdit { get; init; }
}

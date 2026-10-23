// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/hoverTypes.ts (IHoverPart)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Hover;

/// <summary>One piece of a hover: a marker, a provider's Markdown, or the loading message.</summary>
/// <remarks>Threading: implementations are immutable.</remarks>
internal interface IHoverPart
{
    /// <summary>The range the part applies to, on one line.</summary>
    Range Range { get; }

    /// <summary>The sort key of the part (Monaco <c>hoverOrdinal</c>; smaller first).</summary>
    int Order { get; }

    /// <summary>True when the part still fits the anchor (same line is guaranteed by the caller).</summary>
    bool IsValidForHoverAnchor(HoverAnchor anchor);
}

/// <summary>Shared by the three part kinds.</summary>
internal static class HoverPartRules
{
    /// <summary>Monaco <c>isValidForHoverAnchor</c> of the participants.</summary>
    public static bool Covers(Range range, HoverAnchor anchor) =>
        range.StartColumn <= anchor.Range.StartColumn && range.EndColumn >= anchor.Range.EndColumn;
}

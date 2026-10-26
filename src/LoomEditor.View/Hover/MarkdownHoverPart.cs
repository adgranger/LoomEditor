// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/markdownHoverParticipant.ts (MarkdownHover)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;

namespace LoomEditor.View.Hover;

/// <summary>The Markdown a hover provider returned.</summary>
/// <remarks>Threading: immutable.</remarks>
/// <param name="Range">The range, on the anchor line.</param>
/// <param name="Contents">The Markdown blocks.</param>
/// <param name="Ordinal">The index of the provider in the registry order.</param>
internal sealed record MarkdownHoverPart(Range Range, IReadOnlyList<MarkdownString> Contents, int Ordinal) : IHoverPart
{
    /// <inheritdoc />
    public int Order => 3;

    /// <inheritdoc />
    public bool IsValidForHoverAnchor(HoverAnchor anchor) => HoverPartRules.Covers(Range, anchor);
}

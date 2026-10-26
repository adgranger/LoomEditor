// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/markerHoverParticipant.ts (MarkerHover)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Markers;

namespace LoomEditor.View.Hover;

/// <summary>A marker shown in a hover.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record MarkerHoverPart(Range Range, Marker Marker) : IHoverPart
{
    /// <inheritdoc />
    public int Order => 1;

    /// <inheritdoc />
    public bool IsValidForHoverAnchor(HoverAnchor anchor) => HoverPartRules.Covers(Range, anchor);
}

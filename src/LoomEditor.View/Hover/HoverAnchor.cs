// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/hoverTypes.ts (HoverRangeAnchor)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Hover;

/// <summary>
/// The range under the mouse (or the cursor) that a hover is about. Equality is the range only (Monaco <c>HoverRangeAnchor.equals</c>).
/// Not ported: the foreign-element anchor and <c>priority</c>.
/// </summary>
/// <remarks>Threading: immutable.</remarks>
/// <param name="Range">The anchor range.</param>
/// <param name="InitialMouseX">The mouse x when the hover started, if known.</param>
/// <param name="InitialMouseY">The mouse y when the hover started, if known.</param>
internal sealed record HoverAnchor(Range Range, double? InitialMouseX = null, double? InitialMouseY = null)
{
    /// <summary>True when a visible hover shown at <paramref name="showAtPosition"/> can stay for this anchor.</summary>
    public bool CanAdoptVisibleHover(HoverAnchor last, Position showAtPosition) => showAtPosition.LineNumber == Range.StartLineNumber;

    /// <inheritdoc />
    public bool Equals(HoverAnchor? other) => other is not null && Range == other.Range;

    /// <inheritdoc />
    public override int GetHashCode() => Range.GetHashCode();
}

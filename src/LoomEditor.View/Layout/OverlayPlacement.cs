// Inspired by microsoft/vscode: src/vs/editor/contrib/hover/browser/contentHoverWidget.ts (layout)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>A rectangle in the DIPs of the overlay container.</summary>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct OverlayRect(double X, double Y, double Width, double Height);

/// <summary>Where a popup goes relative to an anchor: below it, above it when there is no room, always inside the container.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class OverlayPlacement
{
    /// <summary>Places a popup of the given size next to the anchor line (all values in the container's DIPs).</summary>
    public static OverlayRect Place(
        double anchorX, double anchorTop, double anchorBottom, double width, double height,
        double containerWidth, double containerHeight, bool preferBelow = true)
    {
        Check(anchorX, nameof(anchorX));
        Check(anchorTop, nameof(anchorTop));
        Check(anchorBottom, nameof(anchorBottom));
        Check(width, nameof(width));
        Check(height, nameof(height));
        Check(containerWidth, nameof(containerWidth));
        Check(containerHeight, nameof(containerHeight));

        width = Math.Min(width, containerWidth);
        double x = Math.Clamp(anchorX, 0, containerWidth - width);
        double spaceBelow = Math.Max(0, containerHeight - anchorBottom);
        double spaceAbove = anchorTop;

        bool below;
        if (preferBelow)
        {
            below = height <= spaceBelow || (height > spaceAbove && spaceBelow >= spaceAbove);
        }
        else
        {
            below = !(height <= spaceAbove) && (height <= spaceBelow || spaceBelow > spaceAbove);
        }

        height = Math.Min(height, below ? spaceBelow : spaceAbove);
        return new OverlayRect(x, below ? anchorBottom : anchorTop - height, width, height);
    }

    private static void Check(double value, string name)
    {
        if (double.IsNaN(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Must be a non-negative number.");
        }
    }
}

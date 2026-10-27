// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/diffEditorSash.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>
/// Where the sash between the two sides of the diff editor is (Monaco: <c>SashLayout</c>). It uses no WinUI type so that the
/// unit tests can link this file.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class SashLayout(double width, double splitViewDefaultRatio, bool enableSplitViewResizing)
{
    private const double MinimumEditorWidth = 100;

    /// <summary>The width available to the two sides, in DIPs.</summary>
    public double Width { get; set; } = width;

    /// <summary>The share of the width of the original side when the sash was not dragged.</summary>
    public double SplitViewDefaultRatio { get; set; } = splitViewDefaultRatio;

    /// <summary>Whether the sash can be dragged; when false the sash stays at the midpoint.</summary>
    public bool EnableSplitViewResizing { get; set; } = enableSplitViewResizing;

    /// <summary>The ratio set by dragging, or <c>null</c> to use <see cref="SplitViewDefaultRatio"/>.</summary>
    public double? Ratio { get; private set; }

    /// <summary>The x of the sash, i.e. the width of the original side.</summary>
    public double SashLeft => ComputeSashLeft(Ratio ?? SplitViewDefaultRatio);

    /// <summary>Moves the sash (Monaco: the <c>sashLeft</c> setter).</summary>
    public void SetSashLeft(double value) => Ratio = Width > 0 ? value / Width : null;

    /// <summary>Goes back to the default ratio (Monaco: <c>resetSash</c>).</summary>
    public void ResetSash() => Ratio = null;

    /// <summary>Monaco: <c>_computeSashLeft</c>.</summary>
    public double ComputeSashLeft(double desiredRatio)
    {
        double midPoint = Math.Floor(SplitViewDefaultRatio * Width);
        double sashLeft = EnableSplitViewResizing ? Math.Floor(desiredRatio * Width) : midPoint;

        if (Width <= MinimumEditorWidth * 2)
        {
            return midPoint;
        }

        if (sashLeft < MinimumEditorWidth)
        {
            return MinimumEditorWidth;
        }

        if (sashLeft > Width - MinimumEditorWidth)
        {
            return Width - MinimumEditorWidth;
        }

        return sashLeft;
    }
}

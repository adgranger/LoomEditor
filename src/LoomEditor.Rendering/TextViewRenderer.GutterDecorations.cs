// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/glyphMargin/glyphMargin.ts (DedupOverlay)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Also ported from linesDecorations.ts and marginDecorations.ts (same commit): the decorations of the gutter.
// Tooltips are not ported. Phase 6 cut point: glyph margin decorations (`GlyphMarginClassName` is not drawn).
internal sealed partial class TextViewRenderer
{
    private const float ChevronStrokeWidth = 1.5f;

    /// <summary>
    /// Whether the mouse is over the gutter: the expanded folding controls are drawn only then (folding.css: the
    /// <c>:hover</c> of the margin). The 0.5 s opacity transition is not ported.
    /// </summary>
    public bool IsMarginHovered { get; set; }

    // Monaco: `DecorationToRender`, without the tooltip.
    private sealed record DecorationToRender(int StartLineNumber, int EndLineNumber, string ClassName, int ZIndex);

    // Monaco: `MarginViewLineDecorationsOverlay._getDecorations`.
    private static List<DecorationToRender> GetMarginDecorations(ViewportData data)
    {
        var r = new List<DecorationToRender>();
        foreach (ViewModelDecoration d in data.GetDecorationsInViewport())
        {
            if (d.Options.MarginClassName is { Length: > 0 } marginClassName)
            {
                r.Add(new DecorationToRender(d.Range.StartLineNumber, d.Range.EndLineNumber, marginClassName, d.Options.ZIndex));
            }
        }

        return r;
    }

    // Monaco: `LinesDecorationsOverlay._getDecorations`.
    private static List<DecorationToRender> GetLinesDecorations(ViewportData data)
    {
        var r = new List<DecorationToRender>();
        foreach (ViewModelDecoration d in data.GetDecorationsInViewport())
        {
            if (d.Options.LinesDecorationsClassName is { Length: > 0 } linesDecorationsClassName)
            {
                r.Add(new DecorationToRender(d.Range.StartLineNumber, d.Range.EndLineNumber, linesDecorationsClassName, d.Options.ZIndex));
            }

            if (d.Options.FirstLineDecorationClassName is { Length: > 0 } firstLineDecorationClassName)
            {
                r.Add(new DecorationToRender(d.Range.StartLineNumber, d.Range.StartLineNumber, firstLineDecorationClassName, d.Options.ZIndex));
            }
        }

        return r;
    }

    // folding.css 5–30: the codicon is centered in the line-decorations box (`margin-left: 2px`); here two strokes of
    // an 8-DIP chevron replace the glyph. Hidden unless the margin is hovered, the region is collapsed or `alwaysShowFoldIcons`.
    private void DrawGutterIcon(IRenderSurface surface, string className, EditorLayoutInfo layoutInfo, float top, int lineHeight, bool hovered)
    {
        if (DecorationStyles.Get(className) is not { GutterIcon: not GutterIcon.None } style
            || !(style.AlwaysVisible || hovered)
            || Theme.FoldingControlForeground is not { } color)
        {
            return;
        }

        float left = layoutInfo.DecorationsLeft + 2;
        float cx = (left + layoutInfo.DecorationsLeft + layoutInfo.DecorationsWidth) / 2;
        float cy = top + lineHeight / 2f;
        if (style.GutterIcon == GutterIcon.ChevronDown)
        {
            surface.DrawLine(cx - 4, cy - 2, cx, cy + 2, color, ChevronStrokeWidth);
            surface.DrawLine(cx, cy + 2, cx + 4, cy - 2, color, ChevronStrokeWidth);
        }
        else
        {
            surface.DrawLine(cx - 2, cy - 4, cx + 2, cy, color, ChevronStrokeWidth);
            surface.DrawLine(cx + 2, cy, cx - 2, cy + 4, color, ChevronStrokeWidth);
        }
    }

    // Monaco: `DedupOverlay._render`: the class names to draw on each visible line. `zIndex` is carried but, as in
    // Monaco, does not reorder.
    private static List<string>[] RenderDedup(int visibleStartLineNumber, int visibleEndLineNumber, List<DecorationToRender> decorations)
    {
        var output = new List<string>[visibleEndLineNumber - visibleStartLineNumber + 1];
        for (int lineIndex = 0; lineIndex < output.Length; lineIndex++)
        {
            output[lineIndex] = [];
        }

        if (decorations.Count == 0)
        {
            return output;
        }

        // Sort decorations by className, then by startLineNumber and then by endLineNumber (stable sort).
        IEnumerable<DecorationToRender> sorted = decorations
            .OrderBy(d => d.ClassName, StringComparer.Ordinal)
            .ThenBy(d => d.StartLineNumber)
            .ThenBy(d => d.EndLineNumber);

        string? prevClassName = null;
        int prevEndLineIndex = 0;
        foreach (DecorationToRender d in sorted)
        {
            string className = d.ClassName;
            int startLineIndex = Math.Max(d.StartLineNumber, visibleStartLineNumber) - visibleStartLineNumber;
            int endLineIndex = Math.Min(d.EndLineNumber, visibleEndLineNumber) - visibleStartLineNumber;

            if (prevClassName == className)
            {
                // Here we avoid rendering the same className multiple times on the same line
                startLineIndex = Math.Max(prevEndLineIndex + 1, startLineIndex);
                prevEndLineIndex = Math.Max(prevEndLineIndex, endLineIndex);
            }
            else
            {
                prevClassName = className;
                prevEndLineIndex = endLineIndex;
            }

            for (int lineIndex = startLineIndex; lineIndex <= prevEndLineIndex; lineIndex++)
            {
                output[lineIndex].Add(className);
            }
        }

        return output;
    }
}

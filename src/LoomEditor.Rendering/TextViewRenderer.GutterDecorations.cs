// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/glyphMargin/glyphMargin.ts (DedupOverlay)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Also ported from linesDecorations.ts and marginDecorations.ts (same commit): the decorations of the gutter.
// Tooltips are not ported. Phase 6 cut point: glyph margin decorations (`GlyphMarginClassName` is not drawn).
internal sealed partial class TextViewRenderer
{
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

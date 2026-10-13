using System.Globalization;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Features;

namespace LoomEditor.Rendering;

/// <summary>
/// Decides what to draw for the text area and the gutter (line numbers), and draws it on an <see cref="IRenderSurface"/>.
/// </summary>
/// <remarks>
/// <para>
/// Phase 0: plain text, line numbers and the selection, monospaced font, only the visible lines are drawn.
/// Phase 2 brings scrolling, real text layout and caching.
/// </para>
/// <para>Threading: UI thread only (it reads the editor context).</para>
/// </remarks>
public sealed class TextViewRenderer
{
    /// <summary>Gutter padding, in characters: 1 before the line numbers, 2 after them.</summary>
    private const int GutterPaddingInCharacters = 3;

    /// <summary>The colors to use.</summary>
    public EditorTheme Theme { get; set; } = EditorTheme.Dark;

    /// <summary>Draws the visible part of the document.</summary>
    /// <param name="surface">Where to draw.</param>
    /// <param name="context">The editor to draw.</param>
    /// <param name="viewportHeight">Height of the visible area, in DIPs. Lines below it are skipped.</param>
    public void Render(IRenderSurface surface, IEditorContext context, float viewportHeight)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(context);

        FontMetrics metrics = surface.FontMetrics;
        TextModel model = context.Model;
        int lineCount = model.LineCount;
        int visibleLineCount = Math.Min(lineCount, (int)Math.Ceiling(viewportHeight / metrics.LineHeight));
        float textLeft = GetGutterWidth(lineCount, metrics);

        surface.Clear(Theme.Background);
        DrawSelection(surface, context.Selection, model, visibleLineCount, textLeft, metrics);

        for (int index = 0; index < visibleLineCount; index++)
        {
            float y = index * metrics.LineHeight;
            DrawLineNumber(surface, index + 1, lineCount, y, metrics);
            surface.DrawText(model.GetLineContent(index + 1), textLeft, y, Theme.Foreground);
        }
    }

    /// <summary>Width of the gutter: enough for the largest line number, plus padding.</summary>
    public static float GetGutterWidth(int lineCount, FontMetrics metrics) =>
        (CountDigits(lineCount) + GutterPaddingInCharacters) * metrics.CharacterWidth;

    private void DrawLineNumber(IRenderSurface surface, int lineNumber, int lineCount, float y, FontMetrics metrics)
    {
        // Right-align line numbers: monospaced digits, so pad on the left.
        int padding = CountDigits(lineCount) - CountDigits(lineNumber);
        float x = (padding + 1) * metrics.CharacterWidth;
        surface.DrawText(lineNumber.ToString(CultureInfo.InvariantCulture), x, y, Theme.LineNumberForeground);
    }

    private void DrawSelection(
        IRenderSurface surface, Selection selection, TextModel model, int visibleLineCount, float textLeft, FontMetrics metrics)
    {
        if (selection.IsEmpty)
        {
            return;
        }

        Range range = selection.Range;
        int lastVisibleLine = Math.Min(range.EndLineNumber, visibleLineCount);
        for (int lineNumber = range.StartLineNumber; lineNumber <= lastVisibleLine; lineNumber++)
        {
            int startColumn = lineNumber == range.StartLineNumber ? range.StartColumn : 1;
            // Selected line breaks are shown as one extra character, like in VS Code.
            int endColumn = lineNumber == range.EndLineNumber ? range.EndColumn : model.GetLineLength(lineNumber) + 2;

            float x = textLeft + ((startColumn - 1) * metrics.CharacterWidth);
            float width = (endColumn - startColumn) * metrics.CharacterWidth;
            surface.FillRectangle(x, (lineNumber - 1) * metrics.LineHeight, width, metrics.LineHeight, Theme.SelectionBackground);
        }
    }

    private static int CountDigits(int value) => value.ToString(CultureInfo.InvariantCulture).Length;
}

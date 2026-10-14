using System.Globalization;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Features;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

/// <summary>
/// Decides what to draw for the text area and the gutter (line numbers), and draws it on an <see cref="IRenderSurface"/>.
/// </summary>
/// <remarks>
/// <para>
/// Plain text, line numbers and the selection; every line is laid out by an <see cref="ITextMeasurer"/> and only the
/// visible lines are drawn. Scrolling and layout caching come in the next cards.
/// </para>
/// <para>Threading: UI thread only (it reads the editor context).</para>
/// </remarks>
internal sealed class TextViewRenderer
{
    /// <summary>Gutter padding, in digit widths: 1 before the line numbers, 2 after them.</summary>
    private const int GutterPaddingInDigits = 3;

    /// <summary>The colors to use.</summary>
    public EditorTheme Theme { get; set; } = EditorTheme.Dark;

    /// <summary>Draws the visible part of the document.</summary>
    /// <param name="surface">Where to draw.</param>
    /// <param name="measurer">Lays out the lines.</param>
    /// <param name="context">The editor to draw.</param>
    /// <param name="viewportHeight">Height of the visible area, in DIPs. Lines below it are skipped.</param>
    public void Render(IRenderSurface surface, ITextMeasurer measurer, IEditorContext context, float viewportHeight)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(measurer);
        ArgumentNullException.ThrowIfNull(context);

        FontInfo font = measurer.FontInfo;
        TextModel model = context.Model;
        int lineCount = model.LineCount;
        int visibleLineCount = Math.Min(lineCount, (int)Math.Ceiling(viewportHeight / font.LineHeight));
        float textLeft = GetGutterWidth(lineCount, font);
        float numbersRight = textLeft - (2 * (float)font.MaxDigitWidth);

        surface.Clear(Theme.Background);

        for (int lineNumber = 1; lineNumber <= visibleLineCount; lineNumber++)
        {
            float lineTop = (lineNumber - 1) * font.LineHeight;
            using ILineLayout layout = measurer.CreateLineLayout(model.GetLineContent(lineNumber));
            DrawSelection(surface, context.Selection, lineNumber, layout, textLeft, lineTop, font);

            using ILineLayout number = measurer.CreateLineLayout(lineNumber.ToString(CultureInfo.InvariantCulture));
            surface.DrawLineLayout(number, numbersRight - (float)number.Width, lineTop, Theme.LineNumberForeground);
            surface.DrawLineLayout(layout, textLeft, lineTop, Theme.Foreground);
        }
    }

    /// <summary>Width of the gutter: enough for the largest line number, plus padding.</summary>
    public static float GetGutterWidth(int lineCount, FontInfo font)
    {
        ArgumentNullException.ThrowIfNull(font);

        return (CountDigits(lineCount) + GutterPaddingInDigits) * (float)font.MaxDigitWidth;
    }

    private void DrawSelection(
        IRenderSurface surface, Selection selection, int lineNumber, ILineLayout layout,
        float textLeft, float lineTop, FontInfo font)
    {
        Range range = selection.Range;
        if (selection.IsEmpty || lineNumber < range.StartLineNumber || lineNumber > range.EndLineNumber)
        {
            return;
        }

        int startColumn = lineNumber == range.StartLineNumber ? range.StartColumn : 1;
        double startX = layout.GetCharacterX(startColumn - 1);
        // Selected line breaks are shown as one extra character, like in VS Code.
        double endX = lineNumber == range.EndLineNumber
            ? layout.GetCharacterX(range.EndColumn - 1)
            : layout.Width + font.TypicalHalfwidthCharacterWidth;

        surface.FillRectangle(
            textLeft + (float)startX, lineTop, (float)(endX - startX), font.LineHeight, Theme.SelectionBackground);
    }

    private static int CountDigits(int value) => value.ToString(CultureInfo.InvariantCulture).Length;
}

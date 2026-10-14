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
/// <para>
/// Line layouts are kept in a <see cref="LineLayoutCache"/>. <see cref="Dispose"/> releases it; the renderer can still
/// be used afterwards (the cache is created again), e.g. when the control is unloaded and loaded again.
/// </para>
/// <para>Threading: UI thread only (it reads the editor context).</para>
/// </remarks>
internal sealed class TextViewRenderer : IDisposable
{
    /// <summary>Gutter padding, in digit widths: 1 before the line numbers, 2 after them.</summary>
    private const int GutterPaddingInDigits = 3;

    /// <summary>
    /// Lines longer than this are cut, and a "Show more" label is drawn after them.
    /// Monaco: the <c>stopRenderingLineAfter</c> editor option (default 10000).
    /// </summary>
    private const int StopRenderingLineAfter = 10_000;

    private const int MinimumCacheCapacity = 256;

    private LineLayoutCache? _cache;
    private ITextMeasurer? _cacheMeasurer;

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

        LineLayoutCache cache = GetCache(measurer, visibleLineCount);

        surface.Clear(Theme.Background);

        for (int lineNumber = 1; lineNumber <= visibleLineCount; lineNumber++)
        {
            float lineTop = (lineNumber - 1) * font.LineHeight;
            int lineLength = model.GetLineLength(lineNumber);
            bool isOverflowing = lineLength > StopRenderingLineAfter;
            // Never read a whole very long line during a frame: only the part that is drawn.
            string text = isOverflowing
                ? model.GetValueInRange(new Range(lineNumber, 1, lineNumber, StopRenderingLineAfter + 1))
                : model.GetLineContent(lineNumber);

            ILineLayout layout = cache.Get(text);
            DrawSelection(surface, context.Selection, lineNumber, layout, textLeft, lineTop, font);

            ILineLayout number = cache.Get(lineNumber.ToString(CultureInfo.InvariantCulture));
            surface.DrawLineLayout(number, numbersRight - (float)number.Width, lineTop, Theme.LineNumberForeground);
            surface.DrawLineLayout(layout, textLeft, lineTop, Theme.Foreground);

            if (isOverflowing)
            {
                ILineLayout label = cache.Get(OverflowLabel.Format(lineLength - StopRenderingLineAfter));
                surface.DrawLineLayout(label, textLeft + (float)layout.Width, lineTop, Theme.LineNumberForeground);
            }
        }
    }

    /// <summary>Releases the layout cache.</summary>
    public void Dispose()
    {
        _cache?.Dispose();
        _cache = null;
        _cacheMeasurer = null;
    }

    /// <summary>Width of the gutter: enough for the largest line number, plus padding.</summary>
    public static float GetGutterWidth(int lineCount, FontInfo font)
    {
        ArgumentNullException.ThrowIfNull(font);

        return (CountDigits(lineCount) + GutterPaddingInDigits) * (float)font.MaxDigitWidth;
    }

    // Two layouts per visible line (number and text), plus the labels of very long lines.
    private LineLayoutCache GetCache(ITextMeasurer measurer, int visibleLineCount)
    {
        int capacity = Math.Max(MinimumCacheCapacity, 4 * visibleLineCount);
        if (_cache is null || !ReferenceEquals(_cacheMeasurer, measurer))
        {
            // A new measurer means a new font (or graphics device): the old layouts are useless.
            _cache?.Dispose();
            _cache = new LineLayoutCache(measurer, capacity);
            _cacheMeasurer = measurer;
        }

        _cache.EnsureCapacity(capacity);
        return _cache;
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
        // The layout of a very long line is cut: clamp to its end.
        double startX = layout.GetCharacterX(Math.Min(startColumn - 1, layout.Text.Length));
        // Selected line breaks are shown as one extra character, like in VS Code.
        double endX = lineNumber == range.EndLineNumber
            ? layout.GetCharacterX(Math.Min(range.EndColumn - 1, layout.Text.Length))
            : layout.Width + font.TypicalHalfwidthCharacterWidth;

        surface.FillRectangle(
            textLeft + (float)startX, lineTop, (float)(endX - startX), font.LineHeight, Theme.SelectionBackground);
    }

    private static int CountDigits(int value) => value.ToString(CultureInfo.InvariantCulture).Length;
}

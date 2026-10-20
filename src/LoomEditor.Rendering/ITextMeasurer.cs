using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

/// <summary>
/// Measures the editor font and lays out lines of text with it.
/// </summary>
/// <remarks>
/// Create one per control and reuse it: measuring the font is not free.
/// Threading: UI thread only.
/// </remarks>
internal interface ITextMeasurer : IDisposable
{
    /// <summary>The font, with its measured character widths (Monaco's <c>FontInfo</c>).</summary>
    FontInfo FontInfo { get; }

    /// <summary>Width of a tab stop, in characters (of a space).</summary>
    int TabSize { get; }

    /// <summary>Lays out one line of text with the colors and font styles of <paramref name="style"/>. The caller owns the result and must dispose it.</summary>
    ILineLayout CreateLineLayout(string text, LineStyle style);
}

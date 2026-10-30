namespace LoomEditor.WinUI.Controls;

/// <summary>The part of the gutter that was clicked.</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
public enum GutterArea
{
    /// <summary>The glyph margin, left of the line numbers.</summary>
    GlyphMargin,

    /// <summary>The line numbers.</summary>
    LineNumbers,

    /// <summary>The line decorations, between the line numbers and the text (the folding controls included).</summary>
    LineDecorations,
}

/// <summary>The data of <see cref="LoomEditor.GutterClicked"/>.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class GutterClickedEventArgs : EventArgs
{
    internal GutterClickedEventArgs(int lineNumber, GutterArea area)
    {
        LineNumber = lineNumber;
        Area = area;
    }

    /// <summary>The 1-based model line of the click.</summary>
    public int LineNumber { get; }

    /// <summary>The part of the gutter that was clicked.</summary>
    public GutterArea Area { get; }

    /// <summary>Set to <see langword="true"/> so the editor does nothing else with the press: no line selection, no fold toggle.</summary>
    public bool Handled { get; set; }
}

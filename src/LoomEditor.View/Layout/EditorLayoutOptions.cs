namespace LoomEditor.View.Layout;

/// <summary>
/// The options read by <see cref="EditorLayoutInfoComputer.ComputeLayout"/>. Monaco reads them from
/// <c>IComputedEditorOptions</c>; there is no options system yet.
/// </summary>
/// <remarks>
/// Threading: immutable, safe from any thread. <see cref="GlyphMargin"/> defaults to <c>false</c>
/// (Monaco: <c>true</c>) because no feature draws in it yet.
/// </remarks>
internal sealed record EditorLayoutOptions
{
    /// <summary>Monaco: <c>glyphMargin</c>.</summary>
    public bool GlyphMargin { get; init; }

    /// <summary>Line numbers on or off (Monaco: <c>lineNumbers.renderType !== Off</c>).</summary>
    public bool LineNumbers { get; init; } = true;

    /// <summary>Monaco: <c>lineNumbersMinChars</c>.</summary>
    public int LineNumbersMinChars { get; init; } = 5;

    /// <summary>Monaco: <c>lineDecorationsWidth</c>.</summary>
    public int LineDecorationsWidth { get; init; } = 10;

    /// <summary>Monaco: <c>folding</c>.</summary>
    public bool Folding { get; init; } = true;

    /// <summary>Monaco: <c>showFoldingControls !== 'never'</c>.</summary>
    public bool ShowFoldingDecoration { get; init; } = true;

    /// <summary>Monaco: <c>scrollbar.verticalScrollbarSize</c>.</summary>
    public int VerticalScrollbarSize { get; init; } = 14;

    /// <summary>Monaco: <c>scrollbar.horizontalScrollbarSize</c>.</summary>
    public int HorizontalScrollbarSize { get; init; } = 12;

    /// <summary>Monaco: <c>scrollbar.verticalHasArrows</c>.</summary>
    public bool VerticalHasArrows { get; init; }

    /// <summary>Monaco: <c>scrollbar.arrowSize</c>.</summary>
    public int ArrowSize { get; init; } = 11;

    /// <summary>The word-wrap settings (Monaco: <c>wordWrap</c>, <c>wordWrapColumn</c>, <c>wordWrapIndicator</c>).</summary>
    public EditorWrappingOptions Wrapping { get; init; } = new();
}

namespace LoomEditor.Rendering;

/// <summary>
/// One line of text, laid out: tabs, wide characters and proportional fonts have their real width.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface ILineLayout : IDisposable
{
    /// <summary>The text that was laid out.</summary>
    string Text { get; }

    /// <summary>Advance width of the whole text, in DIPs.</summary>
    double Width { get; }

    /// <summary>
    /// X of the leading edge of the UTF-16 unit at <paramref name="index"/>, in DIPs.
    /// <c>index == Text.Length</c> gives <see cref="Width"/>.
    /// </summary>
    double GetCharacterX(int index);

    /// <summary>
    /// The offset of the caret position nearest to <paramref name="x"/> (DIPs from the start of the line): <c>0 … Text.Length</c>.
    /// A hit on the trailing half of a character gives the offset after it; it is never inside a surrogate pair.
    /// </summary>
    int HitTest(double x);
}

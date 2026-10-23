namespace LoomEditor.View.Layout;

/// <summary>When lines wrap (Monaco: <c>wordWrap</c>).</summary>
internal enum WordWrap
{
    /// <summary>Lines never wrap.</summary>
    Off,

    /// <summary>Lines wrap at the viewport width.</summary>
    On,

    /// <summary>Lines wrap at <see cref="EditorWrappingOptions.WordWrapColumn"/>.</summary>
    WordWrapColumn,

    /// <summary>Lines wrap at the minimum of the viewport width and <see cref="EditorWrappingOptions.WordWrapColumn"/>.</summary>
    Bounded,
}

/// <summary>Parsing of <see cref="WordWrap"/>.</summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal static class WordWraps
{
    /// <summary>Case-sensitive; anything unknown (or <c>null</c>) is <see cref="WordWrap.Off"/>.</summary>
    /// <param name="value">One of <c>off</c>, <c>on</c>, <c>wordWrapColumn</c>, <c>bounded</c>.</param>
    /// <returns>The parsed value.</returns>
    public static WordWrap FromString(string? value) => value switch
    {
        "on" => WordWrap.On,
        "wordWrapColumn" => WordWrap.WordWrapColumn,
        "bounded" => WordWrap.Bounded,
        _ => WordWrap.Off,
    };
}

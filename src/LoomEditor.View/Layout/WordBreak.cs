namespace LoomEditor.View.Layout;

/// <summary>Word-break rules for CJK text (Monaco: <c>wordBreak</c>).</summary>
internal enum WordBreak
{
    /// <summary>Default line-break rules.</summary>
    Normal,

    /// <summary>No word breaks in Chinese/Japanese/Korean text.</summary>
    KeepAll,
}

/// <summary>Parsing of <see cref="WordBreak"/>.</summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal static class WordBreaks
{
    /// <summary>Case-sensitive; anything unknown (or <c>null</c>) is <see cref="WordBreak.Normal"/>.</summary>
    /// <param name="value">One of <c>normal</c>, <c>keepAll</c>.</param>
    /// <returns>The parsed value.</returns>
    public static WordBreak FromString(string? value) => value == "keepAll" ? WordBreak.KeepAll : WordBreak.Normal;
}

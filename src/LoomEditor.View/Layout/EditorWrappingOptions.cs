namespace LoomEditor.View.Layout;

/// <summary>The word-wrap settings (Monaco: the <c>wordWrap*</c>, <c>wrappingIndent</c>, <c>wordBreak</c> options).</summary>
/// <remarks>
/// Threading: immutable, safe from any thread. Not ported: <c>wordWrapOverride1/2</c> (workbench override hooks),
/// <c>wrappingStrategy</c> (only Monaco's <c>simple</c> strategy exists, the editor uses a monospace font).
/// The screen reader override of the indent is applied by <see cref="EditorLayoutOptions.GetEffectiveWrapping"/>.
/// </remarks>
internal sealed record EditorWrappingOptions
{
    /// <summary>Monaco's default for <see cref="WordWrapBreakBeforeCharacters"/>.</summary>
    public const string DefaultBreakBeforeCharacters = "([{‘“〈《「『【〔（［｛｢£¥＄￡￥+＋";

    /// <summary>Monaco's default for <see cref="WordWrapBreakAfterCharacters"/>.</summary>
    public const string DefaultBreakAfterCharacters = " \t})]?|/&.,;¢°′″‰℃、。｡､￠，．：；？！％・･ゝゞヽヾーァィゥェォッャュョヮヵヶぁぃぅぇぉっゃゅょゎゕゖㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇺㇻㇼㇽㇾㇿ々〻ｧｨｩｪｫｬｭｮｯｰ”〉》」』】〕）］｝｣";

    /// <summary>Monaco: <c>wordWrap</c>.</summary>
    public WordWrap WordWrap { get; init; } = WordWrap.Off;

    /// <summary>Monaco: <c>wordWrapColumn</c> (minimum 1, see <see cref="Validated"/>).</summary>
    public int WordWrapColumn { get; init; } = 80;

    /// <summary>Monaco: <c>wordWrapIndicator</c>. Only reserved in the layout; drawing the indicator is not ported.</summary>
    public bool WordWrapIndicator { get; init; }

    /// <summary>Monaco: <c>wrappingIndent</c>.</summary>
    public WrappingIndent WrappingIndent { get; init; } = WrappingIndent.Same;

    /// <summary>Monaco: <c>wordBreak</c>.</summary>
    public WordBreak WordBreak { get; init; } = WordBreak.Normal;

    /// <summary>Monaco: <c>wordWrapBreakBeforeCharacters</c>.</summary>
    public string WordWrapBreakBeforeCharacters { get; init; } = DefaultBreakBeforeCharacters;

    /// <summary>Monaco: <c>wordWrapBreakAfterCharacters</c>.</summary>
    public string WordWrapBreakAfterCharacters { get; init; } = DefaultBreakAfterCharacters;

    /// <summary>Monaco: <c>wrapOnEscapedLineFeeds</c>. Read only when the view model is created (identical to Monaco): changing it later has no effect until the model is flushed.</summary>
    public bool WrapOnEscapedLineFeeds { get; init; }

    /// <summary>
    /// Monaco's <c>effectiveFullwidthCharacterWidth === 'twoCells'</c>: true forces full-width characters into two
    /// monospace cells; only meaningful for a monospace font.
    /// </summary>
    public bool UseTwoCellFullwidthCharacters { get; init; }

    /// <summary>Returns a copy with <see cref="WordWrapColumn"/> clamped to at least 1.</summary>
    /// <returns>The validated options.</returns>
    public EditorWrappingOptions Validated() => WordWrapColumn < 1 ? this with { WordWrapColumn = 1 } : this;
}

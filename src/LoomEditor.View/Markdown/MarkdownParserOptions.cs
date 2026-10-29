namespace LoomEditor.View.Markdown;

/// <summary>Options of <see cref="MarkdownParser"/>.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="IsExtended">Also understand <c>&gt; quote</c>, <c>~~strike~~</c> and <c>&lt;u&gt;</c>/<c>&lt;ins&gt;</c> (the comment toolbar's output). Off for hovers.</param>
internal sealed record MarkdownParserOptions(bool IsExtended = false)
{
    /// <summary>The subset the hover and completion documentation use.</summary>
    public static readonly MarkdownParserOptions Default = new();

    /// <summary>The subset plus quote, strikethrough and underline.</summary>
    public static readonly MarkdownParserOptions Extended = new(true);
}

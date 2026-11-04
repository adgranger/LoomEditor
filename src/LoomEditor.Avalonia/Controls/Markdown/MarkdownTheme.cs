using Avalonia.Media;

namespace LoomEditor.Avalonia.Controls.Markdown;

/// <summary>The brushes a <see cref="MarkdownElementBuilder"/> draws with; the owner keeps them up to date with its theme.</summary>
/// <remarks>Threading: UI thread only (Avalonia brushes).</remarks>
internal sealed class MarkdownTheme
{
    /// <summary>The text color.</summary>
    public required IBrush Foreground { get; init; }

    /// <summary>The color of link runs.</summary>
    public required IBrush LinkBrush { get; init; }

    /// <summary>The background of a code block; none when <see langword="null"/>.</summary>
    public IBrush? CodeBackground { get; init; }

    /// <summary>The left bar of a blockquote.</summary>
    public required IBrush QuoteBar { get; init; }

    /// <summary>The line drawn for a horizontal rule.</summary>
    public required IBrush Separator { get; init; }
}

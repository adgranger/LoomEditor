using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Markdown;

/// <summary>The brushes a <see cref="MarkdownElementBuilder"/> draws with; the owner keeps them up to date with its theme.</summary>
/// <remarks>Threading: UI thread only (XAML brushes).</remarks>
internal sealed class MarkdownTheme
{
    /// <summary>The text color.</summary>
    public required Brush Foreground { get; init; }

    /// <summary>The color of link runs.</summary>
    public required Brush LinkBrush { get; init; }

    /// <summary>A background for code that a caller draws itself (<see cref="MarkdownBuildOptions.CustomCodeBlock"/>); the default code paragraph has none.</summary>
    public Brush? CodeBackground { get; init; }

    /// <summary>The left bar of a blockquote.</summary>
    public required Brush QuoteBar { get; init; }

    /// <summary>The line drawn for a horizontal rule.</summary>
    public required Brush Separator { get; init; }
}

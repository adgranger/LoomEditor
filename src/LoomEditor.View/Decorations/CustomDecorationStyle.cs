using LoomEditor.Themes;

namespace LoomEditor.View.Decorations;

/// <summary>What a host decoration class draws: colors for light and dark themes, and border widths.</summary>
/// <remarks>
/// The dark colors are used in the <c>Dark</c> and <c>HighContrastDark</c> themes; a <see langword="null"/> dark color falls back to the light one.
/// Threading: immutable; thread-safe.
/// </remarks>
public sealed record CustomDecorationStyle
{
    /// <summary>The background, in light themes.</summary>
    public ThemeColor? Background { get; init; }

    /// <summary>The background, in dark themes.</summary>
    public ThemeColor? DarkBackground { get; init; }

    /// <summary>The border color, in light themes; a border is drawn only when it is set.</summary>
    public ThemeColor? Border { get; init; }

    /// <summary>The border color, in dark themes.</summary>
    public ThemeColor? DarkBorder { get; init; }

    /// <summary>The color of the decorated text, in light themes (for <c>InlineClassName</c>).</summary>
    public ThemeColor? Foreground { get; init; }

    /// <summary>The color of the decorated text, in dark themes.</summary>
    public ThemeColor? DarkForeground { get; init; }

    /// <summary>The width of the box border, in DIPs; 1 by default. Used when <see cref="Border"/> is set.</summary>
    public double BorderWidth { get; init; } = 1;

    /// <summary>The width of a bar on the left edge, in DIPs, in the border color; 0 (none) by default.</summary>
    public double LeftBorderWidth { get; init; }
}

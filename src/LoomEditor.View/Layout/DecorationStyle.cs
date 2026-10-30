using LoomEditor.Themes;

namespace LoomEditor.View.Layout;

/// <summary>
/// What the CSS class of a decoration draws (ADR 0009, decision 4). Colors are VS Code color ids, resolved by the
/// renderer with the theme; an id that resolves to nothing draws nothing (CSS with an undefined variable).
/// </summary>
/// <param name="BackgroundColorId">The background of the decorated range.</param>
/// <param name="BorderColorId">The border of the decorated range, inside it (<c>box-sizing: border-box</c>).</param>
/// <param name="BorderWidth">The width of the border, in DIPs.</param>
/// <param name="ForegroundColorId">The color of the decorated text.</param>
/// <param name="ForegroundResolver">Computes the color of the decorated text from the theme; used instead of <paramref name="ForegroundColorId"/> when set.</param>
/// <param name="AfterContentText">Text drawn after the decorated range (CSS <c>::after</c> content).</param>
/// <param name="AfterContentColorId">The color of <paramref name="AfterContentText"/>.</param>
/// <param name="Important">The foreground wins over the other inline foregrounds (CSS <c>!important</c>).</param>
/// <param name="GutterIcon">The icon drawn in the line-decorations area (the codicon of the CSS class).</param>
/// <param name="Underline">The underline drawn at the bottom of the decorated range (the squiggles of markers).</param>
/// <param name="UnderlineColorId">The color of <paramref name="Underline"/>.</param>
/// <param name="LeftBorderWidth">The width of a border drawn on the left edge only, in DIPs, in <paramref name="BorderColorId"/>.</param>
/// <param name="RightBorderWidth">The width of a border drawn on the right edge only, in DIPs, in <paramref name="BorderColorId"/>.</param>
/// <param name="StrikeThrough">A line drawn through the middle of the decorated text (CSS <c>text-decoration: line-through</c>).</param>
/// <param name="BackgroundFallbackColorId">The color used when <paramref name="BackgroundColorId"/> resolves to nothing.</param>
/// <param name="BackgroundResolver">Computes the background from the theme; used instead of <paramref name="BackgroundColorId"/> when set (host styles).</param>
/// <param name="BorderResolver">Computes the border color from the theme; used instead of <paramref name="BorderColorId"/> when set (host styles).</param>
/// <param name="AlwaysVisible">The icon is drawn even when the margin is not hovered (<c>alwaysShowFoldIcons</c>, collapsed regions).</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record DecorationStyle(
    string? BackgroundColorId = null,
    string? BorderColorId = null,
    float BorderWidth = 1,
    string? ForegroundColorId = null,
    string? AfterContentText = null,
    string? AfterContentColorId = null,
    Func<EditorTheme, ThemeColor?>? ForegroundResolver = null,
    bool Important = false,
    GutterIcon GutterIcon = GutterIcon.None,
    bool AlwaysVisible = false,
    UnderlineStyle Underline = UnderlineStyle.None,
    string? UnderlineColorId = null,
    float LeftBorderWidth = 0,
    bool StrikeThrough = false,
    string? BackgroundFallbackColorId = null,
    float RightBorderWidth = 0,
    Func<EditorTheme, ThemeColor?>? BackgroundResolver = null,
    Func<EditorTheme, ThemeColor?>? BorderResolver = null);

/// <summary>The underlines of a decoration (Monaco: the <c>squiggly-*</c> CSS classes).</summary>
internal enum UnderlineStyle
{
    None,
    Squiggle,
    Dots,
}

/// <summary>The icons of the gutter that have no codicon font: drawn as strokes.</summary>
internal enum GutterIcon
{
    None,
    ChevronDown,
    ChevronRight,
    Plus,
    Minus,
}

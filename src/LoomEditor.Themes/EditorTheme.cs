namespace LoomEditor.Themes;

/// <summary>
/// The colors used to paint the editor. Property names follow VS Code theme keys
/// (e.g. <see cref="Background"/> is <c>editor.background</c>).
/// </summary>
/// <remarks>
/// <para>Phase 4 will load these values from VS Code <c>.json</c> themes; for now only built-in themes exist.</para>
/// <para>Thread safety: immutable, safe to share between threads.</para>
/// </remarks>
public sealed record EditorTheme
{
    /// <summary>A dark theme close to VS Code's "Dark Modern".</summary>
    public static EditorTheme Dark { get; } = new()
    {
        Name = "Dark",
        Background = ThemeColor.Parse("#1F1F1F"),
        Foreground = ThemeColor.Parse("#CCCCCC"),
        LineNumberForeground = ThemeColor.Parse("#6E7681"),
        LineNumberActiveForeground = ThemeColor.Parse("#CCCCCC"),
        LineHighlightBackground = null,
        LineHighlightBorder = ThemeColor.Parse("#282828"),
        CursorForeground = ThemeColor.Parse("#AEAFAD"),
        SelectionBackground = ThemeColor.Parse("#264F78"),
        CompositionBorder = ThemeColor.Parse("#FFFFFF"),
    };

    /// <summary>A light theme close to VS Code's "Light Modern".</summary>
    public static EditorTheme Light { get; } = new()
    {
        Name = "Light",
        Background = ThemeColor.Parse("#FFFFFF"),
        Foreground = ThemeColor.Parse("#3B3B3B"),
        LineNumberForeground = ThemeColor.Parse("#6E7681"),
        LineNumberActiveForeground = ThemeColor.Parse("#171184"),
        LineHighlightBackground = null,
        LineHighlightBorder = ThemeColor.Parse("#EEEEEE"),
        CursorForeground = ThemeColor.Parse("#000000"),
        SelectionBackground = ThemeColor.Parse("#ADD6FF"),
        CompositionBorder = ThemeColor.Parse("#000000"),
    };

    /// <summary>Display name of the theme.</summary>
    public required string Name { get; init; }

    /// <summary>Editor background (<c>editor.background</c>).</summary>
    public required ThemeColor Background { get; init; }

    /// <summary>Default text color (<c>editor.foreground</c>).</summary>
    public required ThemeColor Foreground { get; init; }

    /// <summary>Line numbers in the gutter (<c>editorLineNumber.foreground</c>).</summary>
    public required ThemeColor LineNumberForeground { get; init; }

    /// <summary>Line number of the cursor line (<c>editorLineNumber.activeForeground</c>).</summary>
    public required ThemeColor LineNumberActiveForeground { get; init; }

    /// <summary>
    /// Background of the cursor line (<c>editor.lineHighlightBackground</c>); when <see langword="null"/>, the
    /// <see cref="LineHighlightBorder"/> is drawn instead.
    /// </summary>
    public ThemeColor? LineHighlightBackground { get; init; }

    /// <summary>Border of the cursor line (<c>editor.lineHighlightBorder</c>), used when there is no background.</summary>
    public ThemeColor? LineHighlightBorder { get; init; }

    /// <summary>Color of the text cursor (<c>editorCursor.foreground</c>).</summary>
    public required ThemeColor CursorForeground { get; init; }

    /// <summary>Background of selected text (<c>editor.selectionBackground</c>).</summary>
    public required ThemeColor SelectionBackground { get; init; }

    /// <summary>Underline of the text being composed with an IME (<c>editor.compositionBorder</c>); defaults to white.</summary>
    public ThemeColor CompositionBorder { get; init; } = ThemeColor.Parse("#FFFFFF");
}

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
        SelectionBackground = ThemeColor.Parse("#264F78"),
    };

    /// <summary>A light theme close to VS Code's "Light Modern".</summary>
    public static EditorTheme Light { get; } = new()
    {
        Name = "Light",
        Background = ThemeColor.Parse("#FFFFFF"),
        Foreground = ThemeColor.Parse("#3B3B3B"),
        LineNumberForeground = ThemeColor.Parse("#6E7681"),
        SelectionBackground = ThemeColor.Parse("#ADD6FF"),
    };

    /// <summary>Display name of the theme.</summary>
    public required string Name { get; init; }

    /// <summary>Editor background (<c>editor.background</c>).</summary>
    public required ThemeColor Background { get; init; }

    /// <summary>Default text color (<c>editor.foreground</c>).</summary>
    public required ThemeColor Foreground { get; init; }

    /// <summary>Line numbers in the gutter (<c>editorLineNumber.foreground</c>).</summary>
    public required ThemeColor LineNumberForeground { get; init; }

    /// <summary>Background of selected text (<c>editor.selectionBackground</c>).</summary>
    public required ThemeColor SelectionBackground { get; init; }
}

namespace LoomEditor.Themes;

/// <summary>
/// The colors used to paint the editor, and the token color rules of the syntax highlighter. Property names follow
/// VS Code theme keys (e.g. <see cref="Background"/> is <c>editor.background</c>).
/// </summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
public sealed record EditorTheme
{
    private static readonly Lazy<IReadOnlyList<EditorTheme>> BuiltInThemes = new(Themes.BuiltInThemes.LoadAll);
    private static readonly Lazy<EditorTheme> DarkTheme = new(() => Themes.BuiltInThemes.Load("Dark Modern"));
    private static readonly Lazy<EditorTheme> LightTheme = new(() => Themes.BuiltInThemes.Load("Light Modern"));

    private IReadOnlyList<TokenColorRule>? _mapSource;
    private IReadOnlyList<ThemeColor>? _tokenColorMap;

    /// <summary>VS Code's "Dark Modern" theme.</summary>
    public static EditorTheme Dark => DarkTheme.Value;

    /// <summary>VS Code's "Light Modern" theme.</summary>
    public static EditorTheme Light => LightTheme.Value;

    /// <summary>The ten default themes of VS Code, in the order of its <c>package.json</c>.</summary>
    public static IReadOnlyList<EditorTheme> BuiltIn => BuiltInThemes.Value;

    /// <summary>The type of the theme (decides the defaults of the colors the theme does not set).</summary>
    public ThemeType Type { get; init; } = ThemeType.Dark;

    /// <summary>The token color rules, in VS Code's order: rule 0 is the default foreground and background.</summary>
    public IReadOnlyList<TokenColorRule> TokenColors { get; init; } = [];

    /// <summary>The colors the theme file sets, by id (empty for a theme built in code).</summary>
    internal IReadOnlyDictionary<string, ThemeColor> Colors { get; init; } = new Dictionary<string, ThemeColor>();

    /// <summary>
    /// The color of <paramref name="colorId"/>: the one the theme sets, else VS Code's default for the theme type;
    /// <see langword="null"/> when there is none.
    /// </summary>
    internal ThemeColor? GetColor(string colorId) =>
        EditorColorDefaults.Resolve(colorId, Type, id => Colors.TryGetValue(id, out ThemeColor color) ? color : null);

    /// <summary>Loads a VS Code color theme (<c>.json</c>, with comments and trailing commas).</summary>
    /// <param name="json">The content of the theme file.</param>
    /// <param name="uiTheme">The <c>uiTheme</c> of the theme: <c>vs</c>, <c>vs-dark</c>, <c>hc-black</c> or <c>hc-light</c>.</param>
    /// <param name="name">The display name of the theme.</param>
    /// <param name="readInclude">Reads a file the theme includes, by its path relative to the theme file; <see langword="null"/> when it does not exist.</param>
    /// <exception cref="FormatException">The theme or a file it includes is not a valid theme.</exception>
    public static EditorTheme Load(string json, string uiTheme, string name, Func<string, string?>? readInclude = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(uiTheme);
        ArgumentNullException.ThrowIfNull(name);
        const string Root = "theme.json";
        VsCodeTheme theme = VsCodeThemeLoader.Load(Root, uiTheme, path => path == Root ? json : readInclude?.Invoke(path));
        ThemeColor Color(string id) => theme.GetColor(id) ?? throw new FormatException($"The theme has no color {id}.");
        return new EditorTheme
        {
            Name = name,
            Type = theme.Type,
            TokenColors = theme.TokenColors,
            Colors = theme.Colors,
            Background = Color("editor.background"),
            Foreground = Color("editor.foreground"),
            LineNumberForeground = Color("editorLineNumber.foreground"),
            LineNumberActiveForeground = Color("editorLineNumber.activeForeground"),
            LineHighlightBackground = theme.GetColor("editor.lineHighlightBackground"),
            LineHighlightBorder = theme.GetColor("editor.lineHighlightBorder"),
            FoldingControlForeground = theme.GetColor("editorGutter.foldingControlForeground"),
            CursorForeground = Color("editorCursor.foreground"),
            SelectionBackground = Color("editor.selectionBackground"),
            CompositionBorder = Color("editor.compositionBorder"),
        };
    }

    /// <summary>The colors of <see cref="TokenColors"/> by id (the color map of the tokenizer); index 0 is unused.</summary>
    internal IReadOnlyList<ThemeColor> TokenColorMap
    {
        get
        {
            if (!ReferenceEquals(_mapSource, TokenColors))
            {
                var index = new TokenColorIndex();
                foreach (TokenColorRule rule in TokenColors)
                {
                    index.Add(rule.Foreground);
                    index.Add(rule.Background);
                }

                _tokenColorMap = index.AsArray();
                _mapSource = TokenColors;
            }

            return _tokenColorMap!;
        }
    }

    /// <summary>Display name of the theme.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Editor background (<c>editor.background</c>).</summary>
    public ThemeColor Background { get; init; }

    /// <summary>Default text color (<c>editor.foreground</c>).</summary>
    public ThemeColor Foreground { get; init; }

    /// <summary>Line numbers in the gutter (<c>editorLineNumber.foreground</c>).</summary>
    public ThemeColor LineNumberForeground { get; init; }

    /// <summary>Line number of the cursor line (<c>editorLineNumber.activeForeground</c>).</summary>
    public ThemeColor LineNumberActiveForeground { get; init; }

    /// <summary>
    /// Background of the cursor line (<c>editor.lineHighlightBackground</c>); when <see langword="null"/>, the
    /// <see cref="LineHighlightBorder"/> is drawn instead.
    /// </summary>
    public ThemeColor? LineHighlightBackground { get; init; }

    /// <summary>Border of the cursor line (<c>editor.lineHighlightBorder</c>), used when there is no background.</summary>
    public ThemeColor? LineHighlightBorder { get; init; }

    /// <summary>The folding chevrons of the gutter (<c>editorGutter.foldingControlForeground</c>).</summary>
    public ThemeColor? FoldingControlForeground { get; init; }

    /// <summary>Color of the text cursor (<c>editorCursor.foreground</c>).</summary>
    public ThemeColor CursorForeground { get; init; }

    /// <summary>Background of selected text (<c>editor.selectionBackground</c>).</summary>
    public ThemeColor SelectionBackground { get; init; }

    /// <summary>Underline of the text being composed with an IME (<c>editor.compositionBorder</c>); defaults to white.</summary>
    public ThemeColor CompositionBorder { get; init; } = ThemeColor.Parse("#FFFFFF");
}

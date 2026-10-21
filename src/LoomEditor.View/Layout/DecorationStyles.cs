namespace LoomEditor.View.Layout;

/// <summary>
/// The hand-written table that replaces Monaco's CSS for decoration class names (ADR 0009, decision 4).
/// Feature cards that need a new class add their row here.
/// </summary>
/// <remarks>
/// Behavior differences: high-contrast <c>dotted</c> / <c>dashed</c> borders are drawn solid, the <c>padding: 1px</c>
/// of <c>.currentFindMatch</c> is ignored, and <c>selectionHighlight</c> always uses the focused colors (the unfocused
/// <c>transparent(0.5)</c> rule of <c>highlightDecorations.ts</c> is not ported).
/// Threading: immutable, safe from any thread.
/// </remarks>
internal static class DecorationStyles
{
    private static readonly Dictionary<string, DecorationStyle> Table = new(StringComparer.Ordinal)
    {
        // findWidget.css 226–241, findWidget.ts 1474–1488
        ["findMatch"] = new("editor.findMatchHighlightBackground", "editor.findMatchHighlightBorder"),
        ["currentFindMatch"] = new("editor.findMatchBackground", "editor.findMatchBorder", BorderWidth: 2),
        ["findScope"] = new("editor.findRangeHighlightBackground", "editor.findRangeHighlightBorder"),

        // findWidget.ts 1489–1496: Monaco crosses the colors (the inline current match uses the highlight foreground).
        ["findMatchInline"] = new(ForegroundColorId: "editor.findMatchForeground"),
        ["currentFindMatchInline"] = new(ForegroundColorId: "editor.findMatchHighlightForeground"),

        // editor.css 32–39
        ["rangeHighlight"] = new("editor.rangeHighlightBackground", "editor.rangeHighlightBorder"),

        // highlightDecorations.css 6–40 (`.focused .selectionHighlight`)
        ["selectionHighlight"] = new("editor.selectionHighlightBackground", "editor.selectionHighlightBorder"),
        ["wordHighlight"] = new("editor.wordHighlightBackground", "editor.wordHighlightBorder"),
        ["wordHighlightStrong"] = new("editor.wordHighlightStrongBackground", "editor.wordHighlightStrongBorder"),
        ["wordHighlightText"] = new("editor.wordHighlightTextBackground", "editor.wordHighlightTextBorder"),

        // bracketMatching.css 6–10, bracketMatching.ts 422–429
        ["bracket-match"] = new("editorBracketMatch.background", "editorBracketMatch.border"),
        ["bracket-match-inline"] = new(ForegroundColorId: "editorBracketMatch.foreground", Important: true),

        // folding.css 33–44
        ["folded-background"] = new("editor.foldBackground"),
        // colorizedBracketPairsDecorationProvider.ts 106–128: the only computed rows (see BracketPairColorizationColors)
        ["unexpected-closing-bracket"] = new(ForegroundResolver: BracketPairColorizationColors.GetUnexpectedBracketColor),

        ["inline-folded"] = new(AfterContentText: "⋯", AfterContentColorId: "editor.foldPlaceholderForeground"),
    };

    static DecorationStyles()
    {
        for (int level = 0; level < 30; level++)
        {
            int l = level;
            Table[$"bracket-highlighting-{level}"] = new(ForegroundResolver: theme => BracketPairColorizationColors.GetLevelColor(theme, l));
        }
    }

    /// <summary>Every color id of the table.</summary>
    internal static IEnumerable<string> ColorIds =>
        Table.Values
            .SelectMany(style => new[] { style.BackgroundColorId, style.BorderColorId, style.ForegroundColorId, style.AfterContentColorId })
            .OfType<string>()
            .Distinct(StringComparer.Ordinal);

    /// <summary>
    /// The style of a space-separated list of class names: the row of the first listed class that has one, else
    /// <see langword="null"/> (unknown classes draw nothing).
    /// </summary>
    public static DecorationStyle? Get(string? className)
    {
        if (className is null)
        {
            return null;
        }

        foreach (string name in className.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Table.TryGetValue(name, out DecorationStyle? style))
            {
                return style;
            }
        }

        return null;
    }
}

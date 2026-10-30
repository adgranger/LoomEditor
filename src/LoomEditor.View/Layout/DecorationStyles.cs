namespace LoomEditor.View.Layout;

/// <summary>
/// The hand-written table that replaces Monaco's CSS for decoration class names (ADR 0009, decision 4).
/// Feature cards that need a new class add their row here.
/// </summary>
/// <remarks>
/// Behavior differences: high-contrast <c>dotted</c> / <c>dashed</c> borders are drawn solid, the <c>padding: 1px</c>
/// of <c>.currentFindMatch</c> is ignored, and <c>selectionHighlight</c> always uses the focused colors (the unfocused
/// <c>transparent(0.5)</c> rule of <c>highlightDecorations.ts</c> is not ported). The diff rows have no CSS fallback chains
/// (<c>var(a, var(b))</c>: every id has its own default; only a custom theme that sets the text color but not the line
/// color differs), except the gutter background, which falls back to the line background.
/// Threading: the built-in table is immutable; the host table is copy-on-write; safe from any thread.
/// </remarks>
internal static class DecorationStyles
{
    private static readonly Lock CustomLock = new();

    // The host classes (CustomDecorationStyles): replaced as a whole on each write, so readers never lock.
    private static volatile Dictionary<string, DecorationStyle> _custom = [];

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

        // LoomEditor's own class (phase 12): the lines of a multi-line comment thread
        ["comment-thread-range"] = new("loom.commentThreadRangeBackground", "editor.rangeHighlightBorder"),

        // LoomEditor's own class (phase 12): the exact text of a comment thread
        ["comment-thread-text"] = new("loom.commentThreadRangeBackground", "editor.rangeHighlightBorder"),

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

        // folding.css 5–30, foldingDecorations.ts 33–60: collapsed icons are always visible; `alwaysShowFoldIcons` is handled by `Get`.
        ["codicon-folding-expanded"] = new(GutterIcon: GutterIcon.ChevronDown),
        ["codicon-folding-manual-expanded"] = new(GutterIcon: GutterIcon.ChevronDown),
        ["codicon-folding-collapsed"] = new(GutterIcon: GutterIcon.ChevronRight, AlwaysVisible: true),
        ["codicon-folding-manual-collapsed"] = new(GutterIcon: GutterIcon.ChevronRight, AlwaysVisible: true),

        // codeEditorWidget.ts 2524–2560. `squiggly-unnecessary` and the inline unnecessary / deprecated classes have no row.
        ["squiggly-error"] = new(Underline: UnderlineStyle.Squiggle, UnderlineColorId: "editorError.foreground"),
        ["squiggly-warning"] = new(Underline: UnderlineStyle.Squiggle, UnderlineColorId: "editorWarning.foreground"),
        ["squiggly-info"] = new(Underline: UnderlineStyle.Squiggle, UnderlineColorId: "editorInfo.foreground"),
        ["squiggly-hint"] = new(Underline: UnderlineStyle.Dots, UnderlineColorId: "editorHint.foreground"),

        // Gallery demo ("Inject text"): a dim hint, like an inlay hint
        ["gallery-injected-hint"] = new(ForegroundColorId: "editorLineNumber.foreground"),

        ["inline-folded"] = new(AfterContentText: "⋯", AfterContentColorId: "editor.foldPlaceholderForeground"),

        // registrations.contribution.ts 14–96, style.css 274–285, 327–346, 373–425. The CSS fallback chains `var(a, var(b))`
        // are not ported: every id has its own default.
        ["line-insert"] = new("diffEditor.insertedLineBackground", "diffEditor.insertedTextBorder"),
        ["line-delete"] = new("diffEditor.removedLineBackground", "diffEditor.removedTextBorder"),
        ["char-insert"] = new("diffEditor.insertedTextBackground", "diffEditor.insertedTextBorder"),
        ["char-delete"] = new("diffEditor.removedTextBackground", "diffEditor.removedTextBorder"),
        ["char-insert diff-range-empty"] = new(BorderColorId: "diffEditor.insertedTextBackground", BorderWidth: 0, LeftBorderWidth: 3),
        ["char-delete diff-range-empty"] = new(BorderColorId: "diffEditor.removedTextBackground", BorderWidth: 0, LeftBorderWidth: 3),
        // anchorSelect.css 5-8: the anchor is a 2 px bar
        ["selection-anchor"] = new(BorderColorId: "loom.selectionAnchor", BorderWidth: 0, LeftBorderWidth: 2),
        // snippetSession.css 5-19: the CSS outline is drawn as the 1 px border; min-width: 2px is not ported
        ["snippet-placeholder"] = new("editor.snippetTabstopHighlightBackground", "editor.snippetTabstopHighlightBorder", BorderWidth: 1),
        ["finish-snippet-placeholder"] = new("editor.snippetFinalTabstopHighlightBackground", "editor.snippetFinalTabstopHighlightBorder", BorderWidth: 1),

        // dnd.css 5-25: the drop target is a 2 px bar (dotted in Monaco, solid here) after the empty range
        ["dnd-target"] = new(BorderColorId: "editorCursor.foreground", BorderWidth: 0, RightBorderWidth: 2),
        ["gutter-insert"] = new("diffEditorGutter.insertedLineBackground", BackgroundFallbackColorId: "diffEditor.insertedLineBackground"),
        ["gutter-delete"] = new("diffEditorGutter.removedLineBackground", BackgroundFallbackColorId: "diffEditor.removedLineBackground"),
        ["inline-deleted-text"] = new("diffEditor.removedTextBackground", StrikeThrough: true),
        ["insert-sign"] = new(GutterIcon: GutterIcon.Plus, AlwaysVisible: true),
        ["delete-sign"] = new(GutterIcon: GutterIcon.Minus, AlwaysVisible: true),
        // style.css 283–285
        ["diff-unchanged-lines"] = new("diffEditor.unchangedCodeBackground"),
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
            .SelectMany(style => new[] { style.BackgroundColorId, style.BorderColorId, style.BackgroundFallbackColorId, style.ForegroundColorId, style.AfterContentColorId, style.UnderlineColorId })
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

        // A class list registered as one string (`char-insert diff-range-empty`) has its own row.
        if (Table.TryGetValue(className, out DecorationStyle? whole))
        {
            return whole;
        }

        string[] names = className.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string name in names)
        {
            if (TryGetStyle(name, out DecorationStyle? style))
            {
                // `.alwaysShowFoldIcons` (folding.css) makes the icon of the same list visible without hover.
                return style.GutterIcon != GutterIcon.None && Array.IndexOf(names, "alwaysShowFoldIcons") >= 0
                    ? style with { AlwaysVisible = true }
                    : style;
            }
        }

        return null;
    }

    /// <summary>Whether <paramref name="className"/> is a built-in class name (a host class cannot replace it).</summary>
    internal static bool IsBuiltIn(string className) => Table.ContainsKey(className);

    /// <summary>Adds or replaces a host class.</summary>
    internal static void SetCustom(string className, DecorationStyle style)
    {
        lock (CustomLock)
        {
            _custom = new Dictionary<string, DecorationStyle>(_custom) { [className] = style };
        }
    }

    /// <summary>Removes a host class; <see langword="false"/> when it was not registered.</summary>
    internal static bool RemoveCustom(string className)
    {
        lock (CustomLock)
        {
            if (!_custom.ContainsKey(className))
            {
                return false;
            }

            var copy = new Dictionary<string, DecorationStyle>(_custom);
            copy.Remove(className);
            _custom = copy;
            return true;
        }
    }

    // The built-ins win over the host classes.
    private static bool TryGetStyle(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out DecorationStyle? style) =>
        Table.TryGetValue(name, out style) || _custom.TryGetValue(name, out style);
}

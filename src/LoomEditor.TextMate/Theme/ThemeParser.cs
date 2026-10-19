// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Grammar;

namespace LoomEditor.TextMate.Theme;

/// <summary><c>parseTheme</c>: turns a raw theme into rules.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class ThemeParser
{
    /// <summary>
    /// <c>parseTheme</c>: one rule per scope of each setting. A string scope is split on <c>,</c> after removing leading
    /// and trailing commas; a setting without a scope gives the scope <c>""</c>. Invalid colors are ignored.
    /// </summary>
    public static List<ParsedThemeRule> ParseTheme(RawTheme? source)
    {
        var result = new List<ParsedThemeRule>();
        if (source?.Settings is not IReadOnlyList<RawThemeSetting> settings)
        {
            return result;
        }

        for (int i = 0; i < settings.Count; i++)
        {
            RawThemeSetting entry = settings[i];

            if (entry.Settings is null)
            {
                continue;
            }

            IReadOnlyList<string> scopes;
            if (entry.ScopeText is string scopeText)
            {
                // remove leading and trailing commas
                scopes = scopeText.Trim(',').Split(',');
            }
            else if (entry.Scope is not null)
            {
                scopes = entry.Scope;
            }
            else
            {
                scopes = [string.Empty];
            }

            FontStyle fontStyle = FontStyle.NotSet;
            if (entry.Settings.FontStyle is string fontStyleText)
            {
                fontStyle = FontStyle.None;

                foreach (string segment in fontStyleText.Split(' '))
                {
                    switch (segment)
                    {
                        case "italic":
                            fontStyle |= FontStyle.Italic;
                            break;
                        case "bold":
                            fontStyle |= FontStyle.Bold;
                            break;
                        case "underline":
                            fontStyle |= FontStyle.Underline;
                            break;
                        case "strikethrough":
                            fontStyle |= FontStyle.Strikethrough;
                            break;
                    }
                }
            }

            string? foreground = null;
            if (entry.Settings.Foreground is string foregroundText && Strings.IsValidHexColor(foregroundText))
            {
                foreground = foregroundText;
            }

            string? background = null;
            if (entry.Settings.Background is string backgroundText && Strings.IsValidHexColor(backgroundText))
            {
                background = backgroundText;
            }

            foreach (string scopeEntry in scopes)
            {
                string[] segments = scopeEntry.Trim().Split(' ');

                string scope = segments[^1];
                string[]? parentScopes = null;
                if (segments.Length > 1)
                {
                    parentScopes = segments[..^1];
                    Array.Reverse(parentScopes);
                }

                result.Add(new ParsedThemeRule(scope, parentScopes, i, fontStyle, foreground, background));
            }
        }

        return result;
    }

    /// <summary>
    /// <c>resolveParsedThemeRules</c>: resolves inheritance. Rules are sorted (stable) by scope, parent scopes and index;
    /// the leading rules with the scope <c>""</c> give the defaults (black on white, no font style, when not set).
    /// </summary>
    public static Theme ResolveParsedThemeRules(IReadOnlyList<ParsedThemeRule> parsedThemeRules, IReadOnlyList<string>? colorMap)
    {
        ArgumentNullException.ThrowIfNull(parsedThemeRules);

        // Sort rules lexicographically, and then by index if necessary
        List<ParsedThemeRule> rules = [.. parsedThemeRules.Order(Comparer<ParsedThemeRule>.Create((a, b) =>
        {
            int r = Strings.Strcmp(a!.Scope, b!.Scope);
            if (r != 0)
            {
                return r;
            }

            r = Strings.StrArrCmp(a.ParentScopes, b.ParentScopes);
            if (r != 0)
            {
                return r;
            }

            return a.Index - b.Index;
        }))];

        // Determine defaults
        FontStyle defaultFontStyle = FontStyle.None;
        string defaultForeground = "#000000";
        string defaultBackground = "#ffffff";

        int first = 0;
        while (first < rules.Count && rules[first].Scope.Length == 0)
        {
            ParsedThemeRule incomingDefaults = rules[first++];
            if (incomingDefaults.FontStyle != FontStyle.NotSet)
            {
                defaultFontStyle = incomingDefaults.FontStyle;
            }

            if (incomingDefaults.Foreground is not null)
            {
                defaultForeground = incomingDefaults.Foreground;
            }

            if (incomingDefaults.Background is not null)
            {
                defaultBackground = incomingDefaults.Background;
            }
        }

        var map = new ColorMap(colorMap);
        var defaults = new StyleAttributes(defaultFontStyle, map.GetId(defaultForeground), map.GetId(defaultBackground));

        var root = new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, 0, 0), []);
        for (int i = first; i < rules.Count; i++)
        {
            ParsedThemeRule rule = rules[i];
            root.Insert(0, rule.Scope, rule.ParentScopes, rule.FontStyle, map.GetId(rule.Foreground), map.GetId(rule.Background));
        }

        return new Theme(map, defaults, root);
    }
}

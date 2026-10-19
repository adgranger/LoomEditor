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
}

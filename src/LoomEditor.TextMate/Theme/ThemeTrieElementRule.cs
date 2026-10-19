// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;

namespace LoomEditor.TextMate.Theme;

/// <summary><c>ThemeTrieElementRule</c>: the style of a trie node, for one list of parent scopes. No font attributes.</summary>
/// <remarks>Threading: mutable while the theme is built (<see cref="AcceptOverwrite"/>); read-only afterwards.</remarks>
internal sealed class ThemeTrieElementRule
{
    /// <summary>Creates a rule; <see langword="null"/> parent scopes become <see cref="ThemeScopes.EmptyParentScopes"/>.</summary>
    public ThemeTrieElementRule(int scopeDepth, IReadOnlyList<string>? parentScopes, FontStyle fontStyle, int foreground, int background)
    {
        ScopeDepth = scopeDepth;
        ParentScopes = parentScopes ?? ThemeScopes.EmptyParentScopes;
        FontStyle = fontStyle;
        Foreground = foreground;
        Background = background;
    }

    /// <summary><c>scopeDepth</c>: the number of dot-separated segments of the rule's scope.</summary>
    public int ScopeDepth { get; private set; }

    /// <summary><c>parentScopes</c>: deepest first.</summary>
    public IReadOnlyList<string> ParentScopes { get; }

    /// <summary><c>fontStyle</c>.</summary>
    public FontStyle FontStyle { get; private set; }

    /// <summary><c>foreground</c>: a color id, 0 when not set.</summary>
    public int Foreground { get; private set; }

    /// <summary><c>background</c>: a color id, 0 when not set.</summary>
    public int Background { get; private set; }

    /// <summary><c>cloneArr</c>.</summary>
    public static List<ThemeTrieElementRule> CloneArr(List<ThemeTrieElementRule> arr) => arr.ConvertAll(rule => rule.Clone());

    /// <summary><c>clone</c>.</summary>
    public ThemeTrieElementRule Clone() => new(ScopeDepth, ParentScopes, FontStyle, Foreground, Background);

    /// <summary><c>acceptOverwrite</c>: takes the values that are set.</summary>
    public void AcceptOverwrite(int scopeDepth, FontStyle fontStyle, int foreground, int background)
    {
        if (ScopeDepth > scopeDepth)
        {
            Debug.WriteLine("how did this happen?");
        }
        else
        {
            ScopeDepth = scopeDepth;
        }

        if (fontStyle != FontStyle.NotSet)
        {
            FontStyle = fontStyle;
        }

        if (foreground != 0)
        {
            Foreground = foreground;
        }

        if (background != 0)
        {
            Background = background;
        }
    }
}

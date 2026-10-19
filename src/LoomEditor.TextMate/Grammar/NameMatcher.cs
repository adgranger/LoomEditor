// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Grammar;

/// <summary>The <c>nameMatcher</c> of <c>grammar.ts</c>, shared by injections and balanced bracket selectors.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class NameMatcher
{
    /// <summary>
    /// <c>nameMatcher</c>: whether <paramref name="identifiers"/> all appear in <paramref name="scopes"/>, in order, each
    /// one equal to a scope or a dot-separated prefix of it (<c>scopesAreMatching</c>).
    /// </summary>
    public static bool Matches(string[] identifiers, string[] scopes)
    {
        if (scopes.Length < identifiers.Length)
        {
            return false;
        }

        int lastIndex = 0;
        foreach (string identifier in identifiers)
        {
            bool found = false;
            for (int i = lastIndex; i < scopes.Length; i++)
            {
                if (scopes[i].Length > 0 && ThemeScopes.MatchesScope(scopes[i], identifier))
                {
                    lastIndex = i + 1;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }
}

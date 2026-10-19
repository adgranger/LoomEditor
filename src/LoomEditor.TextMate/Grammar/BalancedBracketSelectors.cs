// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>BalancedBracketSelectors</c>: decides whether tokens with given scopes take part in bracket matching.</summary>
/// <remarks>Threading: immutable after construction.</remarks>
internal sealed class BalancedBracketSelectors
{
    private readonly List<Func<string[], bool>> _balancedBracketScopes = [];
    private readonly List<Func<string[], bool>> _unbalancedBracketScopes = [];
    private readonly bool _allowAny;

    /// <summary>Compiles the selectors; <c>*</c> in <paramref name="balancedBracketScopes"/> matches every scope.</summary>
    public BalancedBracketSelectors(IEnumerable<string> balancedBracketScopes, IEnumerable<string> unbalancedBracketScopes)
    {
        ArgumentNullException.ThrowIfNull(balancedBracketScopes);
        ArgumentNullException.ThrowIfNull(unbalancedBracketScopes);
        foreach (string selector in balancedBracketScopes)
        {
            if (selector == "*")
            {
                _allowAny = true;
                continue;
            }

            foreach (MatcherWithPriority<string[]> m in Matcher.CreateMatchers<string[]>(selector, NameMatcher.Matches))
            {
                _balancedBracketScopes.Add(m.Matcher);
            }
        }

        foreach (string selector in unbalancedBracketScopes)
        {
            foreach (MatcherWithPriority<string[]> m in Matcher.CreateMatchers<string[]>(selector, NameMatcher.Matches))
            {
                _unbalancedBracketScopes.Add(m.Matcher);
            }
        }
    }

    /// <summary><c>matchesAlways</c>.</summary>
    public bool MatchesAlways => _allowAny && _unbalancedBracketScopes.Count == 0;

    /// <summary><c>matchesNever</c>.</summary>
    public bool MatchesNever => _balancedBracketScopes.Count == 0 && !_allowAny;

    /// <summary><c>match</c>: an unbalanced selector wins over a balanced one.</summary>
    public bool Match(string[] scopes)
    {
        foreach (Func<string[], bool> excluder in _unbalancedBracketScopes)
        {
            if (excluder(scopes))
            {
                return false;
            }
        }

        foreach (Func<string[], bool> includer in _balancedBracketScopes)
        {
            if (includer(scopes))
            {
                return true;
            }
        }

        return _allowAny;
    }
}

// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Grammar;

namespace LoomEditor.TextMate.Theme;

/// <summary><c>ThemeTrieElement</c>: one node of the theme trie, keyed by dot-separated scope segments.</summary>
/// <remarks>Threading: built once by <c>resolveParsedThemeRules</c>; read-only afterwards.</remarks>
internal sealed class ThemeTrieElement
{
    private static readonly Comparer<ThemeTrieElementRule> BySpecificity = Comparer<ThemeTrieElementRule>.Create(CmpBySpecificity);

    private readonly ThemeTrieElementRule _mainRule;
    private readonly List<ThemeTrieElementRule> _rulesWithParentScopes;
    private readonly Dictionary<string, ThemeTrieElement> _children;

    /// <summary>Creates a node.</summary>
    public ThemeTrieElement(ThemeTrieElementRule mainRule, List<ThemeTrieElementRule>? rulesWithParentScopes = null,
        Dictionary<string, ThemeTrieElement>? children = null)
    {
        _mainRule = mainRule;
        _rulesWithParentScopes = rulesWithParentScopes ?? [];
        _children = children ?? new Dictionary<string, ThemeTrieElement>(StringComparer.Ordinal);
    }

    /// <summary>The rule without parent scopes (for tests).</summary>
    internal ThemeTrieElementRule MainRule => _mainRule;

    /// <summary>The rules with parent scopes (for tests).</summary>
    internal IReadOnlyList<ThemeTrieElementRule> RulesWithParentScopes => _rulesWithParentScopes;

    /// <summary>The children by scope segment (<c>ITrieChildrenMap</c>, for tests).</summary>
    internal IReadOnlyDictionary<string, ThemeTrieElement> Children => _children;

    /// <summary>
    /// <c>match</c>: the rules of the deepest node for <paramref name="scope"/>, most specific first (a stable sort, as
    /// JavaScript's <c>Array.prototype.sort</c>).
    /// </summary>
    public List<ThemeTrieElementRule> Match(string scope)
    {
        if (scope.Length != 0)
        {
            (string head, string tail) = SplitHead(scope);
            if (_children.TryGetValue(head, out ThemeTrieElement? child))
            {
                return child.Match(tail);
            }
        }

        return [.. _rulesWithParentScopes.Append(_mainRule).Order(BySpecificity)];
    }

    /// <summary><c>insert</c>.</summary>
    public void Insert(int scopeDepth, string scope, IReadOnlyList<string>? parentScopes, FontStyle fontStyle, int foreground, int background)
    {
        if (scope.Length == 0)
        {
            DoInsertHere(scopeDepth, parentScopes, fontStyle, foreground, background);
            return;
        }

        (string head, string tail) = SplitHead(scope);

        if (!_children.TryGetValue(head, out ThemeTrieElement? child))
        {
            child = new ThemeTrieElement(_mainRule.Clone(), ThemeTrieElementRule.CloneArr(_rulesWithParentScopes));
            _children[head] = child;
        }

        child.Insert(scopeDepth + 1, tail, parentScopes, fontStyle, foreground, background);
    }

    // _cmpBySpecificity
    private static int CmpBySpecificity(ThemeTrieElementRule? a, ThemeTrieElementRule? b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        // First, compare the scope depths of both rules. The "scope depth" of a rule is
        // the number of segments (delimited by dots) in the rule's deepest scope name
        // (i.e. the final scope name in the scope path delimited by spaces).
        if (a.ScopeDepth != b.ScopeDepth)
        {
            return b.ScopeDepth - a.ScopeDepth;
        }

        // Traverse the parent scopes depth-first, comparing the specificity of both
        // rules' parent scopes, which matches the behavior described by "Ranking Matches"
        // in TextMate 1.5's manual: https://macromates.com/manual/en/scope_selectors
        // Start at index 0 for both rules, since the parent scopes were reversed
        // beforehand (i.e. index 0 is the deepest parent scope).
        int aParentIndex = 0;
        int bParentIndex = 0;

        while (true)
        {
            // Child combinators don't affect specificity.
            if (aParentIndex < a.ParentScopes.Count && a.ParentScopes[aParentIndex] == ">")
            {
                aParentIndex++;
            }

            if (bParentIndex < b.ParentScopes.Count && b.ParentScopes[bParentIndex] == ">")
            {
                bParentIndex++;
            }

            // This is a scope-by-scope comparison, so we need to stop once a rule runs
            // out of parent scopes.
            if (aParentIndex >= a.ParentScopes.Count || bParentIndex >= b.ParentScopes.Count)
            {
                break;
            }

            // When sorting by scope name specificity, it's safe to treat a longer parent
            // scope as more specific. If both rules' parent scopes match a given scope
            // path, the longer parent scope will always be more specific.
            int parentScopeLengthDiff = b.ParentScopes[bParentIndex].Length - a.ParentScopes[aParentIndex].Length;

            if (parentScopeLengthDiff != 0)
            {
                return parentScopeLengthDiff;
            }

            aParentIndex++;
            bParentIndex++;
        }

        // If a depth-first, scope-by-scope comparison resulted in a tie, the rule with
        // more parent scopes is considered more specific.
        return b.ParentScopes.Count - a.ParentScopes.Count;
    }

    private static (string Head, string Tail) SplitHead(string scope)
    {
        int dotIndex = scope.IndexOf('.', StringComparison.Ordinal);
        return dotIndex == -1 ? (scope, string.Empty) : (scope[..dotIndex], scope[(dotIndex + 1)..]);
    }

    // _doInsertHere
    private void DoInsertHere(int scopeDepth, IReadOnlyList<string>? parentScopes, FontStyle fontStyle, int foreground, int background)
    {
        if (parentScopes is null)
        {
            // Merge into the main rule
            _mainRule.AcceptOverwrite(scopeDepth, fontStyle, foreground, background);
            return;
        }

        // Try to merge into existing rule
        foreach (ThemeTrieElementRule rule in _rulesWithParentScopes)
        {
            if (Strings.StrArrCmp(rule.ParentScopes, parentScopes) == 0)
            {
                // bingo! => we get to merge this into an existing one
                rule.AcceptOverwrite(scopeDepth, fontStyle, foreground, background);
                return;
            }
        }

        // Must add a new rule

        // Inherit from main rule
        if (fontStyle == FontStyle.NotSet)
        {
            fontStyle = _mainRule.FontStyle;
        }

        if (foreground == 0)
        {
            foreground = _mainRule.Foreground;
        }

        if (background == 0)
        {
            background = _mainRule.Background;
        }

        _rulesWithParentScopes.Add(new ThemeTrieElementRule(scopeDepth, parentScopes, fontStyle, foreground, background));
    }
}

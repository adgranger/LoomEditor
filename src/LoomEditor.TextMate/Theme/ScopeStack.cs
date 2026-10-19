// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Theme;

/// <summary><c>ScopeStack</c>: an immutable linked list of scope names, the deepest last.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class ScopeStack
{
    /// <summary>Creates a stack with <paramref name="scopeName"/> on top of <paramref name="parent"/>.</summary>
    public ScopeStack(ScopeStack? parent, string scopeName)
    {
        Parent = parent;
        ScopeName = scopeName;
    }

    /// <summary><c>parent</c>.</summary>
    public ScopeStack? Parent { get; }

    /// <summary><c>scopeName</c>.</summary>
    public string ScopeName { get; }

    /// <summary><c>ScopeStack.push(path, scopeNames)</c>: pushes each name in order.</summary>
    public static ScopeStack? Push(ScopeStack? path, IEnumerable<string> scopeNames)
    {
        ArgumentNullException.ThrowIfNull(scopeNames);
        foreach (string name in scopeNames)
        {
            path = new ScopeStack(path, name);
        }

        return path;
    }

    /// <summary><c>ScopeStack.from(...segments)</c>: <see langword="null"/> when there is no segment.</summary>
    public static ScopeStack? From(params ReadOnlySpan<string> segments)
    {
        ScopeStack? result = null;
        for (int i = 0; i < segments.Length; i++)
        {
            result = new ScopeStack(result, segments[i]);
        }

        return result;
    }

    /// <summary><c>push</c>.</summary>
    public ScopeStack Push(string scopeName) => new(this, scopeName);

    /// <summary><c>getSegments</c>: the scope names, outermost first.</summary>
    public List<string> GetSegments()
    {
        var result = new List<string>();
        for (ScopeStack? item = this; item is not null; item = item.Parent)
        {
            result.Add(item.ScopeName);
        }

        result.Reverse();
        return result;
    }

    /// <summary><c>toString</c>: the segments separated by spaces.</summary>
    public override string ToString() => string.Join(' ', GetSegments());

    /// <summary><c>extends</c>: whether <paramref name="other"/> is this stack or one of its parents (by reference).</summary>
    public bool Extends(ScopeStack other)
    {
        for (ScopeStack? item = this; item is not null; item = item.Parent)
        {
            if (ReferenceEquals(item, other))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <c>getExtensionIfDefined</c>: the scope names pushed on top of <paramref name="base"/> (outermost first), or
    /// <see langword="null"/> when this stack does not extend it.
    /// </summary>
    public List<string>? GetExtensionIfDefined(ScopeStack? @base)
    {
        var result = new List<string>();
        ScopeStack? item = this;
        while (item is not null && !ReferenceEquals(item, @base))
        {
            result.Add(item.ScopeName);
            item = item.Parent;
        }

        if (!ReferenceEquals(item, @base))
        {
            return null;
        }

        result.Reverse();
        return result;
    }
}

/// <summary>Scope matching helpers of <c>theme.ts</c> (<c>_scopePathMatchesParentScopes</c>, <c>_matchesScope</c>).</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class ThemeScopes
{
    /// <summary><c>emptyParentScopes</c>.</summary>
    public static readonly IReadOnlyList<string> EmptyParentScopes = [];

    /// <summary>
    /// <c>_scopePathMatchesParentScopes</c>: whether the parent scopes of a rule (deepest first, <c>&gt;</c> for a
    /// child combinator) are found, in order, in <paramref name="scopePath"/>.
    /// </summary>
    public static bool ScopePathMatchesParentScopes(ScopeStack? scopePath, IReadOnlyList<string> parentScopes)
    {
        if (parentScopes.Count == 0)
        {
            return true;
        }

        // Starting with the deepest parent scope, look for a match in the scope path.
        for (int index = 0; index < parentScopes.Count; index++)
        {
            string scopePattern = parentScopes[index];
            bool scopeMustMatch = false;

            // Check for a child combinator (a parent-child relationship)
            if (scopePattern == ">")
            {
                if (index == parentScopes.Count - 1)
                {
                    // Invalid use of child combinator
                    return false;
                }

                scopePattern = parentScopes[++index];
                scopeMustMatch = true;
            }

            while (scopePath is not null)
            {
                if (MatchesScope(scopePath.ScopeName, scopePattern))
                {
                    break;
                }

                if (scopeMustMatch)
                {
                    // If a child combinator was used, the parent scope must match.
                    return false;
                }

                scopePath = scopePath.Parent;
            }

            if (scopePath is null)
            {
                // No more potential matches
                return false;
            }

            scopePath = scopePath.Parent;
        }

        // All parent scopes were matched.
        return true;
    }

    /// <summary><c>_matchesScope</c>: equal, or a dot-separated prefix (<c>a.b</c> matches <c>a</c>).</summary>
    public static bool MatchesScope(string scopeName, string scopePattern) =>
        scopePattern == scopeName
        || (scopeName.StartsWith(scopePattern, StringComparison.Ordinal)
            && scopeName.Length > scopePattern.Length
            && scopeName[scopePattern.Length] == '.');
}

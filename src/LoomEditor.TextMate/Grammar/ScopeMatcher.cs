// Ported from microsoft/vscode-textmate: src/grammar/basicScopesAttributeProvider.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>ScopeMatcher</c>: finds the value of the longest registered scope that a scope is equal to or nested in.</summary>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>
/// Threading: immutable after construction. The regex is built at run time (once per provider; results are cached by
/// <see cref="CachedFn{TKey, TValue}"/>, so this is not a hot path).
/// </remarks>
internal sealed class ScopeMatcher<TValue>
{
    private readonly Dictionary<string, TValue>? _values;
    private readonly Regex? _scopesRegExp;

    /// <summary>Builds the matcher; when a scope name is repeated, the last value wins (like <c>new Map(values)</c>).</summary>
    public ScopeMatcher(IReadOnlyList<KeyValuePair<string, TValue>> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            return;
        }

        _values = new Dictionary<string, TValue>(StringComparer.Ordinal);
        List<string> escapedScopes = [];
        foreach (KeyValuePair<string, TValue> entry in values)
        {
            _values[entry.Key] = entry.Value;
            escapedScopes.Add(Strings.EscapeRegExpCharacters(entry.Key));
        }

        escapedScopes.Sort(StringComparer.Ordinal);
        escapedScopes.Reverse(); // Longest scope first

        // \z, not $: .NET's $ also matches before a final "\n".
        _scopesRegExp = new Regex(
            $@"^(({string.Join(")|(", escapedScopes)}))(\z|\.)",
            RegexOptions.CultureInvariant);
    }

    /// <summary><c>match</c>: the value of the matching scope, or <see langword="default"/> when none matches.</summary>
    public bool TryMatch(string scope, out TValue value)
    {
        if (_scopesRegExp is not null && _scopesRegExp.Match(scope) is { Success: true } m)
        {
            value = _values![m.Groups[1].Value];
            return true;
        }

        value = default!;
        return false;
    }
}

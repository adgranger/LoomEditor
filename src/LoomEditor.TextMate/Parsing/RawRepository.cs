// Ported from microsoft/vscode-textmate: src/rawGrammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics.CodeAnalysis;

namespace LoomEditor.TextMate.Parsing;

/// <summary>
/// <c>IRawRepository</c> (<c>IRawRepositoryMap</c>): rules by name, in document order. <c>initGrammar</c> adds
/// <c>$self</c> and <c>$base</c>. Also used for the <c>injections</c> of a grammar (rules by selector).
/// </summary>
/// <remarks>Threading: read-only after loading (see <see cref="RawRule"/>).</remarks>
internal sealed class RawRepository
{
    private readonly OrderedDictionary<string, RawRule> _rules = new(StringComparer.Ordinal);

    /// <summary>The number of rules.</summary>
    public int Count => _rules.Count;

    /// <summary>The names, in document (insertion) order.</summary>
    public IEnumerable<string> Names => _rules.Keys;

    /// <summary>The rules, in document (insertion) order.</summary>
    public IEnumerable<KeyValuePair<string, RawRule>> Entries => _rules;

    /// <summary>Gets or sets (adds at the end, or replaces in place) the rule called <paramref name="name"/>.</summary>
    public RawRule this[string name]
    {
        get => _rules[name];
        set => _rules[name] = value;
    }

    /// <summary>Gets the rule called <paramref name="name"/>, if any.</summary>
    public bool TryGetValue(string name, [MaybeNullWhen(false)] out RawRule rule) => _rules.TryGetValue(name, out rule);

    /// <summary>A deep copy (<c>clone</c>).</summary>
    public RawRepository DeepClone()
    {
        var clone = new RawRepository();
        foreach (KeyValuePair<string, RawRule> entry in _rules)
        {
            clone._rules.Add(entry.Key, entry.Value.DeepClone());
        }

        return clone;
    }
}

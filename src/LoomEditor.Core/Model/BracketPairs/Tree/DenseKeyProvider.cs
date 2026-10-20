// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/smallImmutableSet.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>Maps a value to a small dense integer key (Monaco: <c>IDenseKeyProvider</c>).</summary>
/// <remarks>Threading: depends on the implementation.</remarks>
internal interface IDenseKeyProvider<in T>
{
    /// <summary>Returns the key of <paramref name="value"/>.</summary>
    int GetKey(T value);
}

/// <summary>The key of an integer is the integer itself (Monaco: <c>identityKeyProvider</c>).</summary>
/// <remarks>Threading: stateless, safe to share between threads.</remarks>
internal sealed class IdentityKeyProvider : IDenseKeyProvider<int>
{
    /// <summary>The shared instance.</summary>
    public static readonly IdentityKeyProvider Instance = new();

    private IdentityKeyProvider()
    {
    }

    /// <inheritdoc />
    public int GetKey(int value)
    {
        return value;
    }
}

/// <summary>Assigns values a unique incrementing key (Monaco: <c>DenseKeyProvider</c>).</summary>
/// <remarks>Threading: single-threaded, used by one tree.</remarks>
internal sealed class DenseKeyProvider<T> : IDenseKeyProvider<T>
    where T : notnull
{
    private readonly Dictionary<T, int> _keys = new();
    private readonly List<T> _items = [];

    /// <inheritdoc />
    public int GetKey(T value)
    {
        if (!_keys.TryGetValue(value, out var existing))
        {
            existing = _items.Count;
            _keys.Add(value, existing);
            _items.Add(value);
        }

        return existing;
    }

    /// <summary>Returns the value that has the given key, or <c>default</c> when there is none.</summary>
    public T? ReverseLookup(int key)
    {
        return key >= 0 && key < _items.Count ? _items[key] : default;
    }

    /// <summary>
    /// Returns the values whose keys are in the set, in key order (insertion order).
    /// </summary>
    /// <remarks>
    /// Behavior difference: the set holds the <b>keys</b> (<see cref="SmallImmutableSet{T}"/> of <see cref="int"/>, as in
    /// <see cref="Token.BracketIds"/>), where Monaco types it <c>SmallImmutableSet&lt;T&gt;</c> and relies on TypeScript's
    /// method bivariance to pass a set of numbers.
    /// </remarks>
    public List<T> ReverseLookupSet(SmallImmutableSet<int> set)
    {
        var result = new List<T>();
        foreach (var item in _items)
        {
            if (set.Has(GetKey(item), IdentityKeyProvider.Instance))
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>The values, in insertion order.</summary>
    public IReadOnlyList<T> Keys => _items;
}

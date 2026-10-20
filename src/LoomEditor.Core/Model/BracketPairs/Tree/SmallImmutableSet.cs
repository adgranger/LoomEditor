// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/smallImmutableSet.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// An immutable set that works best for a small number of elements (less than 32): membership is encoded in bits
/// (Monaco: <c>SmallImmutableSet</c>).
/// </summary>
/// <remarks>
/// <para>
/// Behavior difference: the cache of the 129 small sets is filled eagerly in the static constructor and there is
/// one cache per closed generic type; it is never written afterwards.
/// </para>
/// <para>Threading: immutable value, safe to share between threads.</para>
/// </remarks>
internal sealed class SmallImmutableSet<T> : IEquatable<SmallImmutableSet<T>>
{
    private static readonly int[] EmptyArr = [];
    private static readonly SmallImmutableSet<T>[] Cache = BuildCache();

    private readonly int _items;
    private readonly int[] _additionalItems;

    private SmallImmutableSet(int items, int[] additionalItems)
    {
        _items = items;
        _additionalItems = additionalItems;
    }

    /// <summary>The empty set.</summary>
    public static SmallImmutableSet<T> Empty => Cache[0];

    private static SmallImmutableSet<T>[] BuildCache()
    {
        var cache = new SmallImmutableSet<T>[129];
        for (var i = 0; i < cache.Length; i++)
        {
            cache[i] = new SmallImmutableSet<T>(i, EmptyArr);
        }

        return cache;
    }

    private static SmallImmutableSet<T> Create(int items, int[] additionalItems)
    {
        // The cache covers all sets with up to 7 (dense) elements.
        return items is >= 0 and <= 128 && additionalItems.Length == 0
            ? Cache[items]
            : new SmallImmutableSet<T>(items, additionalItems);
    }

    /// <summary>Returns the set plus <paramref name="value"/>.</summary>
    public SmallImmutableSet<T> Add(T value, IDenseKeyProvider<T> keyProvider)
    {
        var key = keyProvider.GetKey(value);
        var idx = key >> 5; // divided by 32
        if (idx == 0)
        {
            var newItem = (1 << key) | _items;
            return newItem == _items ? this : Create(newItem, _additionalItems);
        }

        idx--;
        var newItems = new List<int>(_additionalItems);
        while (newItems.Count <= idx)
        {
            newItems.Add(0);
        }

        newItems[idx] |= 1 << (key & 31);
        return Create(_items, newItems.ToArray());
    }

    /// <summary>True when the set contains <paramref name="value"/>.</summary>
    public bool Has(T value, IDenseKeyProvider<T> keyProvider)
    {
        var key = keyProvider.GetKey(value);
        var idx = key >> 5; // divided by 32
        if (idx == 0)
        {
            return (_items & (1 << key)) != 0;
        }

        idx--;
        return ((idx < _additionalItems.Length ? _additionalItems[idx] : 0) & (1 << (key & 31))) != 0;
    }

    /// <summary>Returns the union of both sets.</summary>
    public SmallImmutableSet<T> Merge(SmallImmutableSet<T> other)
    {
        var merged = _items | other._items;
        if (ReferenceEquals(_additionalItems, EmptyArr) && ReferenceEquals(other._additionalItems, EmptyArr))
        {
            if (merged == _items)
            {
                return this;
            }

            return merged == other._items ? other : Create(merged, EmptyArr);
        }

        // This can be optimized, but it's not a common case.
        var count = Math.Max(_additionalItems.Length, other._additionalItems.Length);
        var newItems = new int[count];
        for (var i = 0; i < count; i++)
        {
            var item1 = i < _additionalItems.Length ? _additionalItems[i] : 0;
            var item2 = i < other._additionalItems.Length ? other._additionalItems[i] : 0;
            newItems[i] = item1 | item2;
        }

        return Create(merged, newItems);
    }

    /// <summary>True when both sets have at least one element in common.</summary>
    public bool Intersects(SmallImmutableSet<T> other)
    {
        if ((_items & other._items) != 0)
        {
            return true;
        }

        var count = Math.Min(_additionalItems.Length, other._additionalItems.Length);
        for (var i = 0; i < count; i++)
        {
            if ((_additionalItems[i] & other._additionalItems[i]) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool Equals(SmallImmutableSet<T>? other)
    {
        return other is not null
            && _items == other._items
            && _additionalItems.AsSpan().SequenceEqual(other._additionalItems);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as SmallImmutableSet<T>);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_items);
        foreach (var item in _additionalItems)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}

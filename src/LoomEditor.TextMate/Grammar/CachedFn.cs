// Ported from microsoft/vscode-textmate: src/utils.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>CachedFn</c>: a function whose results are cached by key.</summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>Threading: single-threaded, like the grammar that owns it.</remarks>
internal sealed class CachedFn<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _cache = [];
    private readonly Func<TKey, TValue> _fn;

    /// <summary>Wraps <paramref name="fn"/>.</summary>
    public CachedFn(Func<TKey, TValue> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        _fn = fn;
    }

    /// <summary><c>get</c>: the cached value, computed on the first call for <paramref name="key"/>.</summary>
    public TValue Get(TKey key)
    {
        if (_cache.TryGetValue(key, out TValue? value))
        {
            return value;
        }

        value = _fn(key);
        _cache[key] = value;
        return value;
    }
}

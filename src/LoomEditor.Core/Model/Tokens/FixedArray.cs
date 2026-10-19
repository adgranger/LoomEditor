// Ported from microsoft/vscode: src/vs/editor/common/model/fixedArray.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Tokens;

/// <summary>An array that avoids being sparse by always filling up unused indices with a default value (Monaco: <c>FixedArray</c>).</summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class FixedArray<T>
{
    private readonly T _default;
    private List<T> _store = [];

    /// <summary>Creates an array whose unset elements are <paramref name="defaultValue"/>.</summary>
    public FixedArray(T defaultValue)
    {
        _default = defaultValue;
    }

    /// <summary><c>get</c>.</summary>
    public T Get(int index) => index < _store.Count ? _store[index] : _default;

    /// <summary><c>set</c>.</summary>
    public void Set(int index, T value)
    {
        while (index >= _store.Count)
        {
            _store.Add(_default);
        }

        _store[index] = value;
    }

    /// <summary><c>replace</c>: replaces <paramref name="oldLength"/> elements at <paramref name="index"/> by <paramref name="newLength"/> defaults.</summary>
    public void Replace(int index, int oldLength, int newLength)
    {
        if (index >= _store.Count)
        {
            return;
        }

        if (oldLength == 0)
        {
            Insert(index, newLength);
            return;
        }

        if (newLength == 0)
        {
            Delete(index, oldLength);
            return;
        }

        var result = new List<T>(_store.Count - oldLength + newLength);
        result.AddRange(_store.GetRange(0, index));
        result.AddRange(Enumerable.Repeat(_default, newLength));
        if (index + oldLength < _store.Count)
        {
            result.AddRange(_store.GetRange(index + oldLength, _store.Count - index - oldLength));
        }

        _store = result;
    }

    /// <summary><c>delete</c>.</summary>
    public void Delete(int deleteIndex, int deleteCount)
    {
        if (deleteCount == 0 || deleteIndex >= _store.Count)
        {
            return;
        }

        _store.RemoveRange(deleteIndex, Math.Min(deleteCount, _store.Count - deleteIndex));
    }

    /// <summary><c>insert</c>.</summary>
    public void Insert(int insertIndex, int insertCount)
    {
        if (insertCount == 0 || insertIndex >= _store.Count)
        {
            return;
        }

        _store.InsertRange(insertIndex, Enumerable.Repeat(_default, insertCount));
    }
}

// Ported from microsoft/vscode: src/vs/base/common/arrays.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.DiffEditor;

/// <summary>A queue backed by a list that is consumed from both ends by moving indices, without copying (Monaco: <c>ArrayQueue</c>).</summary>
/// <remarks>
/// <para>Not ported: <c>removeLast</c>, <c>peekLast</c>, <c>takeCount</c>, <c>length</c> (unused by the diff editor).</para>
/// <para>Threading: not thread-safe; use from one thread.</para>
/// </remarks>
internal sealed class ArrayQueue<T>(IReadOnlyList<T> items)
{
    private int _firstIdx;
    private int _lastIdx = items.Count - 1;

    /// <summary>Consumes items from the start while <paramref name="predicate"/> holds; <c>null</c> when nothing was consumed.</summary>
    public List<T>? TakeWhile(Func<T, bool> predicate)
    {
        int startIdx = _firstIdx;
        while (startIdx <= _lastIdx && predicate(items[startIdx]))
        {
            startIdx++;
        }

        List<T>? result = startIdx == _firstIdx ? null : Slice(_firstIdx, startIdx);
        _firstIdx = startIdx;
        return result;
    }

    /// <summary>Consumes items from the end while <paramref name="predicate"/> holds; <c>null</c> when nothing was consumed. The result keeps the list order.</summary>
    public List<T>? TakeFromEndWhile(Func<T, bool> predicate)
    {
        int endIdx = _lastIdx;
        while (endIdx >= _firstIdx && predicate(items[endIdx]))
        {
            endIdx--;
        }

        List<T>? result = endIdx == _lastIdx ? null : Slice(endIdx + 1, _lastIdx + 1);
        _lastIdx = endIdx;
        return result;
    }

    /// <summary>Gets the first item (Monaco: <c>peek</c>, which returns <c>undefined</c> when empty).</summary>
    /// <returns>False when the queue is empty.</returns>
    public bool TryPeek(out T item)
    {
        if (_lastIdx - _firstIdx + 1 == 0)
        {
            item = default!;
            return false;
        }

        item = items[_firstIdx];
        return true;
    }

    /// <summary>Removes the first item. Only call after a successful <see cref="TryPeek"/>.</summary>
    public void Dequeue() => _firstIdx++;

    private List<T> Slice(int start, int endExclusive)
    {
        var result = new List<T>(endExclusive - start);
        for (int i = start; i < endExclusive; i++)
        {
            result.Add(items[i]);
        }

        return result;
    }
}

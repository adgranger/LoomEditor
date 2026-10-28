// Ported from microsoft/vscode: src/vs/base/common/arrays.ts (groupAdjacentBy, forEachAdjacent)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.DiffEditor;

/// <summary>The two array helpers the accessible diff viewer needs.</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class ArrayHelpers
{
    /// <summary>
    /// Splits <paramref name="items"/> into runs: an item joins the current run when <paramref name="shouldBeGrouped"/> accepts the
    /// previous item and it. The order is preserved.
    /// </summary>
    public static IEnumerable<List<T>> GroupAdjacentBy<T>(IEnumerable<T> items, Func<T, T, bool> shouldBeGrouped)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(shouldBeGrouped);

        List<T>? currentGroup = null;
        T? last = default;
        bool hasLast = false;
        foreach (T item in items)
        {
            if (hasLast && shouldBeGrouped(last!, item))
            {
                currentGroup!.Add(item);
            }
            else
            {
                if (currentGroup is not null)
                {
                    yield return currentGroup;
                }

                currentGroup = [item];
            }

            last = item;
            hasLast = true;
        }

        if (currentGroup is not null)
        {
            yield return currentGroup;
        }
    }

    /// <summary>
    /// Calls <paramref name="f"/> for every pair of neighbors, with <c>null</c> before the first item and after the last one
    /// (so an array of n items gives n + 1 calls).
    /// </summary>
    public static void ForEachAdjacent<T>(IReadOnlyList<T> array, Action<T?, T?> f)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(f);

        for (int i = 0; i <= array.Count; i++)
        {
            f(i == 0 ? null : array[i - 1], i == array.Count ? null : array[i]);
        }
    }
}

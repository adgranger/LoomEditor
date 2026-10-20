// Ported from microsoft/vscode: src/vs/base/common/arrays.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs;

/// <summary>
/// A lazily computed sequence that pushes its items to a callback, and stops when the callback returns
/// <see langword="false"/>. Faster than an iterator and an array for lazily computed data (Monaco: <c>CallbackIterable</c>).
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
/// <remarks>
/// <para>
/// Behavior difference: members that may find no item return <typeparamref name="T"/>? with a <see langword="class"/>
/// constraint, as all the users are classes; <see cref="Empty"/> is one static instance per <typeparamref name="T"/>
/// (Monaco's <c>CallbackIterable&lt;never&gt;</c>).
/// </para>
/// <para>
/// <see cref="FindLastMaxBy"/> replaces its result only when an item is strictly greater, so of equal maxima the first
/// one is kept, whatever the name says (as in Monaco).
/// </para>
/// <para>Threading: stateless; thread-safety depends on the iterate function.</para>
/// </remarks>
internal sealed class CallbackIterable<T>
    where T : class
{
    private readonly Action<Func<T, bool>> _iterate;

    /// <summary>Creates an iterable.</summary>
    /// <param name="iterate">Calls the callback for every item. Stops when the callback returns <see langword="false"/>.</param>
    public CallbackIterable(Action<Func<T, bool>> iterate)
    {
        ArgumentNullException.ThrowIfNull(iterate);

        _iterate = iterate;
    }

    /// <summary>An iterable without any item.</summary>
    public static CallbackIterable<T> Empty { get; } = new(static _ => { });

    /// <summary>Calls <paramref name="callback"/> for every item; stops when it returns <see langword="false"/>.</summary>
    public void Iterate(Func<T, bool> callback)
    {
        _iterate(callback);
    }

    /// <summary>Calls <paramref name="handler"/> for every item.</summary>
    public void ForEach(Action<T> handler)
    {
        _iterate(item =>
        {
            handler(item);
            return true;
        });
    }

    /// <summary>Returns all the items.</summary>
    public T[] ToArray()
    {
        var result = new List<T>();
        _iterate(item =>
        {
            result.Add(item);
            return true;
        });
        return [.. result];
    }

    /// <summary>The items that satisfy <paramref name="predicate"/>.</summary>
    public CallbackIterable<T> Filter(Func<T, bool> predicate)
    {
        return new CallbackIterable<T>(cb => _iterate(item => !predicate(item) || cb(item)));
    }

    /// <summary>The items mapped with <paramref name="mapFn"/>.</summary>
    public CallbackIterable<TResult> Map<TResult>(Func<T, TResult> mapFn)
        where TResult : class
    {
        return new CallbackIterable<TResult>(cb => _iterate(item => cb(mapFn(item))));
    }

    /// <summary>True if an item satisfies <paramref name="predicate"/>; stops at the first one.</summary>
    public bool Some(Func<T, bool> predicate)
    {
        var result = false;
        _iterate(item =>
        {
            result = predicate(item);
            return !result;
        });
        return result;
    }

    /// <summary>The first item that satisfies <paramref name="predicate"/>, or <see langword="null"/>; stops iterating at it.</summary>
    public T? FindFirst(Func<T, bool> predicate)
    {
        T? result = default;
        _iterate(item =>
        {
            if (predicate(item))
            {
                result = item;
                return false;
            }

            return true;
        });
        return result;
    }

    /// <summary>The last item that satisfies <paramref name="predicate"/>, or <see langword="null"/>.</summary>
    public T? FindLast(Func<T, bool> predicate)
    {
        T? result = default;
        _iterate(item =>
        {
            if (predicate(item))
            {
                result = item;
            }

            return true;
        });
        return result;
    }

    /// <summary>
    /// The greatest item according to <paramref name="comparator"/>, or <see langword="null"/> if there is no item.
    /// Of equal maxima, the first one is kept.
    /// </summary>
    public T? FindLastMaxBy(Comparison<T> comparator)
    {
        T? result = default;
        var first = true;
        _iterate(item =>
        {
            if (first || comparator(item, result!) > 0)
            {
                first = false;
                result = item;
            }

            return true;
        });
        return result;
    }
}

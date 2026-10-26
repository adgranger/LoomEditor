// Ported from microsoft/vscode: src/vs/editor/common/languageFeatureRegistry.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Features;

/// <summary>
/// Holds the providers of one feature, registered for a language id or <c>"*"</c>, and finds them ordered by how well they match.
/// Differences from Monaco: selectors are strings only (no <c>LanguageFilter</c>, notebooks or scheme/pattern matching), queries take a
/// language id instead of a model and recompute every time (no <c>MatchCandidate</c> cache), and the <c>isBuiltin</c> ordering rule is dropped.
/// </summary>
/// <typeparam name="T">The provider interface.</typeparam>
/// <remarks>Threading: UI thread only (registration and queries); the providers' tasks are not the registry's concern.</remarks>
public sealed class LanguageFeatureRegistry<T>
    where T : class
{
    private const int ExactScore = 10;
    private const int StarScore = 5;
    private const string Star = "*";

    private readonly List<Entry> _entries = [];
    private int _time;

    /// <summary>Raised after a registration and after a disposal that removed an entry; the argument is the number of entries.</summary>
    public event EventHandler<int>? Changed;

    /// <summary>Registers <paramref name="provider"/> for <paramref name="selector"/> (a language id or <c>"*"</c>).</summary>
    /// <returns>A disposable that removes the registration; disposing it twice is harmless.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> or <paramref name="provider"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="selector"/> is empty.</exception>
    public IDisposable Register(string selector, T provider)
    {
        ArgumentException.ThrowIfNullOrEmpty(selector);
        ArgumentNullException.ThrowIfNull(provider);

        var entry = new Entry(selector, provider, ++_time);
        _entries.Add(entry);
        Changed?.Invoke(this, _entries.Count);
        return new Registration(this, entry);
    }

    /// <summary>True when at least one provider matches <paramref name="languageId"/>.</summary>
    public bool Has(string languageId) => Sorted(languageId).Count > 0;

    /// <summary>The matching providers, best first (Monaco <c>all</c> iterates the sorted entries too).</summary>
    public IReadOnlyList<T> All(string languageId) => Ordered(languageId);

    /// <summary>The matching providers: score descending, then most recently registered first.</summary>
    public IReadOnlyList<T> Ordered(string languageId) => Sorted(languageId).ConvertAll(e => e.Provider);

    /// <summary>The matching providers with equal scores in one group; groups in descending score.</summary>
    public IReadOnlyList<IReadOnlyList<T>> OrderedGroups(string languageId)
    {
        var groups = new List<IReadOnlyList<T>>();
        List<T>? group = null;
        var previousScore = -1;
        foreach (var entry in Sorted(languageId))
        {
            var score = Score(entry.Selector, languageId);
            if (group is null || score != previousScore)
            {
                group = [];
                groups.Add(group);
                previousScore = score;
            }

            group.Add(entry.Provider);
        }

        return groups;
    }

    /// <summary>Every provider, in registration order, whatever its selector.</summary>
    public IReadOnlyList<T> AllNoModel() => _entries.ConvertAll(e => e.Provider);

    private List<Entry> Sorted(string languageId)
    {
        ArgumentNullException.ThrowIfNull(languageId);
        var result = _entries.FindAll(e => Score(e.Selector, languageId) > 0);

        // Monaco: _compareByScoreAndTime
        result.Sort((a, b) =>
        {
            var byScore = Score(b.Selector, languageId).CompareTo(Score(a.Selector, languageId));
            return byScore != 0 ? byScore : b.Time.CompareTo(a.Time);
        });
        return result;
    }

    // Monaco: languageSelector.ts score, string selectors only
    private static int Score(string selector, string languageId)
    {
        if (string.Equals(selector, languageId, StringComparison.Ordinal))
        {
            return ExactScore;
        }

        return selector == Star ? StarScore : 0;
    }

    private sealed record Entry(string Selector, T Provider, int Time);

    private sealed class Registration(LanguageFeatureRegistry<T> owner, Entry entry) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (owner._entries.Remove(entry))
            {
                owner.Changed?.Invoke(owner, owner._entries.Count);
            }
        }
    }
}

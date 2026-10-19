using System.Collections.Concurrent;
using System.Diagnostics;
using LoomEditor.TextMate.Parsing;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>
/// Parses each grammar file once (ADR 0008 decision 5): raw grammars are shared read-only between the UI thread and the
/// worker; every <c>Grammar</c> clones what it gets (trap 7).
/// </summary>
/// <remarks>Threading: thread-safe.</remarks>
internal sealed class RawGrammarCache
{
    private readonly ConcurrentDictionary<string, Lazy<RawGrammar?>> _grammars = new(StringComparer.Ordinal);

    /// <summary>How many files were parsed (or tried).</summary>
    public int ParseCount => _grammars.Values.Count(l => l.IsValueCreated);

    /// <summary>
    /// The raw grammar of <paramref name="location"/>: read with <paramref name="readFile"/> and parsed on the first call
    /// only. An unreadable or broken file is traced and gives <see langword="null"/> (and is not tried again).
    /// </summary>
    public RawGrammar? Get(string location, Func<string, string?> readFile) =>
        _grammars.GetOrAdd(location, key => new Lazy<RawGrammar?>(() => Parse(key, readFile))).Value;

    private static RawGrammar? Parse(string location, Func<string, string?> readFile)
    {
        try
        {
            string? content = readFile(location);
            return content is null ? null : RawGrammarReader.ParseRawGrammar(content, location);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Unable to load and parse grammar from {location}: {e}");
            return null;
        }
    }
}

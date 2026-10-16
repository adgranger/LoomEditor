// Ported from microsoft/vscode: src/vs/editor/common/core/wordCharacterClassifier.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>The class of a character for word navigation (Monaco: <c>WordCharacterClass</c>).</summary>
internal enum WordCharacterClass
{
    Regular = 0,
    Whitespace = 1,
    WordSeparator = 2,
}

/// <summary>A word found by the Intl word segmenter (Monaco: <c>IntlWordSegmentData</c>).</summary>
internal readonly record struct IntlWordSegment(string Segment, int Index);

/// <summary>
/// Classifies characters as regular, whitespace or word separator (Monaco: <c>WordCharacterClassifier</c>).
/// <para>
/// Behavior difference: Intl word segmentation is not ported (there is no Native AOT-friendly equivalent of
/// <c>Intl.Segmenter</c>). <see cref="FindPrevIntlWordBeforeOrAtOffset"/> and <see cref="FindNextIntlWordAtOrAfterOffset"/>
/// always return <c>null</c>, and <see cref="IntlSegmenterLocales"/> is only kept so that word operations port unchanged.
/// </para>
/// </summary>
/// <remarks>Threading: immutable after construction; thread-safe and can be shared.</remarks>
internal sealed class WordCharacterClassifier : CharacterClassifier<WordCharacterClass>
{
    public WordCharacterClassifier(string wordSeparators, IReadOnlyList<string> intlSegmenterLocales)
        : base(WordCharacterClass.Regular)
    {
        IntlSegmenterLocales = intlSegmenterLocales;

        for (int i = 0, len = wordSeparators.Length; i < len; i++)
        {
            Set(wordSeparators[i], WordCharacterClass.WordSeparator);
        }

        Set(' ', WordCharacterClass.Whitespace);
        Set('\t', WordCharacterClass.Whitespace);
    }

    /// <summary>The locales of the <c>wordSegmenterLocales</c> option. Ignored: see the class remarks.</summary>
    public IReadOnlyList<string> IntlSegmenterLocales { get; }

    /// <summary>The last word segment starting at or before <paramref name="offset"/>; always <c>null</c> (no <c>Intl.Segmenter</c>).</summary>
    public IntlWordSegment? FindPrevIntlWordBeforeOrAtOffset(string line, int offset)
    {
        IntlWordSegment? candidate = null;
        foreach (var segment in GetIntlSegmenterWordsOnLine(line))
        {
            if (segment.Index > offset)
            {
                break;
            }

            candidate = segment;
        }

        return candidate;
    }

    /// <summary>The first word segment starting at or after <paramref name="offset"/>; always <c>null</c> (no <c>Intl.Segmenter</c>).</summary>
    public IntlWordSegment? FindNextIntlWordAtOrAfterOffset(string lineContent, int offset)
    {
        foreach (var segment in GetIntlSegmenterWordsOnLine(lineContent))
        {
            if (segment.Index < offset)
            {
                continue;
            }

            return segment;
        }

        return null;
    }

    // Behavior difference: Monaco segments the line with Intl.Segmenter when locales are set. Here there are never any segments.
    private IReadOnlyList<IntlWordSegment> GetIntlSegmenterWordsOnLine(string line)
    {
        if (IntlSegmenterLocales.Count == 0 || line.Length == 0)
        {
            return [];
        }

        return [];
    }
}

/// <summary>An LRU cache of 10 word classifiers (Monaco: <c>getMapForWordSeparators</c>).</summary>
/// <remarks>Threading: thread-safe (a lock): the cache is static, so several editors, or several tests, share it. Monaco has no lock (single thread).</remarks>
internal static class WordCharacterClassifierCache
{
    private const int Limit = 10;

    private static readonly Dictionary<string, LinkedListNode<(string Key, WordCharacterClassifier Value)>> Nodes = [];
    private static readonly LinkedList<(string Key, WordCharacterClassifier Value)> Order = new();
    private static readonly Lock Gate = new();

    public static WordCharacterClassifier Get(string wordSeparators, IReadOnlyList<string> intlSegmenterLocales)
    {
        lock (Gate)
        {
            return GetCore(wordSeparators, intlSegmenterLocales);
        }
    }

    private static WordCharacterClassifier GetCore(string wordSeparators, IReadOnlyList<string> intlSegmenterLocales)
    {
        var key = $"{wordSeparators}/{string.Join(',', intlSegmenterLocales)}";
        if (Nodes.TryGetValue(key, out var node))
        {
            Order.Remove(node);
            Order.AddLast(node);
            return node.Value.Value;
        }

        var result = new WordCharacterClassifier(wordSeparators, intlSegmenterLocales);
        Nodes[key] = Order.AddLast((key, result));
        if (Nodes.Count > Limit)
        {
            var oldest = Order.First!;
            Order.RemoveFirst();
            Nodes.Remove(oldest.Value.Key);
        }

        return result;
    }
}

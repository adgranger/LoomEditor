// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/completionModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Filters;
using LoomEditor.Core.Languages.Features;

namespace LoomEditor.View.Suggest;

/// <summary>
/// The sorted, filtered completion list (Monaco: <c>CompletionModel</c>). Items match the typed word (fuzzy), best first; a longer
/// word only re-scores the previous result, a shorter one re-scores everything.
/// Not ported: <c>stats</c>, <c>WordDistance</c> (the distance is always 0), clipboard, <c>snippetSuggestions: none</c>.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CompletionModel
{
    private static readonly FuzzyScorer PlainScorer = Fuzzy.Score;
    private static readonly FuzzyScorer GracefulScorer = Fuzzy.ScoreGracefulAggressive;

    private static readonly Comparer<SuggestItem> Plain = Comparer<SuggestItem>.Create(CompareCompletionItems);
    private static readonly Comparer<SuggestItem> SnippetsUp = Comparer<SuggestItem>.Create(CompareSnippetsUp);
    private static readonly Comparer<SuggestItem> SnippetsDown = Comparer<SuggestItem>.Create(CompareSnippetsDown);

    private readonly IReadOnlyList<SuggestItem> _items;
    private readonly int _column;
    private readonly SuggestModelOptions _options;
    private readonly FuzzyScoreOptions _fuzzyScoreOptions;
    private readonly FuzzyScorer? _scorerOverride;
    private readonly Comparer<SuggestItem> _snippetCompare;

    private CompletionLineContext _lineContext;
    private Refilter _refilterKind = Refilter.All;
    private List<SuggestItem>? _filteredItems;
    private Dictionary<ICompletionItemProvider, List<SuggestItem>> _itemsByProvider = [];

    /// <summary>Creates the model of <paramref name="items"/> requested at <paramref name="column"/>.</summary>
    public CompletionModel(
        IReadOnlyList<SuggestItem> items,
        int column,
        CompletionLineContext lineContext,
        SuggestModelOptions options,
        FuzzyScoreOptions? fuzzyScoreOptions = null)
        : this(items, column, lineContext, options, fuzzyScoreOptions, null)
    {
    }

    /// <summary>Test hook: like the public constructor, but <paramref name="scorer"/> replaces the scorer chosen from the options.</summary>
    internal CompletionModel(
        IReadOnlyList<SuggestItem> items,
        int column,
        CompletionLineContext lineContext,
        SuggestModelOptions options,
        FuzzyScoreOptions? fuzzyScoreOptions,
        FuzzyScorer? scorer)
    {
        _items = items;
        _column = column;
        _lineContext = lineContext;
        _options = options;
        _fuzzyScoreOptions = fuzzyScoreOptions ?? FuzzyScoreOptions.Default;
        _scorerOverride = scorer;
        _snippetCompare = options.Snippets switch
        {
            SnippetSuggestions.Top => SnippetsUp,
            SnippetSuggestions.Bottom => SnippetsDown,
            _ => Plain,
        };
    }

    private enum Refilter
    {
        Nothing,
        All,
        Incr,
    }

    /// <summary>What the user typed. A changed context schedules a refilter: incremental when the word got longer.</summary>
    public CompletionLineContext LineContext
    {
        get => _lineContext;
        set
        {
            if (_lineContext.LeadingLineContent != value.LeadingLineContent || _lineContext.CharacterCountDelta != value.CharacterCountDelta)
            {
                _refilterKind = _lineContext.CharacterCountDelta < value.CharacterCountDelta && _filteredItems is not null ? Refilter.Incr : Refilter.All;
                _lineContext = value;
            }
        }
    }

    /// <summary>The filtered, sorted items. The same list instance is returned until the context changes.</summary>
    public IReadOnlyList<SuggestItem> Items
    {
        get
        {
            EnsureCachedState();
            return _filteredItems!;
        }
    }

    /// <summary>The valid items by provider, before filtering by the typed word.</summary>
    public IReadOnlyDictionary<ICompletionItemProvider, IReadOnlyList<SuggestItem>> GetItemsByProvider()
    {
        EnsureCachedState();
        return _itemsByProvider.ToDictionary(p => p.Key, p => (IReadOnlyList<SuggestItem>)p.Value);
    }

    /// <summary>The providers whose list is marked incomplete.</summary>
    public ISet<ICompletionItemProvider> GetIncompleteProvider()
    {
        EnsureCachedState();
        var result = new HashSet<ICompletionItemProvider>();
        foreach ((ICompletionItemProvider provider, List<SuggestItem> items) in _itemsByProvider)
        {
            if (items.Count > 0 && items[0].Container.Incomplete)
            {
                result.Add(provider);
            }
        }

        return result;
    }

    /// <summary>Monaco: <c>(!filterGraceful || source.length &gt; 2000) ? fuzzyScore : fuzzyScoreGracefulAggressive</c>.</summary>
    internal static FuzzyScorer ChooseScorer(SuggestModelOptions options, int sourceCount)
        => !options.FilterGraceful || sourceCount > 2000 ? PlainScorer : GracefulScorer;

    private static int CompareCompletionItems(SuggestItem a, SuggestItem b)
    {
        if (a.Score[0] != b.Score[0])
        {
            return a.Score[0] > b.Score[0] ? -1 : 1;
        }

        if (a.Distance != b.Distance)
        {
            return a.Distance < b.Distance ? -1 : 1;
        }

        return a.Idx.CompareTo(b.Idx);
    }

    private static int CompareSnippetsDown(SuggestItem a, SuggestItem b)
    {
        if (a.Completion.Kind != b.Completion.Kind)
        {
            if (a.Completion.Kind == CompletionItemKind.Snippet)
            {
                return 1;
            }

            if (b.Completion.Kind == CompletionItemKind.Snippet)
            {
                return -1;
            }
        }

        return CompareCompletionItems(a, b);
    }

    private static int CompareSnippetsUp(SuggestItem a, SuggestItem b)
    {
        if (a.Completion.Kind != b.Completion.Kind)
        {
            if (a.Completion.Kind == CompletionItemKind.Snippet)
            {
                return -1;
            }

            if (b.Completion.Kind == CompletionItemKind.Snippet)
            {
                return 1;
            }
        }

        return CompareCompletionItems(a, b);
    }

    // JavaScript: string.slice(-n)
    private static string SliceFromEnd(string value, int n)
        => n > 0 ? value.Substring(value.Length - Math.Min(n, value.Length)) : value.Substring(Math.Min(-n, value.Length));

    private void EnsureCachedState()
    {
        if (_refilterKind != Refilter.Nothing)
        {
            CreateCachedState();
        }
    }

    private void CreateCachedState()
    {
        _itemsByProvider = [];

        (string leadingLineContent, int characterCountDelta) = _lineContext;
        var word = string.Empty;
        var wordLow = string.Empty;

        // incrementally filter less
        IReadOnlyList<SuggestItem> source = _refilterKind == Refilter.All ? _items : _filteredItems!;
        var target = new List<SuggestItem>();

        FuzzyScorer scoreFn = _scorerOverride ?? ChooseScorer(_options, source.Count);

        for (var i = 0; i < source.Count; i++)
        {
            SuggestItem item = source[i];
            if (item.IsInvalid)
            {
                continue; // SKIP invalid items
            }

            // keep all items by their provider
            if (_itemsByProvider.TryGetValue(item.Provider, out List<SuggestItem>? arr))
            {
                arr.Add(item);
            }
            else
            {
                _itemsByProvider[item.Provider] = [item];
            }

            // 'word' is the remainder of the current line that we filter and score against. In theory each suggestion uses a
            // different word, but in practice not - that's why we cache
            var overwriteBefore = item.Position.Column - item.EditStart.Column;
            var wordLen = overwriteBefore + characterCountDelta - (item.Position.Column - _column);
            if (word.Length != wordLen)
            {
                word = wordLen == 0 ? string.Empty : SliceFromEnd(leadingLineContent, wordLen);
                wordLow = word.ToLowerInvariant();
            }

            // remember the word against which this item was scored
            item.Word = word;

            if (wordLen == 0)
            {
                // nothing to score against: a const rank (-100 is out of the bounds of fuzzyScore) and the initial order
                item.Score = FuzzyScore.Default;
            }
            else
            {
                // skip word characters that are whitespace until we have hit the replace range (overwriteBefore)
                var wordPos = 0;
                while (wordPos < overwriteBefore && wordPos < word.Length && (word[wordPos] == ' ' || word[wordPos] == '\t'))
                {
                    wordPos += 1;
                }

                if (wordPos >= wordLen)
                {
                    // the whole word is skipped: same rules as having no word
                    item.Score = FuzzyScore.Default;
                }
                else if (item.FilterTextLow is not null)
                {
                    // a `filterText` must match the `word`; the label then only provides the highlights
                    string filterText = item.Completion.FilterText!;
                    int[]? match = scoreFn(word, wordLow, wordPos, filterText, item.FilterTextLow, 0, _fuzzyScoreOptions);
                    if (match is null)
                    {
                        continue; // NO match
                    }

                    if (string.Equals(filterText, item.TextLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Score = match;
                    }
                    else
                    {
                        // re-run the scorer on the label in the hope of a result BUT use the rank of the filterText match
                        item.Score = Fuzzy.AnyScore(word, wordLow, wordPos, item.TextLabel, item.LabelLow, 0);
                        item.Score[0] = match[0];
                    }
                }
                else
                {
                    int[]? match = scoreFn(word, wordLow, wordPos, item.TextLabel, item.LabelLow, 0, _fuzzyScoreOptions);
                    if (match is null)
                    {
                        continue; // NO match
                    }

                    item.Score = match;
                }
            }

            item.Idx = i;
            item.Distance = 0; // WordDistance.None
            target.Add(item);
        }

        // OrderBy is stable, like Monaco's Array.prototype.sort.
        _filteredItems = target.OrderBy(x => x, _snippetCompare).ToList();
        _refilterKind = Refilter.Nothing;
    }
}

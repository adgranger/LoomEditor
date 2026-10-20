// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsImpl.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.BracketPairs;

/// <summary>
/// The bracket API of a <see cref="TextModel"/> (Monaco: <c>BracketPairsTextModelPart</c>, the members of
/// <c>IBracketPairsTextModelPart</c> from <c>textModelBracketPairs.ts</c>). It answers from the bracket pair tree, which is built
/// lazily on the first request and kept up to date on text edits, token changes, background tokenization, language,
/// options and language configuration changes.
/// </summary>
/// <remarks>
/// <para>
/// Monaco uses the tree whenever the text is at most <see cref="MaxSupportedDocumentLength"/> characters long; it does not
/// tie the tree to colorization. For larger documents <see cref="FindMatchingBracketUp"/> and <see cref="MatchBracket"/> use the
/// legacy regex search over the syntax tokens (<c>BracketPairsTextModelPart.Legacy.cs</c>). The legacy branches of
/// <see cref="FindPrevBracket"/>, <see cref="FindNextBracket"/> and <see cref="FindEnclosingBrackets"/> are ported by P5-33:
/// until then they return <see langword="null"/>.
/// </para>
/// <para>
/// Differences from Monaco: <c>MutableDisposable&lt;IReference&lt;BracketPairsTree&gt;&gt;</c> and <c>createDisposableRef</c>
/// are a nullable tree field and the subscription to its <c>Changed</c> event; <c>maxDuration</c> is a
/// <see cref="TimeSpan"/> used only by the legacy paths, measured with <see cref="System.Diagnostics.Stopwatch"/> instead of
/// <c>Date.now()</c>; results that may be absent are nullable.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed partial class BracketPairsTextModelPart
{
    private readonly TextModel _textModel;
    private readonly LanguageConfigurationRegistry _languageConfigurationRegistry;

    private BracketPairsTree? _bracketPairsTree;
    private bool _bracketsRequested;

    /// <summary>Creates the part of <paramref name="textModel"/>.</summary>
    /// <param name="textModel">The model; its buffer and options may not exist yet.</param>
    /// <param name="languageConfigurationRegistry">Where the language configurations are looked up.</param>
    public BracketPairsTextModelPart(TextModel textModel, LanguageConfigurationRegistry languageConfigurationRegistry)
    {
        ArgumentNullException.ThrowIfNull(textModel);
        ArgumentNullException.ThrowIfNull(languageConfigurationRegistry);

        _textModel = textModel;
        _languageConfigurationRegistry = languageConfigurationRegistry;
    }

    /// <summary>
    /// The brackets of the text may have changed: the tree was built, dropped, or changed (Monaco: <c>onDidChange</c>).
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Documents longer than this (50 000 lines of 100 characters on average) are not parsed into a tree. An instance
    /// property, so that tests can force the legacy path.
    /// </summary>
    internal int MaxSupportedDocumentLength { get; set; } = 50_000 * 100;

    private bool CanBuildAst => _textModel.GetValueLength() <= MaxSupportedDocumentLength;

    #region TextModel events

    /// <summary>Rebuilds the tree if the configuration of <paramref name="e"/>'s language was used by it (or if every language changed).</summary>
    public void HandleLanguageConfigurationServiceChange(LanguageConfigurationChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (string.IsNullOrEmpty(e.LanguageId) || (_bracketPairsTree?.DidLanguageChange(e.LanguageId) ?? false))
        {
            ClearTree();
            UpdateBracketPairsTree();
        }
    }

    /// <summary>Rebuilds the tree: the options (tab size) change the minimum indentation of the pairs.</summary>
    public void HandleDidChangeOptions(ModelOptionsChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        ClearTree();
        UpdateBracketPairsTree();
    }

    /// <summary>Rebuilds the tree: the language decides the brackets.</summary>
    public void HandleDidChangeLanguage(ModelLanguageChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        ClearTree();
        UpdateBracketPairsTree();
    }

    /// <summary>Queues the text change in the tree.</summary>
    public void HandleDidChangeContent(ModelContentChangedEventArgs e)
    {
        _bracketPairsTree?.HandleContentChanged(e);
    }

    /// <summary>Tells the tree that the background tokenization may have completed.</summary>
    public void HandleDidChangeBackgroundTokenizationState()
    {
        _bracketPairsTree?.HandleDidChangeBackgroundTokenizationState();
    }

    /// <summary>Queues the token change in the tree.</summary>
    public void HandleDidChangeTokens(ModelTokensChangedEventArgs e)
    {
        _bracketPairsTree?.HandleDidChangeTokens(e);
    }

    #endregion

    private void ClearTree()
    {
        if (_bracketPairsTree is { } tree)
        {
            tree.Changed -= OnTreeChanged;
            _bracketPairsTree = null;
        }
    }

    private void OnTreeChanged(object? sender, EventArgs e) => Changed?.Invoke(this, EventArgs.Empty);

    private void UpdateBracketPairsTree()
    {
        if (_bracketsRequested && CanBuildAst)
        {
            if (_bracketPairsTree is null)
            {
                var tree = new BracketPairsTree(_textModel, _languageConfigurationRegistry.GetLanguageConfiguration);
                tree.Changed += OnTreeChanged;
                _bracketPairsTree = tree;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
        else if (_bracketPairsTree is not null)
        {
            ClearTree();
            // Important: Don't raise if there was no change!
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RequestBrackets()
    {
        _bracketsRequested = true;
        UpdateBracketPairsTree();
    }

    /// <summary>All the bracket pairs that intersect <paramref name="range"/>, sorted by their start position.</summary>
    public CallbackIterable<BracketPairInfo> GetBracketPairsInRange(Range range)
    {
        RequestBrackets();
        return _bracketPairsTree?.GetBracketPairsInRange(range, false).Map(static pair => (BracketPairInfo)pair)
            ?? CallbackIterable<BracketPairInfo>.Empty;
    }

    /// <summary>Like <see cref="GetBracketPairsInRange"/>, with the minimum indentation inside each pair.</summary>
    public CallbackIterable<BracketPairWithMinIndentationInfo> GetBracketPairsInRangeWithMinIndentation(Range range)
    {
        RequestBrackets();
        return _bracketPairsTree?.GetBracketPairsInRange(range, true) ?? CallbackIterable<BracketPairWithMinIndentationInfo>.Empty;
    }

    /// <summary>The brackets that intersect <paramref name="range"/> with their nesting levels.</summary>
    /// <param name="range">The range to search.</param>
    /// <param name="onlyColorizedBrackets">Whether to skip the pairs that are not colorized.</param>
    public CallbackIterable<BracketInfo> GetBracketsInRange(Range range, bool onlyColorizedBrackets = false)
    {
        RequestBrackets();
        return _bracketPairsTree?.GetBracketsInRange(range, onlyColorizedBrackets) ?? CallbackIterable<BracketInfo>.Empty;
    }

    /// <summary>
    /// Whether an unmatched closing bracket that could close <paramref name="openingBracket"/> follows
    /// <paramref name="position"/>. Always false until the tokens of the whole model are accurate.
    /// </summary>
    /// <param name="position">The position to search from.</param>
    /// <param name="openingBracket">The text of an opening bracket of the language at <paramref name="position"/>.</param>
    public bool HasUnmatchedClosingBracketAfter(Position position, string openingBracket)
    {
        ArgumentNullException.ThrowIfNull(openingBracket);

        position = _textModel.ValidatePosition(position);
        var languageId = _textModel.Tokenization.GetLanguageIdAtPosition(position.LineNumber, position.Column);
        var openingBracketInfo = _languageConfigurationRegistry
            .GetLanguageConfiguration(languageId)
            .BracketsNew
            .GetOpeningBracketInfo(openingBracket);
        if (openingBracketInfo is null)
        {
            return false;
        }

        RequestBrackets();
        return _bracketPairsTree?.HasUnmatchedClosingBracketAfter(position, openingBracketInfo) ?? false;
    }

    /// <summary>
    /// The range of the opening bracket of the innermost pair at <paramref name="position"/> that <paramref name="bracket"/> closes,
    /// or <see langword="null"/>.
    /// </summary>
    /// <param name="bracket">The text of a closing bracket.</param>
    /// <param name="position">The position to search from.</param>
    /// <param name="maxDuration">The time budget of the legacy search; <see langword="null"/> for none.</param>
    public Range? FindMatchingBracketUp(string bracket, Position position, TimeSpan? maxDuration = null)
    {
        ArgumentNullException.ThrowIfNull(bracket);

        var validated = _textModel.ValidatePosition(position);
        var languageId = _textModel.Tokenization.GetLanguageIdAtPosition(validated.LineNumber, validated.Column);

        if (CanBuildAst)
        {
            var closingBracketInfo = _languageConfigurationRegistry
                .GetLanguageConfiguration(languageId)
                .BracketsNew
                .GetClosingBracketInfo(bracket);
            if (closingBracketInfo is null)
            {
                return null;
            }

            var bracketPair = GetBracketPairsInRange(Range.Collapsed(position))
                .FindLast(b => closingBracketInfo.Closes(b.OpeningBracketInfo));
            return bracketPair?.OpeningBracketRange;
        }

        // Fallback to old bracket matching code:
        string lowerBracket = bracket.ToLowerInvariant();

        var bracketsSupport = _languageConfigurationRegistry.GetLanguageConfiguration(languageId).Brackets;
        if (bracketsSupport is null)
        {
            return null;
        }

        if (!bracketsSupport.TextIsBracket.TryGetValue(lowerBracket, out var data))
        {
            return null;
        }

        return StripBracketSearchCanceled(FindMatchingBracketUpLegacy(data, validated, CreateTimeBasedContinueBracketSearchPredicate(maxDuration)));
    }

    /// <summary>
    /// The ranges of the opening and closing brackets (in this order) of the pair that has a bracket at
    /// <paramref name="position"/>, or <see langword="null"/>.
    /// </summary>
    /// <param name="position">The position to match.</param>
    /// <param name="maxDuration">The time budget of the legacy search; <see langword="null"/> for none.</param>
    public (Range Opening, Range Closing)? MatchBracket(Position position, TimeSpan? maxDuration = null)
    {
        if (CanBuildAst)
        {
            var bracketPair = GetBracketPairsInRange(Range.Collapsed(position))
                .Filter(item => item.ClosingBracketRange is not null
                    && (item.OpeningBracketRange.Contains(position) || item.ClosingBracketRange.Value.Contains(position)))
                .FindLastMaxBy((a, b) => Range.CompareUsingStarts(BracketAt(a, position), BracketAt(b, position)));
            return bracketPair is null ? null : (bracketPair.OpeningBracketRange, bracketPair.ClosingBracketRange!.Value);
        }

        // Fallback to old bracket matching code:
        var continueSearchPredicate = CreateTimeBasedContinueBracketSearchPredicate(maxDuration);
        return MatchBracketLegacy(_textModel.ValidatePosition(position), continueSearchPredicate);
    }

    private static Range BracketAt(BracketPairInfo item, Position position) =>
        item.OpeningBracketRange.Contains(position) ? item.OpeningBracketRange : item.ClosingBracketRange!.Value;

    /// <summary>The last bracket that starts before <paramref name="position"/>, or <see langword="null"/>.</summary>
    public FoundBracket? FindPrevBracket(Position position)
    {
        position = _textModel.ValidatePosition(position);

        if (CanBuildAst)
        {
            RequestBrackets();
            return _bracketPairsTree?.GetFirstBracketBefore(position);
        }

        // Fallback to the old bracket matching code: ported by P5-33.
        return null;
    }

    /// <summary>The first bracket that starts after <paramref name="position"/>, or <see langword="null"/>.</summary>
    public FoundBracket? FindNextBracket(Position position)
    {
        position = _textModel.ValidatePosition(position);

        if (CanBuildAst)
        {
            RequestBrackets();
            return _bracketPairsTree?.GetFirstBracketAfter(position);
        }

        // Fallback to the old bracket matching code: ported by P5-33.
        return null;
    }

    /// <summary>
    /// The ranges of the opening and closing brackets (in this order) of the innermost closed pair that strictly contains
    /// <paramref name="position"/>, or <see langword="null"/>.
    /// </summary>
    /// <param name="position">The position to search from.</param>
    /// <param name="maxDuration">The time budget of the legacy search; <see langword="null"/> for none.</param>
    public (Range Opening, Range Closing)? FindEnclosingBrackets(Position position, TimeSpan? maxDuration = null)
    {
        position = _textModel.ValidatePosition(position);

        if (CanBuildAst)
        {
            var range = Range.Collapsed(position);
            var bracketPair = GetBracketPairsInRange(range)
                .FindLast(item => item.ClosingBracketRange is not null && item.Range.StrictContains(range));
            return bracketPair is null ? null : (bracketPair.OpeningBracketRange, bracketPair.ClosingBracketRange!.Value);
        }

        // Fallback to the old bracket matching code: ported by P5-33.
        _ = maxDuration;
        return null;
    }
}

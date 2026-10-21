// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findState.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>The state every find part shares: search and replace strings, toggles, scope, "n of m" (Monaco: <c>FindReplaceState</c>).</summary>
/// <remarks>
/// Behavior difference: not generic and without <c>filters</c> (a notebook concept); <c>Changed</c> replaces
/// <c>onFindReplaceStateChange</c>; there is nothing to dispose.
/// Threading: UI thread only.
/// </remarks>
internal sealed class FindReplaceState
{
    private bool _isRegex;
    private bool _wholeWord;
    private bool _matchCase;
    private bool _preserveCase;
    private FindOptionOverride _isRegexOverride;
    private FindOptionOverride _wholeWordOverride;
    private FindOptionOverride _matchCaseOverride;
    private FindOptionOverride _preserveCaseOverride;
    private bool _loop = true;

    /// <summary>Raised when something changed, with what changed.</summary>
    public event EventHandler<FindReplaceStateChangedEventArgs>? Changed;

    public string SearchString { get; private set; } = "";

    public string ReplaceString { get; private set; } = "";

    public bool IsRevealed { get; private set; }

    public bool IsReplaceRevealed { get; private set; }

    /// <summary>The effective value: the override if there is one, else the stored value.</summary>
    public bool IsRegex => EffectiveOptionValue(_isRegexOverride, _isRegex);

    public bool WholeWord => EffectiveOptionValue(_wholeWordOverride, _wholeWord);

    public bool MatchCase => EffectiveOptionValue(_matchCaseOverride, _matchCase);

    public bool PreserveCase => EffectiveOptionValue(_preserveCaseOverride, _preserveCase);

    public bool ActualIsRegex => _isRegex;

    public bool ActualWholeWord => _wholeWord;

    public bool ActualMatchCase => _matchCase;

    public bool ActualPreserveCase => _preserveCase;

    public IReadOnlyList<Range>? SearchScope { get; private set; }

    public int MatchesPosition { get; private set; }

    public int MatchesCount { get; private set; }

    public Range? CurrentMatch { get; private set; }

    public bool IsSearching { get; private set; }

    private static bool EffectiveOptionValue(FindOptionOverride @override, bool value) => @override switch
    {
        FindOptionOverride.True => true,
        FindOptionOverride.False => false,
        _ => value,
    };

    /// <summary>Updates the "n of m" information; <paramref name="setCurrentMatch"/> <c>false</c> keeps the current match (Monaco: <c>undefined</c>).</summary>
    public void ChangeMatchInfo(int matchesPosition, int matchesCount, Range? currentMatch, bool setCurrentMatch)
    {
        var matchesPositionChanged = false;
        var matchesCountChanged = false;
        var currentMatchChanged = false;

        if (matchesCount == 0)
        {
            matchesPosition = 0;
        }

        if (matchesPosition > matchesCount)
        {
            matchesPosition = matchesCount;
        }

        if (MatchesPosition != matchesPosition)
        {
            MatchesPosition = matchesPosition;
            matchesPositionChanged = true;
        }

        if (MatchesCount != matchesCount)
        {
            MatchesCount = matchesCount;
            matchesCountChanged = true;
        }

        if (setCurrentMatch && CurrentMatch != currentMatch)
        {
            CurrentMatch = currentMatch;
            currentMatchChanged = true;
        }

        if (matchesPositionChanged || matchesCountChanged || currentMatchChanged)
        {
            Changed?.Invoke(this, new FindReplaceStateChangedEventArgs
            {
                MatchesPosition = matchesPositionChanged,
                MatchesCount = matchesCountChanged,
                CurrentMatch = currentMatchChanged,
            });
        }
    }

    /// <summary>Applies <paramref name="newState"/> and raises <see cref="Changed"/> if something changed.</summary>
    public void Change(NewFindReplaceState newState, bool moveCursor, bool updateHistory = true)
    {
        ArgumentNullException.ThrowIfNull(newState);

        var somethingChanged = false;
        var searchStringChanged = false;
        var replaceStringChanged = false;
        var isRevealedChanged = false;
        var isReplaceRevealedChanged = false;
        var searchScopeChanged = false;
        var loopChanged = false;
        var isSearchingChanged = false;

        bool oldEffectiveIsRegex = IsRegex;
        bool oldEffectiveWholeWords = WholeWord;
        bool oldEffectiveMatchCase = MatchCase;
        bool oldEffectivePreserveCase = PreserveCase;

        if (newState.SearchString is { } searchString && SearchString != searchString)
        {
            SearchString = searchString;
            searchStringChanged = true;
            somethingChanged = true;
        }

        if (newState.ReplaceString is { } replaceString && ReplaceString != replaceString)
        {
            ReplaceString = replaceString;
            replaceStringChanged = true;
            somethingChanged = true;
        }

        if (newState.IsRevealed is { } isRevealed && IsRevealed != isRevealed)
        {
            IsRevealed = isRevealed;
            isRevealedChanged = true;
            somethingChanged = true;
        }

        if (newState.IsReplaceRevealed is { } isReplaceRevealed && IsReplaceRevealed != isReplaceRevealed)
        {
            IsReplaceRevealed = isReplaceRevealed;
            isReplaceRevealedChanged = true;
            somethingChanged = true;
        }

        _isRegex = newState.IsRegex ?? _isRegex;
        _wholeWord = newState.WholeWord ?? _wholeWord;
        _matchCase = newState.MatchCase ?? _matchCase;
        _preserveCase = newState.PreserveCase ?? _preserveCase;

        // Monaco: `!newScope?.every(n => this._searchScope?.some(e => !equals(e, n)))`: a null scope always replaces;
        // ranges replace unless every new range differs from some existing one (an empty list never replaces).
        if (newState.HasSearchScope
            && (newState.SearchScope is null
                || !newState.SearchScope.All(n => SearchScope is not null && SearchScope.Any(e => e != n))))
        {
            SearchScope = newState.SearchScope;
            searchScopeChanged = true;
            somethingChanged = true;
        }

        if (newState.Loop is { } loop && _loop != loop)
        {
            _loop = loop;
            loopChanged = true;
            somethingChanged = true;
        }

        if (newState.IsSearching is { } isSearching && IsSearching != isSearching)
        {
            IsSearching = isSearching;
            isSearchingChanged = true;
            somethingChanged = true;
        }

        // Overrides get set when they explicitly come in and get reset anytime something else changes
        _isRegexOverride = newState.IsRegexOverride ?? FindOptionOverride.NotSet;
        _wholeWordOverride = newState.WholeWordOverride ?? FindOptionOverride.NotSet;
        _matchCaseOverride = newState.MatchCaseOverride ?? FindOptionOverride.NotSet;
        _preserveCaseOverride = newState.PreserveCaseOverride ?? FindOptionOverride.NotSet;

        var isRegexChanged = oldEffectiveIsRegex != IsRegex;
        var wholeWordChanged = oldEffectiveWholeWords != WholeWord;
        var matchCaseChanged = oldEffectiveMatchCase != MatchCase;
        var preserveCaseChanged = oldEffectivePreserveCase != PreserveCase;
        somethingChanged |= isRegexChanged || wholeWordChanged || matchCaseChanged || preserveCaseChanged;

        if (somethingChanged)
        {
            Changed?.Invoke(this, new FindReplaceStateChangedEventArgs
            {
                MoveCursor = moveCursor,
                UpdateHistory = updateHistory,
                SearchString = searchStringChanged,
                ReplaceString = replaceStringChanged,
                IsRevealed = isRevealedChanged,
                IsReplaceRevealed = isReplaceRevealedChanged,
                IsRegex = isRegexChanged,
                WholeWord = wholeWordChanged,
                MatchCase = matchCaseChanged,
                PreserveCase = preserveCaseChanged,
                SearchScope = searchScopeChanged,
                Loop = loopChanged,
                IsSearching = isSearchingChanged,
            });
        }
    }

    public bool CanNavigateBack() => CanNavigateInLoop() || MatchesPosition != 1;

    public bool CanNavigateForward() => CanNavigateInLoop() || MatchesPosition < MatchesCount;

    private bool CanNavigateInLoop() => _loop || MatchesCount >= FindConstants.MatchesLimit;
}

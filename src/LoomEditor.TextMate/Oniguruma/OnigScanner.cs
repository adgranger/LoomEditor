// Ported from microsoft/vscode-oniguruma: src/onig.cc
// Upstream commit: 716aeaa229e4ae2e3b0057377b55743e9a3e995b  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Oniguruma;

/// <summary>
/// vscode-oniguruma's <c>OnigScanner</c> (<c>src/index.ts</c>) and its native side (<c>createOnigScanner</c>,
/// <c>freeOnigScanner</c>, <c>toOnigOption</c>, <c>searchOnigRegExp</c>, <c>findNextOnigScannerMatch</c>).
/// </summary>
/// <remarks>
/// <para>
/// Always the per-regex path of <c>findNextOnigScannerMatch</c> (ADR 0007, decision 3): every regex is searched from
/// the start position (with its per-string cache), the earliest match wins, the lowest pattern index wins a tie, and
/// the loop stops at a match that starts at the start position. vscode-oniguruma uses <c>onig_regset_search</c> for
/// strings shorter than 1000 UTF-8 bytes; it returns the same match.
/// </para>
/// <para>Threading: single-threaded; used and disposed by the thread that created it. It owns its regexes.</para>
/// </remarks>
internal sealed class OnigScanner : IOnigScanner
{
    private readonly OnigRegex[] _regexes;
    private readonly SearchCache[] _caches;

    /// <summary><c>createOnigScanner</c>: compiles every pattern.</summary>
    /// <exception cref="OnigException">A pattern does not compile (the regexes compiled so far are freed).</exception>
    public OnigScanner(IReadOnlyList<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        _regexes = new OnigRegex[patterns.Count];
        _caches = new SearchCache[patterns.Count];
        for (int i = 0; i < patterns.Count; i++)
        {
            try
            {
                _regexes[i] = new OnigRegex(patterns[i]);
            }
            catch
            {
                // parsing this regex failed, so clean up all the ones created so far
                for (int j = 0; j < i; j++)
                {
                    _regexes[j].Dispose();
                }

                throw;
            }
        }
    }

    /// <inheritdoc />
    public IOnigMatch? FindNextMatchSync(IOnigString str, int startPosition, FindOption options = FindOption.None)
    {
        ArgumentNullException.ThrowIfNull(str);
        if (str is not OnigString onigString)
        {
            throw new ArgumentException("The string must come from the same IOnigLib.", nameof(str));
        }

        return FindNextMatch(onigString, startPosition, options);
    }

    /// <inheritdoc />
    public IOnigMatch? FindNextMatchSync(string str, int startPosition, FindOption options = FindOption.None)
    {
        using var onigString = new OnigString(str);
        return FindNextMatch(onigString, startPosition, options);
    }

    /// <summary><c>freeOnigScanner</c>.</summary>
    public void Dispose()
    {
        foreach (OnigRegex regex in _regexes)
        {
            regex.Dispose();
        }
    }

    // toOnigOption. DebugCall is ignored.
    private static OnigSearchOptions ToOnigOption(FindOption option)
    {
        OnigSearchOptions onigOption = OnigSearchOptions.None;
        if ((option & FindOption.NotBeginString) != 0)
        {
            onigOption |= OnigSearchOptions.NotBeginString;
        }

        if ((option & FindOption.NotEndString) != 0)
        {
            onigOption |= OnigSearchOptions.NotEndString;
        }

        if ((option & FindOption.NotBeginPosition) != 0)
        {
            onigOption |= OnigSearchOptions.NotBeginPosition;
        }

        return onigOption;
    }

    // _findNextMatchSync + findNextOnigScannerMatch (per-regex branch). The start position is clamped to the string
    // like convertUtf16OffsetToUtf8 does.
    private OnigMatch? FindNextMatch(OnigString str, int startPosition, FindOption options)
    {
        int position = Math.Clamp(startPosition, 0, str.Content.Length);
        OnigSearchOptions onigOption = ToOnigOption(options);

        int bestLocation = 0;
        int bestResultIndex = 0;
        OnigCaptureIndex[]? bestResult = null;
        for (int i = 0; i < _regexes.Length; i++)
        {
            OnigCaptureIndex[]? result = SearchOnigRegExp(i, str, position, onigOption);
            if (result is not null && result.Length > 0)
            {
                int location = result[0].Start;

                if (bestResult is null || location < bestLocation)
                {
                    bestLocation = location;
                    bestResult = result;
                    bestResultIndex = i;
                }

                if (location == position)
                {
                    break;
                }
            }
        }

        return bestResult is null ? null : new OnigMatch(bestResultIndex, bestResult);
    }

    // searchOnigRegExp: the per-regex cache.
    private OnigCaptureIndex[]? SearchOnigRegExp(int index, OnigString str, int position, OnigSearchOptions onigOption)
    {
        OnigRegex regex = _regexes[index];
        if (regex.HasGAnchor)
        {
            // Should not use caching, because the regular expression
            // targets the current search position (\G)
            return regex.Search(str.Content, position, onigOption);
        }

        ref SearchCache cache = ref _caches[index];
        if (cache.StringId == str.Id && cache.Option == onigOption && cache.Position <= position)
        {
            if (cache.Result is null)
            {
                // last time there was no match
                return null;
            }

            if (cache.Result[0].Start >= position)
            {
                // last time there was a match and it occured after position
                return cache.Result;
            }
        }

        cache.StringId = str.Id;
        cache.Position = position;
        cache.Option = onigOption;
        cache.Result = regex.Search(str.Content, position, onigOption);
        return cache.Result;
    }

    // The lastSearch* fields of OnigRegExp; Result is null when the last search did not match (lastSearchMatched).
    private struct SearchCache
    {
        public int StringId;
        public int Position;
        public OnigSearchOptions Option;
        public OnigCaptureIndex[]? Result;
    }
}

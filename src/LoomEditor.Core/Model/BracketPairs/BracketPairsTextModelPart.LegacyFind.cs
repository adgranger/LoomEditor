// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsImpl.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.BracketPairs;

// The legacy search of the previous, next and enclosing brackets over the syntax tokens.
internal sealed partial class BracketPairsTextModelPart
{
    // Monaco: `findPrevBracket`, after the tree branch.
    private FoundBracket? FindPrevBracketLegacy(Position position)
    {
        string? languageId = null;
        RichEditBrackets? modeBrackets = null;
        LanguageBracketsConfiguration? bracketConfig = null;

        void SetLanguage(string id)
        {
            languageId = id;
            var configuration = _languageConfigurationRegistry.GetLanguageConfiguration(id);
            modeBrackets = configuration.Brackets;
            bracketConfig = configuration.BracketsNew;
        }

        FoundBracket? Search(int lineNumber, string lineText, int start, int end)
        {
            var r = BracketsUtils.FindPrevBracketInRange(modeBrackets!.ReversedRegex, lineNumber, lineText, start, end);
            return r is { } range ? ToFoundBracket(bracketConfig!, range) : null;
        }

        for (int lineNumber = position.LineNumber; lineNumber >= 1; lineNumber--)
        {
            var lineTokens = _textModel.Tokenization.GetLineTokens(lineNumber);
            int tokenCount = lineTokens.GetCount();
            string lineText = _textModel.GetLineContent(lineNumber);

            int tokenIndex = tokenCount - 1;
            int searchStartOffset = lineText.Length;
            int searchEndOffset = lineText.Length;
            if (lineNumber == position.LineNumber)
            {
                tokenIndex = lineTokens.FindTokenIndexAtOffset(position.Column - 1);
                searchStartOffset = position.Column - 1;
                searchEndOffset = position.Column - 1;
                string tokenLanguageId = lineTokens.GetLanguageId(tokenIndex);
                if (languageId != tokenLanguageId)
                {
                    SetLanguage(tokenLanguageId);
                }
            }

            bool prevSearchInToken = true;
            for (; tokenIndex >= 0; tokenIndex--)
            {
                string tokenLanguageId = lineTokens.GetLanguageId(tokenIndex);

                if (languageId != tokenLanguageId)
                {
                    // language id change!
                    if (modeBrackets is not null && bracketConfig is not null && prevSearchInToken && searchStartOffset != searchEndOffset)
                    {
                        if (Search(lineNumber, lineText, searchStartOffset, searchEndOffset) is { } found)
                        {
                            return found;
                        }

                        prevSearchInToken = false;
                    }

                    SetLanguage(tokenLanguageId);
                }

                bool searchInToken = modeBrackets is not null && !ScopedLineTokens.IgnoreBracketsInToken(lineTokens.GetStandardTokenType(tokenIndex));

                if (searchInToken)
                {
                    // this token should be searched
                    if (prevSearchInToken)
                    {
                        // the previous token should be searched, simply extend searchStartOffset
                        searchStartOffset = lineTokens.GetStartOffset(tokenIndex);
                    }
                    else
                    {
                        // the previous token should not be searched
                        searchStartOffset = lineTokens.GetStartOffset(tokenIndex);
                        searchEndOffset = lineTokens.GetEndOffset(tokenIndex);
                    }
                }
                else if (bracketConfig is not null && modeBrackets is not null && prevSearchInToken && searchStartOffset != searchEndOffset)
                {
                    // this token should not be searched
                    if (Search(lineNumber, lineText, searchStartOffset, searchEndOffset) is { } found)
                    {
                        return found;
                    }
                }

                prevSearchInToken = searchInToken;
            }

            if (bracketConfig is not null && modeBrackets is not null && prevSearchInToken && searchStartOffset != searchEndOffset)
            {
                if (Search(lineNumber, lineText, searchStartOffset, searchEndOffset) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }

    // Monaco: `findNextBracket`, after the tree branch.
    private FoundBracket? FindNextBracketLegacy(Position position)
    {
        int lineCount = _textModel.LineCount;

        string? languageId = null;
        RichEditBrackets? modeBrackets = null;
        LanguageBracketsConfiguration? bracketConfig = null;

        void SetLanguage(string id)
        {
            languageId = id;
            var configuration = _languageConfigurationRegistry.GetLanguageConfiguration(id);
            modeBrackets = configuration.Brackets;
            bracketConfig = configuration.BracketsNew;
        }

        FoundBracket? Search(int lineNumber, string lineText, int start, int end)
        {
            var r = BracketsUtils.FindNextBracketInRange(modeBrackets!.ForwardRegex, lineNumber, lineText, start, end);
            return r is { } range ? ToFoundBracket(bracketConfig!, range) : null;
        }

        for (int lineNumber = position.LineNumber; lineNumber <= lineCount; lineNumber++)
        {
            var lineTokens = _textModel.Tokenization.GetLineTokens(lineNumber);
            int tokenCount = lineTokens.GetCount();
            string lineText = _textModel.GetLineContent(lineNumber);

            int tokenIndex = 0;
            int searchStartOffset = 0;
            int searchEndOffset = 0;
            if (lineNumber == position.LineNumber)
            {
                tokenIndex = lineTokens.FindTokenIndexAtOffset(position.Column - 1);
                searchStartOffset = position.Column - 1;
                searchEndOffset = position.Column - 1;
                string tokenLanguageId = lineTokens.GetLanguageId(tokenIndex);
                if (languageId != tokenLanguageId)
                {
                    SetLanguage(tokenLanguageId);
                }
            }

            bool prevSearchInToken = true;
            for (; tokenIndex < tokenCount; tokenIndex++)
            {
                string tokenLanguageId = lineTokens.GetLanguageId(tokenIndex);

                if (languageId != tokenLanguageId)
                {
                    // language id change!
                    if (bracketConfig is not null && modeBrackets is not null && prevSearchInToken && searchStartOffset != searchEndOffset)
                    {
                        if (Search(lineNumber, lineText, searchStartOffset, searchEndOffset) is { } found)
                        {
                            return found;
                        }

                        prevSearchInToken = false;
                    }

                    SetLanguage(tokenLanguageId);
                }

                bool searchInToken = modeBrackets is not null && !ScopedLineTokens.IgnoreBracketsInToken(lineTokens.GetStandardTokenType(tokenIndex));
                if (searchInToken)
                {
                    // this token should be searched
                    if (prevSearchInToken)
                    {
                        // the previous token should be searched, simply extend searchEndOffset
                        searchEndOffset = lineTokens.GetEndOffset(tokenIndex);
                    }
                    else
                    {
                        // the previous token should not be searched
                        searchStartOffset = lineTokens.GetStartOffset(tokenIndex);
                        searchEndOffset = lineTokens.GetEndOffset(tokenIndex);
                    }
                }
                else if (bracketConfig is not null && modeBrackets is not null && prevSearchInToken && searchStartOffset != searchEndOffset)
                {
                    // this token should not be searched
                    if (Search(lineNumber, lineText, searchStartOffset, searchEndOffset) is { } found)
                    {
                        return found;
                    }
                }

                prevSearchInToken = searchInToken;
            }

            if (bracketConfig is not null && modeBrackets is not null && prevSearchInToken && searchStartOffset != searchEndOffset)
            {
                if (Search(lineNumber, lineText, searchStartOffset, searchEndOffset) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }

    // Monaco: `findEnclosingBrackets`, after the tree branch.
    private (Range Opening, Range Closing)? FindEnclosingBracketsLegacy(Position position, TimeSpan? maxDuration)
    {
        var continueSearchPredicate = CreateTimeBasedContinueBracketSearchPredicate(maxDuration);
        int lineCount = _textModel.LineCount;
        var savedCounts = new Dictionary<string, int[]>();

        int[] counts = [];
        void ResetCounts(string languageId, RichEditBrackets? modeBrackets)
        {
            if (!savedCounts.TryGetValue(languageId, out var saved))
            {
                saved = new int[modeBrackets?.Brackets.Count ?? 0];
                savedCounts[languageId] = saved;
            }

            counts = saved;
        }

        int totalCallCount = 0;
        SearchResult<(Range Opening, Range Closing)> SearchInRange(RichEditBrackets modeBrackets, int lineNumber, string lineText, int searchStartOffset, int searchEndOffset)
        {
            while (true)
            {
                if (++totalCallCount % 100 == 0 && !continueSearchPredicate())
                {
                    return SearchResult<(Range Opening, Range Closing)>.CanceledResult;
                }

                if (BracketsUtils.FindNextBracketInRange(modeBrackets.ForwardRegex, lineNumber, lineText, searchStartOffset, searchEndOffset) is not { } r)
                {
                    break;
                }

                string hitText = lineText.Substring(r.StartColumn - 1, r.EndColumn - r.StartColumn).ToLowerInvariant();
                if (modeBrackets.TextIsBracket.TryGetValue(hitText, out var bracket))
                {
                    if (bracket.IsOpen(hitText))
                    {
                        counts[bracket.Index]++;
                    }
                    else if (bracket.IsClose(hitText))
                    {
                        counts[bracket.Index]--;
                    }

                    if (counts[bracket.Index] == -1)
                    {
                        return MatchFoundBracket(r, bracket, false, continueSearchPredicate);
                    }
                }

                searchStartOffset = r.EndColumn - 1;
            }

            return default;
        }

        string? languageId = null;
        RichEditBrackets? modeBrackets = null;
        for (int lineNumber = position.LineNumber; lineNumber <= lineCount; lineNumber++)
        {
            var lineTokens = _textModel.Tokenization.GetLineTokens(lineNumber);
            int tokenCount = lineTokens.GetCount();
            string lineText = _textModel.GetLineContent(lineNumber);

            int tokenIndex = 0;
            int searchStartOffset = 0;
            int searchEndOffset = 0;
            if (lineNumber == position.LineNumber)
            {
                tokenIndex = lineTokens.FindTokenIndexAtOffset(position.Column - 1);
                searchStartOffset = position.Column - 1;
                searchEndOffset = position.Column - 1;
                string tokenLanguageId = lineTokens.GetLanguageId(tokenIndex);
                if (languageId != tokenLanguageId)
                {
                    languageId = tokenLanguageId;
                    modeBrackets = _languageConfigurationRegistry.GetLanguageConfiguration(languageId).Brackets;
                    ResetCounts(languageId, modeBrackets);
                }
            }

            bool prevSearchInToken = true;
            for (; tokenIndex < tokenCount; tokenIndex++)
            {
                string tokenLanguageId = lineTokens.GetLanguageId(tokenIndex);

                if (languageId != tokenLanguageId)
                {
                    // language id change!
                    if (modeBrackets is not null && prevSearchInToken && searchStartOffset != searchEndOffset)
                    {
                        var r = SearchInRange(modeBrackets, lineNumber, lineText, searchStartOffset, searchEndOffset);
                        if (r.IsHit)
                        {
                            return StripBracketSearchCanceled(r);
                        }

                        prevSearchInToken = false;
                    }

                    languageId = tokenLanguageId;
                    modeBrackets = _languageConfigurationRegistry.GetLanguageConfiguration(languageId).Brackets;
                    ResetCounts(languageId, modeBrackets);
                }

                bool searchInToken = modeBrackets is not null && !ScopedLineTokens.IgnoreBracketsInToken(lineTokens.GetStandardTokenType(tokenIndex));
                if (searchInToken)
                {
                    // this token should be searched
                    if (prevSearchInToken)
                    {
                        // the previous token should be searched, simply extend searchEndOffset
                        searchEndOffset = lineTokens.GetEndOffset(tokenIndex);
                    }
                    else
                    {
                        // the previous token should not be searched
                        searchStartOffset = lineTokens.GetStartOffset(tokenIndex);
                        searchEndOffset = lineTokens.GetEndOffset(tokenIndex);
                    }
                }
                else if (modeBrackets is not null && prevSearchInToken && searchStartOffset != searchEndOffset)
                {
                    // this token should not be searched
                    var r = SearchInRange(modeBrackets, lineNumber, lineText, searchStartOffset, searchEndOffset);
                    if (r.IsHit)
                    {
                        return StripBracketSearchCanceled(r);
                    }
                }

                prevSearchInToken = searchInToken;
            }

            if (modeBrackets is not null && prevSearchInToken && searchStartOffset != searchEndOffset)
            {
                var r = SearchInRange(modeBrackets, lineNumber, lineText, searchStartOffset, searchEndOffset);
                if (r.IsHit)
                {
                    return StripBracketSearchCanceled(r);
                }
            }
        }

        return null;
    }

    // Monaco: `_toFoundBracket`.
    private FoundBracket? ToFoundBracket(LanguageBracketsConfiguration bracketConfig, Range r)
    {
        string text = _textModel.GetValueInRange(r).ToLowerInvariant();
        return bracketConfig.GetBracketInfo(text) is { } bracketInfo ? new FoundBracket(r, bracketInfo) : null;
    }
}

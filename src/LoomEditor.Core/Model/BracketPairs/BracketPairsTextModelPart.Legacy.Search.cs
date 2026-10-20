// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsImpl.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.BracketPairs;

// The legacy search of the matching bracket, up and down, over the syntax tokens.
internal sealed partial class BracketPairsTextModelPart
{
    // Monaco: `_findMatchingBracketUp`.
    private SearchResult<Range> FindMatchingBracketUpLegacy(RichEditBracket bracket, Position position, Func<bool> continueSearchPredicate)
    {
        string languageId = bracket.LanguageId;
        var reversedBracketRegex = bracket.ReversedRegex;
        int count = -1;

        int totalCallCount = 0;
        SearchResult<Range> SearchPrevMatchingBracketInRange(int lineNumber, string lineText, int searchStartOffset, int searchEndOffset)
        {
            while (true)
            {
                if (++totalCallCount % 100 == 0 && !continueSearchPredicate())
                {
                    return SearchResult<Range>.CanceledResult;
                }

                if (BracketsUtils.FindPrevBracketInRange(reversedBracketRegex, lineNumber, lineText, searchStartOffset, searchEndOffset) is not { } r)
                {
                    break;
                }

                string hitText = lineText.Substring(r.StartColumn - 1, r.EndColumn - r.StartColumn).ToLowerInvariant();
                if (bracket.IsOpen(hitText))
                {
                    count++;
                }
                else if (bracket.IsClose(hitText))
                {
                    count--;
                }

                if (count == 0)
                {
                    return new SearchResult<Range>(r, false);
                }

                searchEndOffset = r.StartColumn - 1;
            }

            return default;
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
            }

            bool prevSearchInToken = true;
            for (; tokenIndex >= 0; tokenIndex--)
            {
                bool searchInToken = lineTokens.GetLanguageId(tokenIndex) == languageId
                    && !ScopedLineTokens.IgnoreBracketsInToken(lineTokens.GetStandardTokenType(tokenIndex));

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
                else
                {
                    // this token should not be searched
                    if (prevSearchInToken && searchStartOffset != searchEndOffset)
                    {
                        var r = SearchPrevMatchingBracketInRange(lineNumber, lineText, searchStartOffset, searchEndOffset);
                        if (r.IsHit)
                        {
                            return r;
                        }
                    }
                }

                prevSearchInToken = searchInToken;
            }

            if (prevSearchInToken && searchStartOffset != searchEndOffset)
            {
                var r = SearchPrevMatchingBracketInRange(lineNumber, lineText, searchStartOffset, searchEndOffset);
                if (r.IsHit)
                {
                    return r;
                }
            }
        }

        return default;
    }

    // Monaco: `_findMatchingBracketDown`.
    private SearchResult<Range> FindMatchingBracketDownLegacy(RichEditBracket bracket, Position position, Func<bool> continueSearchPredicate)
    {
        string languageId = bracket.LanguageId;
        var bracketRegex = bracket.ForwardRegex;
        int count = 1;

        int totalCallCount = 0;
        SearchResult<Range> SearchNextMatchingBracketInRange(int lineNumber, string lineText, int searchStartOffset, int searchEndOffset)
        {
            while (true)
            {
                if (++totalCallCount % 100 == 0 && !continueSearchPredicate())
                {
                    return SearchResult<Range>.CanceledResult;
                }

                if (BracketsUtils.FindNextBracketInRange(bracketRegex, lineNumber, lineText, searchStartOffset, searchEndOffset) is not { } r)
                {
                    break;
                }

                string hitText = lineText.Substring(r.StartColumn - 1, r.EndColumn - r.StartColumn).ToLowerInvariant();
                if (bracket.IsOpen(hitText))
                {
                    count++;
                }
                else if (bracket.IsClose(hitText))
                {
                    count--;
                }

                if (count == 0)
                {
                    return new SearchResult<Range>(r, false);
                }

                searchStartOffset = r.EndColumn - 1;
            }

            return default;
        }

        int lineCount = _textModel.LineCount;
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
            }

            bool prevSearchInToken = true;
            for (; tokenIndex < tokenCount; tokenIndex++)
            {
                bool searchInToken = lineTokens.GetLanguageId(tokenIndex) == languageId
                    && !ScopedLineTokens.IgnoreBracketsInToken(lineTokens.GetStandardTokenType(tokenIndex));

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
                else
                {
                    // this token should not be searched
                    if (prevSearchInToken && searchStartOffset != searchEndOffset)
                    {
                        var r = SearchNextMatchingBracketInRange(lineNumber, lineText, searchStartOffset, searchEndOffset);
                        if (r.IsHit)
                        {
                            return r;
                        }
                    }
                }

                prevSearchInToken = searchInToken;
            }

            if (prevSearchInToken && searchStartOffset != searchEndOffset)
            {
                var r = SearchNextMatchingBracketInRange(lineNumber, lineText, searchStartOffset, searchEndOffset);
                if (r.IsHit)
                {
                    return r;
                }
            }
        }

        return default;
    }
}

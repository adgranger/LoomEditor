// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsImpl.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.BracketPairs;

// The legacy bracket matching: a regex search over the syntax tokens, used when the text is too long for the tree.
internal sealed partial class BracketPairsTextModelPart
{
    // Monaco: `T | null | BracketSearchCanceled`. A hit is a value or a cancellation.
    private readonly record struct SearchResult<T>(T? Value, bool Canceled) where T : struct
    {
        public static SearchResult<T> CanceledResult => new(null, true);

        public bool IsHit => Canceled || Value.HasValue;
    }

    // Monaco: `createTimeBasedContinueBracketSearchPredicate`; `undefined` gives a predicate that always continues.
    private static Func<bool> CreateTimeBasedContinueBracketSearchPredicate(TimeSpan? maxDuration)
    {
        if (maxDuration is not { } duration)
        {
            return static () => true;
        }

        long startTime = Stopwatch.GetTimestamp();
        return () => Stopwatch.GetElapsedTime(startTime) <= duration;
    }

    // Monaco: `stripBracketSearchCanceled`.
    private static T? StripBracketSearchCanceled<T>(SearchResult<T> result) where T : struct => result.Canceled ? null : result.Value;

    // Monaco: `_establishBracketSearchOffsets`.
    private static (int SearchStartOffset, int SearchEndOffset) EstablishBracketSearchOffsets(
        Position position, LineTokens lineTokens, RichEditBrackets modeBrackets, int tokenIndex)
    {
        int tokenCount = lineTokens.GetCount();
        string currentLanguageId = lineTokens.GetLanguageId(tokenIndex);

        // limit search to not go before `maxBracketLength`
        int searchStartOffset = Math.Max(0, position.Column - 1 - modeBrackets.MaxBracketLength);
        for (int i = tokenIndex - 1; i >= 0; i--)
        {
            int tokenEndOffset = lineTokens.GetEndOffset(i);
            if (tokenEndOffset <= searchStartOffset)
            {
                break;
            }

            if (ScopedLineTokens.IgnoreBracketsInToken(lineTokens.GetStandardTokenType(i)) || lineTokens.GetLanguageId(i) != currentLanguageId)
            {
                searchStartOffset = tokenEndOffset;
                break;
            }
        }

        // limit search to not go after `maxBracketLength`
        int searchEndOffset = Math.Min(lineTokens.GetLineContent().Length, position.Column - 1 + modeBrackets.MaxBracketLength);
        for (int i = tokenIndex + 1; i < tokenCount; i++)
        {
            int tokenStartOffset = lineTokens.GetStartOffset(i);
            if (tokenStartOffset >= searchEndOffset)
            {
                break;
            }

            if (ScopedLineTokens.IgnoreBracketsInToken(lineTokens.GetStandardTokenType(i)) || lineTokens.GetLanguageId(i) != currentLanguageId)
            {
                searchEndOffset = tokenStartOffset;
                break;
            }
        }

        return (searchStartOffset, searchEndOffset);
    }

    // Monaco: `_matchBracket`.
    private (Range Opening, Range Closing)? MatchBracketLegacy(Position position, Func<bool> continueSearchPredicate)
    {
        int lineNumber = position.LineNumber;
        var lineTokens = _textModel.Tokenization.GetLineTokens(lineNumber);
        string lineText = _textModel.GetLineContent(lineNumber);

        int tokenIndex = lineTokens.FindTokenIndexAtOffset(position.Column - 1);
        if (tokenIndex < 0)
        {
            return null;
        }

        var currentModeBrackets = _languageConfigurationRegistry.GetLanguageConfiguration(lineTokens.GetLanguageId(tokenIndex)).Brackets;

        // check that the token is not to be ignored
        if (currentModeBrackets is not null && !ScopedLineTokens.IgnoreBracketsInToken(lineTokens.GetStandardTokenType(tokenIndex)))
        {
            var (searchStartOffset, searchEndOffset) = EstablishBracketSearchOffsets(position, lineTokens, currentModeBrackets, tokenIndex);

            // it might be the case that [currentTokenStart -> currentTokenEnd] contains multiple brackets
            // `bestResult` will contain the most right-side result
            (Range Opening, Range Closing)? bestResult = null;
            while (true)
            {
                if (BracketsUtils.FindNextBracketInRange(currentModeBrackets.ForwardRegex, lineNumber, lineText, searchStartOffset, searchEndOffset) is not { } foundBracket)
                {
                    // there are no more brackets in this text
                    break;
                }

                // check that we didn't hit a bracket too far away from position
                if (foundBracket.StartColumn <= position.Column && position.Column <= foundBracket.EndColumn)
                {
                    string foundBracketText = lineText.Substring(foundBracket.StartColumn - 1, foundBracket.EndColumn - foundBracket.StartColumn).ToLowerInvariant();
                    currentModeBrackets.TextIsBracket.TryGetValue(foundBracketText, out var data);
                    currentModeBrackets.TextIsOpenBracket.TryGetValue(foundBracketText, out bool isOpen);
                    var r = MatchFoundBracket(foundBracket, data, isOpen, continueSearchPredicate);
                    if (r.IsHit)
                    {
                        if (r.Canceled)
                        {
                            return null;
                        }

                        bestResult = r.Value;
                    }
                }

                searchStartOffset = foundBracket.EndColumn - 1;
            }

            if (bestResult is not null)
            {
                return bestResult;
            }
        }

        // If position is in between two tokens, try also looking in the previous token
        if (tokenIndex > 0 && lineTokens.GetStartOffset(tokenIndex) == position.Column - 1)
        {
            int prevTokenIndex = tokenIndex - 1;
            var prevModeBrackets = _languageConfigurationRegistry.GetLanguageConfiguration(lineTokens.GetLanguageId(prevTokenIndex)).Brackets;

            // check that previous token is not to be ignored
            if (prevModeBrackets is not null && !ScopedLineTokens.IgnoreBracketsInToken(lineTokens.GetStandardTokenType(prevTokenIndex)))
            {
                var (searchStartOffset, searchEndOffset) = EstablishBracketSearchOffsets(position, lineTokens, prevModeBrackets, prevTokenIndex);

                var foundBracket = BracketsUtils.FindPrevBracketInRange(prevModeBrackets.ReversedRegex, lineNumber, lineText, searchStartOffset, searchEndOffset);

                // check that we didn't hit a bracket too far away from position
                if (foundBracket is { } found && found.StartColumn <= position.Column && position.Column <= found.EndColumn)
                {
                    string foundBracketText = lineText.Substring(found.StartColumn - 1, found.EndColumn - found.StartColumn).ToLowerInvariant();
                    prevModeBrackets.TextIsBracket.TryGetValue(foundBracketText, out var data);
                    prevModeBrackets.TextIsOpenBracket.TryGetValue(foundBracketText, out bool isOpen);
                    var r = MatchFoundBracket(found, data, isOpen, continueSearchPredicate);
                    if (r.IsHit)
                    {
                        return r.Canceled ? null : r.Value;
                    }
                }
            }
        }

        return null;
    }

    // Monaco: `_matchFoundBracket`.
    private SearchResult<(Range Opening, Range Closing)> MatchFoundBracket(Range foundBracket, RichEditBracket? data, bool isOpen, Func<bool> continueSearchPredicate)
    {
        if (data is null)
        {
            return default;
        }

        var matched = isOpen
            ? FindMatchingBracketDownLegacy(data, foundBracket.End, continueSearchPredicate)
            : FindMatchingBracketUpLegacy(data, foundBracket.Start, continueSearchPredicate);

        if (!matched.IsHit)
        {
            return default;
        }

        if (matched.Canceled)
        {
            return SearchResult<(Range Opening, Range Closing)>.CanceledResult;
        }

        return new SearchResult<(Range Opening, Range Closing)>((foundBracket, matched.Value!.Value), false);
    }
}

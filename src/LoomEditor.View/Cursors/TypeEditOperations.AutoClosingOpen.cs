// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>The open and close text of an auto-closing pair that applies to a typed character (Monaco: <c>{ open, close }</c>).</summary>
internal readonly record struct AutoClosingPairResult(string Open, string Close);

/// <summary>Typing an opening character inserts its closing text (Monaco: <c>AutoClosingOpenCharTypeOperation</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class AutoClosingOpenCharTypeOperation
{
    private readonly record struct PositionBeforeAfter(int LineNumber, int BeforeColumn, int AfterColumn);

    public static EditOperationResult? GetEdits(
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<Selection> selections,
        string ch,
        bool chIsAlreadyTyped,
        bool isDoingComposition)
    {
        if (!isDoingComposition)
        {
            AutoClosingPairResult? autoClosingPair = GetAutoClosingPair(config, model, selections, ch, chIsAlreadyTyped);
            if (autoClosingPair is { } pair)
            {
                return RunAutoClosingOpenCharType(model, selections, ch, chIsAlreadyTyped, pair);
            }
        }

        return null;
    }

    private static EditOperationResult RunAutoClosingOpenCharType(
        TextModel model,
        IReadOnlyList<Selection> selections,
        string ch,
        bool chIsAlreadyTyped,
        AutoClosingPairResult autoClosingPair)
    {
        if (!chIsAlreadyTyped && autoClosingPair.Open.Length > ch.Length)
        {
            var typeCommands = new List<IEditCommand?>(selections.Count);
            foreach (Selection selection in selections)
            {
                typeCommands.Add(TypeEditHelpers.TypeCommand(selection.Range, ch, false));
            }

            return TypeAndAutoCloseWithUndoStop(model, typeCommands, ch, autoClosingPair.Close);
        }

        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            commands.Add(new TypeWithAutoClosingCommand(selection.Range, ch, !chIsAlreadyTyped, autoClosingPair.Close));
        }

        return new EditOperationResult(EditOperationType.TypingOther, commands, true, false);
    }

    /// <summary>
    /// Executes <paramref name="typeCommands"/>, which type <paramref name="ch"/>, and then inserts
    /// <paramref name="autoClosingPairClose"/> after <paramref name="ch"/> with an undo stop in between.
    /// This way, undo first removes only the auto-closed characters and keeps a typed multi-character opening pair (e.g. <c>begin</c>).
    /// </summary>
    public static EditOperationResult TypeAndAutoCloseWithUndoStop(
        TextModel model,
        IReadOnlyList<IEditCommand?> typeCommands,
        string ch,
        string autoClosingPairClose)
    {
        int expectedVersionId = model.VersionId + 1;
        return new EditOperationResult(EditOperationType.TypingOther, typeCommands, true, true, selections =>
        {
            bool canAutoClose = model.VersionId == expectedVersionId && selections.Count == typeCommands.Count;
            if (canAutoClose)
            {
                foreach (Selection s in selections)
                {
                    if (!s.IsEmpty
                        || s.PositionColumn <= ch.Length
                        || model.GetValueInRange(new Range(s.PositionLineNumber, s.PositionColumn - ch.Length, s.PositionLineNumber, s.PositionColumn)) != ch)
                    {
                        canAutoClose = false;
                        break;
                    }
                }
            }

            if (!canAutoClose)
            {
                return null;
            }

            var commands = new List<IEditCommand?>(selections.Count);
            foreach (Selection selection in selections)
            {
                commands.Add(new TypeWithAutoClosingCommand(selection.Range, ch, false, autoClosingPairClose));
            }

            return new EditOperationResult(EditOperationType.TypingOther, commands, false, false);
        });
    }

    /// <summary>The auto-closing pair whose opening ends with <paramref name="ch"/>, with the closing text to insert; <see langword="null"/> when none applies.</summary>
    public static AutoClosingPairResult? GetAutoClosingPair(
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<Selection> selections,
        string ch,
        bool chIsAlreadyTyped)
    {
        foreach (Selection selection in selections)
        {
            if (!selection.IsEmpty)
            {
                return null;
            }
        }

        // This method is called both when typing (regularly) and when composition ends: `ch` is sometimes not in the text
        // buffer yet and sometimes already there, so work with the position before `ch` and the position after it.
        var positions = new List<PositionBeforeAfter>(selections.Count);
        foreach (Selection s in selections)
        {
            Position position = s.Position;
            positions.Add(chIsAlreadyTyped
                ? new PositionBeforeAfter(position.LineNumber, position.Column - ch.Length, position.Column)
                : new PositionBeforeAfter(position.LineNumber, position.Column, position.Column));
        }

        // Find the longest auto-closing open pair in case of multiple ending in `ch`
        // e.g. when having [f","] and [","], it picks [f","] if the character before is f
        var beforePositions = new List<Position>(positions.Count);
        foreach (PositionBeforeAfter p in positions)
        {
            beforePositions.Add(new Position(p.LineNumber, p.BeforeColumn));
        }

        StandardAutoClosingPairConditional? pair = FindAutoClosingPairOpen(config, model, beforePositions, ch);
        if (pair is null)
        {
            return null;
        }

        EditorAutoClosingStrategy autoCloseConfig;
        Func<char, bool> shouldAutoCloseBefore;
        bool shouldCheckBracketBalance = false;

        if (CursorCommon.IsQuote(ch))
        {
            autoCloseConfig = config.AutoClosingQuotes;
            shouldAutoCloseBefore = config.ShouldAutoCloseBefore.Quote;
        }
        else
        {
            bool pairIsForComments = !string.IsNullOrEmpty(config.BlockCommentStartToken)
                && pair.Open.Contains(config.BlockCommentStartToken, StringComparison.Ordinal);
            if (pairIsForComments)
            {
                autoCloseConfig = config.AutoClosingComments;
                shouldAutoCloseBefore = config.ShouldAutoCloseBefore.Comment;
            }
            else
            {
                autoCloseConfig = config.AutoClosingBrackets;
                shouldAutoCloseBefore = config.ShouldAutoCloseBefore.Bracket;
                shouldCheckBracketBalance = true;
            }
        }

        if (autoCloseConfig == EditorAutoClosingStrategy.Never)
        {
            return null;
        }

        // Sometimes, it is possible to have two auto-closing pairs that have a containment relationship
        // e.g. when having [(,)] and [(*,*)]
        // - when typing (, the resulting state is (|)
        // - when typing *, the desired resulting state is (*|*), not (*|*))
        StandardAutoClosingPairConditional? containedPair = FindContainedAutoClosingPair(config, pair);
        string containedPairClose = containedPair?.Close ?? string.Empty;
        bool isContainedPairPresent = true;

        foreach (PositionBeforeAfter position in positions)
        {
            (int lineNumber, int beforeColumn, int afterColumn) = (position.LineNumber, position.BeforeColumn, position.AfterColumn);
            string lineText = model.GetLineContent(lineNumber);
            string lineBefore = lineText.Substring(0, beforeColumn - 1);
            string lineAfter = lineText.Substring(afterColumn - 1);

            if (!lineAfter.StartsWith(containedPairClose, StringComparison.Ordinal))
            {
                isContainedPairPresent = false;
            }

            // Only consider auto closing the pair if an allowed character follows or if another autoclosed pair closing brace follows
            if (lineAfter.Length > 0)
            {
                char characterAfter = lineAfter[0];
                bool isBeforeCloseBrace = IsBeforeClosingBrace(config, lineAfter);
                if (!isBeforeCloseBrace && !shouldAutoCloseBefore(characterAfter))
                {
                    return null;
                }
            }

            // Phase 5 step C cut point: model.bracketPairs.hasUnmatchedClosingBracketAfter (bracketPairsImpl.ts 121–133).
            const bool hasUnmatchedClosingBracketAfter = false;
            if (shouldCheckBracketBalance
                // When 'always', always insert the closing bracket
                && autoCloseConfig != EditorAutoClosingStrategy.Always
                // Need to check character is not already typed so brackets are still imbalanced
                && !chIsAlreadyTyped
                && hasUnmatchedClosingBracketAfter)
            {
                return null;
            }

            // Do not auto-close ' or " after a word character
            if (pair.Open.Length == 1 && (ch == "'" || ch == "\"") && autoCloseConfig != EditorAutoClosingStrategy.Always)
            {
                WordCharacterClassifier wordSeparators = WordCharacterClassifierCache.Get(config.WordSeparators, []);
                if (lineBefore.Length > 0)
                {
                    char characterBefore = lineBefore[^1];
                    if (wordSeparators.Get(characterBefore) == WordCharacterClass.Regular)
                    {
                        return null;
                    }
                }
            }

            if (!model.Tokenization.IsCheapToTokenize(lineNumber))
            {
                // Do not force tokenization
                return null;
            }

            model.Tokenization.ForceTokenization(lineNumber);
            LineTokens lineTokens = model.Tokenization.GetLineTokens(lineNumber);
            ScopedLineTokens scopedLineTokens = ScopedLineTokens.Create(lineTokens, beforeColumn - 1);
            if (!pair.ShouldAutoClose(scopedLineTokens, beforeColumn - scopedLineTokens.FirstCharOffset))
            {
                return null;
            }

            // Typing for example a quote could either start a new string, in which case auto-closing is desirable
            // or it could end a previously started string, in which case auto-closing is not desirable.
            // So pretend to type a different character and ask the tokenizer what the outcome of doing that is.
            string? neutralCharacter = pair.FindNeutralCharacter();
            if (!string.IsNullOrEmpty(neutralCharacter))
            {
                StandardTokenType tokenType = model.Tokenization.GetTokenTypeIfInsertingCharacter(lineNumber, beforeColumn, neutralCharacter);
                if (!pair.IsOK(tokenType))
                {
                    return null;
                }
            }
        }

        return isContainedPairPresent
            ? new AutoClosingPairResult(pair.Open, pair.Close.Substring(0, pair.Close.Length - containedPairClose.Length))
            : new AutoClosingPairResult(pair.Open, pair.Close);
    }

    /// <summary>
    /// Another auto-closing pair that is contained by the one passed in: when having [(,)] and [(*,*)] as auto-closing pairs,
    /// [(,)] is the containment pair of [(*,*)].
    /// </summary>
    private static StandardAutoClosingPairConditional? FindContainedAutoClosingPair(CursorConfiguration config, StandardAutoClosingPairConditional pair)
    {
        if (pair.Open.Length <= 1)
        {
            return null;
        }

        string lastChar = pair.Close.Length > 0 ? pair.Close[^1].ToString() : string.Empty;
        // get candidates with the same last character as close
        if (!config.AutoClosingPairs.AutoClosingPairsCloseByEnd.TryGetValue(lastChar, out List<StandardAutoClosingPairConditional>? candidates))
        {
            return null;
        }

        StandardAutoClosingPairConditional? result = null;
        foreach (StandardAutoClosingPairConditional candidate in candidates)
        {
            if (candidate.Open != pair.Open
                && pair.Open.Contains(candidate.Open, StringComparison.Ordinal)
                && pair.Close.EndsWith(candidate.Close, StringComparison.Ordinal)
                && (result is null || candidate.Open.Length > result.Open.Length))
            {
                result = candidate;
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if typing <paramref name="ch"/> at all <paramref name="positions"/> results in an auto closing open sequence being typed.
    /// The longest auto-closing open sequence is returned in case of ambiguity.
    /// </summary>
    private static StandardAutoClosingPairConditional? FindAutoClosingPairOpen(
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<Position> positions,
        string ch)
    {
        if (!config.AutoClosingPairs.AutoClosingPairsOpenByEnd.TryGetValue(ch, out List<StandardAutoClosingPairConditional>? candidates))
        {
            return null;
        }

        // Determine which auto-closing pair it is
        StandardAutoClosingPairConditional? result = null;
        foreach (StandardAutoClosingPairConditional candidate in candidates)
        {
            if (result is null || candidate.Open.Length > result.Open.Length)
            {
                bool candidateIsMatch = true;
                foreach (Position position in positions)
                {
                    // The start column can be below 1 for a long candidate at the start of a line: Monaco clamps it.
                    int startColumn = Math.Max(1, position.Column - candidate.Open.Length + 1);
                    string relevantText = model.GetValueInRange(new Range(position.LineNumber, startColumn, position.LineNumber, position.Column));
                    if (relevantText + ch != candidate.Open)
                    {
                        candidateIsMatch = false;
                        break;
                    }
                }

                if (candidateIsMatch)
                {
                    result = candidate;
                }
            }
        }

        return result;
    }

    private static bool IsBeforeClosingBrace(CursorConfiguration config, string lineAfter)
    {
        // If the start of lineAfter can be interpreted as both a starting or ending brace, default to returning false
        string nextChar = lineAfter[0].ToString();
        bool isBeforeStartingBrace = config.AutoClosingPairs.AutoClosingPairsOpenByStart.TryGetValue(nextChar, out List<StandardAutoClosingPairConditional>? starting)
            && starting.Exists(x => lineAfter.StartsWith(x.Open, StringComparison.Ordinal));
        bool isBeforeClosingBrace = config.AutoClosingPairs.AutoClosingPairsCloseByStart.TryGetValue(nextChar, out List<StandardAutoClosingPairConditional>? closing)
            && closing.Exists(x => lineAfter.StartsWith(x.Close, StringComparison.Ordinal));

        return !isBeforeStartingBrace && isBeforeClosingBrace;
    }
}

/// <summary>Types an opening character (optionally) and its closing text, with the cursor between them (Monaco: <c>TypeWithAutoClosingCommand</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class TypeWithAutoClosingCommand(Range selection, string openCharacter, bool insertOpenCharacter, string closeCharacter)
    : BaseTypeWithAutoClosingCommand(
        selection,
        (insertOpenCharacter ? openCharacter : string.Empty) + closeCharacter,
        0,
        -closeCharacter.Length,
        openCharacter,
        closeCharacter)
{
    public override Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Range range = helper.GetInverseEditOperations()[0].Range;
        return ComputeCursorStateWithRange(model, range, helper);
    }
}

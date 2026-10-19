// Ported from microsoft/vscode-textmate: src/grammar/tokenizeString.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

// _tokenizeString: the tokenization loop.
internal static partial class TokenizeString
{
    /// <summary>
    /// <c>_tokenizeString</c>: tokenizes a string (upstream's <c>lineFonts</c> is not ported, ADR 0008 decision 3).
    /// </summary>
    /// <param name="grammar">The grammar.</param>
    /// <param name="lineText">The line, including its trailing <c>\n</c>.</param>
    /// <param name="isFirstLine">Whether <c>\A</c> may match at the start.</param>
    /// <param name="linePos">The position to start at.</param>
    /// <param name="stack">The state at <paramref name="linePos"/>.</param>
    /// <param name="lineTokens">Collects the tokens.</param>
    /// <param name="checkWhileConditions">Whether to check the <c>while</c> conditions of the stack first.</param>
    /// <param name="timeLimit">The time limit in milliseconds; use <c>0</c> to indicate no time limit.</param>
    public static TokenizeStringResult Tokenize(
        Grammar grammar,
        IOnigString lineText,
        bool isFirstLine,
        int linePos,
        StateStackImpl stack,
        LineTokens lineTokens,
        bool checkWhileConditions,
        int timeLimit)
    {
        var state = new ScanState(stack, linePos, isFirstLine);

        if (checkWhileConditions)
        {
            WhileCheckResult whileCheckResult = CheckWhileConditions(grammar, lineText, isFirstLine, linePos, stack, lineTokens);
            state.Stack = whileCheckResult.Stack;
            state.LinePos = whileCheckResult.LinePos;
            state.IsFirstLine = whileCheckResult.IsFirstLine;
            state.AnchorPosition = whileCheckResult.AnchorPosition;
        }

        long startTime = Stopwatch.GetTimestamp();
        while (!state.Stop)
        {
            if (timeLimit != 0)
            {
                double elapsedTime = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
                if (elapsedTime > timeLimit)
                {
                    return new TokenizeStringResult(state.Stack, true);
                }
            }

            ScanNext(grammar, lineText, lineTokens, state); // potentially modifies linePos && anchorPosition
        }

        return new TokenizeStringResult(state.Stack, false);
    }

    // The variables that upstream's scanNext closure shares with _tokenizeString.
    private sealed class ScanState(StateStackImpl stack, int linePos, bool isFirstLine)
    {
        public StateStackImpl Stack { get; set; } = stack;

        public int LinePos { get; set; } = linePos;

        public bool IsFirstLine { get; set; } = isFirstLine;

        public int AnchorPosition { get; set; } = -1;

        public bool Stop { get; set; }
    }

    // scanNext
    private static void ScanNext(Grammar grammar, IOnigString lineText, LineTokens lineTokens, ScanState s)
    {
        int lineLength = lineText.Content.Length;
        MatchResult? r = MatchRuleOrInjections(grammar, lineText, s.IsFirstLine, s.LinePos, s.Stack, s.AnchorPosition);

        if (r is null)
        {
            // No match
            lineTokens.Produce(s.Stack, lineLength);
            s.Stop = true;
            return;
        }

        IReadOnlyList<OnigCaptureIndex> captureIndices = r.CaptureIndices;
        RuleId matchedRuleId = r.MatchedRuleId;

        bool hasAdvanced = captureIndices.Count > 0 && captureIndices[0].End > s.LinePos;

        if (matchedRuleId == RuleId.End)
        {
            // We matched the `end` for this rule => pop it
            var poppedRule = (BeginEndRule)s.Stack.GetRule(grammar);

            lineTokens.Produce(s.Stack, captureIndices[0].Start);
            s.Stack = s.Stack.WithContentNameScopesList(s.Stack.NameScopesList!);
            HandleCaptures(grammar, lineText, s.IsFirstLine, s.Stack, lineTokens, poppedRule.EndCaptures, captureIndices);
            lineTokens.Produce(s.Stack, captureIndices[0].End);

            // pop
            StateStackImpl popped = s.Stack;
            s.Stack = s.Stack.Parent!;
            s.AnchorPosition = popped.GetAnchorPos();

            if (!hasAdvanced && popped.GetEnterPos() == s.LinePos)
            {
                // Grammar pushed & popped a rule without advancing
                // See https://github.com/Microsoft/vscode-textmate/issues/12
                // Let's assume this was a mistake by the grammar author and the intent was to continue in this state
                s.Stack = popped;

                lineTokens.Produce(s.Stack, lineLength);
                s.Stop = true;
                return;
            }
        }
        else
        {
            // We matched a rule!
            Rule rule = grammar.GetRule(matchedRuleId)!;

            lineTokens.Produce(s.Stack, captureIndices[0].Start);

            StateStackImpl beforePush = s.Stack;
            // push it on the stack rule
            string? scopeName = rule.GetName(lineText.Content, captureIndices);
            AttributedScopeStack nameScopesList = s.Stack.ContentNameScopesList!.PushAttributed(scopeName, grammar);
            s.Stack = s.Stack.Push(
                matchedRuleId,
                s.LinePos,
                s.AnchorPosition,
                captureIndices[0].End == lineLength,
                null,
                nameScopesList,
                nameScopesList);

            if (rule is BeginEndRule pushedRule)
            {
                HandleCaptures(grammar, lineText, s.IsFirstLine, s.Stack, lineTokens, pushedRule.BeginCaptures, captureIndices);
                lineTokens.Produce(s.Stack, captureIndices[0].End);
                s.AnchorPosition = captureIndices[0].End;

                string? contentName = pushedRule.GetContentName(lineText.Content, captureIndices);
                AttributedScopeStack contentNameScopesList = nameScopesList.PushAttributed(contentName, grammar);
                s.Stack = s.Stack.WithContentNameScopesList(contentNameScopesList);

                if (pushedRule.EndHasBackReferences)
                {
                    s.Stack = s.Stack.WithEndRule(pushedRule.GetEndWithResolvedBackReferences(lineText.Content, captureIndices));
                }

                if (!hasAdvanced && beforePush.HasSameRuleAs(s.Stack))
                {
                    // Grammar pushed the same rule without advancing
                    s.Stack = s.Stack.Pop()!;
                    lineTokens.Produce(s.Stack, lineLength);
                    s.Stop = true;
                    return;
                }
            }
            else if (rule is BeginWhileRule pushedWhileRule)
            {
                HandleCaptures(grammar, lineText, s.IsFirstLine, s.Stack, lineTokens, pushedWhileRule.BeginCaptures, captureIndices);
                lineTokens.Produce(s.Stack, captureIndices[0].End);
                s.AnchorPosition = captureIndices[0].End;
                string? contentName = pushedWhileRule.GetContentName(lineText.Content, captureIndices);
                AttributedScopeStack contentNameScopesList = nameScopesList.PushAttributed(contentName, grammar);
                s.Stack = s.Stack.WithContentNameScopesList(contentNameScopesList);

                if (pushedWhileRule.WhileHasBackReferences)
                {
                    s.Stack = s.Stack.WithEndRule(pushedWhileRule.GetWhileWithResolvedBackReferences(lineText.Content, captureIndices));
                }

                if (!hasAdvanced && beforePush.HasSameRuleAs(s.Stack))
                {
                    // Grammar pushed the same rule without advancing
                    s.Stack = s.Stack.Pop()!;
                    lineTokens.Produce(s.Stack, lineLength);
                    s.Stop = true;
                    return;
                }
            }
            else
            {
                var matchingRule = (MatchRule)rule;

                HandleCaptures(grammar, lineText, s.IsFirstLine, s.Stack, lineTokens, matchingRule.Captures, captureIndices);
                lineTokens.Produce(s.Stack, captureIndices[0].End);

                // pop rule immediately since it is a MatchRule
                s.Stack = s.Stack.Pop()!;

                if (!hasAdvanced)
                {
                    // Grammar is not advancing, nor is it pushing/popping
                    s.Stack = s.Stack.SafePop();
                    lineTokens.Produce(s.Stack, lineLength);
                    s.Stop = true;
                    return;
                }
            }
        }

        if (captureIndices[0].End > s.LinePos)
        {
            // Advance stream
            s.LinePos = captureIndices[0].End;
            s.IsFirstLine = false;
        }
    }
}

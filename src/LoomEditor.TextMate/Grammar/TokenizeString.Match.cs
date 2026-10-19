// Ported from microsoft/vscode-textmate: src/grammar/tokenizeString.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>TokenizeStringResult</c>.</summary>
/// <param name="Stack"><c>stack</c>.</param>
/// <param name="StoppedEarly"><c>stoppedEarly</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record TokenizeStringResult(StateStackImpl Stack, bool StoppedEarly);

/// <summary><c>IMatchResult</c>: the rule (or <see cref="RuleId.End"/>) that matched and its captures.</summary>
/// <param name="CaptureIndices"><c>captureIndices</c>.</param>
/// <param name="MatchedRuleId"><c>matchedRuleId</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal record MatchResult(IReadOnlyList<OnigCaptureIndex> CaptureIndices, RuleId MatchedRuleId);

/// <summary><c>IMatchInjectionsResult</c>: a <see cref="MatchResult"/> of an injection.</summary>
/// <param name="PriorityMatch"><c>priorityMatch</c>: the injection has priority <c>L:</c>.</param>
/// <param name="CaptureIndices"><c>captureIndices</c>.</param>
/// <param name="MatchedRuleId"><c>matchedRuleId</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record MatchInjectionsResult(bool PriorityMatch, IReadOnlyList<OnigCaptureIndex> CaptureIndices, RuleId MatchedRuleId)
    : MatchResult(CaptureIndices, MatchedRuleId);

/// <summary>The module of <c>tokenizeString.ts</c>: matching rules and injections, and the tokenization loop (P4-19).</summary>
/// <remarks>Threading: stateless; the grammar and state stack it works on are single-threaded.</remarks>
internal static partial class TokenizeString
{
    /// <summary><c>matchRuleOrInjections</c>: the match of the current rule, unless an injection wins.</summary>
    public static MatchResult? MatchRuleOrInjections(
        Grammar grammar,
        IOnigString lineText,
        bool isFirstLine,
        int linePos,
        StateStackImpl stack,
        int anchorPosition)
    {
        // Look for normal grammar rule
        MatchResult? matchResult = MatchRule(grammar, lineText, isFirstLine, linePos, stack, anchorPosition);

        // Look for injected rules
        List<Injection> injections = grammar.GetInjections();
        if (injections.Count == 0)
        {
            // No injections whatsoever => early return
            return matchResult;
        }

        MatchInjectionsResult? injectionResult = MatchInjections(injections, grammar, lineText, isFirstLine, linePos, stack, anchorPosition);
        if (injectionResult is null)
        {
            // No injections matched => early return
            return matchResult;
        }

        if (matchResult is null)
        {
            // Only injections matched => early return
            return injectionResult;
        }

        // Decide if `matchResult` or `injectionResult` should win
        int matchResultScore = matchResult.CaptureIndices[0].Start;
        int injectionResultScore = injectionResult.CaptureIndices[0].Start;

        if (injectionResultScore < matchResultScore || (injectionResult.PriorityMatch && injectionResultScore == matchResultScore))
        {
            // injection won!
            return injectionResult;
        }

        return matchResult;
    }

    /// <summary><c>matchRule</c>: searches the rule on top of the stack from <paramref name="linePos"/>.</summary>
    public static MatchResult? MatchRule(
        Grammar grammar,
        IOnigString lineText,
        bool isFirstLine,
        int linePos,
        StateStackImpl stack,
        int anchorPosition)
    {
        Rule rule = stack.GetRule(grammar);
        (CompiledRule ruleScanner, FindOption findOptions) =
            PrepareRuleSearch(rule, grammar, stack.EndRule, isFirstLine, linePos == anchorPosition);

        IFindNextMatchResult? r = ruleScanner.FindNextMatchSync(lineText, linePos, findOptions);
        return r is null ? null : new MatchResult(r.CaptureIndices, r.RuleId);
    }

    /// <summary>
    /// <c>matchInjections</c>: the best injection match: the earliest start, and for equal starts the first injection
    /// (they are sorted by priority).
    /// </summary>
    public static MatchInjectionsResult? MatchInjections(
        List<Injection> injections,
        Grammar grammar,
        IOnigString lineText,
        bool isFirstLine,
        int linePos,
        StateStackImpl stack,
        int anchorPosition)
    {
        // The lower the better
        int bestMatchRating = int.MaxValue;
        IReadOnlyList<OnigCaptureIndex>? bestMatchCaptureIndices = null;
        RuleId bestMatchRuleId = default;
        int bestMatchResultPriority = 0;

        string[] scopes = [.. stack.ContentNameScopesList!.GetScopeNames()];

        foreach (Injection injection in injections)
        {
            if (!injection.Matcher(scopes))
            {
                // injection selector doesn't match stack
                continue;
            }

            Rule rule = grammar.GetRule(injection.RuleId)!;
            (CompiledRule ruleScanner, FindOption findOptions) =
                PrepareRuleSearch(rule, grammar, null, isFirstLine, linePos == anchorPosition);
            IFindNextMatchResult? matchResult = ruleScanner.FindNextMatchSync(lineText, linePos, findOptions);
            if (matchResult is null)
            {
                continue;
            }

            int matchRating = matchResult.CaptureIndices[0].Start;
            if (matchRating >= bestMatchRating)
            {
                // Injections are sorted by priority, so the previous injection had a better or equal priority
                continue;
            }

            bestMatchRating = matchRating;
            bestMatchCaptureIndices = matchResult.CaptureIndices;
            bestMatchRuleId = matchResult.RuleId;
            bestMatchResultPriority = injection.Priority;

            if (bestMatchRating == linePos)
            {
                // No more need to look at the rest of the injections.
                break;
            }
        }

        if (bestMatchCaptureIndices is not null)
        {
            return new MatchInjectionsResult(bestMatchResultPriority == -1, bestMatchCaptureIndices, bestMatchRuleId);
        }

        return null;
    }

    /// <summary>
    /// <c>prepareRuleSearch</c>: compiles the rule for a search, with <c>\A</c> and <c>\G</c> resolved in the regex
    /// sources (<c>compileAG</c>); the scanner is called without find options (<c>UseOnigurumaFindOptions</c> is false).
    /// </summary>
    public static (CompiledRule RuleScanner, FindOption FindOptions) PrepareRuleSearch(
        Rule rule,
        Grammar grammar,
        string? endRegexSource,
        bool allowA,
        bool allowG) =>
        (rule.CompileAG(grammar, endRegexSource, allowA, allowG), FindOption.None);

    /// <summary><c>prepareRuleWhileSearch</c>: like <see cref="PrepareRuleSearch"/> for the <c>while</c> regex.</summary>
    public static (CompiledRule RuleScanner, FindOption FindOptions) PrepareRuleWhileSearch(
        BeginWhileRule rule,
        Grammar grammar,
        string? endRegexSource,
        bool allowA,
        bool allowG) =>
        (rule.CompileWhileAG(grammar, endRegexSource, allowA, allowG), FindOption.None);

    /// <summary>
    /// <c>getFindOptions</c>: the Oniguruma options that disable <c>\A</c> and <c>\G</c>. Kept for completeness; unused
    /// because <c>UseOnigurumaFindOptions</c> is false.
    /// </summary>
    public static FindOption GetFindOptions(bool allowA, bool allowG)
    {
        FindOption options = FindOption.None;
        if (!allowA)
        {
            options |= FindOption.NotBeginString;
        }

        if (!allowG)
        {
            options |= FindOption.NotBeginPosition;
        }

        return options;
    }
}

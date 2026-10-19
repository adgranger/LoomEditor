// Ported from microsoft/vscode-textmate: src/grammar/tokenizeString.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>IWhileCheckResult</c>.</summary>
/// <param name="Stack"><c>stack</c>.</param>
/// <param name="LinePos"><c>linePos</c>.</param>
/// <param name="AnchorPosition"><c>anchorPosition</c>.</param>
/// <param name="IsFirstLine"><c>isFirstLine</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record WhileCheckResult(StateStackImpl Stack, int LinePos, int AnchorPosition, bool IsFirstLine);

// _checkWhileConditions
internal static partial class TokenizeString
{
    /// <summary>
    /// <c>_checkWhileConditions</c>: walks the stack from bottom to top, and checks each <c>while</c> condition in this
    /// order. If any fails, cuts off the entire stack above the failed condition. <c>while</c> conditions may also
    /// advance the line position.
    /// </summary>
    public static WhileCheckResult CheckWhileConditions(
        Grammar grammar,
        IOnigString lineText,
        bool isFirstLine,
        int linePos,
        StateStackImpl stack,
        LineTokens lineTokens)
    {
        int anchorPosition = stack.BeginRuleCapturedEol ? 0 : -1;

        var whileRules = new List<(StateStackImpl Stack, BeginWhileRule Rule)>();
        for (StateStackImpl? node = stack; node is not null; node = node.Pop())
        {
            if (node.GetRule(grammar) is BeginWhileRule nodeRule)
            {
                whileRules.Add((node, nodeRule));
            }
        }

        // whileRules.pop(): from the bottom of the stack to the top.
        for (int i = whileRules.Count - 1; i >= 0; i--)
        {
            (StateStackImpl whileStack, BeginWhileRule whileRule) = whileRules[i];
            (CompiledRule ruleScanner, FindOption findOptions) =
                PrepareRuleWhileSearch(whileRule, grammar, whileStack.EndRule, isFirstLine, linePos == anchorPosition);
            IFindNextMatchResult? r = ruleScanner.FindNextMatchSync(lineText, linePos, findOptions);

            if (r is not null)
            {
                if (r.RuleId != RuleId.While)
                {
                    // we shouldn't end up here
                    stack = whileStack.Pop()!;
                    break;
                }

                if (r.CaptureIndices.Count > 0)
                {
                    lineTokens.Produce(whileStack, r.CaptureIndices[0].Start);
                    HandleCaptures(grammar, lineText, isFirstLine, whileStack, lineTokens, whileRule.WhileCaptures, r.CaptureIndices);
                    lineTokens.Produce(whileStack, r.CaptureIndices[0].End);
                    anchorPosition = r.CaptureIndices[0].End;
                    if (r.CaptureIndices[0].End > linePos)
                    {
                        linePos = r.CaptureIndices[0].End;
                        isFirstLine = false;
                    }
                }
            }
            else
            {
                stack = whileStack.Pop()!;
                break;
            }
        }

        return new WhileCheckResult(stack, linePos, anchorPosition, isFirstLine);
    }
}

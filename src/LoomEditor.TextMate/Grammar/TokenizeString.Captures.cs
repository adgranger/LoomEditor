// Ported from microsoft/vscode-textmate: src/grammar/tokenizeString.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>LocalStackElement</c>: the scopes of a capture that is still open, and where it ends.</summary>
/// <param name="Scopes"><c>scopes</c>.</param>
/// <param name="EndPos"><c>endPos</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record LocalStackElement(AttributedScopeStack Scopes, int EndPos);

// handleCaptures
internal static partial class TokenizeString
{
    /// <summary>
    /// <c>handleCaptures</c>: produces the tokens of the captures of a match, nesting the scopes of captures that
    /// contain others and retokenizing the captures that have patterns (<c>retokenizeCapturedWithRuleId</c>).
    /// </summary>
    public static void HandleCaptures(
        Grammar grammar,
        IOnigString lineText,
        bool isFirstLine,
        StateStackImpl stack,
        LineTokens lineTokens,
        IReadOnlyList<CaptureRule?> captures,
        IReadOnlyList<OnigCaptureIndex> captureIndices)
    {
        if (captures.Count == 0)
        {
            return;
        }

        string lineTextContent = lineText.Content;

        int len = Math.Min(captures.Count, captureIndices.Count);
        var localStack = new List<LocalStackElement>();
        int maxEnd = captureIndices[0].End;

        for (int i = 0; i < len; i++)
        {
            CaptureRule? captureRule = captures[i];
            if (captureRule is null)
            {
                // Not interested
                continue;
            }

            OnigCaptureIndex captureIndex = captureIndices[i];

            if (captureIndex.Length == 0)
            {
                // Nothing really captured
                continue;
            }

            if (captureIndex.Start > maxEnd)
            {
                // Capture going beyond consumed string
                break;
            }

            // pop captures while needed
            while (localStack.Count > 0 && localStack[^1].EndPos <= captureIndex.Start)
            {
                // pop!
                lineTokens.ProduceFromScopes(localStack[^1].Scopes, localStack[^1].EndPos);
                localStack.RemoveAt(localStack.Count - 1);
            }

            if (localStack.Count > 0)
            {
                lineTokens.ProduceFromScopes(localStack[^1].Scopes, captureIndex.Start);
            }
            else
            {
                lineTokens.Produce(stack, captureIndex.Start);
            }

            if (captureRule.RetokenizeCapturedWithRuleId.Value != 0)
            {
                // the capture requires additional matching
                string? scopeName = captureRule.GetName(lineTextContent, captureIndices);
                AttributedScopeStack nameScopesList = stack.ContentNameScopesList!.PushAttributed(scopeName, grammar);
                string? contentName = captureRule.GetContentName(lineTextContent, captureIndices);
                AttributedScopeStack contentNameScopesList = nameScopesList.PushAttributed(contentName, grammar);

                StateStackImpl stackClone = stack.Push(
                    captureRule.RetokenizeCapturedWithRuleId,
                    captureIndex.Start,
                    -1,
                    false,
                    null,
                    nameScopesList,
                    contentNameScopesList);
                using IOnigString onigSubStr = grammar.CreateOnigString(lineTextContent.Substring(0, captureIndex.End));
                Tokenize(
                    grammar,
                    onigSubStr,
                    isFirstLine && captureIndex.Start == 0,
                    captureIndex.Start,
                    stackClone,
                    lineTokens,
                    false,
                    /* no time limit */ 0);
                continue;
            }

            string? captureRuleScopeName = captureRule.GetName(lineTextContent, captureIndices);
            if (captureRuleScopeName is not null)
            {
                // push
                AttributedScopeStack? baseScopes = localStack.Count > 0 ? localStack[^1].Scopes : stack.ContentNameScopesList;
                AttributedScopeStack captureRuleScopesList = baseScopes!.PushAttributed(captureRuleScopeName, grammar);
                localStack.Add(new LocalStackElement(captureRuleScopesList, captureIndex.End));
            }
        }

        while (localStack.Count > 0)
        {
            // pop!
            lineTokens.ProduceFromScopes(localStack[^1].Scopes, localStack[^1].EndPos);
            localStack.RemoveAt(localStack.Count - 1);
        }
    }
}

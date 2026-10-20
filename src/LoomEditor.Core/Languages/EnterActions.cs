// Ported from microsoft/vscode: src/vs/editor/common/languages/enterAction.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Languages;

/// <summary>Computes what Enter should do at a range (Monaco: the module function of <c>enterAction.ts</c>).</summary>
/// <remarks>Threading: use it from the thread of the editor.</remarks>
internal static class EnterActions
{
    /// <summary>
    /// The enter action at <paramref name="range"/> (on-enter rules and brackets), with the indentation of its line,
    /// or <see langword="null"/> when no rule applies (Monaco: <c>getEnterAction</c>).
    /// </summary>
    public static CompleteEnterAction? GetEnterAction(EditorAutoIndentStrategy autoIndent, TextModel model, Range range, LanguageConfigurationRegistry registry)
    {
        model.Tokenization.ForceTokenization(range.StartLineNumber);
        string languageId = model.Tokenization.GetLanguageIdAtPosition(range.StartLineNumber, range.StartColumn);
        ResolvedLanguageConfiguration richEditSupport = registry.GetLanguageConfiguration(languageId);
        var indentationContextProcessor = new IndentationContextProcessor(model, registry);
        ProcessedTokenContext processedContextTokens = indentationContextProcessor.GetProcessedTokenContextAroundRange(range);
        string previousLineText = processedContextTokens.PreviousLineProcessedTokens.GetLineContent();
        string beforeEnterText = processedContextTokens.BeforeRangeProcessedTokens.GetLineContent();
        string afterEnterText = processedContextTokens.AfterRangeProcessedTokens.GetLineContent();

        EnterAction? enterResult = richEditSupport.OnEnter(autoIndent, previousLineText, beforeEnterText, afterEnterText);
        if (enterResult is not { } result)
        {
            return null;
        }

        IndentAction indentAction = result.IndentAction;
        string? appendText = result.AppendText;
        int removeText = result.RemoveText;

        // Here we add `\t` to appendText first because enterAction is leveraging appendText and removeText to change indentation.
        if (string.IsNullOrEmpty(appendText))
        {
            if (indentAction == IndentAction.Indent || indentAction == IndentAction.IndentOutdent)
            {
                appendText = "\t";
            }
            else
            {
                appendText = "";
            }
        }
        else if (indentAction == IndentAction.Indent)
        {
            appendText = "\t" + appendText;
        }

        string indentation = LanguageConfigurationRegistry.GetIndentationAtPosition(model, range.StartLineNumber, range.StartColumn);
        if (removeText != 0)
        {
            indentation = indentation[..(indentation.Length - removeText)];
        }

        return new CompleteEnterAction(indentAction, appendText, removeText, indentation);
    }
}

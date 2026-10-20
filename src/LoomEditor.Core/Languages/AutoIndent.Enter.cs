// Ported from microsoft/vscode: src/vs/editor/common/languages/autoIndent.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Languages;

internal static partial class AutoIndent
{
    /// <summary>
    /// The indentation of the line before and of the new line after Enter is pressed at <paramref name="range"/>, or
    /// <see langword="null"/> (Monaco: <c>getIndentForEnter</c>).
    /// </summary>
    public static (string BeforeEnter, string AfterEnter)? GetIndentForEnter(
        EditorAutoIndentStrategy autoIndent,
        TextModel model,
        Range range,
        IIndentConverter indentConverter,
        LanguageConfigurationRegistry registry)
    {
        if (autoIndent < EditorAutoIndentStrategy.Full)
        {
            return null;
        }

        string languageId = model.Tokenization.GetLanguageIdAtPosition(range.StartLineNumber, range.StartColumn);
        IndentRulesSupport? indentRulesSupport = registry.GetLanguageConfiguration(languageId).IndentRulesSupport;
        if (indentRulesSupport is null)
        {
            return null;
        }

        model.Tokenization.ForceTokenization(range.StartLineNumber);
        var indentationContextProcessor = new IndentationContextProcessor(model, registry);
        ProcessedTokenContext processedContextTokens = indentationContextProcessor.GetProcessedTokenContextAroundRange(range);
        IViewLineTokens afterEnterProcessedTokens = processedContextTokens.AfterRangeProcessedTokens;
        IViewLineTokens beforeEnterProcessedTokens = processedContextTokens.BeforeRangeProcessedTokens;
        string beforeEnterIndent = Strings.GetLeadingWhitespace(beforeEnterProcessedTokens.GetLineContent());

        var virtualModel = new VirtualModelWithModifiedTokensAtLine(model, range.StartLineNumber, beforeEnterProcessedTokens);
        bool languageIsDifferentFromLineStart = IndentationContextProcessor.IsLanguageDifferentFromLineStart(model, range.Start);
        string currentLine = model.GetLineContent(range.StartLineNumber);
        string currentLineIndent = Strings.GetLeadingWhitespace(currentLine);
        // Monaco passes undefined for honorIntentialIndent, which defaults to true.
        InheritIndentResult? afterEnterAction = GetInheritIndentForLine(autoIndent, virtualModel, range.StartLineNumber + 1, true, registry);
        if (afterEnterAction is not { } action)
        {
            string beforeEnter = languageIsDifferentFromLineStart ? currentLineIndent : beforeEnterIndent;
            return (beforeEnter, beforeEnter);
        }

        string afterEnterIndent = languageIsDifferentFromLineStart ? currentLineIndent : action.Indentation;

        if (action.Action == IndentAction.Indent)
        {
            afterEnterIndent = indentConverter.ShiftIndent(afterEnterIndent);
        }

        if (indentRulesSupport.ShouldDecrease(afterEnterProcessedTokens.GetLineContent()))
        {
            afterEnterIndent = indentConverter.UnshiftIndent(afterEnterIndent);
        }

        return (languageIsDifferentFromLineStart ? currentLineIndent : beforeEnterIndent, afterEnterIndent);
    }

    /// <summary>
    /// The indentation of the line after <paramref name="ch"/> is typed at <paramref name="range"/>, or
    /// <see langword="null"/> to keep it (Monaco: <c>getIndentActionForType</c>).
    /// We should always allow intentional indentation. It means, if users change the indentation of `lineNumber` and the content of
    /// this line doesn't match decreaseIndentPattern, we should not adjust the indentation.
    /// </summary>
    /// <remarks>
    /// Monaco passes the whole <c>CursorConfiguration</c>; only its <c>autoIndent</c> and <c>autoClosingPairs</c> are read.
    /// </remarks>
    public static string? GetIndentActionForType(
        EditorAutoIndentStrategy autoIndent,
        AutoClosingPairs autoClosingPairs,
        TextModel model,
        Range range,
        string ch,
        IIndentConverter indentConverter,
        LanguageConfigurationRegistry registry)
    {
        if (autoIndent < EditorAutoIndentStrategy.Full)
        {
            return null;
        }

        bool languageIsDifferentFromLineStart = IndentationContextProcessor.IsLanguageDifferentFromLineStart(model, range.Start);
        if (languageIsDifferentFromLineStart)
        {
            // this line has mixed languages and indentation rules will not work
            return null;
        }

        string languageId = model.Tokenization.GetLanguageIdAtPosition(range.StartLineNumber, range.StartColumn);
        IndentRulesSupport? indentRulesSupport = registry.GetLanguageConfiguration(languageId).IndentRulesSupport;
        if (indentRulesSupport is null)
        {
            return null;
        }

        var indentationContextProcessor = new IndentationContextProcessor(model, registry);
        ProcessedTokenContext processedContextTokens = indentationContextProcessor.GetProcessedTokenContextAroundRange(range);
        string beforeRangeText = processedContextTokens.BeforeRangeProcessedTokens.GetLineContent();
        string afterRangeText = processedContextTokens.AfterRangeProcessedTokens.GetLineContent();
        string textAroundRange = beforeRangeText + afterRangeText;
        string textAroundRangeWithCharacter = beforeRangeText + ch + afterRangeText;

        // If previous content already matches decreaseIndentPattern, it means indentation of this line should already be adjusted
        // Users might change the indentation by purpose and we should honor that instead of readjusting.
        if (!indentRulesSupport.ShouldDecrease(textAroundRange) && indentRulesSupport.ShouldDecrease(textAroundRangeWithCharacter))
        {
            // after typing `ch`, the content matches decreaseIndentPattern, we should adjust the indent to a good manner.
            // 1. Get inherited indent action
            InheritIndentResult? r = GetInheritIndentForLine(autoIndent, model, range.StartLineNumber, false, registry);
            if (r is not { } inherited)
            {
                return null;
            }

            string indentation = inherited.Indentation;
            if (inherited.Action != IndentAction.Indent)
            {
                indentation = indentConverter.UnshiftIndent(indentation);
            }

            return indentation;
        }

        int previousLineNumber = range.StartLineNumber - 1;
        if (range.IsEmpty && previousLineNumber > 0)
        {
            string previousLine = model.GetLineContent(previousLineNumber);
            if (indentRulesSupport.ShouldIndentNextLine(previousLine) && indentRulesSupport.ShouldIncrease(textAroundRangeWithCharacter))
            {
                InheritIndentResult? inheritedIndentationData = GetInheritIndentForLine(autoIndent, model, range.StartLineNumber, false, registry);
                string? inheritedIndentation = inheritedIndentationData?.Indentation;
                if (inheritedIndentation is not null)
                {
                    string currentLine = model.GetLineContent(range.StartLineNumber);
                    string actualCurrentIndentation = Strings.GetLeadingWhitespace(currentLine);
                    string inferredCurrentIndentation = indentConverter.ShiftIndent(inheritedIndentation);
                    // If the inferred current indentation is not equal to the actual current indentation, then the indentation has been intentionally changed, in that case keep it
                    bool inferredIndentationEqualsActual = inferredCurrentIndentation == actualCurrentIndentation;
                    bool textAroundRangeContainsOnlyWhitespace = string.IsNullOrWhiteSpace(textAroundRange);
                    bool autoClosingPairExists = autoClosingPairs.AutoClosingPairsOpenByEnd.TryGetValue(ch, out List<StandardAutoClosingPairConditional>? pairs) && pairs.Count > 0;
                    bool isChFirstNonWhitespaceCharacterAndInAutoClosingPair = autoClosingPairExists && textAroundRangeContainsOnlyWhitespace;
                    if (inferredIndentationEqualsActual && isChFirstNonWhitespaceCharacterAndInAutoClosingPair)
                    {
                        return inheritedIndentation;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The <see cref="IndentConsts"/> flags of a line, or <see langword="null"/> (Monaco: <c>getIndentMetadata</c>).
    /// </summary>
    public static int? GetIndentMetadata(TextModel model, int lineNumber, LanguageConfigurationRegistry registry)
    {
        IndentRulesSupport? indentRulesSupport = registry.GetLanguageConfiguration(model.Tokenization.GetLanguageId()).IndentRulesSupport;
        if (indentRulesSupport is null)
        {
            return null;
        }

        if (lineNumber < 1 || lineNumber > model.LineCount)
        {
            return null;
        }

        return indentRulesSupport.GetIndentMetadata(model.GetLineContent(lineNumber));
    }

    /// <summary>
    /// The model with other tokens and content on one line (Monaco: <c>createVirtualModelWithModifiedTokensAtLine</c>).
    /// </summary>
    /// <remarks>Threading: use it from the thread of the editor.</remarks>
    private sealed class VirtualModelWithModifiedTokensAtLine(TextModel model, int modifiedLineNumber, IViewLineTokens modifiedTokens) : IVirtualModel
    {
        public IViewLineTokens GetLineTokens(int lineNumber) =>
            lineNumber == modifiedLineNumber ? modifiedTokens : model.Tokenization.GetLineTokens(lineNumber);

        public string GetLanguageId() => model.Tokenization.GetLanguageId();

        public string GetLanguageIdAtPosition(int lineNumber, int column) => model.Tokenization.GetLanguageIdAtPosition(lineNumber, column);

        // Monaco's object has no forceTokenization.
        public void ForceTokenization(int lineNumber)
        {
        }

        public string GetLineContent(int lineNumber) =>
            lineNumber == modifiedLineNumber ? modifiedTokens.GetLineContent() : model.GetLineContent(lineNumber);
    }
}

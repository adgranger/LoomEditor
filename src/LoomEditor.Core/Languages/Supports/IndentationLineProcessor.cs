// Ported from microsoft/vscode: src/vs/editor/common/languages/supports/indentationLineProcessor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Languages.Supports;

/// <summary>
/// This class is a wrapper class around <see cref="IndentRulesSupport"/>.
/// It processes the lines by removing the language configuration brackets from the regex, string and comment tokens.
/// It then calls into the <see cref="IndentRulesSupport"/> to validate the indentation conditions.
/// </summary>
/// <remarks>Threading: use it from the thread of the editor.</remarks>
internal sealed class ProcessedIndentRulesSupport
{
    private readonly IndentRulesSupport _indentRulesSupport;
    private readonly IndentationLineProcessor _indentationLineProcessor;

    public ProcessedIndentRulesSupport(IVirtualModel model, IndentRulesSupport indentRulesSupport, LanguageConfigurationRegistry registry)
    {
        _indentRulesSupport = indentRulesSupport;
        _indentationLineProcessor = new IndentationLineProcessor(model, registry);
    }

    /// <summary>Apply the new indentation and return whether the indentation level should be increased after the given line number.</summary>
    public bool ShouldIncrease(int lineNumber, string? newIndentation = null)
    {
        string processedLine = _indentationLineProcessor.GetProcessedLine(lineNumber, newIndentation);
        return _indentRulesSupport.ShouldIncrease(processedLine);
    }

    /// <summary>Apply the new indentation and return whether the indentation level should be decreased after the given line number.</summary>
    public bool ShouldDecrease(int lineNumber, string? newIndentation = null)
    {
        string processedLine = _indentationLineProcessor.GetProcessedLine(lineNumber, newIndentation);
        return _indentRulesSupport.ShouldDecrease(processedLine);
    }

    /// <summary>Apply the new indentation and return whether the indentation level should remain unchanged at the given line number.</summary>
    public bool ShouldIgnore(int lineNumber, string? newIndentation = null)
    {
        string processedLine = _indentationLineProcessor.GetProcessedLine(lineNumber, newIndentation);
        return _indentRulesSupport.ShouldIgnore(processedLine);
    }

    /// <summary>Apply the new indentation and return whether the indentation level should increase on the line after the given line number.</summary>
    public bool ShouldIndentNextLine(int lineNumber, string? newIndentation = null)
    {
        string processedLine = _indentationLineProcessor.GetProcessedLine(lineNumber, newIndentation);
        return _indentRulesSupport.ShouldIndentNextLine(processedLine);
    }
}

/// <summary>The processed tokens around a range (the result of <see cref="IndentationContextProcessor.GetProcessedTokenContextAroundRange"/>).</summary>
/// <param name="BeforeRangeProcessedTokens">The processed text before the range and on the same start line.</param>
/// <param name="AfterRangeProcessedTokens">The processed text after the range and on the same end line.</param>
/// <param name="PreviousLineProcessedTokens">The processed text on the previous line.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct ProcessedTokenContext(
    IViewLineTokens BeforeRangeProcessedTokens,
    IViewLineTokens AfterRangeProcessedTokens,
    IViewLineTokens PreviousLineProcessedTokens);

/// <summary>
/// This class fetches the processed text around a range which can be used for indentation evaluation.
/// It returns:
/// - The processed text before the given range and on the same start line
/// - The processed text after the given range and on the same end line
/// - The processed text on the previous line
/// </summary>
/// <remarks>Threading: use it from the thread of the editor.</remarks>
internal sealed class IndentationContextProcessor
{
    private readonly TextModel _model;
    private readonly IndentationLineProcessor _indentationLineProcessor;

    public IndentationContextProcessor(TextModel model, LanguageConfigurationRegistry registry)
    {
        _model = model;
        _indentationLineProcessor = new IndentationLineProcessor(model, registry);
    }

    /// <summary>
    /// Returns the processed text, stripped from the language configuration brackets within the string, comment and
    /// regex tokens, around the given range.
    /// </summary>
    public ProcessedTokenContext GetProcessedTokenContextAroundRange(Range range)
    {
        IViewLineTokens beforeRangeProcessedTokens = GetProcessedTokensBeforeRange(range);
        IViewLineTokens afterRangeProcessedTokens = GetProcessedTokensAfterRange(range);
        IViewLineTokens previousLineProcessedTokens = GetProcessedPreviousLineTokens(range);
        return new ProcessedTokenContext(beforeRangeProcessedTokens, afterRangeProcessedTokens, previousLineProcessedTokens);
    }

    /// <summary>
    /// Whether the language at <paramref name="position"/> is embedded in the line: its scope does not start the line
    /// and differs from the language of the line start (Monaco's module function <c>isLanguageDifferentFromLineStart</c>).
    /// </summary>
    public static bool IsLanguageDifferentFromLineStart(TextModel model, Position position)
    {
        model.Tokenization.ForceTokenization(position.LineNumber);
        LineTokens lineTokens = model.Tokenization.GetLineTokens(position.LineNumber);
        ScopedLineTokens scopedLineTokens = ScopedLineTokens.Create(lineTokens, position.Column - 1);
        bool doesScopeStartAtOffsetZero = scopedLineTokens.FirstCharOffset == 0;
        bool isScopedLanguageEqualToFirstLanguageOnLine = lineTokens.GetLanguageId(0) == scopedLineTokens.LanguageId;
        bool languageIsDifferentFromLineStart = !doesScopeStartAtOffsetZero && !isScopedLanguageEqualToFirstLanguageOnLine;
        return languageIsDifferentFromLineStart;
    }

    private IViewLineTokens GetProcessedTokensBeforeRange(Range range)
    {
        _model.Tokenization.ForceTokenization(range.StartLineNumber);
        LineTokens lineTokens = _model.Tokenization.GetLineTokens(range.StartLineNumber);
        ScopedLineTokens scopedLineTokens = ScopedLineTokens.Create(lineTokens, range.StartColumn - 1);
        IViewLineTokens slicedTokens;
        if (IsLanguageDifferentFromLineStart(_model, range.Start))
        {
            int columnIndexWithinScope = (range.StartColumn - 1) - scopedLineTokens.FirstCharOffset;
            int firstCharacterOffset = scopedLineTokens.FirstCharOffset;
            int lastCharacterOffset = firstCharacterOffset + columnIndexWithinScope;
            slicedTokens = lineTokens.SliceAndInflate(firstCharacterOffset, lastCharacterOffset, 0);
        }
        else
        {
            int columnWithinLine = range.StartColumn - 1;
            slicedTokens = lineTokens.SliceAndInflate(0, columnWithinLine, 0);
        }

        IViewLineTokens processedTokens = _indentationLineProcessor.GetProcessedTokens(slicedTokens);
        return processedTokens;
    }

    private IViewLineTokens GetProcessedTokensAfterRange(Range range)
    {
        Position position = range.IsEmpty ? range.Start : range.End;
        _model.Tokenization.ForceTokenization(position.LineNumber);
        LineTokens lineTokens = _model.Tokenization.GetLineTokens(position.LineNumber);
        ScopedLineTokens scopedLineTokens = ScopedLineTokens.Create(lineTokens, position.Column - 1);
        int columnIndexWithinScope = position.Column - 1 - scopedLineTokens.FirstCharOffset;
        int firstCharacterOffset = scopedLineTokens.FirstCharOffset + columnIndexWithinScope;
        int lastCharacterOffset = scopedLineTokens.FirstCharOffset + scopedLineTokens.GetLineLength();
        IViewLineTokens slicedTokens = lineTokens.SliceAndInflate(firstCharacterOffset, lastCharacterOffset, 0);
        IViewLineTokens processedTokens = _indentationLineProcessor.GetProcessedTokens(slicedTokens);
        return processedTokens;
    }

    private IViewLineTokens GetProcessedPreviousLineTokens(Range range)
    {
        ScopedLineTokens GetScopedLineTokensAtEndColumnOfLine(int lineNumber)
        {
            _model.Tokenization.ForceTokenization(lineNumber);
            LineTokens lineTokens = _model.Tokenization.GetLineTokens(lineNumber);
            int endColumnOfLine = _model.GetLineMaxColumn(lineNumber) - 1;
            ScopedLineTokens scopedLineTokensAtEndColumn = ScopedLineTokens.Create(lineTokens, endColumnOfLine);
            return scopedLineTokensAtEndColumn;
        }

        _model.Tokenization.ForceTokenization(range.StartLineNumber);
        LineTokens lineTokens = _model.Tokenization.GetLineTokens(range.StartLineNumber);
        ScopedLineTokens scopedLineTokens = ScopedLineTokens.Create(lineTokens, range.StartColumn - 1);
        LineTokens emptyTokens = LineTokens.CreateEmpty(string.Empty, scopedLineTokens.LanguageIdCodec);
        int previousLineNumber = range.StartLineNumber - 1;
        bool isFirstLine = previousLineNumber == 0;
        if (isFirstLine)
        {
            return emptyTokens;
        }

        bool canScopeExtendOnPreviousLine = scopedLineTokens.FirstCharOffset == 0;
        if (!canScopeExtendOnPreviousLine)
        {
            return emptyTokens;
        }

        ScopedLineTokens scopedLineTokensAtEndColumnOfPreviousLine = GetScopedLineTokensAtEndColumnOfLine(previousLineNumber);
        bool doesLanguageContinueOnPreviousLine = scopedLineTokens.LanguageId == scopedLineTokensAtEndColumnOfPreviousLine.LanguageId;
        if (!doesLanguageContinueOnPreviousLine)
        {
            return emptyTokens;
        }

        IViewLineTokens previousSlicedLineTokens = scopedLineTokensAtEndColumnOfPreviousLine.ToIViewLineTokens();
        IViewLineTokens processedTokens = _indentationLineProcessor.GetProcessedTokens(previousSlicedLineTokens);
        return processedTokens;
    }
}

/// <summary>
/// This class performs the actual processing of the indentation lines.
/// The brackets of the language configuration are removed from the regex, string and comment tokens.
/// </summary>
/// <remarks>Threading: use it from the thread of the editor.</remarks>
internal sealed class IndentationLineProcessor(IVirtualModel model, LanguageConfigurationRegistry registry)
{
    /// <summary>
    /// Get the processed line for the given line number and potentially adjust the indentation level.
    /// Remove the language configuration brackets from the regex, string and comment tokens.
    /// </summary>
    public string GetProcessedLine(int lineNumber, string? newIndentation)
    {
        static string ReplaceIndentation(string line, string newIndentation)
        {
            string currentIndentation = Strings.GetLeadingWhitespace(line);
            string adjustedLine = newIndentation + line[currentIndentation.Length..];
            return adjustedLine;
        }

        model.ForceTokenization(lineNumber);
        IViewLineTokens tokens = model.GetLineTokens(lineNumber);
        string processedLine = GetProcessedTokens(tokens).GetLineContent();
        if (newIndentation is not null)
        {
            processedLine = ReplaceIndentation(processedLine, newIndentation);
        }

        return processedLine;
    }

    /// <summary>Process the line with the given tokens, remove the language configuration brackets from the regex, string and comment tokens.</summary>
    public IViewLineTokens GetProcessedTokens(IViewLineTokens tokens)
    {
        static bool ShouldRemoveBracketsFromTokenType(StandardTokenType tokenType) =>
            tokenType == StandardTokenType.String
            || tokenType == StandardTokenType.RegEx
            || tokenType == StandardTokenType.Comment;

        string languageId = tokens.GetLanguageId(0);
        LanguageBracketsConfiguration bracketsConfiguration = registry.GetLanguageConfiguration(languageId).BracketsNew;
        Regex bracketsRegExp = bracketsConfiguration.GetBracketRegExp();
        var textAndMetadata = new List<(string Text, uint Metadata)>();
        tokens.ForEach(tokenIndex =>
        {
            StandardTokenType tokenType = tokens.GetStandardTokenType(tokenIndex);
            string text = tokens.GetTokenText(tokenIndex);
            if (ShouldRemoveBracketsFromTokenType(tokenType))
            {
                text = bracketsRegExp.Replace(text, string.Empty);
            }

            uint metadata = tokens.GetMetadata(tokenIndex);
            textAndMetadata.Add((text, metadata));
        });
        LineTokens processedLineTokens = LineTokens.CreateFromTextAndMetadata(textAndMetadata, tokens.LanguageIdCodec);
        return processedLineTokens;
    }
}

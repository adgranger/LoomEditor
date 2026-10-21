// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/moveLinesCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Features.Indentation;

namespace LoomEditor.View.Features.LinesOperations;

// The auto-indent helpers of MoveLinesCommand (buildIndentConverter ... getIndentEditsOfMovingBlock).
internal sealed partial class MoveLinesCommand
{
    private static IndentConverter BuildIndentConverter(int tabSize, int indentSize, bool insertSpaces) =>
        new IndentConverter(tabSize, indentSize, insertSpaces);

    // Monaco's /^\s+/ removal; string.TrimStart uses Unicode white space, which is what JavaScript's \s is up to a few characters.
    private static string TrimStart(string str) => str.TrimStart();

    private int? ParseEnterResult(TextModel model, IIndentConverter indentConverter, int tabSize, int line, CompleteEnterAction? enter)
    {
        if (enter is not { } e)
        {
            return null;
        }

        string enterPrefix = e.Indentation;

        if (e.IndentAction == IndentAction.None)
        {
            enterPrefix = e.Indentation + e.AppendText;
        }
        else if (e.IndentAction == IndentAction.Indent)
        {
            enterPrefix = e.Indentation + e.AppendText;
        }
        else if (e.IndentAction == IndentAction.IndentOutdent)
        {
            enterPrefix = e.Indentation;
        }
        else if (e.IndentAction == IndentAction.Outdent)
        {
            enterPrefix = indentConverter.UnshiftIndent(e.Indentation) + e.AppendText;
        }

        string movingLineText = model.GetLineContent(line);
        if (TrimStart(movingLineText).Contains(TrimStart(enterPrefix), StringComparison.Ordinal))
        {
            string oldIndentation = Strings.GetLeadingWhitespace(model.GetLineContent(line));
            string newIndentation = Strings.GetLeadingWhitespace(enterPrefix);
            int? indentMetadataOfMovelingLine = AutoIndent.GetIndentMetadata(model, line, languageConfigurations);
            if (indentMetadataOfMovelingLine is { } metadata && (metadata & IndentConsts.DecreaseMask) != 0)
            {
                newIndentation = indentConverter.UnshiftIndent(newIndentation);
            }

            int newSpaceCnt = IndentUtils.GetSpaceCnt(newIndentation, tabSize);
            int oldSpaceCnt = IndentUtils.GetSpaceCnt(oldIndentation, tabSize);
            return newSpaceCnt - oldSpaceCnt;
        }

        return null;
    }

    /// <param name="model">The model.</param>
    /// <param name="indentConverter">The indent converter.</param>
    /// <param name="tabSize">The tab size.</param>
    /// <param name="line">The line moving down.</param>
    /// <param name="futureAboveLineNumber">The line which will be at the <paramref name="line"/> position.</param>
    /// <param name="futureAboveLineText">The text of that line.</param>
    private int? MatchEnterRuleMovingDown(TextModel model, IIndentConverter indentConverter, int tabSize, int line, int futureAboveLineNumber, string futureAboveLineText)
    {
        if (Strings.LastNonWhitespaceIndex(futureAboveLineText) >= 0)
        {
            // break
            int maxColumn = model.GetLineMaxColumn(futureAboveLineNumber);
            CompleteEnterAction? enter = EnterActions.GetEnterAction(autoIndent, model, new Range(futureAboveLineNumber, maxColumn, futureAboveLineNumber, maxColumn), languageConfigurations);
            return ParseEnterResult(model, indentConverter, tabSize, line, enter);
        }
        else
        {
            // go upwards, starting from `line - 1`
            int validPrecedingLine = line - 1;
            while (validPrecedingLine >= 1)
            {
                string lineContent = model.GetLineContent(validPrecedingLine);
                int nonWhitespaceIdx = Strings.LastNonWhitespaceIndex(lineContent);

                if (nonWhitespaceIdx >= 0)
                {
                    break;
                }

                validPrecedingLine--;
            }

            if (validPrecedingLine < 1 || line > model.LineCount)
            {
                return null;
            }

            int maxColumn = model.GetLineMaxColumn(validPrecedingLine);
            CompleteEnterAction? enter = EnterActions.GetEnterAction(autoIndent, model, new Range(validPrecedingLine, maxColumn, validPrecedingLine, maxColumn), languageConfigurations);
            return ParseEnterResult(model, indentConverter, tabSize, line, enter);
        }
    }

    private int? MatchEnterRule(TextModel model, IIndentConverter indentConverter, int tabSize, int line, int oneLineAbove)
    {
        int validPrecedingLine = oneLineAbove;
        while (validPrecedingLine >= 1)
        {
            // ship empty lines as empty lines just inherit indentation
            string lineContent = model.GetLineContent(validPrecedingLine);

            int nonWhitespaceIdx = Strings.LastNonWhitespaceIndex(lineContent);
            if (nonWhitespaceIdx >= 0)
            {
                break;
            }

            validPrecedingLine--;
        }

        if (validPrecedingLine < 1 || line > model.LineCount)
        {
            return null;
        }

        int maxColumn = model.GetLineMaxColumn(validPrecedingLine);
        CompleteEnterAction? enter = EnterActions.GetEnterAction(autoIndent, model, new Range(validPrecedingLine, maxColumn, validPrecedingLine, maxColumn), languageConfigurations);
        return ParseEnterResult(model, indentConverter, tabSize, line, enter);
    }

    private bool ShouldAutoIndent(TextModel model, Selection s)
    {
        if (autoIndent < EditorAutoIndentStrategy.Full)
        {
            return false;
        }

        // if it's not easy to tokenize, we stop auto indent.
        if (!model.Tokenization.IsCheapToTokenize(s.Range.StartLineNumber))
        {
            return false;
        }

        string languageAtSelectionStart = model.Tokenization.GetLanguageIdAtPosition(s.Range.StartLineNumber, 1);
        string languageAtSelectionEnd = model.Tokenization.GetLanguageIdAtPosition(s.Range.EndLineNumber, 1);

        if (languageAtSelectionStart != languageAtSelectionEnd)
        {
            return false;
        }

        return languageConfigurations.GetLanguageConfiguration(languageAtSelectionStart).IndentRulesSupport is not null;
    }

    private void GetIndentEditsOfMovingBlock(TextModel model, IEditOperationBuilder builder, Selection s, int tabSize, bool insertSpaces, int offset)
    {
        for (int i = s.Range.StartLineNumber; i <= s.Range.EndLineNumber; i++)
        {
            string lineContent = model.GetLineContent(i);
            string originalIndent = Strings.GetLeadingWhitespace(lineContent);
            int originalSpacesCnt = IndentUtils.GetSpaceCnt(originalIndent, tabSize);
            int newSpacesCnt = originalSpacesCnt + offset;
            string newIndent = IndentUtils.GenerateIndent(newSpacesCnt, tabSize, insertSpaces);

            if (newIndent != originalIndent)
            {
                builder.AddEditOperation(new Range(i, 1, i, originalIndent.Length + 1), newIndent);

                if (i == s.Range.EndLineNumber && s.Range.EndColumn <= originalIndent.Length + 1 && newIndent.Length == 0)
                {
                    // as users select part of the original indent white spaces
                    // when we adjust the indentation of endLine, we should adjust the cursor position as well.
                    _moveEndLineSelectionShrink = true;
                }
            }
        }
    }

    private sealed class IndentConverter(int tabSize, int indentSize, bool insertSpaces) : IIndentConverter
    {
        public string ShiftIndent(string indentation) =>
            ShiftCommand.ShiftIndent(indentation, indentation.Length + 1, tabSize, indentSize, insertSpaces);

        public string UnshiftIndent(string indentation) =>
            ShiftCommand.UnshiftIndent(indentation, indentation.Length + 1, tabSize, indentSize, insertSpaces);
    }

    // Monaco: createVirtualModel. The line number mapper applies to the tokens and to the content, then the override wins.
    private sealed class MappedVirtualModel(TextModel model, Func<int, int> lineNumberMapper, Func<int, string?>? contentOverride) : IVirtualModel
    {
        public IViewLineTokens GetLineTokens(int lineNumber) => model.Tokenization.GetLineTokens(lineNumberMapper(lineNumber));

        public string GetLanguageId() => model.Tokenization.GetLanguageId();

        public string GetLanguageIdAtPosition(int lineNumber, int column) => model.Tokenization.GetLanguageIdAtPosition(lineNumber, column);

        // Monaco's object has no forceTokenization.
        public void ForceTokenization(int lineNumber)
        {
        }

        public string GetLineContent(int lineNumber) =>
            contentOverride?.Invoke(lineNumber) ?? model.GetLineContent(lineNumberMapper(lineNumber));
    }
}

// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/browser/indentation.ts (AutoIndentOnPaste, isStartOrEndInString)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Indentation;

/// <summary>After a keyboard paste, gives the first pasted line the indentation the language rules want and moves the others by the same amount.</summary>
/// <remarks>
/// Monaco's <c>update()</c> (subscribe again when the configuration, model or language changes) becomes a check at the
/// start of the paste handler. Threading: UI thread only.
/// </remarks>
internal sealed class AutoIndentOnPaste : IDisposable
{
    private readonly ViewModel _viewModel;
    private readonly Func<EditorIndentationOptions> _options;
    private readonly LanguageConfigurationRegistry _languageConfigurations;

    public AutoIndentOnPaste(ViewModel viewModel, Func<EditorIndentationOptions> options, LanguageConfigurationRegistry languageConfigurations)
    {
        _viewModel = viewModel;
        _options = options;
        _languageConfigurations = languageConfigurations;
        _viewModel.DidPaste += OnDidPaste;
    }

    public void Dispose() => _viewModel.DidPaste -= OnDidPaste;

    private void OnDidPaste(object? sender, Range range)
    {
        if (!_options().AutoIndentOnPaste || _viewModel.CursorConfig.AutoIndent < EditorAutoIndentStrategy.Full)
        {
            return;
        }

        Trigger(range);
    }

    /// <summary>Re-indents the pasted <paramref name="range"/>. It does not check the options (like Monaco's tests, which call it directly).</summary>
    internal void Trigger(Range range)
    {
        if (_viewModel.GetSelections().Count > 1)
        {
            return;
        }

        TextModel model = _viewModel.Model;
        if (RangeContainsOnlyWhitespaceCharacters(model, range))
        {
            return;
        }

        if (!_options().AutoIndentOnPasteWithinString && IsStartOrEndInString(model, range))
        {
            return;
        }

        if (!model.Tokenization.IsCheapToTokenize(range.StartLineNumber))
        {
            return;
        }

        EditorAutoIndentStrategy autoIndent = _viewModel.CursorConfig.AutoIndent;
        TextModelOptions modelOptions = model.Options;
        int tabSize = modelOptions.TabSize;
        bool insertSpaces = modelOptions.InsertSpaces;
        var indentConverter = new IndentConverter(modelOptions);
        var textEdits = new List<(Range Range, string Text)>();

        int startLineNumber = range.StartLineNumber;

        string firstLineText = model.GetLineContent(startLineNumber);
        if (Strings.IsJavaScriptWhitespaceOnly(firstLineText[..(range.StartColumn - 1)]))
        {
            string? indentOfFirstLine = AutoIndent.GetGoodIndentForLine(autoIndent, model, model.Tokenization.GetLanguageId(), startLineNumber, indentConverter, _languageConfigurations);

            if (indentOfFirstLine is not null)
            {
                string oldIndentation = Strings.GetLeadingWhitespace(firstLineText);
                int newSpaceCnt = IndentUtils.GetSpaceCnt(indentOfFirstLine, tabSize);
                int oldSpaceCnt = IndentUtils.GetSpaceCnt(oldIndentation, tabSize);

                if (newSpaceCnt != oldSpaceCnt)
                {
                    string newIndent = IndentUtils.GenerateIndent(newSpaceCnt, tabSize, insertSpaces);
                    textEdits.Add((new Range(startLineNumber, 1, startLineNumber, oldIndentation.Length + 1), newIndent));
                    firstLineText = newIndent + firstLineText[oldIndentation.Length..];
                }
                else
                {
                    int? indentMetadata = AutoIndent.GetIndentMetadata(model, startLineNumber, _languageConfigurations);

                    if (indentMetadata is 0 or IndentConsts.UnIndentMask)
                    {
                        // we paste content into a line where only contains whitespaces
                        // after pasting, the indentation of the first line is already correct
                        // the first line doesn't match any indentation rule
                        // then no-op.
                        return;
                    }
                }
            }
        }

        int firstLineNumber = startLineNumber;

        // ignore empty or ignored lines
        while (startLineNumber < range.EndLineNumber)
        {
            if (Strings.IsJavaScriptWhitespaceOnly(model.GetLineContent(startLineNumber + 1)))
            {
                startLineNumber++;
                continue;
            }

            break;
        }

        if (startLineNumber != range.EndLineNumber)
        {
            var virtualModel = new PastedVirtualModel(model, firstLineNumber, firstLineText);
            string? indentOfSecondLine = AutoIndent.GetGoodIndentForLine(autoIndent, virtualModel, model.Tokenization.GetLanguageId(), startLineNumber + 1, indentConverter, _languageConfigurations);
            if (indentOfSecondLine is not null)
            {
                int newSpaceCntOfSecondLine = IndentUtils.GetSpaceCnt(indentOfSecondLine, tabSize);
                int oldSpaceCntOfSecondLine = IndentUtils.GetSpaceCnt(Strings.GetLeadingWhitespace(model.GetLineContent(startLineNumber + 1)), tabSize);

                if (newSpaceCntOfSecondLine != oldSpaceCntOfSecondLine)
                {
                    int spaceCntOffset = newSpaceCntOfSecondLine - oldSpaceCntOfSecondLine;
                    for (int i = startLineNumber + 1; i <= range.EndLineNumber; i++)
                    {
                        string lineContent = model.GetLineContent(i);
                        string originalIndent = Strings.GetLeadingWhitespace(lineContent);
                        int originalSpacesCnt = IndentUtils.GetSpaceCnt(originalIndent, tabSize);
                        int newSpacesCnt = originalSpacesCnt + spaceCntOffset;
                        string newIndent = IndentUtils.GenerateIndent(newSpacesCnt, tabSize, insertSpaces);

                        if (newIndent != originalIndent)
                        {
                            textEdits.Add((new Range(i, 1, i, originalIndent.Length + 1), newIndent));
                        }
                    }
                }
            }
        }

        if (textEdits.Count > 0)
        {
            model.PushStackElement();
            _viewModel.ExecuteCommand(new AutoIndentOnPasteCommand(textEdits, _viewModel.GetSelection()), "autoIndentOnPaste");
            model.PushStackElement();
        }
    }

    private static bool RangeContainsOnlyWhitespaceCharacters(TextModel model, Range range)
    {
        bool containsOnlyWhitespace = true;
        if (range.StartLineNumber == range.EndLineNumber)
        {
            string lineContent = model.GetLineContent(range.StartLineNumber);
            containsOnlyWhitespace = Strings.IsJavaScriptWhitespaceOnly(lineContent[(range.StartColumn - 1)..(range.EndColumn - 1)]);
        }
        else
        {
            for (int i = range.StartLineNumber; i <= range.EndLineNumber; i++)
            {
                string lineContent = model.GetLineContent(i);
                if (i == range.StartLineNumber)
                {
                    containsOnlyWhitespace = Strings.IsJavaScriptWhitespaceOnly(lineContent[(range.StartColumn - 1)..]);
                }
                else if (i == range.EndLineNumber)
                {
                    containsOnlyWhitespace = Strings.IsJavaScriptWhitespaceOnly(lineContent[..(range.EndColumn - 1)]);
                }
                else
                {
                    containsOnlyWhitespace = model.GetLineFirstNonWhitespaceColumn(i) == 0;
                }

                if (!containsOnlyWhitespace)
                {
                    break;
                }
            }
        }

        return containsOnlyWhitespace;
    }

    private static bool IsStartOrEndInString(TextModel model, Range range)
    {
        static bool IsPositionInString(TextModel model, Position position) =>
            LineTokens.GetStandardTokenTypeAtPosition(model, position) == StandardTokenType.String;

        return IsPositionInString(model, range.Start) || IsPositionInString(model, range.End);
    }

    private sealed class IndentConverter(TextModelOptions options) : IIndentConverter
    {
        public string ShiftIndent(string indentation) =>
            ShiftCommand.ShiftIndent(indentation, indentation.Length + 1, options.TabSize, options.IndentSize, options.InsertSpaces);

        public string UnshiftIndent(string indentation) =>
            ShiftCommand.UnshiftIndent(indentation, indentation.Length + 1, options.TabSize, options.IndentSize, options.InsertSpaces);
    }

    // The model, except that the first pasted line has the content it will have once its indentation is fixed.
    private sealed class PastedVirtualModel(TextModel model, int firstLineNumber, string firstLineText) : IVirtualModel
    {
        public IViewLineTokens GetLineTokens(int lineNumber) => model.Tokenization.GetLineTokens(lineNumber);

        public string GetLanguageId() => model.Tokenization.GetLanguageId();

        public string GetLanguageIdAtPosition(int lineNumber, int column) => model.Tokenization.GetLanguageIdAtPosition(lineNumber, column);

        public void ForceTokenization(int lineNumber) => model.Tokenization.ForceTokenization(lineNumber);

        public string GetLineContent(int lineNumber) => lineNumber == firstLineNumber ? firstLineText : model.GetLineContent(lineNumber);
    }
}

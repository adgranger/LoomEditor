// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/moveLinesCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Features.Indentation;

namespace LoomEditor.View.Features.LinesOperations;

/// <summary>Moves the selected lines up or down, re-indenting them with the rules of the language when auto indent is full.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class MoveLinesCommand(
    Selection selection,
    bool isMovingDown,
    EditorAutoIndentStrategy autoIndent,
    LanguageConfigurationRegistry languageConfigurations) : IEditCommand
{
    private string? _selectionId;
    private bool _moveEndPositionDown;
    private bool _moveEndLineSelectionShrink;

    public bool InsertsAutoWhitespace => false;

    private static MappedVirtualModel CreateVirtualModel(
        TextModel model,
        Func<int, int> lineNumberMapper,
        Func<int, string?>? contentOverride = null) =>
        new MappedVirtualModel(model, lineNumberMapper, contentOverride);

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        int modelLineCount = model.LineCount;

        if (isMovingDown && selection.Range.EndLineNumber == modelLineCount)
        {
            _selectionId = builder.TrackSelection(selection);
            return;
        }

        if (!isMovingDown && selection.Range.StartLineNumber == 1)
        {
            _selectionId = builder.TrackSelection(selection);
            return;
        }

        _moveEndPositionDown = false;
        Selection s = selection;

        if (s.Range.StartLineNumber < s.Range.EndLineNumber && s.Range.EndColumn == 1)
        {
            _moveEndPositionDown = true;
            s = s.WithEnd(new Position(s.Range.EndLineNumber - 1, model.GetLineMaxColumn(s.Range.EndLineNumber - 1)));
        }

        TextModelOptions options = model.Options;
        int tabSize = options.TabSize;
        bool insertSpaces = options.InsertSpaces;
        IIndentConverter indentConverter = BuildIndentConverter(tabSize, options.IndentSize, insertSpaces);

        int startLineNumber = s.Range.StartLineNumber;
        int endLineNumber = s.Range.EndLineNumber;

        if (startLineNumber == endLineNumber && model.GetLineMaxColumn(startLineNumber) == 1)
        {
            // Current line is empty
            int lineNumber = startLineNumber;
            int otherLineNumber = isMovingDown ? lineNumber + 1 : lineNumber - 1;

            if (model.GetLineMaxColumn(otherLineNumber) == 1)
            {
                // Other line number is empty too, so no editing is needed
                // Add a no-op to force running by the model
                builder.AddEditOperation(new Range(1, 1, 1, 1), null);
            }
            else
            {
                // Type content from other line number on line number
                builder.AddEditOperation(new Range(lineNumber, 1, lineNumber, 1), model.GetLineContent(otherLineNumber));

                // Remove content from other line number
                builder.AddEditOperation(new Range(otherLineNumber, 1, otherLineNumber, model.GetLineMaxColumn(otherLineNumber)), null);
            }

            // Track selection at the other line number
            s = new Selection(otherLineNumber, 1, otherLineNumber, 1);
        }
        else if (isMovingDown)
        {
            MoveDown(model, builder, s, indentConverter, tabSize, insertSpaces);
        }
        else
        {
            MoveUp(model, builder, s, indentConverter, tabSize, insertSpaces);
        }

        _selectionId = builder.TrackSelection(s);
    }

    private void MoveDown(TextModel model, IEditOperationBuilder builder, Selection s, IIndentConverter indentConverter, int tabSize, bool insertSpaces)
    {
        int startLineNumber = s.Range.StartLineNumber;
        int endLineNumber = s.Range.EndLineNumber;
        int movingLineNumber = endLineNumber + 1;
        string movingLineText = model.GetLineContent(movingLineNumber);

        // Delete line that needs to be moved
        builder.AddEditOperation(
            new Range(movingLineNumber - 1, model.GetLineMaxColumn(movingLineNumber - 1), movingLineNumber, model.GetLineMaxColumn(movingLineNumber)),
            null);

        string insertingText = movingLineText;

        if (!ShouldAutoIndent(model, s))
        {
            // Insert line that needs to be moved before
            builder.AddEditOperation(new Range(startLineNumber, 1, startLineNumber, 1), insertingText + "\n");
            return;
        }

        int? movingLineMatchResult = MatchEnterRule(model, indentConverter, tabSize, movingLineNumber, startLineNumber - 1);
        // if s.startLineNumber - 1 matches onEnter rule, we still honor that.
        if (movingLineMatchResult is { } matchResult)
        {
            string oldIndentation = Strings.GetLeadingWhitespace(model.GetLineContent(movingLineNumber));
            int newSpaceCnt = matchResult + IndentUtils.GetSpaceCnt(oldIndentation, tabSize);
            string newIndentation = IndentUtils.GenerateIndent(newSpaceCnt, tabSize, insertSpaces);
            insertingText = newIndentation + TrimStart(movingLineText);
        }
        else
        {
            // no enter rule matches, let's check indentatin rules then.
            IVirtualModel virtualModel = CreateVirtualModel(model, lineNumber => lineNumber == startLineNumber ? movingLineNumber : lineNumber);
            string? indentOfMovingLine = AutoIndent.GetGoodIndentForLine(
                autoIndent,
                virtualModel,
                model.Tokenization.GetLanguageIdAtPosition(movingLineNumber, 1),
                startLineNumber,
                indentConverter,
                languageConfigurations);
            if (indentOfMovingLine is not null)
            {
                string oldIndentation = Strings.GetLeadingWhitespace(model.GetLineContent(movingLineNumber));
                int newSpaceCnt = IndentUtils.GetSpaceCnt(indentOfMovingLine, tabSize);
                int oldSpaceCnt = IndentUtils.GetSpaceCnt(oldIndentation, tabSize);
                if (newSpaceCnt != oldSpaceCnt)
                {
                    string newIndentation = IndentUtils.GenerateIndent(newSpaceCnt, tabSize, insertSpaces);
                    insertingText = newIndentation + TrimStart(movingLineText);
                }
            }
        }

        // add edit operations for moving line first to make sure it's executed after we make indentation change
        // to s.startLineNumber
        builder.AddEditOperation(new Range(startLineNumber, 1, startLineNumber, 1), insertingText + "\n");

        int? ret = MatchEnterRuleMovingDown(model, indentConverter, tabSize, startLineNumber, movingLineNumber, insertingText);

        // check if the line being moved before matches onEnter rules, if so let's adjust the indentation by onEnter rules.
        if (ret is { } enterOffset)
        {
            if (enterOffset != 0)
            {
                GetIndentEditsOfMovingBlock(model, builder, s, tabSize, insertSpaces, enterOffset);
            }
        }
        else
        {
            // it doesn't match onEnter rules, let's check indentation rules then.
            string finalInsertingText = insertingText;
            IVirtualModel virtualModel = CreateVirtualModel(
                model,
                lineNumber =>
                {
                    if (lineNumber == startLineNumber)
                    {
                        // TODO@aiday-mar (Monaco): the tokens here don't correspond exactly to the corresponding content (after indentation adjustment), have to fix this.
                        return movingLineNumber;
                    }

                    return lineNumber >= startLineNumber + 1 && lineNumber <= endLineNumber + 1 ? lineNumber - 1 : lineNumber;
                },
                lineNumber => lineNumber == startLineNumber ? finalInsertingText : null);

            string? newIndentatOfMovingBlock = AutoIndent.GetGoodIndentForLine(
                autoIndent,
                virtualModel,
                model.Tokenization.GetLanguageIdAtPosition(movingLineNumber, 1),
                startLineNumber + 1,
                indentConverter,
                languageConfigurations);

            if (newIndentatOfMovingBlock is not null)
            {
                string oldIndentation = Strings.GetLeadingWhitespace(model.GetLineContent(startLineNumber));
                int newSpaceCnt = IndentUtils.GetSpaceCnt(newIndentatOfMovingBlock, tabSize);
                int oldSpaceCnt = IndentUtils.GetSpaceCnt(oldIndentation, tabSize);
                if (newSpaceCnt != oldSpaceCnt)
                {
                    GetIndentEditsOfMovingBlock(model, builder, s, tabSize, insertSpaces, newSpaceCnt - oldSpaceCnt);
                }
            }
        }
    }

    private void MoveUp(TextModel model, IEditOperationBuilder builder, Selection s, IIndentConverter indentConverter, int tabSize, bool insertSpaces)
    {
        int startLineNumber = s.Range.StartLineNumber;
        int endLineNumber = s.Range.EndLineNumber;
        int movingLineNumber = startLineNumber - 1;
        string movingLineText = model.GetLineContent(movingLineNumber);

        // Delete line that needs to be moved
        builder.AddEditOperation(new Range(movingLineNumber, 1, movingLineNumber + 1, 1), null);

        // Insert line that needs to be moved after
        int endMaxColumn = model.GetLineMaxColumn(endLineNumber);
        builder.AddEditOperation(new Range(endLineNumber, endMaxColumn, endLineNumber, endMaxColumn), "\n" + movingLineText);

        if (!ShouldAutoIndent(model, s))
        {
            return;
        }

        IVirtualModel virtualModel = CreateVirtualModel(model, lineNumber => lineNumber == movingLineNumber ? startLineNumber : lineNumber);

        int? ret = MatchEnterRule(model, indentConverter, tabSize, startLineNumber, startLineNumber - 2);
        // check if s.startLineNumber - 2 matches onEnter rules, if so adjust the moving block by onEnter rules.
        if (ret is { } enterOffset)
        {
            if (enterOffset != 0)
            {
                GetIndentEditsOfMovingBlock(model, builder, s, tabSize, insertSpaces, enterOffset);
            }
        }
        else
        {
            // it doesn't match any onEnter rule, let's check indentation rules then.
            string? indentOfFirstLine = AutoIndent.GetGoodIndentForLine(
                autoIndent,
                virtualModel,
                model.Tokenization.GetLanguageIdAtPosition(startLineNumber, 1),
                movingLineNumber,
                indentConverter,
                languageConfigurations);
            if (indentOfFirstLine is not null)
            {
                // adjust the indentation of the moving block
                string oldIndent = Strings.GetLeadingWhitespace(model.GetLineContent(startLineNumber));
                int newSpaceCnt = IndentUtils.GetSpaceCnt(indentOfFirstLine, tabSize);
                int oldSpaceCnt = IndentUtils.GetSpaceCnt(oldIndent, tabSize);
                if (newSpaceCnt != oldSpaceCnt)
                {
                    GetIndentEditsOfMovingBlock(model, builder, s, tabSize, insertSpaces, newSpaceCnt - oldSpaceCnt);
                }
            }
        }
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Selection result = helper.GetTrackedSelection(_selectionId!);

        if (_moveEndPositionDown)
        {
            result = result.WithEnd(new Position(result.Range.EndLineNumber + 1, 1));
        }

        if (_moveEndLineSelectionShrink && result.Range.StartLineNumber < result.Range.EndLineNumber)
        {
            result = result.WithEnd(new Position(result.Range.EndLineNumber, 2));
        }

        return result;
    }
}

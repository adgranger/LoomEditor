// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/copyLinesCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.LinesOperations;

/// <summary>Copies the selected lines above or below themselves.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CopyLinesCommand(Selection selection, bool isCopyingDown, bool noop = false) : IEditCommand
{
    private SelectionDirection _selectionDirection = SelectionDirection.LeftToRight;
    private string? _selectionId;
    private int _startLineNumberDelta;
    private int _endLineNumberDelta;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        Selection s = selection;

        _startLineNumberDelta = 0;
        _endLineNumberDelta = 0;
        if (s.Range.StartLineNumber < s.Range.EndLineNumber && s.Range.EndColumn == 1)
        {
            _endLineNumberDelta = 1;
            s = s.WithEnd(new Position(s.Range.EndLineNumber - 1, model.GetLineMaxColumn(s.Range.EndLineNumber - 1)));
        }

        var sourceLines = new List<string>();
        for (int i = s.Range.StartLineNumber; i <= s.Range.EndLineNumber; i++)
        {
            sourceLines.Add(model.GetLineContent(i));
        }

        string sourceText = string.Join('\n', sourceLines);

        if (sourceText.Length == 0)
        {
            // Duplicating empty line
            if (isCopyingDown)
            {
                _startLineNumberDelta++;
                _endLineNumberDelta++;
            }
        }

        int endLineNumber = s.Range.EndLineNumber;
        if (noop)
        {
            builder.AddEditOperation(
                new Range(endLineNumber, model.GetLineMaxColumn(endLineNumber), endLineNumber + 1, 1),
                endLineNumber == model.LineCount ? string.Empty : "\n");
        }
        else if (!isCopyingDown)
        {
            int maxColumn = model.GetLineMaxColumn(endLineNumber);
            builder.AddEditOperation(new Range(endLineNumber, maxColumn, endLineNumber, maxColumn), "\n" + sourceText);
        }
        else
        {
            builder.AddEditOperation(new Range(s.Range.StartLineNumber, 1, s.Range.StartLineNumber, 1), sourceText + "\n");
        }

        _selectionId = builder.TrackSelection(s);
        _selectionDirection = selection.Direction;
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Selection result = helper.GetTrackedSelection(_selectionId!);

        if (_startLineNumberDelta != 0 || _endLineNumberDelta != 0)
        {
            int startLineNumber = result.Range.StartLineNumber;
            int startColumn = result.Range.StartColumn;
            int endLineNumber = result.Range.EndLineNumber;
            int endColumn = result.Range.EndColumn;

            if (_startLineNumberDelta != 0)
            {
                startLineNumber += _startLineNumberDelta;
                startColumn = 1;
            }

            if (_endLineNumberDelta != 0)
            {
                endLineNumber += _endLineNumberDelta;
                endColumn = 1;
            }

            result = Selection.FromRange(new Range(startLineNumber, startColumn, endLineNumber, endColumn), _selectionDirection);
        }

        return result;
    }
}

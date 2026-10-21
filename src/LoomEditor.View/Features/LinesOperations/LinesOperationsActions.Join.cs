// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts (JoinLinesAction)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.LinesOperations;

internal static partial class LinesOperationsActions
{
    /// <summary>Joins the selected lines, or the line of a cursor with the next one (Monaco: <c>JoinLinesAction.run</c>).</summary>
    public static void JoinLines(ViewModel viewModel)
    {
        // Monaco sorts the selections array in place: a stable sorted copy is used.
        List<Selection> selections = [.. viewModel.GetSelections().OrderBy(s => s.Range, Comparer<Range>.Create(Range.CompareUsingStarts))];
        Selection primaryCursor = viewModel.GetSelection();

        var reducedSelections = new List<Selection>();

        // Monaco: selections.reduce(...) without an initial value.
        Selection lastSelection = selections[0];
        for (int index = 1; index < selections.Count; index++)
        {
            Selection previousValue = lastSelection;
            Selection currentValue = selections[index];

            if (previousValue.IsEmpty)
            {
                if (previousValue.Range.EndLineNumber == currentValue.Range.StartLineNumber)
                {
                    if (primaryCursor == previousValue)
                    {
                        primaryCursor = currentValue;
                    }

                    lastSelection = currentValue;
                }
                else if (currentValue.Range.StartLineNumber > previousValue.Range.EndLineNumber + 1)
                {
                    reducedSelections.Add(previousValue);
                    lastSelection = currentValue;
                }
                else
                {
                    lastSelection = new Selection(previousValue.Range.StartLineNumber, previousValue.Range.StartColumn, currentValue.Range.EndLineNumber, currentValue.Range.EndColumn);
                }
            }
            else if (currentValue.Range.StartLineNumber > previousValue.Range.EndLineNumber)
            {
                reducedSelections.Add(previousValue);
                lastSelection = currentValue;
            }
            else
            {
                lastSelection = new Selection(previousValue.Range.StartLineNumber, previousValue.Range.StartColumn, currentValue.Range.EndLineNumber, currentValue.Range.EndColumn);
            }
        }

        reducedSelections.Add(lastSelection);

        TextModel model = viewModel.Model;
        var edits = new List<IdentifiedEditOperation>();
        var endCursorState = new List<Selection>();
        Selection endPrimaryCursor = primaryCursor;
        int lineOffset = 0;

        foreach (Selection selection in reducedSelections)
        {
            Range range = selection.Range;
            int startLineNumber = range.StartLineNumber;
            const int StartColumn = 1;
            int columnDeltaOffset = 0;
            int endLineNumber;
            int endColumn;

            int selectionEndPositionOffset = model.GetLineLength(range.EndLineNumber) - range.EndColumn;

            if (selection.IsEmpty || range.StartLineNumber == range.EndLineNumber)
            {
                Position position = range.Start;
                if (position.LineNumber < model.LineCount)
                {
                    endLineNumber = startLineNumber + 1;
                    endColumn = model.GetLineMaxColumn(endLineNumber);
                }
                else
                {
                    endLineNumber = position.LineNumber;
                    endColumn = model.GetLineMaxColumn(position.LineNumber);
                }
            }
            else
            {
                endLineNumber = range.EndLineNumber;
                endColumn = model.GetLineMaxColumn(endLineNumber);
            }

            string trimmedLinesContent = model.GetLineContent(startLineNumber);

            for (int i = startLineNumber + 1; i <= endLineNumber; i++)
            {
                string lineText = model.GetLineContent(i);
                int firstNonWhitespaceIdx = model.GetLineFirstNonWhitespaceColumn(i);

                if (firstNonWhitespaceIdx >= 1)
                {
                    bool insertSpace = true;
                    if (trimmedLinesContent.Length == 0)
                    {
                        insertSpace = false;
                    }

                    if (insertSpace && trimmedLinesContent[^1] is ' ' or '\t')
                    {
                        insertSpace = false;
                        trimmedLinesContent = ReplaceTrailingWhitespaceWithSpace(trimmedLinesContent);
                    }

                    string lineTextWithoutIndent = lineText[(firstNonWhitespaceIdx - 1)..];

                    trimmedLinesContent += (insertSpace ? " " : string.Empty) + lineTextWithoutIndent;

                    columnDeltaOffset = insertSpace ? lineTextWithoutIndent.Length + 1 : lineTextWithoutIndent.Length;
                }
                else
                {
                    columnDeltaOffset = 0;
                }
            }

            var deleteSelection = new Range(startLineNumber, StartColumn, endLineNumber, endColumn);

            if (!deleteSelection.IsEmpty)
            {
                Selection resultSelection;

                edits.Add(new IdentifiedEditOperation(deleteSelection, trimmedLinesContent));
                if (selection.IsEmpty)
                {
                    resultSelection = Selection.Collapsed(new Position(deleteSelection.StartLineNumber - lineOffset, trimmedLinesContent.Length - columnDeltaOffset + 1));
                }
                else if (range.StartLineNumber == range.EndLineNumber)
                {
                    resultSelection = new Selection(range.StartLineNumber - lineOffset, range.StartColumn, range.EndLineNumber - lineOffset, range.EndColumn);
                }
                else
                {
                    resultSelection = new Selection(range.StartLineNumber - lineOffset, range.StartColumn, range.StartLineNumber - lineOffset, trimmedLinesContent.Length - selectionEndPositionOffset);
                }

                if (deleteSelection.Intersect(primaryCursor.Range) is not null)
                {
                    endPrimaryCursor = resultSelection;
                }
                else
                {
                    endCursorState.Add(resultSelection);
                }
            }

            lineOffset += deleteSelection.EndLineNumber - deleteSelection.StartLineNumber;
        }

        endCursorState.Insert(0, endPrimaryCursor);
        ExecuteEdits(viewModel, LinesOperationsCommands.JoinLines, edits, endCursorState);
    }

    // Monaco: replace(/[\s﻿\xA0]+$/g, ' '): the trailing run of JavaScript white space becomes one space.
    private static string ReplaceTrailingWhitespaceWithSpace(string text)
    {
        int end = text.Length;
        while (end > 0 && Strings.IsJavaScriptWhitespaceOnly(text[end - 1].ToString()))
        {
            end--;
        }

        return end == text.Length ? text : string.Concat(text.AsSpan(0, end), " ");
    }
}

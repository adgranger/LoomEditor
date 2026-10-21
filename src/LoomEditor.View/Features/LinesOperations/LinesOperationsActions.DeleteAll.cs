// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts (AbstractDeleteAllToBoundaryAction, DeleteAllLeftAction, DeleteAllRightAction)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.LinesOperations;

internal static partial class LinesOperationsActions
{
    /// <summary>
    /// Deletes from each cursor to the start (<c>deleteAllLeft</c>) or the end (<c>deleteAllRight</c>) of its line, joining
    /// lines at the boundaries (Monaco: <c>AbstractDeleteAllToBoundaryAction.run</c>).
    /// </summary>
    public static void DeleteAllToBoundary(ViewModel viewModel, string id, bool right)
    {
        Range primaryCursor = viewModel.GetSelection().Range;

        List<Range> rangesToDelete = right ? GetRangesToDeleteRight(viewModel) : GetRangesToDeleteLeft(viewModel);
        // merge overlapping selections
        var effectiveRanges = new List<Range>();

        for (int i = 0, count = rangesToDelete.Count - 1; i < count; i++)
        {
            Range range = rangesToDelete[i];
            Range nextRange = rangesToDelete[i + 1];

            if (range.Intersect(nextRange) is null)
            {
                effectiveRanges.Add(range);
            }
            else
            {
                rangesToDelete[i + 1] = range.Plus(nextRange);
            }
        }

        effectiveRanges.Add(rangesToDelete[^1]);

        List<Selection> endCursorState = right
            ? GetEndCursorStateRight(primaryCursor, effectiveRanges)
            : GetEndCursorStateLeft(primaryCursor, effectiveRanges);

        List<IdentifiedEditOperation> edits = [.. effectiveRanges.Select(range => new IdentifiedEditOperation(range, string.Empty))];

        ExecuteEdits(viewModel, id, edits, endCursorState);
    }

    private static List<Selection> GetEndCursorStateLeft(Range primaryCursor, List<Range> rangesToDelete)
    {
        Selection? endPrimaryCursor = null;
        var endCursorState = new List<Selection>();
        int deletedLines = 0;

        foreach (Range range in rangesToDelete)
        {
            Selection endCursor;
            if (range.EndColumn == 1 && deletedLines > 0)
            {
                int newStartLine = range.StartLineNumber - deletedLines;
                endCursor = Selection.Collapsed(new Position(newStartLine, range.StartColumn));
            }
            else
            {
                endCursor = Selection.Collapsed(new Position(range.StartLineNumber, range.StartColumn));
            }

            deletedLines += range.EndLineNumber - range.StartLineNumber;

            if (range.Intersect(primaryCursor) is not null)
            {
                endPrimaryCursor = endCursor;
            }
            else
            {
                endCursorState.Add(endCursor);
            }
        }

        if (endPrimaryCursor is { } primary)
        {
            endCursorState.Insert(0, primary);
        }

        return endCursorState;
    }

    private static List<Range> GetRangesToDeleteLeft(ViewModel viewModel)
    {
        TextModel model = viewModel.Model;

        // Monaco sorts the selections array in place: a stable sorted copy is used.
        return [.. viewModel.GetSelections()
            .OrderBy(s => s.Range, Comparer<Range>.Create(Range.CompareUsingStarts))
            .Select(selection =>
            {
                Range r = selection.Range;
                if (selection.IsEmpty)
                {
                    if (r.StartColumn == 1)
                    {
                        int deleteFromLine = Math.Max(1, r.StartLineNumber - 1);
                        int deleteFromColumn = r.StartLineNumber == 1 ? 1 : model.GetLineLength(deleteFromLine) + 1;
                        return new Range(deleteFromLine, deleteFromColumn, r.StartLineNumber, 1);
                    }

                    return new Range(r.StartLineNumber, 1, r.StartLineNumber, r.StartColumn);
                }

                return new Range(r.StartLineNumber, 1, r.EndLineNumber, r.EndColumn);
            })];
    }

    private static List<Selection> GetEndCursorStateRight(Range primaryCursor, List<Range> rangesToDelete)
    {
        Selection? endPrimaryCursor = null;
        var endCursorState = new List<Selection>();
        const int offset = 0;
        foreach (Range range in rangesToDelete)
        {
            Selection endCursor = Selection.Collapsed(new Position(range.StartLineNumber - offset, range.StartColumn));

            if (range.Intersect(primaryCursor) is not null)
            {
                endPrimaryCursor = endCursor;
            }
            else
            {
                endCursorState.Add(endCursor);
            }
        }

        if (endPrimaryCursor is { } primary)
        {
            endCursorState.Insert(0, primary);
        }

        return endCursorState;
    }

    private static List<Range> GetRangesToDeleteRight(ViewModel viewModel)
    {
        TextModel model = viewModel.Model;

        List<Range> rangesToDelete = [.. viewModel.GetSelections().Select(sel =>
        {
            Range r = sel.Range;
            if (sel.IsEmpty)
            {
                int maxColumn = model.GetLineMaxColumn(r.StartLineNumber);

                return r.StartColumn == maxColumn
                    ? new Range(r.StartLineNumber, r.StartColumn, r.StartLineNumber + 1, 1)
                    : new Range(r.StartLineNumber, r.StartColumn, r.StartLineNumber, maxColumn);
            }

            return r;
        })];

        // List.Sort is not stable: OrderBy is, like Monaco's Array.sort.
        return [.. rangesToDelete.OrderBy(r => r, Comparer<Range>.Create(Range.CompareUsingStarts))];
    }
}

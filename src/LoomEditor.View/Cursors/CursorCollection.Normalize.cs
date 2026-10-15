// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorCollection.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

internal sealed partial class CursorCollection
{
    // Monaco: the SortedCursor interface of normalize
    private sealed class SortedCursor(int index, Selection selection)
    {
        public int Index { get; set; } = index;

        public Selection Selection { get; set; } = selection;
    }

    /// <summary>Merges the cursors whose selections overlap (or touch, when one of them is empty).</summary>
    public void Normalize()
    {
        if (_cursors.Count == 1)
        {
            return;
        }

        List<Cursor> cursors = [.. _cursors];

        var sortedCursors = new List<SortedCursor>();
        for (int i = 0, len = cursors.Count; i < len; i++)
        {
            sortedCursors.Add(new SortedCursor(i, cursors[i].ModelState.Selection));
        }

        // JavaScript's sort is stable and so is OrderBy.
        sortedCursors = sortedCursors
            .OrderBy(static s => s, Comparer<SortedCursor>.Create(static (a, b) => Range.CompareUsingStarts(a.Selection.Range, b.Selection.Range)))
            .ToList();

        for (int sortedCursorIndex = 0; sortedCursorIndex < sortedCursors.Count - 1; sortedCursorIndex++)
        {
            SortedCursor current = sortedCursors[sortedCursorIndex];
            SortedCursor next = sortedCursors[sortedCursorIndex + 1];

            Selection currentSelection = current.Selection;
            Selection nextSelection = next.Selection;

            if (!_context.CursorConfig.MultiCursorMergeOverlapping)
            {
                continue;
            }

            bool shouldMergeCursors;
            if (nextSelection.IsEmpty || currentSelection.IsEmpty)
            {
                // Merge touching cursors if one of them is collapsed
                shouldMergeCursors = nextSelection.Range.Start <= currentSelection.Range.End;
            }
            else
            {
                // Merge only overlapping cursors (i.e. allow touching ranges)
                shouldMergeCursors = nextSelection.Range.Start < currentSelection.Range.End;
            }

            if (shouldMergeCursors)
            {
                int winnerSortedCursorIndex = current.Index < next.Index ? sortedCursorIndex : sortedCursorIndex + 1;
                int looserSortedCursorIndex = current.Index < next.Index ? sortedCursorIndex + 1 : sortedCursorIndex;

                int looserIndex = sortedCursors[looserSortedCursorIndex].Index;
                int winnerIndex = sortedCursors[winnerSortedCursorIndex].Index;

                Selection looserSelection = sortedCursors[looserSortedCursorIndex].Selection;
                Selection winnerSelection = sortedCursors[winnerSortedCursorIndex].Selection;

                if (looserSelection != winnerSelection)
                {
                    Range resultingRange = looserSelection.Range.Plus(winnerSelection.Range);
                    bool looserSelectionIsLtr = looserSelection.SelectionStart == looserSelection.Range.Start;
                    bool winnerSelectionIsLtr = winnerSelection.SelectionStart == winnerSelection.Range.Start;

                    // Give more importance to the last added cursor (think Ctrl-dragging + hitting another cursor)
                    bool resultingSelectionIsLtr;
                    if (looserIndex == _lastAddedCursorIndex)
                    {
                        resultingSelectionIsLtr = looserSelectionIsLtr;
                        _lastAddedCursorIndex = winnerIndex;
                    }
                    else
                    {
                        // Winner takes it all
                        resultingSelectionIsLtr = winnerSelectionIsLtr;
                    }

                    Selection resultingSelection = resultingSelectionIsLtr
                        ? new Selection(resultingRange.Start, resultingRange.End)
                        : new Selection(resultingRange.End, resultingRange.Start);

                    sortedCursors[winnerSortedCursorIndex].Selection = resultingSelection;
                    PartialModelCursorState resultingState = CursorState.FromModelSelection(resultingSelection);
                    cursors[winnerIndex].SetState(_context, resultingState.ModelState, resultingState.ViewState);
                }

                foreach (SortedCursor sortedCursor in sortedCursors)
                {
                    if (sortedCursor.Index > looserIndex)
                    {
                        sortedCursor.Index--;
                    }
                }

                cursors.RemoveAt(looserIndex);
                sortedCursors.RemoveAt(looserSortedCursorIndex);
                RemoveSecondaryCursor(looserIndex - 1);

                sortedCursorIndex--;
            }
        }
    }
}

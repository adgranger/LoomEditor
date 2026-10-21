// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/sortLinesCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.LinesOperations;

/// <summary>Sorts the selected lines.</summary>
/// <remarks>
/// Behavior difference: Monaco compares with <c>Intl.Collator()</c> (the locale of the user); this port uses the invariant
/// culture, with a stable sort. Threading: UI thread only.
/// </remarks>
internal sealed class SortLinesCommand(Selection selection, bool descending) : IEditCommand
{
    private string? _selectionId;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        if (GetSortData(model, selection, descending) is { } data)
        {
            builder.AddEditOperation(
                new Range(data.StartLineNumber, 1, data.EndLineNumber, model.GetLineMaxColumn(data.EndLineNumber)),
                string.Join('\n', data.After));
        }

        _selectionId = builder.TrackSelection(selection);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) => helper.GetTrackedSelection(_selectionId!);

    /// <summary>Whether sorting changes the order of the selected lines.</summary>
    public static bool CanRun(TextModel? model, Selection selection, bool descending)
    {
        if (model is null)
        {
            return false;
        }

        if (GetSortData(model, selection, descending) is not { } data)
        {
            return false;
        }

        for (int i = 0; i < data.Before.Count; i++)
        {
            if (data.Before[i] != data.After[i])
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct SortData(int StartLineNumber, int EndLineNumber, List<string> Before, List<string> After);

    private static SortData? GetSortData(TextModel model, Selection selection, bool descending)
    {
        int startLineNumber = selection.Range.StartLineNumber;
        int endLineNumber = selection.Range.EndLineNumber;

        if (selection.Range.EndColumn == 1)
        {
            endLineNumber--;
        }

        // Nothing to sort if user didn't select anything.
        if (startLineNumber >= endLineNumber)
        {
            return null;
        }

        var linesToSort = new List<string>();

        // Get the contents of the selection to be sorted.
        for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            linesToSort.Add(model.GetLineContent(lineNumber));
        }

        var comparer = Comparer<string>.Create(static (a, b) => CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.None));
        List<string> sorted = [.. linesToSort.OrderBy(line => line, comparer)];

        // If descending, reverse the order.
        if (descending)
        {
            sorted.Reverse();
        }

        return new SortData(startLineNumber, endLineNumber, linesToSort, sorted);
    }
}

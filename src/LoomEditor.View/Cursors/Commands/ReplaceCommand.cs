// Ported from microsoft/vscode: src/vs/editor/common/commands/replaceCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors.Commands;

/// <summary>Replaces a range with a text and puts the cursor at the end of the inserted text (Monaco: <c>ReplaceCommand</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ReplaceCommand(Range range, string text, bool insertsAutoWhitespace = false) : IEditCommand
{
    public bool InsertsAutoWhitespace { get; } = insertsAutoWhitespace;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder) =>
        builder.AddTrackedEditOperation(range, text);

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Range srcRange = helper.GetInverseEditOperations()[0].Range;
        return Selection.Collapsed(srcRange.End);
    }
}

/// <summary>
/// Replaces a range with a text, overwriting as many characters as the text has, and puts the cursor at the end of
/// the inserted text (Monaco: <c>ReplaceOvertypeCommand</c>).
/// </summary>
/// <remarks>Not reachable in phase 3: <c>InputMode</c> is always insert. Threading: UI thread only.</remarks>
internal sealed class ReplaceOvertypeCommand(Range range, string text, bool insertsAutoWhitespace = false) : IEditCommand
{
    public bool InsertsAutoWhitespace { get; } = insertsAutoWhitespace;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        Position initialStartPosition = range.Start;
        Position initialEndPosition = range.End;
        int initialEndLineNumber = initialEndPosition.LineNumber;
        int offsetDelta = text.Length + (range.IsEmpty ? 0 : -1);
        Position endPosition = ReplaceCommandHelpers.AddPositiveOffsetToModelPosition(model, initialEndPosition, offsetDelta);
        if (endPosition.LineNumber > initialEndLineNumber)
        {
            endPosition = new Position(initialEndLineNumber, model.GetLineMaxColumn(initialEndLineNumber));
        }

        var replaceRange = new Range(initialStartPosition, endPosition);
        builder.AddTrackedEditOperation(replaceRange, text);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Range srcRange = helper.GetInverseEditOperations()[0].Range;
        return Selection.Collapsed(srcRange.End);
    }
}

/// <summary>Replaces a range with a text and selects the inserted text (Monaco: <c>ReplaceCommandThatSelectsText</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ReplaceCommandThatSelectsText(Range range, string text) : IEditCommand
{
    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder) =>
        builder.AddTrackedEditOperation(range, text);

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Range srcRange = helper.GetInverseEditOperations()[0].Range;
        return Selection.FromRange(srcRange, SelectionDirection.LeftToRight);
    }
}

/// <summary>Replaces a range with a text and puts the cursor at the start of the inserted text (Monaco: <c>ReplaceCommandWithoutChangingPosition</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ReplaceCommandWithoutChangingPosition(Range range, string text, bool insertsAutoWhitespace = false) : IEditCommand
{
    public bool InsertsAutoWhitespace { get; } = insertsAutoWhitespace;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder) =>
        builder.AddTrackedEditOperation(range, text);

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Range srcRange = helper.GetInverseEditOperations()[0].Range;
        return Selection.Collapsed(srcRange.Start);
    }
}

/// <summary>
/// Replaces a range with a text and puts the cursor at the end of the inserted text, moved by a line and column
/// offset (Monaco: <c>ReplaceCommandWithOffsetCursorState</c>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal class ReplaceCommandWithOffsetCursorState(
    Range range,
    string text,
    int lineNumberDeltaOffset,
    int columnDeltaOffset,
    bool insertsAutoWhitespace = false) : IEditCommand
{
    public bool InsertsAutoWhitespace { get; } = insertsAutoWhitespace;

    public virtual void GetEditOperations(TextModel model, IEditOperationBuilder builder) =>
        builder.AddTrackedEditOperation(range, text);

    public virtual Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Range srcRange = helper.GetInverseEditOperations()[0].Range;
        return Selection.Collapsed(srcRange.End.Delta(lineNumberDeltaOffset, columnDeltaOffset));
    }
}

/// <summary>
/// Deletes, after the range, as many characters as the range has (the characters a composition overwrote) and puts
/// the cursor at the end of the range (Monaco: <c>ReplaceOvertypeCommandOnCompositionEnd</c>).
/// </summary>
/// <remarks>Not reachable in phase 3: <c>InputMode</c> is always insert. Threading: UI thread only.</remarks>
internal sealed class ReplaceOvertypeCommandOnCompositionEnd(Range range) : IEditCommand
{
    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        string text = model.GetValueInRange(range);
        Position initialEndPosition = range.End;
        int initialEndLineNumber = initialEndPosition.LineNumber;
        Position endPosition = ReplaceCommandHelpers.AddPositiveOffsetToModelPosition(model, initialEndPosition, text.Length);
        if (endPosition.LineNumber > initialEndLineNumber)
        {
            endPosition = new Position(initialEndLineNumber, model.GetLineMaxColumn(initialEndLineNumber));
        }

        var replaceRange = new Range(initialEndPosition, endPosition);
        builder.AddTrackedEditOperation(replaceRange, string.Empty);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Range srcRange = helper.GetInverseEditOperations()[0].Range;
        return Selection.Collapsed(srcRange.End);
    }
}

/// <summary>Replaces a range with a text and keeps a selection (tracked through the edit) as the cursor state (Monaco: <c>ReplaceCommandThatPreservesSelection</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ReplaceCommandThatPreservesSelection(Range editRange, string text, Selection initialSelection, bool forceMoveMarkers = false) : IEditCommand
{
    private string? _selectionId;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        builder.AddTrackedEditOperation(editRange, text, forceMoveMarkers);
        _selectionId = builder.TrackSelection(initialSelection);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) =>
        helper.GetTrackedSelection(_selectionId!);
}

internal static class ReplaceCommandHelpers
{
    // Monaco: addPositiveOffsetToModelPosition
    public static Position AddPositiveOffsetToModelPosition(TextModel model, Position position, int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Unexpected negative delta");
        }

        int lineCount = model.LineCount;
        var endPosition = new Position(lineCount, model.GetLineMaxColumn(lineCount));
        for (int lineNumber = position.LineNumber; lineNumber <= lineCount; lineNumber++)
        {
            if (lineNumber == position.LineNumber)
            {
                int futureOffset = offset - model.GetLineMaxColumn(position.LineNumber) + position.Column;
                if (futureOffset <= 0)
                {
                    endPosition = new Position(position.LineNumber, position.Column + offset);
                    break;
                }

                offset = futureOffset;
            }
            else
            {
                int futureOffset = offset - model.GetLineMaxColumn(lineNumber);
                if (futureOffset <= 0)
                {
                    endPosition = new Position(lineNumber, offset);
                    break;
                }

                offset = futureOffset;
            }
        }

        return endPosition;
    }
}

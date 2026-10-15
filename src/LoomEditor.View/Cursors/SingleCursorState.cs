// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>The state of one cursor, either on the model or on the view model (Monaco: <c>SingleCursorState</c>).</summary>
/// <remarks>
/// <para>Equality is Monaco's: it does not compare <see cref="Selection"/>, which is computed from the other values.</para>
/// <para>Threading: immutable; thread-safe.</para>
/// </remarks>
internal sealed class SingleCursorState : IEquatable<SingleCursorState>
{
    public SingleCursorState(
        Range selectionStart,
        SelectionStartKind selectionStartKind,
        int selectionStartLeftoverVisibleColumns,
        Position position,
        int leftoverVisibleColumns)
    {
        SelectionStart = selectionStart;
        SelectionStartKind = selectionStartKind;
        SelectionStartLeftoverVisibleColumns = selectionStartLeftoverVisibleColumns;
        Position = position;
        LeftoverVisibleColumns = leftoverVisibleColumns;
        Selection = ComputeSelection(selectionStart, position);
    }

    public Range SelectionStart { get; }

    public SelectionStartKind SelectionStartKind { get; }

    public int SelectionStartLeftoverVisibleColumns { get; }

    public Position Position { get; }

    public int LeftoverVisibleColumns { get; }

    public Selection Selection { get; }

    public bool Equals(SingleCursorState? other) =>
        other is not null
        && SelectionStartLeftoverVisibleColumns == other.SelectionStartLeftoverVisibleColumns
        && LeftoverVisibleColumns == other.LeftoverVisibleColumns
        && SelectionStartKind == other.SelectionStartKind
        && Position == other.Position
        && SelectionStart == other.SelectionStart;

    public override bool Equals(object? obj) => Equals(obj as SingleCursorState);

    public override int GetHashCode() =>
        HashCode.Combine(SelectionStartLeftoverVisibleColumns, LeftoverVisibleColumns, SelectionStartKind, Position, SelectionStart);

    public bool HasSelection() => !Selection.IsEmpty || !SelectionStart.IsEmpty;

    public SingleCursorState Move(bool inSelectionMode, int lineNumber, int column, int leftoverVisibleColumns)
    {
        if (inSelectionMode)
        {
            // move just position
            return new SingleCursorState(
                SelectionStart,
                SelectionStartKind,
                SelectionStartLeftoverVisibleColumns,
                new Position(lineNumber, column),
                leftoverVisibleColumns);
        }

        // move everything
        return new SingleCursorState(
            new Range(lineNumber, column, lineNumber, column),
            SelectionStartKind.Simple,
            leftoverVisibleColumns,
            new Position(lineNumber, column),
            leftoverVisibleColumns);
    }

    // Monaco: _computeSelection
    private static Selection ComputeSelection(Range selectionStart, Position position)
    {
        if (selectionStart.IsEmpty || !(position <= selectionStart.Start))
        {
            return new Selection(selectionStart.Start, position);
        }

        return new Selection(selectionStart.End, position);
    }
}

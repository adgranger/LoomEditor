// Ported from microsoft/vscode: src/vs/editor/common/cursor/oneCursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>A single cursor: its model state, its view state, and a tracked range that follows its selection (Monaco: <c>Cursor</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class Cursor
{
    private string? _selTrackedRange;
    private bool _trackSelection;

    public Cursor(CursorContext context)
    {
        _selTrackedRange = null;
        _trackSelection = true;

        // Both states are assigned by SetStateCore.
        ModelState = null!;
        ViewState = null!;

        SetStateCore(
            context,
            new SingleCursorState(new Range(1, 1, 1, 1), SelectionStartKind.Simple, 0, new Position(1, 1), 0),
            new SingleCursorState(new Range(1, 1, 1, 1), SelectionStartKind.Simple, 0, new Position(1, 1), 0));
    }

    public SingleCursorState ModelState { get; private set; }

    public SingleCursorState ViewState { get; private set; }

    public void Dispose(CursorContext context) => RemoveTrackedRange(context);

    public void StartTrackingSelection(CursorContext context)
    {
        _trackSelection = true;
        UpdateTrackedRange(context);
    }

    public void StopTrackingSelection(CursorContext context)
    {
        _trackSelection = false;
        RemoveTrackedRange(context);
    }

    private void UpdateTrackedRange(CursorContext context)
    {
        if (!_trackSelection)
        {
            // don't track the selection
            return;
        }

        _selTrackedRange = context.Model.SetTrackedRange(
            _selTrackedRange, ModelState.Selection.Range, TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);
    }

    private void RemoveTrackedRange(CursorContext context) =>
        _selTrackedRange = context.Model.SetTrackedRange(_selTrackedRange, null, TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

    public CursorState AsCursorState() => new(ModelState, ViewState);

    public Selection ReadSelectionFromMarkers(CursorContext context)
    {
        Range range = context.Model.GetTrackedRange(_selTrackedRange!)!.Value;

        if (ModelState.Selection.IsEmpty && !range.IsEmpty)
        {
            // Avoid selecting text when recovering from markers
            return Selection.FromRange(range.CollapseToEnd(), ModelState.Selection.Direction);
        }

        return Selection.FromRange(range, ModelState.Selection.Direction);
    }

    public void EnsureValidState(CursorContext context) => SetStateCore(context, ModelState, ViewState);

    public void SetState(CursorContext context, SingleCursorState? modelState, SingleCursorState? viewState) =>
        SetStateCore(context, modelState, viewState);

    // Monaco: _validatePositionWithCache
    private static Position ValidatePositionWithCache(ICursorSimpleModel viewModel, Position position, Position cacheInput, Position cacheOutput)
    {
        if (position == cacheInput)
        {
            return cacheOutput;
        }

        return viewModel.NormalizePosition(position, PositionAffinity.None);
    }

    // Monaco: _validateViewState
    private static SingleCursorState ValidateViewState(ICursorSimpleModel viewModel, SingleCursorState viewState)
    {
        Position position = viewState.Position;
        Position sStartPosition = viewState.SelectionStart.Start;
        Position sEndPosition = viewState.SelectionStart.End;

        Position validPosition = viewModel.NormalizePosition(position, PositionAffinity.None);
        Position validSStartPosition = ValidatePositionWithCache(viewModel, sStartPosition, position, validPosition);
        Position validSEndPosition = ValidatePositionWithCache(viewModel, sEndPosition, sStartPosition, validSStartPosition);

        if (position == validPosition && sStartPosition == validSStartPosition && sEndPosition == validSEndPosition)
        {
            // fast path: the state is valid
            return viewState;
        }

        return new SingleCursorState(
            new Range(validSStartPosition, validSEndPosition),
            viewState.SelectionStartKind,
            viewState.SelectionStartLeftoverVisibleColumns + sStartPosition.Column - validSStartPosition.Column,
            validPosition,
            viewState.LeftoverVisibleColumns + position.Column - validPosition.Column);
    }

    // Monaco: _setState
    private void SetStateCore(CursorContext context, SingleCursorState? modelState, SingleCursorState? viewState)
    {
        if (viewState is not null)
        {
            viewState = ValidateViewState(context.ViewModel, viewState);
        }

        if (modelState is null)
        {
            if (viewState is null)
            {
                return;
            }

            // We only have the view state => compute the model state
            Range selectionStart = context.Model.ValidateRange(
                context.CoordinatesConverter.ConvertViewRangeToModelRange(viewState.SelectionStart));

            Position position = context.Model.ValidatePosition(
                context.CoordinatesConverter.ConvertViewPositionToModelPosition(viewState.Position));

            modelState = new SingleCursorState(
                selectionStart,
                viewState.SelectionStartKind,
                viewState.SelectionStartLeftoverVisibleColumns,
                position,
                viewState.LeftoverVisibleColumns);
        }
        else
        {
            // Validate new model state
            Range selectionStart = context.Model.ValidateRange(modelState.SelectionStart);
            int selectionStartLeftoverVisibleColumns =
                modelState.SelectionStart == selectionStart ? modelState.SelectionStartLeftoverVisibleColumns : 0;

            Position position = context.Model.ValidatePosition(modelState.Position);
            int leftoverVisibleColumns = modelState.Position == position ? modelState.LeftoverVisibleColumns : 0;

            modelState = new SingleCursorState(
                selectionStart, modelState.SelectionStartKind, selectionStartLeftoverVisibleColumns, position, leftoverVisibleColumns);
        }

        if (viewState is null)
        {
            // We only have the model state => compute the view state
            Position viewSelectionStart1 = context.CoordinatesConverter.ConvertModelPositionToViewPosition(
                new Position(modelState.SelectionStart.StartLineNumber, modelState.SelectionStart.StartColumn));
            Position viewSelectionStart2 = context.CoordinatesConverter.ConvertModelPositionToViewPosition(
                new Position(modelState.SelectionStart.EndLineNumber, modelState.SelectionStart.EndColumn));
            var viewSelectionStart = new Range(
                viewSelectionStart1.LineNumber, viewSelectionStart1.Column, viewSelectionStart2.LineNumber, viewSelectionStart2.Column);
            Position viewPosition = context.CoordinatesConverter.ConvertModelPositionToViewPosition(modelState.Position);
            viewState = new SingleCursorState(
                viewSelectionStart,
                modelState.SelectionStartKind,
                modelState.SelectionStartLeftoverVisibleColumns,
                viewPosition,
                modelState.LeftoverVisibleColumns);
        }
        else
        {
            // Validate new view state
            Range viewSelectionStart = context.CoordinatesConverter.ValidateViewRange(viewState.SelectionStart, modelState.SelectionStart);
            Position viewPosition = context.CoordinatesConverter.ValidateViewPosition(viewState.Position, modelState.Position);
            viewState = new SingleCursorState(
                viewSelectionStart,
                modelState.SelectionStartKind,
                modelState.SelectionStartLeftoverVisibleColumns,
                viewPosition,
                modelState.LeftoverVisibleColumns);
        }

        ModelState = modelState;
        ViewState = viewState;

        UpdateTrackedRange(context);
    }
}

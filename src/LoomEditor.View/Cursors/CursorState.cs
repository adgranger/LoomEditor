// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>A cursor state that may lack its model state or its view state (Monaco: <c>PartialCursorState</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal abstract class PartialCursorState
{
    public abstract SingleCursorState? ModelState { get; }

    public abstract SingleCursorState? ViewState { get; }
}

/// <summary>The state of one cursor on the model and on the view model (Monaco: <c>CursorState</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed class CursorState : PartialCursorState, IEquatable<CursorState>
{
    public CursorState(SingleCursorState modelState, SingleCursorState viewState)
    {
        ModelState = modelState;
        ViewState = viewState;
    }

    public override SingleCursorState ModelState { get; }

    public override SingleCursorState ViewState { get; }

    public static PartialModelCursorState FromModelState(SingleCursorState modelState) => new(modelState);

    public static PartialViewCursorState FromViewState(SingleCursorState viewState) => new(viewState);

    public static PartialModelCursorState FromModelSelection(Selection modelSelection)
    {
        var modelState = new SingleCursorState(
            Range.Collapsed(modelSelection.SelectionStart),
            SelectionStartKind.Simple,
            0,
            modelSelection.Position,
            0);
        return FromModelState(modelState);
    }

    public static PartialModelCursorState[] FromModelSelections(IReadOnlyList<Selection> modelSelections)
    {
        var states = new PartialModelCursorState[modelSelections.Count];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = FromModelSelection(modelSelections[i]);
        }

        return states;
    }

    public bool Equals(CursorState? other) =>
        other is not null && ViewState.Equals(other.ViewState) && ModelState.Equals(other.ModelState);

    public override bool Equals(object? obj) => Equals(obj as CursorState);

    public override int GetHashCode() => HashCode.Combine(ModelState, ViewState);
}

/// <summary>A cursor state that only has its model state (Monaco: <c>PartialModelCursorState</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed class PartialModelCursorState : PartialCursorState
{
    public PartialModelCursorState(SingleCursorState modelState)
    {
        ModelState = modelState;
    }

    public override SingleCursorState ModelState { get; }

    public override SingleCursorState? ViewState => null;
}

/// <summary>A cursor state that only has its view state (Monaco: <c>PartialViewCursorState</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed class PartialViewCursorState : PartialCursorState
{
    public PartialViewCursorState(SingleCursorState viewState)
    {
        ViewState = viewState;
    }

    public override SingleCursorState? ModelState => null;

    public override SingleCursorState ViewState { get; }
}

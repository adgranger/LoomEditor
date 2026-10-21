// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelLines.ts (CoordinatesConverter)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>Converts between model and view coordinates by forwarding to <see cref="ViewModelLinesFromProjectedModel"/> (Monaco: <c>CoordinatesConverter</c>).</summary>
/// <remarks>
/// A <c>null</c> affinity takes the default of the lines: <see cref="PositionAffinity.None"/> for a position and
/// <see cref="PositionAffinity.Left"/> for a range, like Monaco's <c>undefined</c>.
/// Threading: UI thread only.
/// </remarks>
internal sealed class CoordinatesConverter(ViewModelLinesFromProjectedModel lines) : ICoordinatesConverter
{
    // View -> Model conversion and related methods

    public Position ConvertViewPositionToModelPosition(Position viewPosition) =>
        lines.ConvertViewPositionToModelPosition(viewPosition.LineNumber, viewPosition.Column);

    public Range ConvertViewRangeToModelRange(Range viewRange) => lines.ConvertViewRangeToModelRange(viewRange);

    public Position ValidateViewPosition(Position viewPosition, Position expectedModelPosition) =>
        lines.ValidateViewPosition(viewPosition.LineNumber, viewPosition.Column, expectedModelPosition);

    public Range ValidateViewRange(Range viewRange, Range expectedModelRange) => lines.ValidateViewRange(viewRange, expectedModelRange);

    // Model -> View conversion and related methods

    public Position ConvertModelPositionToViewPosition(
        Position modelPosition,
        PositionAffinity? affinity = null,
        bool allowZeroLineNumber = false,
        bool belowHiddenRanges = false) =>
        lines.ConvertModelPositionToViewPosition(
            modelPosition.LineNumber, modelPosition.Column, affinity ?? PositionAffinity.None, allowZeroLineNumber, belowHiddenRanges);

    public Range ConvertModelRangeToViewRange(Range modelRange, PositionAffinity? affinity = null) =>
        lines.ConvertModelRangeToViewRange(modelRange, affinity ?? PositionAffinity.Left);

    public bool ModelPositionIsVisible(Position modelPosition) => lines.ModelPositionIsVisible(modelPosition.LineNumber, modelPosition.Column);

    public int GetModelLineViewLineCount(int modelLineNumber) => lines.GetModelLineViewLineCount(modelLineNumber);

    public int GetViewLineNumberOfModelPosition(int modelLineNumber, int modelColumn) =>
        lines.GetViewLineNumberOfModelPosition(modelLineNumber, modelColumn);
}

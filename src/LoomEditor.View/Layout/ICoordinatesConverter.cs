// Ported from microsoft/vscode: src/vs/editor/common/coordinatesConverter.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>Converts positions and ranges between the model and the view model (Monaco: <c>ICoordinatesConverter</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface ICoordinatesConverter
{
    // View -> Model conversion and related methods
    Position ConvertViewPositionToModelPosition(Position viewPosition);

    Range ConvertViewRangeToModelRange(Range viewRange);

    Position ValidateViewPosition(Position viewPosition, Position expectedModelPosition);

    Range ValidateViewRange(Range viewRange, Range expectedModelRange);

    // Model -> View conversion and related methods

    /// <param name="modelPosition">The model position.</param>
    /// <param name="affinity">Which side to prefer on injected text.</param>
    /// <param name="allowZeroLineNumber">Should it return 0 when there are hidden lines at the top and the position is in the hidden area?</param>
    /// <param name="belowHiddenRanges">When the model position is in a hidden area, should it return the first view position after or before?</param>
    Position ConvertModelPositionToViewPosition(
        Position modelPosition,
        PositionAffinity? affinity = null,
        bool allowZeroLineNumber = false,
        bool belowHiddenRanges = false);

    /// <param name="modelRange">The model range.</param>
    /// <param name="affinity">Only has an effect if the range is empty.</param>
    Range ConvertModelRangeToViewRange(Range modelRange, PositionAffinity? affinity = null);

    bool ModelPositionIsVisible(Position modelPosition);

    int GetModelLineViewLineCount(int modelLineNumber);

    int GetViewLineNumberOfModelPosition(int modelLineNumber, int modelColumn);
}

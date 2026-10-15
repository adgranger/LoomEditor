// Ported from microsoft/vscode: src/vs/editor/common/coordinatesConverter.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>A converter for a view that is the model itself: no word wrap, no hidden lines (Monaco: <c>IdentityCoordinatesConverter</c>).</summary>
/// <remarks>
/// <para>
/// <c>ValidateViewPosition</c> and <c>ValidateViewRange</c> ignore the view value and validate the expected model
/// value, like Monaco. The optional parameters of the model-to-view conversions are ignored.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class IdentityCoordinatesConverter(TextModel model) : ICoordinatesConverter
{
    // View -> Model conversion and related methods

    public Position ConvertViewPositionToModelPosition(Position viewPosition) => model.ValidatePosition(viewPosition);

    public Range ConvertViewRangeToModelRange(Range viewRange) => model.ValidateRange(viewRange);

    public Position ValidateViewPosition(Position viewPosition, Position expectedModelPosition) =>
        model.ValidatePosition(expectedModelPosition);

    public Range ValidateViewRange(Range viewRange, Range expectedModelRange) => model.ValidateRange(expectedModelRange);

    // Model -> View conversion and related methods

    public Position ConvertModelPositionToViewPosition(
        Position modelPosition,
        PositionAffinity? affinity = null,
        bool allowZeroLineNumber = false,
        bool belowHiddenRanges = false) => model.ValidatePosition(modelPosition);

    public Range ConvertModelRangeToViewRange(Range modelRange, PositionAffinity? affinity = null) => model.ValidateRange(modelRange);

    public bool ModelPositionIsVisible(Position modelPosition) =>
        // false for invalid arguments
        modelPosition.LineNumber >= 1 && modelPosition.LineNumber <= model.LineCount;

    public bool ModelRangeIsVisible(Range modelRange) =>
        // false for invalid arguments
        modelRange.StartLineNumber >= 1
        && modelRange.StartLineNumber <= model.LineCount
        && modelRange.EndLineNumber >= 1
        && modelRange.EndLineNumber <= model.LineCount;

    public int GetModelLineViewLineCount(int modelLineNumber) => 1;

    public int GetViewLineNumberOfModelPosition(int modelLineNumber, int modelColumn) => modelLineNumber;
}

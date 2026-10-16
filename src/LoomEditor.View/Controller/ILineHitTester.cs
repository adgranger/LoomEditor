using LoomEditor.Core.Text;

namespace LoomEditor.View.Controller;

/// <summary>
/// Measures lines for hit testing: the part of Monaco's <c>HitTestContext</c> and <c>viewLinesGpu</c> that needs the
/// laid out text. The renderer implements it with the layouts it draws.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface ILineHitTester
{
    /// <summary>Width of the text of the line, in DIPs (Monaco: <c>getLineWidth</c>).</summary>
    double GetLineWidth(int lineNumber);

    /// <summary>
    /// The position of the character boundary nearest to <paramref name="mouseContentHorizontalOffset"/> (DIPs from the
    /// start of the line), or <c>null</c> if it cannot be measured (Monaco: <c>viewLinesGpu.getPositionAtCoordinate</c>).
    /// </summary>
    Position? GetPositionAtCoordinate(int lineNumber, double mouseContentHorizontalOffset);
}

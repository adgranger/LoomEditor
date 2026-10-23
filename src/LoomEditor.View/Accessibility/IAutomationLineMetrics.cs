using LoomEditor.View.Controller;

namespace LoomEditor.View.Accessibility;

/// <summary>What the renderer measures for UI Automation: the hit-testing measures plus the extent of a column range.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IAutomationLineMetrics : ILineHitTester
{
    /// <summary>
    /// Measures the characters <paramref name="startColumn"/>..<paramref name="endColumn"/> of a VIEW line, in DIPs from the start of the
    /// line's content. <c>false</c> when the line does not exist.
    /// </summary>
    bool TryGetHorizontalRange(int viewLineNumber, int startColumn, int endColumn, out double left, out double width);
}

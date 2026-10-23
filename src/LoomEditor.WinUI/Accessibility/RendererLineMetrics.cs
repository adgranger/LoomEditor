using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Controller;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.WinUI.Accessibility;

/// <summary>The measures UI Automation needs, taken from the renderer that draws the control.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class RendererLineMetrics(LoomEditorControl owner) : IAutomationLineMetrics
{
    private ILineHitTester? _hitTester;

    public bool TryGetHorizontalRange(int viewLineNumber, int startColumn, int endColumn, out double left, out double width)
    {
        if (owner.AutomationRenderer is { } renderer)
        {
            return renderer.TryGetHorizontalRange(owner.AutomationDocument.ViewModel, viewLineNumber, startColumn, endColumn, out left, out width);
        }

        left = 0;
        width = 0;
        return false;
    }

    public double GetLineWidth(int lineNumber) => HitTester?.GetLineWidth(lineNumber) ?? 0;

    public Position? GetPositionAtCoordinate(int lineNumber, double mouseContentHorizontalOffset) =>
        HitTester?.GetPositionAtCoordinate(lineNumber, mouseContentHorizontalOffset);

    // Created on first use; an instance lives for one UI Automation call, so the renderer cannot change under it.
    private ILineHitTester? HitTester =>
        _hitTester ??= owner.AutomationRenderer?.CreateLineHitTester(owner.AutomationDocument.ViewModel);
}

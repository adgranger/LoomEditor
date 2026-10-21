using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Hit testing with the layouts that are drawn (Monaco: `HitTestContext.getLineWidth` and `viewLinesGpu.getPositionAtCoordinate`).
internal sealed partial class TextViewRenderer
{
    /// <summary>Returns an <see cref="ILineHitTester"/> over the layout cache and the lines of <paramref name="viewModel"/>.</summary>
    public ILineHitTester CreateLineHitTester(ViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        return new LineHitTester(this, viewModel);
    }

    private sealed class LineHitTester(TextViewRenderer renderer, ViewModel viewModel) : ILineHitTester
    {
        public double GetLineWidth(int lineNumber) => renderer.GetLineLayout(viewModel, lineNumber).Width;

        public Position? GetPositionAtCoordinate(int lineNumber, double mouseContentHorizontalOffset)
        {
            if (lineNumber < 1 || lineNumber > viewModel.GetLineCount())
            {
                return null;
            }

            ILineLayout layout = renderer.GetLineLayout(viewModel, lineNumber);
            return new Position(lineNumber, layout.HitTest(mouseContentHorizontalOffset) + 1);
        }
    }

    // The layout that `Render` draws: a very long line is cut (ADR 0005).
    private ILineLayout GetLineLayout(ViewModel viewModel, int lineNumber)
    {
        int lineLength = viewModel.GetLineLength(lineNumber);
        string text = lineLength > StopRenderingLineAfter
            ? viewModel.GetLineContent(lineNumber)[..StopRenderingLineAfter]
            : viewModel.GetLineContent(lineNumber);
        return _cache.Get(text);
    }
}

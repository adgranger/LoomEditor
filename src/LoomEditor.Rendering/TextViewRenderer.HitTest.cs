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
        // Monaco's DOM holds the `::after` content (the `⋯` of a folded line) inside the line: it counts in the width.
        public double GetLineWidth(int lineNumber) =>
            Math.Max(renderer.GetLineLayout(viewModel, lineNumber).Width, renderer.GetAfterContentRight(viewModel, lineNumber));

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

    // The right edge of the last `After` content of the line, as `DrawAfterContents` places it; 0 when there is none.
    private double GetAfterContentRight(ViewModel viewModel, int lineNumber)
    {
        ILineLayout layout = GetLineLayout(viewModel, lineNumber);
        var lineRange = new Range(lineNumber, 1, lineNumber, viewModel.GetLineMaxColumn(lineNumber));
        double right = 0;
        foreach (LineDecoration d in LineDecoration.Filter(
            viewModel.GetViewportInlineDecorations(lineRange, lineNumber), lineNumber, 1, layout.Text.Length + 1))
        {
            if (d.Type == InlineDecorationType.After && DecorationStyles.Get(d.ClassName) is { AfterContentText: { } text })
            {
                double x = layout.GetCharacterX(Math.Min(d.EndColumn - 1, layout.Text.Length)) + (0.2 * viewModel.FontInfo.FontSize);
                right = Math.Max(right, x + _cache.Get(text).Width);
            }
        }

        return right;
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

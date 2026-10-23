using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Accessibility.TestHelpers;

/// <summary>Monospace measures: every character is <see cref="CharacterWidth"/> DIPs wide.</summary>
internal sealed class FixedWidthLineMetrics(ViewModel viewModel, double characterWidth = 10) : IAutomationLineMetrics
{
    public double CharacterWidth { get; } = characterWidth;

    public bool TryGetHorizontalRange(int viewLineNumber, int startColumn, int endColumn, out double left, out double width)
    {
        left = (startColumn - 1) * CharacterWidth;
        width = (endColumn - startColumn) * CharacterWidth;
        return true;
    }

    public double GetLineWidth(int lineNumber) => viewModel.GetLineLength(lineNumber) * CharacterWidth;

    public Position? GetPositionAtCoordinate(int lineNumber, double x) =>
        new Position(lineNumber, Math.Min((int)Math.Round(x / CharacterWidth) + 1, viewModel.GetLineMaxColumn(lineNumber)));
}

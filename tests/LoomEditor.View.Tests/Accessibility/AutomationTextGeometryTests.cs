using LoomEditor.View.Accessibility;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AutomationTextGeometryTests
{
    private static readonly TestCodeEditorOptions Wrap10 =
        new(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 });

    private static void Run(string text, Action<AutomationTextGeometry, ViewModel> test, TestCodeEditorOptions? options = null) =>
        WithDocument(text, d => test(new AutomationTextGeometry(d, new FixedWidthLineMetrics(d.ViewModel)), d.ViewModel),
            (options ?? new TestCodeEditorOptions()) with { OuterWidth = 1000 });

    private static string Lines(int n) => string.Join('\n', Enumerable.Range(1, n).Select(i => "line" + i));

    private static void Scroll(ViewModel vm, double top, double left = 0) =>
        vm.ViewLayout.SetScrollPosition(new NewScrollPosition(left, top), ScrollType.Immediate);

    private static int LineHeight(ViewModel vm) => vm.ViewLayout.GetLineHeightForLineNumber(1);

    [Fact]
    public void Rectangles_WithinOneLine() =>
        Run("hello world", (g, vm) =>
        {
            var r = Assert.Single(g.GetBoundingRectangles(Rng(1, 1, 1, 6)));
            Assert.Equal(vm.LayoutInfo.ContentLeft, r.X);
            Assert.Equal(50, r.Width);
            Assert.Equal(LineHeight(vm), r.Height);
            Assert.Equal(0, r.Y);
        });

    [Fact]
    public void Rectangles_StartAfterTheFirstColumn() =>
        Run("hello world", (g, vm) =>
            Assert.Equal(vm.LayoutInfo.ContentLeft + (10 * 3), Assert.Single(g.GetBoundingRectangles(Rng(1, 4, 1, 6))).X));

    [Fact]
    public void Rectangles_AcrossTwoLines_OnePerLineWithTheLineBreakWidth() =>
        Run("abc\ndefg", (g, vm) =>
        {
            var rects = g.GetBoundingRectangles(Rng(1, 2, 2, 3));
            Assert.Equal(2, rects.Count);
            Assert.Equal((20 + vm.FontInfo.SpaceWidth, 20), (rects[0].Width, rects[1].Width));
            Assert.True(rects[0].Y < rects[1].Y);
        });

    [Fact]
    public void Rectangles_Degenerate_IsOneRectangleOfWidthOne() =>
        Run("hello", (g, _) => Assert.Equal(1, Assert.Single(g.GetBoundingRectangles(Rng(1, 3, 1, 3))).Width));

    [Fact]
    public void Rectangles_EmptyLineInTheRange_KeepsTheLineBreakWidth() =>
        Run("a\n\nb", (g, vm) =>
        {
            var rects = g.GetBoundingRectangles(Rng(1, 1, 3, 2));
            Assert.Equal(3, rects.Count);
            Assert.Equal(vm.FontInfo.SpaceWidth, rects[1].Width);
        });

    [Fact]
    public void Rectangles_WrappedLine_OnePerViewLine() =>
        Run("aaaa bbbb cccc", (g, _) =>
        {
            var rects = g.GetBoundingRectangles(Rng(1, 1, 1, 15));
            Assert.Equal(2, rects.Count);
            Assert.NotEqual(rects[0].Y, rects[1].Y);
        }, Wrap10);

    [Fact]
    public void Rectangles_ScrolledOffLines_AreDropped() =>
        Run(Lines(100), (g, vm) =>
        {
            Scroll(vm, 19 * LineHeight(vm));
            Assert.Empty(g.GetBoundingRectangles(Rng(1, 1, 1, 3)));
            Assert.Equal(0, Assert.Single(g.GetBoundingRectangles(Rng(20, 1, 20, 3))).Y);
        });

    [Fact]
    public void Rectangles_ClippedToTheContentArea() =>
        Run("hello world", (g, vm) =>
        {
            vm.ViewLayout.SetMaxLineWidth(5000);
            Scroll(vm, 0, 25);
            var r = Assert.Single(g.GetBoundingRectangles(Rng(1, 1, 1, 6)));
            Assert.Equal(vm.LayoutInfo.ContentLeft, r.X);
            Assert.Equal(25, r.Width);
        });

    [Fact]
    public void Rectangles_RangeFullyRightOfTheContentArea_IsDropped() =>
        Run(new string('x', 500), (g, _) => Assert.Empty(g.GetBoundingRectangles(Rng(1, 400, 1, 410))));

    [Fact]
    public void VisibleRange_FirstScreen() =>
        Run(Lines(100), (g, vm) =>
        {
            var range = g.GetVisibleRange();
            Assert.Equal(Pos(1, 1), range.Start);
            Assert.Equal(vm.ViewLayout.GetLinesViewportData().EndLineNumber, range.EndLineNumber);
            Assert.Equal(vm.GetLineMaxColumn(range.EndLineNumber), range.EndColumn);
        });

    [Fact]
    public void VisibleRange_Scrolled() =>
        Run(Lines(100), (g, vm) =>
        {
            Scroll(vm, 19 * LineHeight(vm));
            Assert.Equal(20, g.GetVisibleRange().StartLineNumber);
        });

    [Fact]
    public void VisibleRange_Wrapped_ConvertsViewLinesToModelPositions() =>
        Run("aaaa bbbb cccc", (g, _) => Assert.Equal(Rng(1, 1, 1, 15), g.GetVisibleRange()), Wrap10);

    [Fact]
    public void VisibleRange_EmptyViewport_IsDocumentStart() =>
        Run("abc\ndef", (g, _) => Assert.Equal(Rng(1, 1, 1, 1), g.GetVisibleRange()), new TestCodeEditorOptions { OuterHeight = 0 });

    [Fact]
    public void PositionFromPoint_InsideALine() =>
        Run("hello world", (g, vm) => Assert.Equal(Pos(1, 4), g.PositionFromPoint(vm.LayoutInfo.ContentLeft + 26, 1)));

    [Fact]
    public void PositionFromPoint_SecondLine() =>
        Run("abc\ndef", (g, vm) => Assert.Equal(Pos(2, 1), g.PositionFromPoint(vm.LayoutInfo.ContentLeft, LineHeight(vm) + 1)));

    [Fact]
    public void PositionFromPoint_LeftOfTheContent_IsTheLineStart() =>
        Run("abc", (g, _) => Assert.Equal(Pos(1, 1), g.PositionFromPoint(-50, 1)));

    [Fact]
    public void PositionFromPoint_BelowTheLastLine_IsTheLastLine() =>
        Run("abc\ndef", (g, vm) => Assert.Equal(2, g.PositionFromPoint(vm.LayoutInfo.ContentLeft, 5000).LineNumber));

    [Fact]
    public void PositionFromPoint_AboveTheFirstLine_IsTheFirstLine() =>
        Run("abc\ndef", (g, vm) => Assert.Equal(1, g.PositionFromPoint(vm.LayoutInfo.ContentLeft, -50).LineNumber));

    [Fact]
    public void PositionFromPoint_WrappedLine_ReturnsAModelPosition() =>
        Run("aaaa bbbb cccc", (g, vm) =>
            Assert.Equal(Pos(1, 11), g.PositionFromPoint(vm.LayoutInfo.ContentLeft, LineHeight(vm) + 1)), Wrap10);

    [Fact]
    public void PositionFromPoint_Scrolled_AddsTheScrollOffsets() =>
        Run(Lines(100), (g, vm) =>
        {
            Scroll(vm, 19 * LineHeight(vm));
            Assert.Equal(Pos(20, 1), g.PositionFromPoint(vm.LayoutInfo.ContentLeft, 1));
        });
}

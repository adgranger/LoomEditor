using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class RevealCalculatorTests
{
    // The test font has a line height of 19: 20 lines fit in 380 DIPs.
    private const int LineHeight = 19;
    private const int ViewportHeight = 380;

    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(i => $"line {i}"));

    private static void WithReveal(
        int lineCount, Action<TestCodeEditor, RevealCalculator> callback, int viewportWidth = 500, EditorCursorOptions? cursorOptions = null) =>
        TestCodeEditor.WithTestCodeEditor(
            Lines(lineCount),
            editor => callback(editor, new RevealCalculator(editor.ViewModel)),
            new TestCodeEditorOptions(CursorOptions: cursorOptions, OuterWidth: viewportWidth, OuterHeight: ViewportHeight));

    private static ViewRevealRangeRequestEvent Reveal(
        Range range, VerticalRevealType type = VerticalRevealType.Simple, bool minimal = false, bool horizontal = true, string? source = "keyboard") =>
        new(source, minimal, range, null, type, horizontal, ScrollType.Immediate);

    [Fact]
    public void ComputeMinimumScrolling_BoxAboveTheViewport_ReturnsTheBoxStart()
    {
        Assert.Equal(50, RevealCalculator.ComputeMinimumScrolling(100, 300, 50, 80));
    }

    [Fact]
    public void ComputeMinimumScrolling_BoxBelowTheViewport_ShowsTheBoxEnd()
    {
        Assert.Equal(200, RevealCalculator.ComputeMinimumScrolling(100, 300, 350, 400));
    }

    [Fact]
    public void ComputeMinimumScrolling_BoxInsideTheViewport_KeepsTheViewport()
    {
        Assert.Equal(100, RevealCalculator.ComputeMinimumScrolling(100, 300, 150, 200));
    }

    [Fact]
    public void ComputeMinimumScrolling_BoxLargerThanTheViewport_ReturnsTheBoxStart()
    {
        Assert.Equal(120, RevealCalculator.ComputeMinimumScrolling(100, 300, 120, 400));
    }

    [Fact]
    public void ComputeMinimumScrolling_RevealAtStartOrEnd_ForcesTheSide()
    {
        Assert.Equal(150, RevealCalculator.ComputeMinimumScrolling(100, 300, 150, 200, revealAtStart: true));
        Assert.Equal(0, RevealCalculator.ComputeMinimumScrolling(100, 300, 150, 200, revealAtEnd: true));
    }

    [Fact]
    public void ComputeScrollTopToRevealRange_SimpleOnLine50_LeavesOneLineBelow()
    {
        WithReveal(100, (editor, reveal) =>
        {
            int scrollTop = reveal.ComputeScrollTopToRevealRange(
                new Viewport(0, 0, 500, ViewportHeight), "keyboard", false, Range.Collapsed(new Position(50, 1)), null, VerticalRevealType.Simple);

            // Line 50 spans [49 * 19, 50 * 19); one more line of padding: the box ends at 51 * 19.
            Assert.Equal((51 * LineHeight) - ViewportHeight, scrollTop);
            int lastVisibleLine = (scrollTop + ViewportHeight) / LineHeight;
            Assert.Equal(51, lastVisibleLine);
        });
    }

    [Fact]
    public void ComputeScrollTopToRevealRange_CenterIfOutsideViewport_CentersOnlyOutsideRanges()
    {
        WithReveal(100, (editor, reveal) =>
        {
            var viewport = new Viewport(0, 0, 500, ViewportHeight);

            int outside = reveal.ComputeScrollTopToRevealRange(
                viewport, null, false, Range.Collapsed(new Position(50, 1)), null, VerticalRevealType.CenterIfOutsideViewport);
            int inside = reveal.ComputeScrollTopToRevealRange(
                viewport, null, false, Range.Collapsed(new Position(5, 1)), null, VerticalRevealType.CenterIfOutsideViewport);

            Assert.Equal((int)Math.Floor((((49 * LineHeight) + (50 * LineHeight)) / 2.0) - (ViewportHeight / 2.0)), outside);
            Assert.Equal(0, inside);
        });
    }

    [Fact]
    public void ComputeScrollTopToRevealRange_SelectionsTallerThanTheViewport_Aborts()
    {
        WithReveal(100, (editor, reveal) =>
        {
            Selection[] selections = [new Selection(1, 1, 1, 1), new Selection(60, 1, 60, 1)];

            int scrollTop = reveal.ComputeScrollTopToRevealRange(
                new Viewport(0, 0, 500, ViewportHeight), null, false, null, selections, VerticalRevealType.Simple);

            Assert.Equal(-1, scrollTop);
        });
    }

    [Fact]
    public void ComputeScrollTopToRevealRange_NoRangeAndNoSelections_Aborts()
    {
        WithReveal(10, (editor, reveal) =>
        {
            Assert.Equal(
                -1, reveal.ComputeScrollTopToRevealRange(new Viewport(0, 0, 500, ViewportHeight), null, false, null, null, VerticalRevealType.Simple));
        });
    }

    [Fact]
    public void ComputeScrollTopToRevealRange_CursorSurroundingLines_KeepsLinesAroundTheCursor()
    {
        WithReveal(
            100,
            (editor, reveal) =>
            {
                int scrollTop = reveal.ComputeScrollTopToRevealRange(
                    new Viewport(0, 0, 500, ViewportHeight), "keyboard", false, Range.Collapsed(new Position(50, 1)), null, VerticalRevealType.Simple);

                // 5 lines below (context - 1 = 4, plus the one of Simple) after the box end.
                Assert.Equal((50 * LineHeight) + (5 * LineHeight) - ViewportHeight, scrollTop);
            },
            cursorOptions: new EditorCursorOptions { CursorSurroundingLines = 5 });
    }

    [Fact]
    public void ComputeScrollTopToRevealRange_MouseSourceWithDefaultStyle_IgnoresSurroundingLines()
    {
        WithReveal(
            100,
            (editor, reveal) =>
            {
                int scrollTop = reveal.ComputeScrollTopToRevealRange(
                    new Viewport(0, 0, 500, ViewportHeight), "mouse", false, Range.Collapsed(new Position(50, 1)), null, VerticalRevealType.Simple);

                // One line above (dragging) and one below (Simple): the box is [48 * 19, 51 * 19].
                Assert.Equal((51 * LineHeight) - ViewportHeight, scrollTop);
            },
            cursorOptions: new EditorCursorOptions { CursorSurroundingLines = 5 });
    }

    [Fact]
    public void OnRevealRangeRequest_ScrollsTheViewLayoutAndStoresTheHorizontalRequest()
    {
        WithReveal(100, (editor, reveal) =>
        {
            bool handled = reveal.OnRevealRangeRequest(Reveal(Range.Collapsed(new Position(50, 3))));

            Assert.True(handled);
            Assert.Equal((51 * LineHeight) - ViewportHeight, editor.ViewModel.ViewLayout.GetCurrentScrollTop());
            var request = Assert.IsType<HorizontalRevealRangeRequest>(reveal.PendingHorizontalRequest);
            Assert.Equal(50, request.LineNumber);
            Assert.Equal(3, request.StartColumn);
        });
    }

    [Fact]
    public void OnRevealRangeRequest_RangeOnSeveralLines_ScrollsToTheLeftEdge()
    {
        WithReveal(100, (editor, reveal) =>
        {
            editor.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(40, 0), ScrollType.Immediate);
            editor.ViewModel.ViewLayout.SetMaxLineWidth(2000);

            reveal.OnRevealRangeRequest(Reveal(new Range(2, 1, 3, 1)));

            Assert.Null(reveal.PendingHorizontalRequest);
            Assert.Equal(0, editor.ViewModel.ViewLayout.GetCurrentScrollLeft());
        });
    }

    [Fact]
    public void OnRevealRangeRequest_WithoutRevealHorizontal_ClearsThePendingRequest()
    {
        WithReveal(100, (editor, reveal) =>
        {
            reveal.OnRevealRangeRequest(Reveal(Range.Collapsed(new Position(50, 3))));

            reveal.OnRevealRangeRequest(Reveal(Range.Collapsed(new Position(50, 3)), horizontal: false));

            Assert.Null(reveal.PendingHorizontalRequest);
        });
    }

    [Fact]
    public void OnRevealRangeRequest_TooManyCursorsForTheViewport_ReturnsFalseAndDoesNotScroll()
    {
        WithReveal(100, (editor, reveal) =>
        {
            var request = new ViewRevealRangeRequestEvent(
                null, false, null, [new Selection(1, 1, 1, 1), new Selection(90, 1, 90, 1)], VerticalRevealType.Simple, true, ScrollType.Immediate);

            Assert.False(reveal.OnRevealRangeRequest(request));
            Assert.Equal(0, editor.ViewModel.ViewLayout.GetCurrentScrollTop());
        });
    }

    [Fact]
    public void ComputeScrollLeftToReveal_CaretRightOfTheViewport_ScrollsToShowTheCaretAndThePadding()
    {
        WithReveal(10, (editor, reveal) =>
        {
            var request = new HorizontalRevealRangeRequest(false, 1, 5, 5, 0, 0, ScrollType.Immediate);

            (int ScrollLeft, int MaxHorizontalOffset)? result = reveal.ComputeScrollLeftToReveal(request, (_, _, _) => (900.0, 0.0));

            int viewportWidth = editor.ViewModel.ViewLayout.GetCurrentViewport().Width - editor.ViewModel.LayoutInfo.VerticalScrollbarWidth;
            Assert.NotNull(result);
            Assert.Equal(900 + 15, result.Value.MaxHorizontalOffset);
            Assert.Equal(900 + 15 - viewportWidth, result.Value.ScrollLeft);
        });
    }

    [Fact]
    public void ComputeScrollLeftToReveal_CaretLeftOfTheViewport_ScrollsToTheCaretMinusTheExtraPixels()
    {
        WithReveal(10, (editor, reveal) =>
        {
            editor.ViewModel.ViewLayout.SetMaxLineWidth(3000);
            editor.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(1000, 0), ScrollType.Immediate);
            var request = new HorizontalRevealRangeRequest(false, 1, 5, 5, 0, 0, ScrollType.Immediate);

            (int ScrollLeft, int MaxHorizontalOffset)? result = reveal.ComputeScrollLeftToReveal(request, (_, _, _) => (200.0, 0.0));

            Assert.NotNull(result);
            Assert.Equal(200 - 30, result.Value.ScrollLeft);
        });
    }

    [Fact]
    public void ComputeScrollLeftToReveal_MinimalReveal_AddsNoPadding()
    {
        WithReveal(10, (editor, reveal) =>
        {
            editor.ViewModel.ViewLayout.SetMaxLineWidth(3000);
            editor.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(1000, 0), ScrollType.Immediate);
            var request = new HorizontalRevealRangeRequest(true, 1, 5, 5, 0, 0, ScrollType.Immediate);

            (int ScrollLeft, int MaxHorizontalOffset)? result = reveal.ComputeScrollLeftToReveal(request, (_, _, _) => (200.0, 0.0));

            Assert.Equal(200, result!.Value.ScrollLeft);
        });
    }

    [Fact]
    public void ComputeScrollLeftToReveal_LineNotMeasurable_ReturnsNull()
    {
        WithReveal(10, (editor, reveal) =>
        {
            var request = new HorizontalRevealRangeRequest(false, 1, 1, 1, 0, 0, ScrollType.Immediate);

            Assert.Null(reveal.ComputeScrollLeftToReveal(request, (_, _, _) => null));
        });
    }

    [Fact]
    public void ComputeScrollLeftToReveal_SelectionsWiderThanTheViewport_ReturnsNull()
    {
        WithReveal(10, (editor, reveal) =>
        {
            var request = new HorizontalRevealSelectionsRequest(
                false, [new Selection(1, 1, 1, 9), new Selection(2, 1, 2, 9)], 0, 0, ScrollType.Immediate);

            Assert.Null(reveal.ComputeScrollLeftToReveal(request, (line, _, _) => line == 1 ? (0.0, 10.0) : (900.0, 10.0)));
        });
    }

    [Fact]
    public void TryApplyPendingHorizontalReveal_LinesNotDrawnYet_KeepsTheRequest()
    {
        WithReveal(100, (editor, reveal) =>
        {
            reveal.OnRevealRangeRequest(Reveal(Range.Collapsed(new Position(50, 3))));
            int measured = 0;

            bool applied = reveal.TryApplyPendingHorizontalReveal(1, 20, (_, _, _) => { measured++; return (0.0, 0.0); }, _ => { });

            Assert.False(applied);
            Assert.Equal(0, measured);
            Assert.NotNull(reveal.PendingHorizontalRequest);
        });
    }

    [Fact]
    public void TryApplyPendingHorizontalReveal_LinesDrawn_ScrollsAndClearsTheRequest()
    {
        WithReveal(100, (editor, reveal) =>
        {
            reveal.OnRevealRangeRequest(Reveal(Range.Collapsed(new Position(50, 3))));
            editor.ViewModel.ViewLayout.SetMaxLineWidth(1000);
            int ensured = -1;

            bool applied = reveal.TryApplyPendingHorizontalReveal(40, 60, (_, _, _) => (900.0, 0.0), offset => ensured = offset);

            Assert.True(applied);
            Assert.Null(reveal.PendingHorizontalRequest);
            Assert.Equal(915, ensured);
            Assert.True(editor.ViewModel.ViewLayout.GetCurrentScrollLeft() > 0);
        });
    }

    [Fact]
    public void OnScrollChanged_SomeoneScrollsHorizontally_CancelsTheRequest()
    {
        WithReveal(100, (editor, reveal) =>
        {
            reveal.OnRevealRangeRequest(Reveal(Range.Collapsed(new Position(50, 3))));

            reveal.OnScrollChanged(CreateScrollEvent(scrollLeftChanged: true, scrollTopChanged: false, scrollTop: 0));

            Assert.Null(reveal.PendingHorizontalRequest);
        });
    }

    [Fact]
    public void OnScrollChanged_ScrollTopOutsideTheRevealRange_CancelsTheRequestButNotInsideIt()
    {
        WithReveal(100, (editor, reveal) =>
        {
            reveal.OnRevealRangeRequest(Reveal(Range.Collapsed(new Position(50, 3))));
            double stop = (51 * LineHeight) - ViewportHeight;

            reveal.OnScrollChanged(CreateScrollEvent(scrollLeftChanged: false, scrollTopChanged: true, scrollTop: stop / 2));
            Assert.NotNull(reveal.PendingHorizontalRequest);

            reveal.OnScrollChanged(CreateScrollEvent(scrollLeftChanged: false, scrollTopChanged: true, scrollTop: stop + 100));
            Assert.Null(reveal.PendingHorizontalRequest);
        });
    }

    private static ScrollEvent CreateScrollEvent(bool scrollLeftChanged, bool scrollTopChanged, double scrollTop) =>
        new(false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, scrollTop, false, false, scrollLeftChanged, false, false, scrollTopChanged);
}

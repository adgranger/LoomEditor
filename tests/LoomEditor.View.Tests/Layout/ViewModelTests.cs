using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no test for the layout part of ViewModel: all tests are new.
public class ViewModelTests
{
    private const int LineHeight = 20;
    private const double MaxDigitWidth = 8.4;

    private readonly FakeTimeProvider _clock = new();
    private readonly FakeFrameScheduler _frames = new();

    private static FontInfo CreateFontInfo(int lineHeight = LineHeight)
    {
        return new FontInfo(1, "Consolas", "normal", 14, lineHeight, 0, EditorFontOptions.LigaturesOff, EditorFontOptions.VariationsOff, true, MaxDigitWidth, 16.8, true, MaxDigitWidth, 8, 8, MaxDigitWidth);
    }

    private static string Lines(int count)
    {
        return string.Join('\n', Enumerable.Repeat("x", count));
    }

    private ViewModel CreateViewModel(string text, bool scrollBeyondLastLine = false)
    {
        return new ViewModel(
            new TextModel(text),
            CreateFontInfo(),
            new EditorLayoutOptions(),
            new ViewLayoutOptions(LineHeight, ScrollBeyondLastLine: scrollBeyondLastLine),
            _frames.Schedule,
            _clock);
    }

    private static void Insert(TextModel model, Position position, string text)
    {
        model.PushEditOperations(null, [TextEdit.Insert(position, text)], null);
    }

    [Fact]
    public void RawContentChanged_LinesInserted_GrowsContentHeight()
    {
        using ViewModel viewModel = CreateViewModel(Lines(100));
        Assert.Equal(100 * LineHeight, viewModel.ViewLayout.GetContentHeight());

        Insert(viewModel.Model, new Position(1, 1), new string('\n', 10));

        Assert.Equal(110 * LineHeight, viewModel.ViewLayout.GetContentHeight());
    }

    [Fact]
    public void RawContentChanged_LinesDeleted_ShrinksContentHeight()
    {
        using ViewModel viewModel = CreateViewModel(Lines(100));

        viewModel.Model.PushEditOperations(null, [TextEdit.Delete(new Range(1, 1, 11, 1))], null);

        Assert.Equal(90 * LineHeight, viewModel.ViewLayout.GetContentHeight());
    }

    [Fact]
    public void RawContentChanged_SetValue_ResetsToNewLineCount()
    {
        using ViewModel viewModel = CreateViewModel(Lines(100));

        viewModel.Model.SetValue(Lines(3));

        Assert.Equal(3 * LineHeight, viewModel.ViewLayout.GetContentHeight());
    }

    [Fact]
    public void RawContentChanged_DigitCountGrows_RaisesLayoutInfoChangedAndWidensLineNumbers()
    {
        using ViewModel viewModel = CreateViewModel(Lines(99_999));
        viewModel.SetViewportSize(800, 600);
        int before = viewModel.LayoutInfo.LineNumbersWidth;
        int raised = 0;
        viewModel.LayoutInfoChanged += (_, _) => raised++;

        Insert(viewModel.Model, new Position(99_999, 2), "\ny");

        Assert.Equal(100_000, viewModel.Model.LineCount);
        Assert.Equal(1, raised);
        int expectedGrowth = (int)(Math.Round(6 * MaxDigitWidth, MidpointRounding.AwayFromZero)
            - Math.Round(5 * MaxDigitWidth, MidpointRounding.AwayFromZero));
        Assert.Equal(before + expectedGrowth, viewModel.LayoutInfo.LineNumbersWidth);
        Assert.Equal(viewModel.LayoutInfo.ContentWidth, viewModel.ViewLayout.GetCurrentViewport().Width);
    }

    [Fact]
    public void RawContentChanged_SameDigitCount_DoesNotRaiseLayoutInfoChanged()
    {
        using ViewModel viewModel = CreateViewModel(Lines(100));
        int raised = 0;
        viewModel.LayoutInfoChanged += (_, _) => raised++;

        Insert(viewModel.Model, new Position(1, 1), "\n");

        Assert.Equal(0, raised);
    }

    [Fact]
    public void GetViewportData_AfterScrollingToLine500_StartsAtLine500()
    {
        using ViewModel viewModel = CreateViewModel(Lines(5000));
        viewModel.SetViewportSize(800, 600);
        viewModel.ViewLayout.SetScrollPosition(
            new NewScrollPosition(ScrollTop: viewModel.ViewLayout.GetVerticalOffsetForLineNumber(500)), ScrollType.Immediate);

        ViewportData data = viewModel.GetViewportData([Selection.Collapsed(new Position(500, 1))]);

        Assert.Equal(500, data.StartLineNumber);
        Assert.Equal(500, data.VisibleRange.StartLineNumber);
        Assert.Equal(1, data.VisibleRange.StartColumn);
        Assert.Equal(data.EndLineNumber, data.VisibleRange.EndLineNumber);
        Assert.Equal(2, data.VisibleRange.EndColumn);
        Assert.Equal(LineHeight, data.LineHeight);
        Assert.Single(data.Selections);
        Assert.Equal(data.EndLineNumber - data.StartLineNumber + 1, data.RelativeVerticalOffset.Length);
    }

    [Fact]
    public void SetViewportSize_UpdatesLayoutInfoAndViewport()
    {
        using ViewModel viewModel = CreateViewModel(Lines(100));
        int raised = 0;
        viewModel.LayoutInfoChanged += (_, _) => raised++;

        viewModel.SetViewportSize(800, 600);
        viewModel.SetViewportSize(800, 600);

        Assert.Equal(1, raised);
        Assert.Equal(800, viewModel.LayoutInfo.Width);
        Viewport viewport = viewModel.ViewLayout.GetCurrentViewport();
        Assert.Equal(viewModel.LayoutInfo.ContentWidth, viewport.Width);
        Assert.Equal(600, viewport.Height);
    }

    [Fact]
    public void SetFontInfo_NewLineHeight_UpdatesLineHeightsAndLayout()
    {
        using ViewModel viewModel = CreateViewModel(Lines(100));
        viewModel.SetViewportSize(800, 600);

        viewModel.SetFontInfo(CreateFontInfo(lineHeight: 30));

        Assert.Equal(30, viewModel.ViewLayout.GetLineHeightForLineNumber(1));
        Assert.Equal(100 * 30, viewModel.ViewLayout.GetContentHeight());
    }

    [Fact]
    public void SetOptions_GlyphMarginOn_MovesContentRight()
    {
        using ViewModel viewModel = CreateViewModel(Lines(10));
        viewModel.SetViewportSize(800, 600);
        int before = viewModel.LayoutInfo.ContentLeft;

        viewModel.SetOptions(new EditorLayoutOptions { GlyphMargin = true }, new ViewLayoutOptions(LineHeight));

        Assert.Equal(before + LineHeight, viewModel.LayoutInfo.ContentLeft);
    }

    [Fact]
    public void Dispose_EditsNoLongerReachTheLayout()
    {
        ViewModel viewModel = CreateViewModel(Lines(100));
        TextModel model = viewModel.Model;
        viewModel.Dispose();

        Insert(model, new Position(1, 1), new string('\n', 10));

        Assert.Equal(100 * LineHeight, viewModel.ViewLayout.GetContentHeight());
    }
}

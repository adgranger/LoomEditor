using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no unit test for these parts of ViewModel: all tests are new.
public class ViewModelTokensTests
{
    private const int LineHeight = 20;

    private readonly FakeTimeProvider _clock = new();
    private readonly FakeFrameScheduler _frames = new();

    private sealed class RecordingHandler : IViewEventHandler
    {
        public List<ViewEvent> Events { get; } = [];

        public void HandleEvents(IReadOnlyList<ViewEvent> events) => Events.AddRange(events);
    }

    private ViewModel CreateViewModel(string text)
    {
        var fontInfo = new FontInfo(1, "Consolas", "normal", 14, LineHeight, 0, EditorFontOptions.LigaturesOff, EditorFontOptions.VariationsOff, true, 8.4, 16.8, true, 8.4, 8, 8, 8.4);
        var viewModel = new ViewModel(new TextModel(text), fontInfo, new EditorLayoutOptions(), new ViewLayoutOptions(LineHeight), _frames.Schedule, _clock);
        viewModel.SetViewportSize(400, 100); // 5 lines
        return viewModel;
    }

    private static string Lines(int count) => string.Join('\n', Enumerable.Repeat("x", count));

    [Fact]
    public void Viewport_ReportsVisibleLinesToTheModel()
    {
        using ViewModel viewModel = CreateViewModel(Lines(100));
        var states = new List<AttachedViewState>();
        viewModel.Model.AttachedViews.VisibleRangesChanged += (_, state) => states.Add(state!);

        viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(ScrollTop: 10 * LineHeight), ScrollType.Immediate);

        AttachedViewState moved = Assert.Single(states);
        Assert.False(moved.Stabilized);
        Assert.Equal(11, moved.VisibleLineRanges[0].Start);

        _frames.RunFrame(); // the frame was drawn: the lines are stable

        Assert.True(states[^1].Stabilized);
        Assert.Equal(moved.VisibleLineRanges, states[^1].VisibleLineRanges);
    }

    [Fact]
    public void ModelTokensChanged_RaisesViewTokensChangedEvent()
    {
        using ViewModel viewModel = CreateViewModel(Lines(10));
        var handler = new RecordingHandler();
        viewModel.EventDispatcher.AddViewEventHandler(handler);

        viewModel.Model.RaiseTokensChanged(new LoomEditor.Core.Model.Events.ModelTokensChangedEventArgs([new(2, 4), new(7, 7)]));

        ViewTokensChangedEvent e = Assert.IsType<ViewTokensChangedEvent>(Assert.Single(handler.Events));
        Assert.Equal([new ViewLineRange(2, 4), new ViewLineRange(7, 7)], e.Ranges);
    }

    [Fact]
    public void ViewportData_GetTokens_ReturnsInflatedLineTokens()
    {
        using ViewModel viewModel = CreateViewModel("hello world");

        ViewportData data = viewModel.GetViewportData([Selection.Collapsed(new Position(1, 1))]);
        IViewLineTokens tokens = data.GetTokens(1);

        Assert.Equal("hello world", tokens.GetLineContent());
        Assert.True(tokens.GetCount() >= 1);
        Assert.Equal(11, tokens.GetEndOffset(tokens.GetCount() - 1));
    }
}

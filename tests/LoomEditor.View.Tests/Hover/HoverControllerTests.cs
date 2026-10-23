using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Features;
using LoomEditor.View.Tests.Highlighting.TestHelpers;
using LoomEditor.View.Hover;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.Hover.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Hover;

using Hover = LoomEditor.Core.Languages.Features.Hover;

// Monaco: ContentHoverController._onEditorMouseMove and its helpers; the tests are new (Monaco has no controller test that fits).
public sealed class HoverControllerTests : IDisposable
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

    private readonly ManualEditorScheduler _scheduler = new();
    private readonly ManualUiDispatcher _dispatcher = new();
    private readonly FakeHoverPresenter _presenter = new();
    private readonly EditorContext _context;
    private HoverController? _controller;

    public HoverControllerTests() => _context = CreateContext("let value = 1;\nsecond");

    public void Dispose()
    {
        _controller?.Dispose();
        _context.Dispose();
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private EditorContext CreateContext(string text)
    {
        var context = new EditorContext(static _ => new NoopDisposable(), TimeProvider.System, _dispatcher, _scheduler);
        context.SetText(text);
        context.Model.Tokenization.SetLanguageId("csharp");
        context.HoverPresenter = _presenter;
        return context;
    }

    private HoverController Start(EditorHoverOptions? options = null, bool marker = true)
    {
        if (marker)
        {
            // "value"
            _context.Markers.Change("o", [new MarkerData(MarkerSeverity.Error, "bad", 1, 5, 1, 10)]);
        }

        _controller = new HoverController(_context, options ?? EditorHoverOptions.Default);
        return _controller;
    }

    private static EditorMouseEvent Mouse() => new(10, 10, false, false, false, false, false, false, 0, 0);

    private void Emit(MouseTargetType type, int line, int column, bool isAfterLines = false, double? distance = null)
    {
        var position = new Position(line, column);
        var target = new MouseTarget(type, position, column, Range.Collapsed(position), isAfterLines, distance);
        _context.UserInputEvents.EmitMouseMove(new EditorMouseEventArgs(Mouse(), target));
    }

    private void Move(int line, int column) => Emit(MouseTargetType.ContentText, line, column);

    private void Advance(double milliseconds) => _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds));

    // The provider tasks continue on the thread pool: wait until their result is posted, then run it on the "UI thread".
    private void Pump()
    {
        Assert.True(SpinWait.SpinUntil(() => _dispatcher.PendingCount > 0, TimeSpan.FromSeconds(5)));
        _dispatcher.RunAll();
    }

    private void ShowMarkerHover()
    {
        Move(1, 6);
        Advance(300);
        Assert.True(_presenter.IsVisible);
    }

    private static Hover Md(string text, Range? range = null) => new([new MarkdownString(text)], range);

    private FakeHoverProvider Register(FakeHoverProvider provider)
    {
        _context.LanguageFeatures.HoverProviders.Register("csharp", provider);
        return provider;
    }

    [Fact]
    public void MouseRest_ShowsMarkerHoverAfterTheDelay()
    {
        Start();

        Move(1, 6);
        Advance(299);
        Assert.Equal(0, _presenter.ShowCount);
        Advance(1);

        Assert.Equal(1, _presenter.ShowCount);
        Assert.IsType<MarkerHoverPart>(Assert.Single(_presenter.Last!.Parts));
    }

    [Fact]
    public void MouseRest_ProviderHover_ShownWhenAsyncCompletes()
    {
        Start(marker: false);
        FakeHoverProvider provider = Register(FakeHoverProvider.Pending());

        Move(1, 6);
        Advance(200);
        provider.Complete(Md("doc", new Range(1, 5, 1, 10)));
        Pump();
        Assert.Equal(0, _presenter.ShowCount);
        Advance(100);

        Assert.Equal(1, _presenter.ShowCount);
        Assert.True(_presenter.Last!.IsComplete);
        Assert.IsType<MarkdownHoverPart>(Assert.Single(_presenter.Last.Parts));
    }

    [Fact]
    public void MouseRest_NoMarkerNoProvider_ShowsNothing()
    {
        Start(marker: false);

        Move(1, 6);
        Advance(2000);

        Assert.Equal(0, _presenter.ShowCount);
    }

    [Fact]
    public void MouseMoveToAnotherWord_HidesAndRestarts()
    {
        Start();
        ShowMarkerHover();

        Move(2, 2);
        Advance(2000);

        Assert.Equal(1, _presenter.HideCount);
        Assert.Equal(1, _presenter.ShowCount);
        Assert.False(_presenter.IsVisible);
    }

    [Fact]
    public void MouseMoveWithinSameWord_KeepsHoverVisible()
    {
        Start();
        ShowMarkerHover();

        Move(1, 7);
        Advance(2000);

        Assert.Equal(0, _presenter.HideCount);
        Assert.True(_presenter.IsVisible);
    }

    [Fact]
    public void MouseOverPresenter_StaysVisible()
    {
        Start();
        ShowMarkerHover();
        _presenter.IsMouseOver = true;

        Move(2, 2);
        Advance(2000);

        Assert.Equal(0, _presenter.HideCount);
    }

    [Fact]
    public void MouseDown_HidesUnlessOverPresenter()
    {
        Start();
        ShowMarkerHover();
        _presenter.IsMouseOver = true;

        _context.UserInputEvents.EmitMouseDown(new EditorMouseEventArgs(Mouse(), Target(1, 6)));
        Assert.Equal(0, _presenter.HideCount);

        _presenter.IsMouseOver = false;
        _context.UserInputEvents.EmitMouseDown(new EditorMouseEventArgs(Mouse(), Target(1, 6)));
        Assert.Equal(1, _presenter.HideCount);
    }

    private static MouseTarget Target(int line, int column)
    {
        var position = new Position(line, column);
        return new MouseTarget(MouseTargetType.ContentText, position, column, Range.Collapsed(position), false, null);
    }

    [Fact]
    public void MouseDrag_NoHoverComputed()
    {
        Start();

        _context.UserInputEvents.EmitMouseDown(new EditorMouseEventArgs(Mouse(), Target(1, 6)));
        Move(1, 6);
        Advance(2000);

        Assert.Equal(0, _presenter.ShowCount);
    }

    [Fact]
    public void MouseLeave_Hides()
    {
        Start();

        Move(1, 6);
        _context.UserInputEvents.EmitMouseLeave();
        Advance(2000);

        Assert.Equal(0, _presenter.ShowCount);
    }

    [Fact]
    public void MouseLeave_WhilePresenterVisibleAndSticky_KeepsIt()
    {
        Start();
        ShowMarkerHover();

        _context.UserInputEvents.EmitMouseLeave();

        Assert.Equal(0, _presenter.HideCount);
        Assert.True(_presenter.IsVisible);
    }

    [Fact]
    public void MouseLeave_NotSticky_Hides()
    {
        Start(EditorHoverOptions.Default with { Sticky = false });
        ShowMarkerHover();

        _context.UserInputEvents.EmitMouseLeave();

        Assert.Equal(1, _presenter.HideCount);
    }

    [Fact]
    public void ContentChanged_Hides()
    {
        Start();
        ShowMarkerHover();

        _context.SetText("other");

        Assert.False(_presenter.IsVisible);
    }

    [Fact]
    public void SelectionChanged_Hides()
    {
        Start();
        ShowMarkerHover();

        _context.SetSelections([Selection.Collapsed(new Position(2, 2))]);

        Assert.False(_presenter.IsVisible);
    }

    private EditorContext CreateScrollable(string text)
    {
        _controller?.Dispose();
        _context.Dispose();
        EditorContext context = CreateContext(text);
        context.ViewModel.SetViewportSize(200, 3 * context.ViewModel.FontInfo.LineHeight);
        context.ViewModel.ViewLayout.SetMaxLineWidth(5000);
        context.Markers.Change("o", [new MarkerData(MarkerSeverity.Error, "bad", 1, 5, 1, 10)]);
        _controller = new HoverController(context, EditorHoverOptions.Default);
        return context;
    }

    private void ShowHoverOn(EditorContext context)
    {
        var position = new Position(1, 6);
        context.UserInputEvents.EmitMouseMove(
            new EditorMouseEventArgs(Mouse(), new MouseTarget(MouseTargetType.ContentText, position, 6, Range.Collapsed(position), false, null)));
        Advance(300);
        Assert.True(_presenter.IsVisible);
    }

    [Fact]
    public void Scroll_Hides()
    {
        EditorContext context = CreateScrollable(string.Join('\n', Enumerable.Repeat("let value = 1;", 50)));
        ShowHoverOn(context);

        context.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, 100), ScrollType.Immediate);

        Assert.False(_presenter.IsVisible);
    }

    [Fact]
    public void HorizontalScroll_Hides()
    {
        EditorContext context = CreateScrollable("let value = 1;" + new string('x', 2000));
        ShowHoverOn(context);

        context.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(50, null), ScrollType.Immediate);

        Assert.False(_presenter.IsVisible);
    }

    [Fact]
    public void ScrollWithoutChange_DoesNotHide()
    {
        EditorContext context = CreateScrollable(string.Join('\n', Enumerable.Repeat("let value = 1;", 50)));
        context.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, 100), ScrollType.Immediate);
        ShowHoverOn(context);

        context.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, 100), ScrollType.Immediate);

        Assert.True(_presenter.IsVisible);
    }

    [Fact]
    public void Disabled_DoesNothing()
    {
        Start(EditorHoverOptions.Default with { Enabled = false });

        Move(1, 6);
        Advance(2000);

        Assert.Equal(0, _presenter.ShowCount);
    }

    [Fact]
    public void ContentEmptyNearText_ShowsHover()
    {
        Start();
        // after the last character of line 1, inside the marker range 5-15
        _context.Markers.Change("o", [new MarkerData(MarkerSeverity.Error, "bad", 1, 5, 1, 15)]);

        Emit(MouseTargetType.ContentEmpty, 1, 15, distance: 0);
        Advance(300);

        Assert.Equal(1, _presenter.ShowCount);
    }

    [Fact]
    public void ContentEmptyFarFromText_NoHover()
    {
        Start();
        _context.Markers.Change("o", [new MarkerData(MarkerSeverity.Error, "bad", 1, 5, 1, 15)]);

        Emit(MouseTargetType.ContentEmpty, 1, 15, distance: 1000);
        Advance(2000);

        Assert.Equal(0, _presenter.ShowCount);
    }

    [Fact]
    public void ContentEmptyAfterLines_NoHover()
    {
        Start();

        Emit(MouseTargetType.ContentEmpty, 2, 7, isAfterLines: true, distance: 0);
        Advance(2000);

        Assert.Equal(0, _presenter.ShowCount);
    }

    [Fact]
    public void GutterTarget_NoHover()
    {
        Start();

        Emit(MouseTargetType.GutterLineNumbers, 1, 6);
        Advance(2000);

        Assert.Equal(0, _presenter.ShowCount);
    }

    [Fact]
    public void StaleAsyncResult_AfterCancel_Dropped()
    {
        Start(marker: false);
        FakeHoverProvider provider = Register(FakeHoverProvider.Pending());

        Move(1, 6);
        Advance(200);
        _context.UserInputEvents.EmitMouseLeave();
        provider.Complete(Md("late", new Range(1, 5, 1, 10)));
        _dispatcher.RunAll();
        Advance(2000);

        Assert.Equal(0, _presenter.ShowCount);
    }

    [Fact]
    public void ProviderRange_BeyondTheText_IsValidated()
    {
        Start(marker: false);
        Register(new FakeHoverProvider((_, _) => Md("doc", new Range(1, 5, 1, 500))));

        Move(1, 6);
        Advance(150);
        Pump();
        Advance(150);

        var part = Assert.IsType<MarkdownHoverPart>(Assert.Single(_presenter.Last!.Parts));
        Assert.Equal(new Range(1, 5, 1, 15), part.Range);
    }

    [Fact]
    public void ProviderRange_OnAnotherLine_PartDropped()
    {
        Start(marker: false);
        Register(new FakeHoverProvider((_, _) => Md("doc", new Range(2, 1, 2, 3))));

        Move(1, 6);
        Advance(150);
        Pump();
        Advance(2000);

        Assert.Equal(0, _presenter.ShowCount);
    }

    [Fact]
    public void LoadingPart_ShownAtThreeTimesTheDelay()
    {
        Start(marker: false);
        Register(FakeHoverProvider.Pending());

        Move(1, 6);
        Advance(899);
        Assert.Equal(0, _presenter.ShowCount);
        Advance(1);

        Assert.IsType<LoadingHoverPart>(Assert.Single(_presenter.Last!.Parts));
        Assert.False(_presenter.Last.IsComplete);
    }

    [Fact]
    public void HoverVisible_FlagOnContext_FollowsThePresenter()
    {
        Start();
        Assert.False(_context.HoverVisible);

        ShowMarkerHover();
        Assert.True(_context.HoverVisible);

        _controller!.Hide();
        Assert.False(_context.HoverVisible);
    }

    [Fact]
    public void Sticky_ReschedulesReactionOnMove()
    {
        Start();
        ShowMarkerHover();

        Move(2, 2);
        Advance(299);
        Assert.Equal(0, _presenter.HideCount);
        Advance(1);

        Assert.Equal(1, _presenter.HideCount);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        HoverController controller = Start();
        controller.Dispose();

        Move(1, 6);
        Advance(2000);
        _context.UserInputEvents.EmitMouseLeave();

        Assert.Equal(0, _presenter.ShowCount);
    }
}

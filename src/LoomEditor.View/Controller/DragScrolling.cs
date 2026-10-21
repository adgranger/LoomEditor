// Ported from microsoft/vscode: src/vs/editor/browser/controller/dragScrolling.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Controller;

/// <summary>
/// Scrolls on its own, once per animation frame, while the mouse is held down outside the editor: the further away the mouse,
/// the faster the scroll (Monaco: <c>DragScrolling</c> and its two subclasses).
/// </summary>
/// <remarks>
/// Monaco reads <c>Date.now()</c> and schedules frames with <c>dom.scheduleAtNextAnimationFrame</c>; both are injected. Monaco
/// calls <c>viewHelper.renderNow()</c> after scrolling: here the scroll event of the view layout redraws the control.
/// Threading: UI thread only.
/// </remarks>
internal abstract class DragScrolling : IDisposable
{
    private readonly ViewModel _viewModel;
    private readonly DragScrollingServices _services;
    private DragScrollingOperation? _operation;

    protected DragScrolling(ViewModel viewModel, DragScrollingServices services)
    {
        _viewModel = viewModel;
        _services = services;
    }

    /// <summary>Starts scrolling, or updates the mouse position of the scroll in progress.</summary>
    public void Start(MouseTarget position, EditorMouseEvent mouseEvent)
    {
        if (_operation is not null)
        {
            _operation.SetPosition(position, mouseEvent);
        }
        else
        {
            _operation = CreateOperation(_viewModel, _services, position, mouseEvent);
        }
    }

    /// <summary>Stops scrolling.</summary>
    public void Stop()
    {
        _operation?.Dispose();
        _operation = null;
    }

    public void Dispose() => Stop();

    protected abstract DragScrollingOperation CreateOperation(
        ViewModel viewModel, DragScrollingServices services, MouseTarget position, EditorMouseEvent mouseEvent);
}

/// <summary>What the drag scrolling needs from its owner (Monaco passes the view helper and the target factory).</summary>
/// <param name="CreateMouseTarget">The hit test: relative X and Y in DIPs to a target.</param>
/// <param name="DispatchMouse">Monaco's <c>_dispatchMouse</c> of the mouse-down operation.</param>
/// <param name="ScheduleAtNextAnimationFrame">Runs a callback at the next frame.</param>
/// <param name="TimeProvider">The clock.</param>
internal sealed record DragScrollingServices(
    Func<double, double, MouseTarget> CreateMouseTarget,
    Action<MouseTarget, bool, NavigationCommandRevealType> DispatchMouse,
    Func<Action, IDisposable> ScheduleAtNextAnimationFrame,
    TimeProvider TimeProvider);

/// <summary>Scrolls up or down (Monaco: <c>TopBottomDragScrolling</c>).</summary>
internal sealed class TopBottomDragScrolling(ViewModel viewModel, DragScrollingServices services) : DragScrolling(viewModel, services)
{
    protected override DragScrollingOperation CreateOperation(
        ViewModel viewModel, DragScrollingServices services, MouseTarget position, EditorMouseEvent mouseEvent) =>
        new TopBottomDragScrollingOperation(viewModel, services, position, mouseEvent);
}

/// <summary>Scrolls left or right (Monaco: <c>LeftRightDragScrolling</c>).</summary>
internal sealed class LeftRightDragScrolling(ViewModel viewModel, DragScrollingServices services) : DragScrolling(viewModel, services)
{
    protected override DragScrollingOperation CreateOperation(
        ViewModel viewModel, DragScrollingServices services, MouseTarget position, EditorMouseEvent mouseEvent) =>
        new LeftRightDragScrollingOperation(viewModel, services, position, mouseEvent);
}

/// <summary>One drag scroll in progress (Monaco: <c>DragScrollingOperation</c>).</summary>
internal abstract class DragScrollingOperation : IDisposable
{
    private long _lastTime;
    private IDisposable _animationFrame;

    protected DragScrollingOperation(ViewModel viewModel, DragScrollingServices services, MouseTarget position, EditorMouseEvent mouseEvent)
    {
        ViewModel = viewModel;
        Services = services;
        Target = position;
        MouseEvent = mouseEvent;
        _lastTime = Now();
        _animationFrame = services.ScheduleAtNextAnimationFrame(Execute);
    }

    protected ViewModel ViewModel { get; }

    protected DragScrollingServices Services { get; }

    protected MouseTarget Target { get; private set; }

    protected EditorMouseEvent MouseEvent { get; private set; }

    public void Dispose() => _animationFrame.Dispose();

    public void SetPosition(MouseTarget position, EditorMouseEvent mouseEvent)
    {
        Target = position;
        MouseEvent = mouseEvent;
    }

    protected abstract void Execute();

    /// <summary>Asks for the next frame; call it at the end of <see cref="Execute"/>.</summary>
    protected void ScheduleNextFrame() => _animationFrame = Services.ScheduleAtNextAnimationFrame(Execute);

    /// <summary>Updates the internal state and returns the milliseconds elapsed since the last time.</summary>
    protected long Tick()
    {
        long now = Now();
        long elapsed = now - _lastTime;
        _lastTime = now;
        return elapsed;
    }

    private long Now() => Services.TimeProvider.GetUtcNow().ToUnixTimeMilliseconds();
}

/// <summary>Monaco: <c>TopBottomDragScrollingOperation</c>.</summary>
internal sealed class TopBottomDragScrollingOperation(
    ViewModel viewModel, DragScrollingServices services, MouseTarget position, EditorMouseEvent mouseEvent)
    : DragScrollingOperation(viewModel, services, position, mouseEvent)
{
    // The number of lines per second to auto-scroll.
    private double GetScrollSpeed()
    {
        int lineHeight = ViewModel.FontInfo.LineHeight;
        double viewportInLines = (double)ViewModel.LayoutInfo.Height / lineHeight;
        double outsideDistanceInLines = Target.OutsideDistance / lineHeight;

        if (outsideDistanceInLines <= 1.5)
        {
            return Math.Max(30, viewportInLines * (1 + outsideDistanceInLines));
        }

        if (outsideDistanceInLines <= 3)
        {
            return Math.Max(60, viewportInLines * (2 + outsideDistanceInLines));
        }

        return Math.Max(200, viewportInLines * (7 + outsideDistanceInLines));
    }

    protected override void Execute()
    {
        int lineHeight = ViewModel.FontInfo.LineHeight;
        double scrollSpeedInLines = GetScrollSpeed();
        long elapsed = Tick();
        double scrollInPixels = scrollSpeedInLines * (elapsed / 1000.0) * lineHeight;
        bool above = Target.OutsidePosition == OutsidePosition.Above;
        double scrollValue = above ? -scrollInPixels : scrollInPixels;

        ViewModel.ViewLayout.DeltaScrollNow(0, scrollValue);

        PartialViewLinesViewportData viewportData = ViewModel.ViewLayout.GetLinesViewportData();
        int edgeLineNumber = above ? viewportData.StartLineNumber : viewportData.EndLineNumber;
        bool cannotScrollAnymore = above ? viewportData.StartLineNumber == 1 : viewportData.EndLineNumber == ViewModel.GetLineCount();

        // First, try to find a position that matches the horizontal position of the mouse.
        EditorLayoutInfo layoutInfo = ViewModel.LayoutInfo;
        MouseTarget mouseTarget = Services.CreateMouseTarget(MouseEvent.X, layoutInfo.Height - layoutInfo.HorizontalScrollbarHeight - 0.1);
        if (mouseTarget.Position is not { } hitPosition || hitPosition.LineNumber != edgeLineNumber || cannotScrollAnymore)
        {
            mouseTarget = above
                ? MouseTarget.CreateOutsideEditor(Target.MouseColumn, new Position(edgeLineNumber, 1), OutsidePosition.Above, Target.OutsideDistance)
                : MouseTarget.CreateOutsideEditor(
                    Target.MouseColumn,
                    new Position(edgeLineNumber, ViewModel.GetLineMaxColumn(edgeLineNumber)),
                    OutsidePosition.Below,
                    Target.OutsideDistance);
        }

        Services.DispatchMouse(mouseTarget, true, NavigationCommandRevealType.None);
        ScheduleNextFrame();
    }
}

/// <summary>Monaco: <c>LeftRightDragScrollingOperation</c>.</summary>
internal sealed class LeftRightDragScrollingOperation(
    ViewModel viewModel, DragScrollingServices services, MouseTarget position, EditorMouseEvent mouseEvent)
    : DragScrollingOperation(viewModel, services, position, mouseEvent)
{
    // The number of columns per second to auto-scroll.
    private double GetScrollSpeed()
    {
        double charWidth = ViewModel.FontInfo.TypicalFullwidthCharacterWidth;
        double viewportInChars = ViewModel.LayoutInfo.ContentWidth / charWidth;
        double outsideDistanceInChars = Target.OutsideDistance / charWidth;
        if (outsideDistanceInChars <= 1.5)
        {
            return Math.Max(30, viewportInChars * (1 + outsideDistanceInChars));
        }

        if (outsideDistanceInChars <= 3)
        {
            return Math.Max(60, viewportInChars * (2 + outsideDistanceInChars));
        }

        return Math.Max(200, viewportInChars * (7 + outsideDistanceInChars));
    }

    protected override void Execute()
    {
        double charWidth = ViewModel.FontInfo.TypicalFullwidthCharacterWidth;
        double scrollSpeedInChars = GetScrollSpeed();
        long elapsed = Tick();
        double scrollInPixels = scrollSpeedInChars * (elapsed / 1000.0) * charWidth * 0.5;
        double scrollValue = Target.OutsidePosition == OutsidePosition.Left ? -scrollInPixels : scrollInPixels;

        ViewModel.ViewLayout.DeltaScrollNow(scrollValue, 0);

        if (Target.Position is not { } edgePosition)
        {
            return;
        }

        int edgeLineNumber = edgePosition.LineNumber;

        // Text direction is always left to right (no RTL in phase 3): a line is exceeded on the right.
        MouseTarget mouseTarget;
        if (Target.OutsidePosition == OutsidePosition.Right)
        {
            // Move the selection to the far end of the line.
            int lineWidth = ViewModel.GetLineMaxColumn(edgeLineNumber);
            mouseTarget = MouseTarget.CreateOutsideEditor(lineWidth, new Position(edgeLineNumber, lineWidth), OutsidePosition.Right, Target.OutsideDistance);
        }
        else
        {
            // Move the selection to the beginning of the line.
            mouseTarget = MouseTarget.CreateOutsideEditor(1, new Position(edgeLineNumber, 1), OutsidePosition.Left, Target.OutsideDistance);
        }

        Services.DispatchMouse(mouseTarget, true, NavigationCommandRevealType.None);
        ScheduleNextFrame();
    }
}

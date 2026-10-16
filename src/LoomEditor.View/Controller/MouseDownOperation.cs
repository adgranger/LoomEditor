// Ported from microsoft/vscode: src/vs/editor/browser/controller/mouseHandler.ts (MouseDownOperation)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Controller;

/// <summary>
/// One mouse press, from the button going down to the button going up: counts the clicks, dispatches the first click, then
/// every move while the button is held (drag selection), and scrolls on its own when the mouse leaves the editor.
/// </summary>
/// <remarks>
/// <para>
/// The control owns the pointer capture (Monaco's <c>GlobalEditorPointerMoveMonitor</c>): it calls <see cref="Start"/> on
/// press, <see cref="OnMouseDownThenMove"/> on every move while <see cref="IsActive"/>, and <see cref="OnPointerUp"/> on
/// release. The hit test is a delegate so that this class knows no Rendering type.
/// </para>
/// <para>
/// Not ported: the drag-and-drop of the selection (<c>mouseHandler.ts</c> 461–497, ADR 0006, decision 12), view zones
/// (<c>_helpPositionJumpOverViewZone</c>) and right-to-left lines. Threading: UI thread only.
/// </para>
/// </remarks>
internal sealed class MouseDownOperation : IDisposable
{
    private readonly ViewModel _viewModel;
    private readonly ViewController _viewController;
    private readonly Func<double, double, MouseTarget> _createMouseTarget;
    private readonly MouseDownState _mouseState;
    private readonly TopBottomDragScrolling _topBottomDragScrolling;
    private readonly LeftRightDragScrolling _leftRightDragScrolling;

    /// <summary>Creates the operation.</summary>
    /// <param name="viewModel">The view.</param>
    /// <param name="viewController">Runs the cursor commands.</param>
    /// <param name="createMouseTarget">The hit test of <see cref="MouseTargetFactory"/>: relative X and Y in DIPs to a target.</param>
    /// <param name="scheduleAtNextAnimationFrame">Runs a callback at the next frame (the view model's delegate).</param>
    /// <param name="timeProvider">The clock (click counting and drag scrolling).</param>
    public MouseDownOperation(
        ViewModel viewModel,
        ViewController viewController,
        Func<double, double, MouseTarget> createMouseTarget,
        Func<Action, IDisposable> scheduleAtNextAnimationFrame,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(viewController);
        ArgumentNullException.ThrowIfNull(createMouseTarget);
        ArgumentNullException.ThrowIfNull(scheduleAtNextAnimationFrame);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _viewModel = viewModel;
        _viewController = viewController;
        _createMouseTarget = createMouseTarget;
        _mouseState = new MouseDownState(timeProvider);

        var services = new DragScrollingServices(
            createMouseTarget,
            (target, inSelectionMode, revealType) => DispatchMouse(target, inSelectionMode, revealType),
            scheduleAtNextAnimationFrame,
            timeProvider);
        _topBottomDragScrolling = new TopBottomDragScrolling(viewModel, services);
        _leftRightDragScrolling = new LeftRightDragScrolling(viewModel, services);
    }

    /// <summary>True between <see cref="Start"/> and the release of the button: the control keeps the pointer captured.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Handles the press: counts the click and dispatches it.
    /// </summary>
    /// <param name="targetType">What the hit test found under the pointer.</param>
    /// <param name="e">The press.</param>
    public void Start(MouseTargetType targetType, EditorMouseEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        _mouseState.SetStartedOnLineNumbers(targetType == MouseTargetType.GutterLineNumbers);
        _mouseState.SetStartButtons(e);
        _mouseState.SetModifiers(e);
        MouseTarget? position = FindMousePosition(e);
        if (position?.Position is not { } clickPosition)
        {
            // Ignoring because position is unknown.
            return;
        }

        _mouseState.TrySetCount(e.Detail, clickPosition);

        // Not ported in phase 3: drag-and-drop of the selection (mouseHandler.ts 461–497).
        DispatchMouse(position, e.ShiftKey, NavigationCommandRevealType.Minimal);

        IsActive = true;
    }

    /// <summary>Handles a pointer move while the button is held (Monaco: <c>_onMouseDownThenMove</c>).</summary>
    public void OnMouseDownThenMove(EditorMouseEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        _mouseState.SetModifiers(e);

        MouseTarget? position = FindMousePosition(e);
        if (position is null)
        {
            // Ignoring because position is unknown.
            return;
        }

        if (position.Type == MouseTargetType.OutsideEditor)
        {
            if (position.OutsidePosition is OutsidePosition.Above or OutsidePosition.Below)
            {
                _topBottomDragScrolling.Start(position, e);
                _leftRightDragScrolling.Stop();
            }
            else
            {
                _leftRightDragScrolling.Start(position, e);
                _topBottomDragScrolling.Stop();
            }
        }
        else
        {
            _topBottomDragScrolling.Stop();
            _leftRightDragScrolling.Stop();
            DispatchMouse(position, true, NavigationCommandRevealType.Minimal);
        }
    }

    /// <summary>The button went up (or the pointer capture was lost): ends the operation.</summary>
    public void OnPointerUp() => Stop();

    /// <summary>The editor was resized: ends the operation (Monaco stops monitoring the pointer).</summary>
    public void OnHeightChanged() => Stop();

    /// <summary>Ends the operation and the drag scrolling.</summary>
    public void Dispose() => Stop();

    private void Stop()
    {
        IsActive = false;
        _topBottomDragScrolling.Stop();
        _leftRightDragScrolling.Stop();
    }

    // Monaco: _getMouseColumn.
    private int GetMouseColumn(EditorMouseEvent e)
    {
        double mouseContentHorizontalOffset = _viewModel.ViewLayout.GetCurrentScrollLeft() + e.X - _viewModel.LayoutInfo.ContentLeft;
        return MouseTargetFactory.GetMouseColumn(mouseContentHorizontalOffset, _viewModel.FontInfo.TypicalHalfwidthCharacterWidth);
    }

    // Monaco: _getPositionOutsideEditor. The editor content starts at (0, 0) and is layoutInfo.Height high.
    private MouseTarget? GetPositionOutsideEditor(EditorMouseEvent e)
    {
        ViewLayout viewLayout = _viewModel.ViewLayout;
        EditorLayoutInfo layoutInfo = _viewModel.LayoutInfo;
        int mouseColumn = GetMouseColumn(e);

        if (e.Y < 0)
        {
            double outsideDistance = -e.Y;
            double verticalOffset = Math.Max(viewLayout.GetCurrentScrollTop() - outsideDistance, 0);
            int aboveLineNumber = viewLayout.GetLineNumberAtVerticalOffset((int)verticalOffset);
            return MouseTarget.CreateOutsideEditor(mouseColumn, new Position(aboveLineNumber, 1), OutsidePosition.Above, outsideDistance);
        }

        if (e.Y > layoutInfo.Height)
        {
            double outsideDistance = e.Y - layoutInfo.Height;
            double verticalOffset = viewLayout.GetCurrentScrollTop() + e.Y;
            int belowLineNumber = viewLayout.GetLineNumberAtVerticalOffset((int)verticalOffset);
            return MouseTarget.CreateOutsideEditor(
                mouseColumn, new Position(belowLineNumber, _viewModel.Model.GetLineMaxColumn(belowLineNumber)), OutsidePosition.Below, outsideDistance);
        }

        int possibleLineNumber = viewLayout.GetLineNumberAtVerticalOffset((int)(viewLayout.GetCurrentScrollTop() + e.Y));

        double xLeftBoundary = layoutInfo.ContentLeft;
        if (e.X <= xLeftBoundary)
        {
            double outsideDistance = xLeftBoundary - e.X;
            return MouseTarget.CreateOutsideEditor(mouseColumn, new Position(possibleLineNumber, 1), OutsidePosition.Left, outsideDistance);
        }

        // No minimap: the content ends where the vertical scrollbar starts.
        double xRightBoundary = layoutInfo.Width - layoutInfo.VerticalScrollbarWidth;
        if (e.X >= xRightBoundary)
        {
            double outsideDistance = e.X - xRightBoundary;
            return MouseTarget.CreateOutsideEditor(
                mouseColumn, new Position(possibleLineNumber, _viewModel.Model.GetLineMaxColumn(possibleLineNumber)), OutsidePosition.Right, outsideDistance);
        }

        return null;
    }

    // Monaco: _findMousePosition (view zones are not ported).
    private MouseTarget? FindMousePosition(EditorMouseEvent e)
    {
        MouseTarget? positionOutsideEditor = GetPositionOutsideEditor(e);
        if (positionOutsideEditor is not null)
        {
            return positionOutsideEditor;
        }

        MouseTarget target = _createMouseTarget(e.X, e.Y);
        return target.Position is null ? null : target;
    }

    // Monaco: _dispatchMouse.
    private void DispatchMouse(MouseTarget target, bool inSelectionMode, NavigationCommandRevealType revealType)
    {
        if (target.Position is not { } position)
        {
            return;
        }

        _viewController.DispatchMouse(new MouseDispatchData(
            position,
            target.MouseColumn,
            revealType,
            _mouseState.StartedOnLineNumbers,
            inSelectionMode,
            _mouseState.Count,
            _mouseState.AltKey,
            _mouseState.CtrlKey,
            _mouseState.MetaKey,
            _mouseState.ShiftKey,
            _mouseState.LeftButton,
            _mouseState.MiddleButton,
            OnInjectedText: false));
    }
}

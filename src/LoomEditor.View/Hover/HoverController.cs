// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/contentHoverController.ts, contentHoverWidgetWrapper.ts (the result handling and the anchors)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Controller;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Hover;

/// <summary>
/// Shows a hover when the mouse rests on text (or on request at the cursor) and hides it when the mouse leaves,
/// the text or the selection changes, or the view scrolls.
/// Differences from Monaco: the sticky "mouse getting closer" geometry is replaced by <see cref="IHoverPresenter.IsMouseOver"/>;
/// text and selection changes hide the hover instead of Monaco's key down rule; no focus, resize, keyboard modifier or color picker rules.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class HoverController : IDisposable
{
    private readonly IEditorContext _context;
    private readonly ViewUserInputEvents? _events;
    private readonly ViewModel? _viewModel;
    private readonly HoverOperation? _operation;
    private readonly RunOnceScheduler? _reactRunner;
    private EditorMouseEventArgs? _mouseMoveEvent;
    private bool _isMouseDown;
    private HoverContent? _current;
    private bool _disposed;

    public HoverController(IEditorContext context, EditorHoverOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _context = context;
        Options = options;

        _events = context.GetUserInputEvents();
        _viewModel = context.GetViewModel();
        var features = context.GetLanguageFeatures();
        var markers = context.GetMarkerDecorations();
        if (_events is null || _viewModel is null || features is null || markers is null)
        {
            return;
        }

        var computer = new ContentHoverComputer(context.Model, markers, features.HoverProviders, context.GetLanguageId);
        _operation = new HoverOperation(context.Scheduler, context.GetUiDispatcher(), computer, () => Options.Delay);
        _operation.Result += OnResult;
        _reactRunner = new RunOnceScheduler(context.Scheduler, () =>
        {
            if (_mouseMoveEvent is not null)
            {
                ReactToMouseMove(_mouseMoveEvent);
            }
        }, TimeSpan.Zero);

        _events.MouseMove += OnMouseMove;
        _events.MouseDown += OnMouseDown;
        _events.MouseUp += OnMouseUp;
        _events.MouseLeave += OnMouseLeave;
        context.Model.ContentChanged += OnContentChanged;
        context.SelectionChanged += OnSelectionChanged;
        _viewModel.ViewLayout.Scroll += OnScroll;
    }

    /// <summary>The hover options.</summary>
    public EditorHoverOptions Options { get; set; }

    private IHoverPresenter Presenter => _context.GetHoverPresenter();

    /// <summary>Hides the hover and cancels what is pending (Monaco: <c>hideContentHover</c>).</summary>
    public void Hide()
    {
        _reactRunner?.Cancel();
        _operation?.Cancel();
        SetCurrentResult(null);
    }

    /// <summary>Computes and shows the hover at the cursor now (Monaco: <c>ShowOrFocusHoverAction</c> without focus).</summary>
    public void ShowAtCursor()
    {
        if (_operation is null)
        {
            return;
        }

        var range = Range.Collapsed(_context.Selection.Position);
        _operation.Cancel();
        _operation.Start(HoverStartMode.Immediate, new HoverComputerOptions(new HoverAnchor(range), HoverStartSource.Keyboard));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_events is not null)
        {
            _events.MouseMove -= OnMouseMove;
            _events.MouseDown -= OnMouseDown;
            _events.MouseUp -= OnMouseUp;
            _events.MouseLeave -= OnMouseLeave;
            _context.Model.ContentChanged -= OnContentChanged;
            _context.SelectionChanged -= OnSelectionChanged;
            _viewModel!.ViewLayout.Scroll -= OnScroll;
        }

        Hide();
        _operation?.Dispose();
        _reactRunner?.Dispose();
    }

    // Monaco: _onEditorMouseMove
    private void OnMouseMove(object? sender, EditorMouseEventArgs args)
    {
        if (!Options.Enabled)
        {
            return;
        }

        IHoverPresenter presenter = Presenter;
        if (_isMouseDown && !presenter.IsMouseOver)
        {
            Hide();
            return;
        }

        _mouseMoveEvent = args;
        if (presenter.IsVisible && Options.Sticky && presenter.IsMouseOver)
        {
            _reactRunner!.Cancel();
            return;
        }

        if (presenter.IsVisible && Options.Sticky && Options.HidingDelay > TimeSpan.Zero)
        {
            if (!_reactRunner!.IsScheduled)
            {
                _reactRunner.Schedule(Options.HidingDelay);
            }

            return;
        }

        ReactToMouseMove(args);
    }

    private void OnMouseDown(object? sender, EditorMouseEventArgs args)
    {
        _isMouseDown = true;
        if (!Presenter.IsMouseOver)
        {
            Hide();
        }
    }

    private void OnMouseUp(object? sender, EditorMouseEventArgs args) => _isMouseDown = false;

    private void OnMouseLeave(object? sender, EventArgs e)
    {
        _reactRunner!.Cancel();
        _mouseMoveEvent = null;
        IHoverPresenter presenter = Presenter;
        if (presenter.IsMouseOver || (Options.Sticky && presenter.IsVisible))
        {
            return;
        }

        Hide();
    }

    private void OnContentChanged(object? sender, EventArgs e) => Hide();

    private void OnSelectionChanged(object? sender, EventArgs e) => Hide();

    private void OnScroll(object? sender, ScrollEvent e)
    {
        if (e.ScrollTopChanged || e.ScrollLeftChanged)
        {
            Hide();
        }
    }

    // Monaco: _reactToEditorMouseMove + showsOrWillShow
    private void ReactToMouseMove(EditorMouseEventArgs args)
    {
        HoverAnchor? anchor = FindAnchor(args);
        if (StartShowingOrUpdateHover(anchor, HoverStartMode.Delayed, HoverStartSource.Mouse))
        {
            return;
        }

        Hide();
    }

    // Monaco: _findHoverAnchorCandidates (the range participants' anchors only)
    private HoverAnchor? FindAnchor(EditorMouseEventArgs args)
    {
        MouseTarget target = args.Target;
        switch (target.Type)
        {
            case MouseTargetType.ContentText when target.Range is { } range:
                return new HoverAnchor(range, args.Event.X, args.Event.Y);
            case MouseTargetType.ContentEmpty when !target.IsAfterLines
                && target.HorizontalDistanceToText is { } distance
                && distance < _viewModel!.FontInfo.TypicalHalfwidthCharacterWidth / 2
                && target.Range is { } emptyRange:
                return new HoverAnchor(emptyRange, args.Event.X, args.Event.Y);
            default:
                return null;
        }
    }

    // Monaco: _startShowingOrUpdateHover
    private bool StartShowingOrUpdateHover(HoverAnchor? anchor, HoverStartMode mode, HoverStartSource source)
    {
        if (!Presenter.IsVisible || _current is null)
        {
            if (anchor is null)
            {
                return false;
            }

            StartOperationIfNecessary(anchor, mode, source);
            return true;
        }

        if (anchor is null)
        {
            SetCurrentResult(null);
            return false;
        }

        if (_current.Anchor.Equals(anchor))
        {
            return true;
        }

        if (!anchor.CanAdoptVisibleHover(_current.Anchor, _current.Anchor.Range.Start))
        {
            SetCurrentResult(null);
            StartOperationIfNecessary(anchor, mode, source);
            return true;
        }

        SetCurrentResult(Filter(_current, anchor));
        StartOperationIfNecessary(anchor, mode, source);
        return true;
    }

    private static HoverContent Filter(HoverContent content, HoverAnchor anchor) =>
        HoverContent.Create(
            anchor,
            content.Parts.Where(p => p is not LoadingHoverPart && p.IsValidForHoverAnchor(anchor)),
            content.IsComplete);

    private void StartOperationIfNecessary(HoverAnchor anchor, HoverStartMode mode, HoverStartSource source)
    {
        if (_operation!.Options?.Anchor.Equals(anchor) == true)
        {
            return;
        }

        _operation.Cancel();
        _operation.Start(mode, new HoverComputerOptions(anchor, source));
    }

    // Monaco: _withResult + _addLoadingMessage
    private void OnResult(object? sender, HoverResult result)
    {
        HoverAnchor anchor = result.Options.Anchor;
        var parts = new List<IHoverPart>();
        foreach (IHoverPart part in result.Value)
        {
            if (part is MarkdownHoverPart markdown)
            {
                // The provider ranges come from the server unvalidated.
                Range validated = _context.Model.ValidateRange(markdown.Range);
                if (validated.StartLineNumber != anchor.Range.StartLineNumber || validated.EndLineNumber != anchor.Range.StartLineNumber)
                {
                    continue;
                }

                parts.Add(markdown with { Range = validated });
            }
            else
            {
                parts.Add(part);
            }
        }

        if (result.HasLoadingMessage)
        {
            parts.Add(new LoadingHoverPart(anchor.Range));
        }

        HoverContent content = HoverContent.Create(anchor, parts, result.IsComplete);
        bool visibleComplete = Presenter.IsVisible && _current is { IsComplete: true };
        if (!visibleComplete)
        {
            SetCurrentResult(content);
        }

        if (!result.IsComplete)
        {
            return;
        }

        if (result.Value.Count == 0 && result.Options.InsistOnKeepingHoverVisible)
        {
            return;
        }

        SetCurrentResult(content);
    }

    private void SetCurrentResult(HoverContent? content)
    {
        // A content without parts is no content.
        content = content is { Parts.Count: 0 } ? null : content;
        if (ReferenceEquals(_current, content))
        {
            return;
        }

        _current = content;
        if (_current is null)
        {
            Presenter.Hide();
        }
        else
        {
            Presenter.Show(_current);
        }

        if (_context is EditorContext editor)
        {
            editor.HoverVisible = _current is not null;
        }
    }
}

using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;
using LoomEditor.WinUI.Controls.Comments;
using LoomEditor.WinUI.Controls.Overlay;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls;

/// <remarks>Threading: UI thread only (not checked).</remarks>
public sealed partial class LoomEditor
{
    private CommentThreadsController _commentThreads = null!;
    private CommentZoneHost? _commentZoneHost;
    private Canvas? _zoneWidgetCanvas;

    /// <summary>The zone-widget layer status line, for the Gallery's manual test.</summary>
    internal string CommentZonesDiagnostics => _commentZoneHost?.Diagnostics ?? "no zone-widget layer";

    /// <summary>The comment threads of this editor, in creation order.</summary>
    public IReadOnlyList<CommentThread> CommentThreads => _commentThreads.Threads;

    /// <summary>Raised when the user submits a comment; the host adds it with <see cref="CommentThread.AddComment"/>.</summary>
    public event EventHandler<CommentSubmittedEventArgs>? CommentSubmitted;

    /// <summary>Raised when the user cancels a comment box.</summary>
    public event EventHandler<CommentThreadEventArgs>? CommentCancelled;

    /// <summary>Raised when the add-comment command asks for a thread on a line; nothing is added by the editor.</summary>
    public event EventHandler<AddCommentRequestedEventArgs>? AddCommentRequested;

    /// <summary>Raised after a suggestion of a comment replaced a line.</summary>
    public event EventHandler<SuggestionAppliedEventArgs>? SuggestionApplied;

    /// <summary>Adds a comment thread anchored to a line.</summary>
    /// <param name="lineNumber">The 1-based line.</param>
    /// <param name="title">An optional title.</param>
    /// <param name="startComposing">Opens the comment box at once.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is outside the document.</exception>
    public CommentThread AddCommentThread(int lineNumber, string? title = null, bool startComposing = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(lineNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lineNumber, Model.LineCount);
        CommentThread thread = _commentThreads.Add(lineNumber, title, startComposing);
        if (startComposing)
        {
            _commentZoneHost?.FocusComposer(thread);
        }

        return thread;
    }

    /// <summary>Removes a thread; <see langword="false"/> when it does not belong to this editor.</summary>
    public bool RemoveCommentThread(CommentThread thread)
    {
        ArgumentNullException.ThrowIfNull(thread);
        return _commentThreads.Remove(thread);
    }

    // The optional PART_ZoneWidgetCanvas hosts a box over each comment thread's zone; without the part the zones are only gaps.
    private void InitializeCommentZoneHost(Canvas? canvas)
    {
        _commentZoneHost?.Dispose();
        _commentZoneHost = null;
        if (_zoneWidgetCanvas is not null)
        {
            _zoneWidgetCanvas.SizeChanged -= OnZoneWidgetCanvasSizeChanged;
        }

        _zoneWidgetCanvas = canvas;
        if (canvas is null)
        {
            return;
        }

        canvas.SizeChanged += OnZoneWidgetCanvasSizeChanged;
        _commentZoneHost = new CommentZoneHost(this, new OverlayHost(canvas), _commentThreads);
        _commentZoneHost.ApplyTheme(EffectiveTheme);
    }

    // A Canvas does not clip its children: without this a box scrolled half out would draw outside the editor.
    private void OnZoneWidgetCanvasSizeChanged(object sender, SizeChangedEventArgs e) =>
        ((Canvas)sender).Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };

    private void InitializeCommentThreads()
    {
        _commentThreads = new CommentThreadsController(_context);
        _context.CommentThreads = _commentThreads;
        _commentThreads.CommentSubmitted += (_, e) => CommentSubmitted?.Invoke(this, new CommentSubmittedEventArgs(e.Thread, e.Text));
        _commentThreads.CommentCancelled += (_, t) => CommentCancelled?.Invoke(this, new CommentThreadEventArgs(t));
        _commentThreads.AddCommentRequested += (_, line) => AddCommentRequested?.Invoke(this, new AddCommentRequestedEventArgs(line));
        _commentThreads.SuggestionApplied += (_, e) => SuggestionApplied?.Invoke(this, new SuggestionAppliedEventArgs(e.Thread, e.Comment, e.LineNumber));
    }
}

using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;
using LoomEditor.WinUI.Controls.Comments;
using LoomEditor.WinUI.Controls.Overlay;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.WinUI.Controls;

/// <remarks>Threading: UI thread only (not checked).</remarks>
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="CommentGutterButtonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty CommentGutterButtonEnabledProperty = DependencyProperty.Register(
        nameof(CommentGutterButtonEnabled), typeof(bool), typeof(LoomEditor), new PropertyMetadata(false, OnCommentGutterButtonEnabledChanged));

    private CommentThreadsController _commentThreads = null!;
    private CommentZoneHost? _commentZoneHost;
    private CommentGutterController? _commentGutter;
    private Canvas? _zoneWidgetCanvas;

    /// <summary>The zone-widget layer status line, for the Gallery's manual test.</summary>
    internal string CommentZonesDiagnostics => _commentZoneHost?.Diagnostics ?? "no zone-widget layer";

    /// <summary>
    /// Whether hovering the line numbers shows a "+" button that raises <see cref="AddCommentRequested"/> for that line. Defaults to
    /// <see langword="false"/>. Needs the template part <c>PART_ZoneWidgetCanvas</c>.
    /// </summary>
    public bool CommentGutterButtonEnabled
    {
        get => (bool)GetValue(CommentGutterButtonEnabledProperty);
        set => SetValue(CommentGutterButtonEnabledProperty, value);
    }

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

    /// <summary>Adds a comment thread anchored to a line. Use the overload with two lines for a range.</summary>
    /// <param name="lineNumber">The 1-based line.</param>
    /// <param name="title">An optional title.</param>
    /// <param name="startComposing">Opens the comment box at once.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is outside the document.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Phase 12: the single-line overload stays source compatible.")]
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

    /// <summary>Adds a comment thread anchored to a range of lines; its box sits under the last line.</summary>
    /// <param name="startLineNumber">The 1-based first line.</param>
    /// <param name="endLineNumber">The 1-based last line, not before <paramref name="startLineNumber"/>.</param>
    /// <param name="title">An optional title.</param>
    /// <param name="startComposing">Opens the comment box at once.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startLineNumber"/> is outside the document, or <paramref name="endLineNumber"/> is outside it or before the start.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Phase 12: the single-line overload stays source compatible.")]
    public CommentThread AddCommentThread(int startLineNumber, int endLineNumber, string? title = null, bool startComposing = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(startLineNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startLineNumber, Model.LineCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(endLineNumber, startLineNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endLineNumber, Model.LineCount);
        CommentThread thread = _commentThreads.Add(startLineNumber, endLineNumber, title, startComposing);
        if (startComposing)
        {
            _commentZoneHost?.FocusComposer(thread);
        }

        return thread;
    }

    /// <summary>
    /// Adds a comment thread anchored to the exact text of a range; a range over whole lines, or an empty range, gives a thread on whole lines.
    /// </summary>
    /// <param name="range">The text to comment on; it is clamped to the document.</param>
    /// <param name="title">An optional title.</param>
    /// <param name="startComposing">Opens the comment box at once.</param>
    /// <exception cref="ArgumentOutOfRangeException">The lines of <paramref name="range"/> are outside the document.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Phase 12: the single-line overload stays source compatible.")]
    public CommentThread AddCommentThread(Range range, string? title = null, bool startComposing = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(range.StartLineNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(range.StartLineNumber, Model.LineCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(range.EndLineNumber, Model.LineCount);
        CommentThread thread = _commentThreads.Add(CommentRange.FromRange(Model, range), title, startComposing);
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
        _commentGutter?.Dispose();
        _commentGutter = null;
        if (_zoneWidgetCanvas is not null)
        {
            _zoneWidgetCanvas.SizeChanged -= OnZoneWidgetCanvasSizeChanged;
        }

        _zoneWidgetCanvas = canvas;
        if (canvas is null)
        {
            _hostZoneLayer.Attach(null);
            return;
        }

        canvas.SizeChanged += OnZoneWidgetCanvasSizeChanged;
        var layer = new OverlayHost(canvas);
        _hostZoneLayer.Attach(layer);
        _commentZoneHost = new CommentZoneHost(this, layer, _commentThreads);
        _commentZoneHost.ApplyTheme(EffectiveTheme, _systemHighContrastTheme is not null);
        _commentGutter = new CommentGutterController(this, layer, _commentThreads, CreateMouseTarget) { Enabled = CommentGutterButtonEnabled };
        _commentGutter.ApplyTheme(EffectiveTheme);
    }

    private static void OnCommentGutterButtonEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (((LoomEditor)sender)._commentGutter is { } gutter)
        {
            gutter.Enabled = (bool)e.NewValue;
        }
    }

    // A Canvas does not clip its children: without this a box scrolled half out would draw outside the editor.
    private void OnZoneWidgetCanvasSizeChanged(object sender, SizeChangedEventArgs e) =>
        ((Canvas)sender).Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };

    internal void Announce(string message) => _context.Announcer.Announce(message);

    private void InitializeCommentThreads()
    {
        _commentThreads = new CommentThreadsController(_context);
        _context.CommentThreads = _commentThreads;
        _commentThreads.CommentSubmitted += (_, e) => CommentSubmitted?.Invoke(this, new CommentSubmittedEventArgs(e.Thread, e.Text));
        _commentThreads.CommentCancelled += (_, t) => CommentCancelled?.Invoke(this, new CommentThreadEventArgs(t));
        _commentThreads.AddCommentRequested += (_, e) => AddCommentRequested?.Invoke(this, e.TextRange is { } text ? new AddCommentRequestedEventArgs(text) : new AddCommentRequestedEventArgs(e.StartLineNumber, e.LineNumber));
        _commentThreads.SuggestionApplied += (_, e) => SuggestionApplied?.Invoke(this, new SuggestionAppliedEventArgs(e.Thread, e.Comment, e.StartLineNumber, e.LineNumber));
    }
}

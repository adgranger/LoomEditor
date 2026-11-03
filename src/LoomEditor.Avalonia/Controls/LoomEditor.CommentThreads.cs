using Avalonia;
using LoomEditor.Avalonia.Controls.Comments;
using LoomEditor.Avalonia.Controls.Overlay;
using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;

namespace LoomEditor.Avalonia.Controls;

/// <remarks>Threading: UI thread only (not checked).</remarks>
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="CommentGutterButtonEnabled"/> styled property.</summary>
    public static readonly StyledProperty<bool> CommentGutterButtonEnabledProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(CommentGutterButtonEnabled), false);

    private CommentThreadsController _commentThreads = null!;
    private CommentZoneHost? _commentZoneHost;
    private CommentGutterController? _commentGutter;

    // The box of a thread over its zone, for the tests.
    internal CommentThreadWidget? CommentWidgetOf(CommentThread thread) => _commentZoneHost?.WidgetOf(thread);

    // The gutter "+", for the tests.
    internal CommentGutterController? CommentGutter => _commentGutter;

    /// <summary>
    /// Whether hovering the line numbers shows a "+" button that raises <see cref="AddCommentRequested"/> for that line. Defaults to
    /// <see langword="false"/>. Needs the template part <c>PART_ZoneWidgetCanvas</c>.
    /// </summary>
    public bool CommentGutterButtonEnabled
    {
        get => GetValue(CommentGutterButtonEnabledProperty);
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

    // The optional PART_ZoneWidgetCanvas also hosts the gutter "+" and a box over each comment thread's zone; without the part there is no gutter button.
    private void InitializeCommentGutter(OverlayHost? layer)
    {
        _commentGutter?.Dispose();
        _commentGutter = null;
        _commentZoneHost?.Dispose();
        _commentZoneHost = null;
        if (layer is null)
        {
            return;
        }

        _commentGutter = new CommentGutterController(this, layer, _commentThreads, CreateMouseTarget) { Enabled = CommentGutterButtonEnabled };
        _commentGutter.ApplyTheme(EffectiveTheme);
        _commentZoneHost = new CommentZoneHost(this, layer, _commentThreads);
        _commentZoneHost.ApplyTheme(EffectiveTheme, _systemHighContrastTheme is not null);
    }

    private static void InitializeCommentThreadsProperties() =>
        CommentGutterButtonEnabledProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) =>
        {
            if (editor._commentGutter is { } gutter)
            {
                gutter.Enabled = (bool)e.NewValue!;
            }
        });

    internal void InvalidateComments() => Invalidate();

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

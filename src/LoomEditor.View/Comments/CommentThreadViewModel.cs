using LoomEditor.Core.Comments;

namespace LoomEditor.View.Comments;

/// <summary>The state of one comment box: draft, preview, collapse and the submit / cancel / Escape rules.</summary>
/// <remarks>Threading: UI thread only. Events are raised synchronously.</remarks>
internal sealed class CommentThreadViewModel : IDisposable
{
    private readonly Func<string> _getLineText;
    private readonly Func<string>? _getCommentedText;
    private bool _expandedWhileResolved;
    private bool _disposed;

    public CommentThreadViewModel(CommentThread thread, Func<string> getLineText, Func<string>? getCommentedText = null)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(getLineText);
        Thread = thread;
        _getLineText = getLineText;
        _getCommentedText = getCommentedText;
        thread.Changed += OnThreadChanged;
    }

    /// <summary>Raised when any displayed state changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised with the trimmed draft when the user submits.</summary>
    public event EventHandler<string>? SubmitRequested;

    /// <summary>Raised when the user cancels.</summary>
    public event EventHandler<CancelOutcome>? CancelRequested;

    public CommentThread Thread { get; }

    public string Title => Thread.Title ?? CommentStrings.ThreadTitle(Thread.StartLineNumber, Thread.LineNumber);

    public bool IsComposing { get; private set; }

    /// <summary>The draft, with <c>\n</c> line breaks; never null.</summary>
    public string Draft { get; private set; } = "";

    public bool IsPreview { get; private set; }

    public bool CanSubmit => IsComposing && !string.IsNullOrWhiteSpace(Draft);

    public bool IsCollapsed => Thread.State == CommentThreadState.Resolved && !_expandedWhileResolved;

    public bool ReplyVisible => !IsComposing && Thread.State == CommentThreadState.Open;

    /// <summary>The current text of the thread's lines (joined with <c>\n</c>).</summary>
    public string SuggestionSeed => _getLineText();

    /// <summary>The current text of an exact-text thread (lines joined with <c>\n</c>); <see langword="null"/> for a thread on whole lines.</summary>
    public string? CommentedText => Thread.IsWholeLines || _getCommentedText is null ? null : _getCommentedText();

    public void StartComposing()
    {
        _expandedWhileResolved = true;
        IsComposing = true;
        Raise();
    }

    public void SetDraft(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (normalized != Draft)
        {
            Draft = normalized;
            Raise();
        }
    }

    public void TogglePreview()
    {
        if (IsComposing)
        {
            IsPreview = !IsPreview;
            Raise();
        }
    }

    /// <summary>Raises <see cref="SubmitRequested"/> and returns the trimmed draft; null when it cannot submit. The draft is kept.</summary>
    public string? Submit()
    {
        if (!CanSubmit)
        {
            return null;
        }

        string text = Draft.TrimEnd();
        SubmitRequested?.Invoke(this, text);
        return text;
    }

    public CancelOutcome Cancel()
    {
        Draft = "";
        IsComposing = false;
        IsPreview = false;
        CancelOutcome outcome = Thread.Comments.Count == 0 ? CancelOutcome.RemoveThread : CancelOutcome.Closed;
        Raise();
        CancelRequested?.Invoke(this, outcome);
        return outcome;
    }

    /// <summary>Escape cancels an empty composer; a non-blank draft is never lost to a stray key.</summary>
    public bool TryEscape()
    {
        if (!IsComposing || !string.IsNullOrWhiteSpace(Draft))
        {
            return false;
        }

        Cancel();
        return true;
    }

    public void ToggleCollapsed()
    {
        if (Thread.State == CommentThreadState.Resolved)
        {
            _expandedWhileResolved = !_expandedWhileResolved;
            Raise();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Thread.Changed -= OnThreadChanged;
        }
    }

    private void OnThreadChanged(object? sender, CommentThreadChangedEventArgs e)
    {
        if (e.Kind == CommentThreadChangeKind.CommentAdded && IsComposing)
        {
            Draft = "";
            IsComposing = false;
            IsPreview = false;
        }
        else if (e.Kind == CommentThreadChangeKind.StateChanged)
        {
            _expandedWhileResolved = false;
        }

        Raise();
    }

    private void Raise() => Changed?.Invoke(this, EventArgs.Empty);
}

using LoomEditor.Core.Comments;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Features;

namespace LoomEditor.View.Comments;

/// <summary>
/// Owns the comment threads of one editor (the manager and one view model per thread), raises the events the control
/// exposes and applies a suggestion as one undo step.
/// </summary>
/// <remarks>
/// Not a port: Monaco has no comment widget (ADR 0014). Threading: UI thread only. Events are raised synchronously;
/// a handler may call <see cref="Remove"/> or <see cref="CommentThread.AddComment"/>.
/// </remarks>
internal sealed class CommentThreadsController : IDisposable
{
    private const string ApplySource = "loom.commentThreads.applySuggestion";

    private readonly EditorContext _context;
    private readonly Dictionary<CommentThread, CommentThreadViewModel> _viewModels = [];
    private bool _disposed;

    public CommentThreadsController(EditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
        Manager = new CommentThreadManager(context.Model, context.ViewModel.ViewZones, context.Id);
    }

    /// <summary>Raised after a thread was added.</summary>
    public event EventHandler<CommentThread>? ThreadAdded;

    /// <summary>Raised before a thread is removed (the host moves the focus away first).</summary>
    public event EventHandler<CommentThread>? ThreadRemoving;

    /// <summary>Raised with the trimmed text when the user submits a comment.</summary>
    public event EventHandler<(CommentThread Thread, string Text)>? CommentSubmitted;

    /// <summary>Raised when the user cancels a comment box.</summary>
    public event EventHandler<CommentThread>? CommentCancelled;

    /// <summary>Raised when the add-comment command asks for a thread on a line.</summary>
    public event EventHandler<int>? AddCommentRequested;

    /// <summary>Raised after a suggestion replaced a line.</summary>
    public event EventHandler<(CommentThread Thread, Comment Comment, int LineNumber)>? SuggestionApplied;

    public CommentThreadManager Manager { get; }

    public IReadOnlyList<CommentThread> Threads => Manager.Threads;

    /// <summary>Creates a thread on a line, optionally with its composer open.</summary>
    public CommentThread Add(int lineNumber, string? title, bool startComposing)
    {
        CommentThread thread = Manager.Add(lineNumber, title);
        // Subscribed before the view model: it leaves composing mode on the same event, so read the flag first.
        thread.Changed += (_, e) =>
        {
            if (e.Kind == CommentThreadChangeKind.CommentAdded && _viewModels.TryGetValue(thread, out CommentThreadViewModel? vm) && vm.IsComposing)
            {
                _context.Announcer.Announce(CommentStrings.CommentPosted);
            }
        };
        var viewModel = new CommentThreadViewModel(thread, () => _context.Model.GetLineContent(thread.LineNumber));
        _viewModels[thread] = viewModel;
        viewModel.SubmitRequested += (_, text) => CommentSubmitted?.Invoke(this, (thread, text));
        viewModel.CancelRequested += (_, outcome) =>
        {
            CommentCancelled?.Invoke(this, thread);
            if (outcome == CancelOutcome.RemoveThread)
            {
                Remove(thread);
            }
        };
        if (startComposing)
        {
            viewModel.StartComposing();
            _context.Announcer.Announce(CommentStrings.ThreadAdded(lineNumber));
        }

        ThreadAdded?.Invoke(this, thread);
        return thread;
    }

    /// <summary>Removes a thread; <see langword="false"/> when unknown.</summary>
    public bool Remove(CommentThread thread)
    {
        if (!_viewModels.Remove(thread, out CommentThreadViewModel? viewModel))
        {
            return false;
        }

        ThreadRemoving?.Invoke(this, thread);
        viewModel.Dispose();
        return Manager.Remove(thread);
    }

    /// <summary>The view model of a thread.</summary>
    /// <exception cref="ArgumentException">The thread is unknown.</exception>
    public CommentThreadViewModel GetViewModel(CommentThread thread) =>
        _viewModels.TryGetValue(thread, out CommentThreadViewModel? viewModel)
            ? viewModel
            : throw new ArgumentException("Unknown thread.", nameof(thread));

    /// <summary>The current content of the thread's line.</summary>
    public string GetLineText(CommentThread thread) => _context.Model.GetLineContent(thread.LineNumber);

    /// <summary>Asks the host to add a thread on a line; nothing is added here.</summary>
    public void RequestAdd(int lineNumber) => AddCommentRequested?.Invoke(this, lineNumber);

    /// <summary>Replaces the thread's line with a suggestion of a comment, as one undo step.</summary>
    public ApplySuggestionResult ApplySuggestion(CommentThread thread, string commentId, int suggestionIndex, string? renderedLineText)
    {
        Comment? comment = thread.Comments.FirstOrDefault(c => c.Id == commentId);
        if (!_viewModels.ContainsKey(thread) || comment is null)
        {
            return ApplySuggestionResult.NotFound;
        }

        IReadOnlyList<SuggestionBlock> blocks = SuggestionExtractor.Extract(comment.Body);
        if (suggestionIndex < 0 || suggestionIndex >= blocks.Count)
        {
            return ApplySuggestionResult.NotFound;
        }

        TextModel model = _context.Model;
        int line = thread.LineNumber;
        SuggestionBlock block = blocks[suggestionIndex];
        if (_context.IsReadOnly)
        {
            return ApplySuggestionResult.ReadOnly;
        }

        if (renderedLineText != null && SuggestionEdit.IsStale(model, line, renderedLineText))
        {
            return ApplySuggestionResult.Stale;
        }

        if (SuggestionEdit.IsNoOp(model, line, block) || SuggestionEdit.Create(model, line, block) is not { } edit)
        {
            return ApplySuggestionResult.NoOp;
        }

        int caretLine = Math.Min(line, model.LineCount);
        model.PushStackElement();
        _context.ViewModel.ExecuteEdits(ApplySource, [new IdentifiedEditOperation(edit.Range, edit.Text)], _ => [new Selection(caretLine, 1, caretLine, 1)]);
        model.PushStackElement();
        SuggestionApplied?.Invoke(this, (thread, comment, line));
        _context.Announcer.Announce(CommentStrings.SuggestionApplied);
        return ApplySuggestionResult.Applied;
    }

    /// <summary>Disposes the view models and the manager.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (CommentThreadViewModel viewModel in _viewModels.Values)
        {
            viewModel.Dispose();
        }

        _viewModels.Clear();
        Manager.Dispose();
    }
}

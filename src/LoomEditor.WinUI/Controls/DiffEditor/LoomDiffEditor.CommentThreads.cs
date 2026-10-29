using LoomEditor.Core.Comments;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.WinUI.Controls;

// Comment threads on either side. Each side's editor owns its threads; the diff editor forwards their events (per editor, so threads added
// through Original / Modified directly are included). A comment zone is a whitespace the diff view zones do not own, so the other side is padded.
public sealed partial class LoomDiffEditor
{
    /// <summary>Raised when the user submits a comment on either side; <see cref="GetSide"/> tells which.</summary>
    public event EventHandler<CommentSubmittedEventArgs>? CommentSubmitted;

    /// <summary>Raised when the user cancels a comment box on either side.</summary>
    public event EventHandler<CommentThreadEventArgs>? CommentCancelled;

    /// <summary>Raised when the gutter button or the add-comment command of either side asks for a thread; nothing is added by the editor.</summary>
    public event EventHandler<DiffAddCommentRequestedEventArgs>? AddCommentRequested;

    /// <summary>Raised after a suggestion replaced a line (always on the modified side unless <c>OriginalEditable</c> is on).</summary>
    public event EventHandler<SuggestionAppliedEventArgs>? SuggestionApplied;

    /// <summary>The comment threads: the original side's, then the modified side's.</summary>
    public IReadOnlyList<CommentThread> CommentThreads => (CommentThread[])[.. Original.CommentThreads, .. Modified.CommentThreads];

    /// <summary>
    /// Adds a comment thread under a line of one side. With <paramref name="startComposing"/> and hidden unchanged regions, the line is
    /// revealed first.
    /// </summary>
    /// <param name="side">The side of the line.</param>
    /// <param name="lineNumber">The 1-based line in that side's text.</param>
    /// <param name="title">An optional title.</param>
    /// <param name="startComposing">Opens the comment box at once.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is outside that side's text.</exception>
    public CommentThread AddCommentThread(DiffSide side, int lineNumber, string? title = null, bool startComposing = false)
    {
        LoomEditor editor = EditorOf(side);
        ArgumentOutOfRangeException.ThrowIfLessThan(lineNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lineNumber, editor.Model.LineCount);
        if (startComposing && HideUnchangedRegions && _viewModel is { } viewModel)
        {
            if (side == DiffSide.Original)
            {
                viewModel.EnsureOriginalLineIsVisible(lineNumber, RevealPreference.FromCloserSide);
            }
            else
            {
                viewModel.EnsureModifiedLineIsVisible(lineNumber, RevealPreference.FromCloserSide);
            }
        }

        return editor.AddCommentThread(lineNumber, title, startComposing);
    }

    /// <summary>Removes a thread of either side; <see langword="false"/> when it belongs to neither.</summary>
    public bool RemoveCommentThread(CommentThread thread)
    {
        ArgumentNullException.ThrowIfNull(thread);
        return Original.RemoveCommentThread(thread) || Modified.RemoveCommentThread(thread);
    }

    /// <summary>The side a thread belongs to.</summary>
    /// <exception cref="ArgumentException">The thread belongs to neither side.</exception>
    public DiffSide GetSide(CommentThread thread)
    {
        ArgumentNullException.ThrowIfNull(thread);
        return Original.CommentThreads.Contains(thread) ? DiffSide.Original
            : Modified.CommentThreads.Contains(thread) ? DiffSide.Modified
            : throw new ArgumentException("The thread belongs to neither side of this diff editor.", nameof(thread));
    }

    private LoomEditor EditorOf(DiffSide side) => side switch
    {
        DiffSide.Original => Original,
        DiffSide.Modified => Modified,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private void AttachCommentEvents()
    {
        foreach (LoomEditor editor in (LoomEditor[])[_original!, _modified!])
        {
            editor.CommentSubmitted += OnSideCommentSubmitted;
            editor.CommentCancelled += OnSideCommentCancelled;
            editor.AddCommentRequested += OnSideAddCommentRequested;
            editor.SuggestionApplied += OnSideSuggestionApplied;
        }
    }

    private void DetachCommentEvents()
    {
        foreach (LoomEditor? editor in (LoomEditor?[])[_original, _modified])
        {
            if (editor is not null)
            {
                editor.CommentSubmitted -= OnSideCommentSubmitted;
                editor.CommentCancelled -= OnSideCommentCancelled;
                editor.AddCommentRequested -= OnSideAddCommentRequested;
                editor.SuggestionApplied -= OnSideSuggestionApplied;
            }
        }
    }

    private void OnSideCommentSubmitted(object? sender, CommentSubmittedEventArgs e) => CommentSubmitted?.Invoke(this, e);

    private void OnSideCommentCancelled(object? sender, CommentThreadEventArgs e) => CommentCancelled?.Invoke(this, e);

    private void OnSideSuggestionApplied(object? sender, SuggestionAppliedEventArgs e) => SuggestionApplied?.Invoke(this, e);

    private void OnSideAddCommentRequested(object? sender, AddCommentRequestedEventArgs e) =>
        AddCommentRequested?.Invoke(this, new DiffAddCommentRequestedEventArgs(ReferenceEquals(sender, _original) ? DiffSide.Original : DiffSide.Modified, e.LineNumber));
}

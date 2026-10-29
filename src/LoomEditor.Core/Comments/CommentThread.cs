using LoomEditor.Core.Diagnostics;
using System.Collections.ObjectModel;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Comments;

/// <summary>A discussion anchored on a line, a range of lines or an exact text range: an ordered list of comments, a title and a state.</summary>
/// <remarks>Threading: UI thread only. <see cref="Changed"/> is raised synchronously on the caller's thread, after the
/// state changed; exceptions thrown by handlers are not caught.</remarks>
public sealed class CommentThread
{
    private readonly List<Comment> _comments = [];
    private readonly ReadOnlyCollection<Comment> _view;
    private int _startLineNumber;
    private int _lineNumber;
    private int _startColumn;
    private int _endColumn;
    private string? _title;
    private CommentThreadState _state;

    internal CommentThread(string id, int lineNumber, string? title = null)
        : this(id, lineNumber, lineNumber, title)
    {
    }

    internal CommentThread(string id, int startLineNumber, int lineNumber, string? title = null)
        : this(id, startLineNumber, 1, lineNumber, 1, true, title)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(lineNumber, startLineNumber);
    }

    internal CommentThread(string id, Range textRange, string? title = null)
        : this(id, Validated(textRange).StartLineNumber, textRange.StartColumn, textRange.EndLineNumber, textRange.EndColumn, false, title)
    {
    }

    private CommentThread(string id, int startLineNumber, int startColumn, int lineNumber, int endColumn, bool isWholeLines, string? title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentOutOfRangeException.ThrowIfLessThan(startLineNumber, 1);
        Id = id;
        _startLineNumber = startLineNumber;
        _lineNumber = lineNumber;
        _startColumn = startColumn;
        _endColumn = endColumn;
        IsWholeLines = isWholeLines;
        _title = title;
        _view = new ReadOnlyCollection<Comment>(_comments);
    }

    private static Range Validated(Range textRange)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(textRange.StartLineNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(textRange.StartColumn, 1);
        return textRange;
    }

    /// <summary>Raised after the thread changed.</summary>
    public event EventHandler<CommentThreadChangedEventArgs>? Changed;

    /// <summary>The identifier of the thread.</summary>
    public string Id { get; }

    /// <summary>The first 1-based line the thread covers; equal to <see cref="LineNumber"/> for a single-line thread.
    /// Maintained by the editor.</summary>
    public int StartLineNumber => _startLineNumber;

    /// <summary>The 1-based line the thread's box is under: the last line of its range (GitHub's <c>line</c>).
    /// Maintained by the editor.</summary>
    public int LineNumber => _lineNumber;

    /// <summary>The 1-based column where the commented text starts; 1 for a thread on whole lines. Maintained by the
    /// editor; a change of columns alone raises no <see cref="Changed"/> event.</summary>
    public int StartColumn => _startColumn;

    /// <summary>The 1-based column just after the commented text, on <see cref="LineNumber"/> (exclusive, as in
    /// <see cref="Range"/>); for a thread on whole lines, the end of its last line. Maintained by the editor; a change of
    /// columns alone raises no <see cref="Changed"/> event.</summary>
    public int EndColumn => _endColumn;

    /// <summary>Whether the thread covers whole lines (<see langword="true"/>) or an exact text range; fixed at creation.</summary>
    public bool IsWholeLines { get; }

    internal void SetLines(int startLineNumber, int lineNumber) =>
        SetRange(startLineNumber, _startColumn, lineNumber, _endColumn);

    internal void SetRange(int startLineNumber, int startColumn, int lineNumber, int endColumn)
    {
        Guard.Assert(1 <= startLineNumber && startLineNumber <= lineNumber && startColumn >= 1 && endColumn >= 1
            && (startLineNumber < lineNumber || startColumn <= endColumn));
        bool linesChanged = startLineNumber != _startLineNumber || lineNumber != _lineNumber;
        _startLineNumber = startLineNumber;
        _lineNumber = lineNumber;
        _startColumn = startColumn;
        _endColumn = endColumn;
        if (linesChanged)
        {
            Raise(CommentThreadChangeKind.LineChanged, null);
        }
    }

    /// <summary>An optional title.</summary>
    public string? Title
    {
        get => _title;
        set
        {
            if (value != _title)
            {
                _title = value;
                Raise(CommentThreadChangeKind.TitleChanged, null);
            }
        }
    }

    /// <summary>Whether the thread is open or resolved.</summary>
    public CommentThreadState State
    {
        get => _state;
        set
        {
            if (value != _state)
            {
                _state = value;
                Raise(CommentThreadChangeKind.StateChanged, null);
            }
        }
    }

    /// <summary>The comments, oldest first; a stable read-only view, never <see langword="null"/>.</summary>
    public IReadOnlyList<Comment> Comments => _view;

    /// <summary>Appends a comment.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="comment"/> is null.</exception>
    /// <exception cref="ArgumentException">A comment with the same <see cref="Comment.Id"/> exists.</exception>
    public void AddComment(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        if (IndexOf(comment.Id) >= 0)
        {
            throw new ArgumentException($"A comment with id '{comment.Id}' already exists.", nameof(comment));
        }

        _comments.Add(comment);
        Raise(CommentThreadChangeKind.CommentAdded, comment);
    }

    /// <summary>Removes the comment with the given id; <see langword="false"/> when unknown.</summary>
    public bool RemoveComment(string id)
    {
        int index = IndexOf(id);
        if (index < 0)
        {
            return false;
        }

        Comment removed = _comments[index];
        _comments.RemoveAt(index);
        Raise(CommentThreadChangeKind.CommentRemoved, removed);
        return true;
    }

    /// <summary>Replaces, in place, the comment that has the same id; <see langword="false"/> when unknown.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="comment"/> is null.</exception>
    public bool ReplaceComment(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        int index = IndexOf(comment.Id);
        if (index < 0)
        {
            return false;
        }

        _comments[index] = comment;
        Raise(CommentThreadChangeKind.CommentReplaced, comment);
        return true;
    }

    private int IndexOf(string id)
    {
        for (int i = 0; i < _comments.Count; i++)
        {
            if (_comments[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private void Raise(CommentThreadChangeKind kind, Comment? comment) =>
        Changed?.Invoke(this, new CommentThreadChangedEventArgs(kind, comment));
}

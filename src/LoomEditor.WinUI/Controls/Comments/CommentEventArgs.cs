using LoomEditor.Core.Comments;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.WinUI.Controls;

/// <summary>Data of an event that concerns one comment thread.</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed class CommentThreadEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    public CommentThreadEventArgs(CommentThread thread)
    {
        ArgumentNullException.ThrowIfNull(thread);
        Thread = thread;
    }

    /// <summary>The thread.</summary>
    public CommentThread Thread { get; }
}

/// <summary>Data of <c>CommentSubmitted</c>.</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed class CommentSubmittedEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    public CommentSubmittedEventArgs(CommentThread thread, string text)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(text);
        Thread = thread;
        Text = text;
    }

    /// <summary>The thread the comment was written in.</summary>
    public CommentThread Thread { get; }

    /// <summary>The trimmed text the user submitted; the host decides whether to add it with <see cref="CommentThread.AddComment"/>.</summary>
    public string Text { get; }
}

/// <summary>Data of <c>AddCommentRequested</c>.</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed class AddCommentRequestedEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    public AddCommentRequestedEventArgs(int lineNumber) : this(lineNumber, lineNumber)
    {
    }

    /// <summary>Creates the arguments for a range of lines.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startLineNumber"/> is greater than <paramref name="lineNumber"/>.</exception>
    public AddCommentRequestedEventArgs(int startLineNumber, int lineNumber)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startLineNumber, lineNumber);
        StartLineNumber = startLineNumber;
        LineNumber = lineNumber;
    }

    /// <summary>Creates the arguments for the exact text of a range.</summary>
    /// <param name="textRange">The text the user asked to comment on.</param>
    public AddCommentRequestedEventArgs(Range textRange)
    {
        StartLineNumber = textRange.StartLineNumber;
        LineNumber = textRange.EndLineNumber;
        TextRange = textRange;
    }

    /// <summary>The first line of the requested range; equals <see cref="LineNumber"/> for one line.</summary>
    public int StartLineNumber { get; }

    /// <summary>The last line of the requested range (1-based); the line the user wants to comment for one line.</summary>
    public int LineNumber { get; }

    /// <summary>
    /// The exact text the user asked to comment on; <see langword="null"/> when the request is for whole lines
    /// (<see cref="StartLineNumber"/> to <see cref="LineNumber"/>). Pass it to <c>AddCommentThread(Range, …)</c>.
    /// </summary>
    public Range? TextRange { get; }
}

/// <summary>Data of <c>SuggestionApplied</c>.</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed class SuggestionAppliedEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    public SuggestionAppliedEventArgs(CommentThread thread, Comment comment, int lineNumber)
        : this(thread, comment, lineNumber, lineNumber)
    {
    }

    /// <summary>Creates the arguments for a range of lines.</summary>
    public SuggestionAppliedEventArgs(CommentThread thread, Comment comment, int startLineNumber, int lineNumber)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(comment);
        Thread = thread;
        Comment = comment;
        StartLineNumber = startLineNumber;
        LineNumber = lineNumber;
    }

    /// <summary>The thread.</summary>
    public CommentThread Thread { get; }

    /// <summary>The comment that held the suggestion.</summary>
    public Comment Comment { get; }

    /// <summary>The first line that was replaced (before the edit); equals <see cref="LineNumber"/> for one line.</summary>
    public int StartLineNumber { get; }

    /// <summary>The last line that was replaced (before the edit).</summary>
    public int LineNumber { get; }
}

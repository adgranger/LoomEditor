using LoomEditor.Core.Comments;

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
    public AddCommentRequestedEventArgs(int lineNumber) => LineNumber = lineNumber;

    /// <summary>The line the user wants to comment (1-based).</summary>
    public int LineNumber { get; }
}

/// <summary>Data of <c>SuggestionApplied</c>.</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed class SuggestionAppliedEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    public SuggestionAppliedEventArgs(CommentThread thread, Comment comment, int lineNumber)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(comment);
        Thread = thread;
        Comment = comment;
        LineNumber = lineNumber;
    }

    /// <summary>The thread.</summary>
    public CommentThread Thread { get; }

    /// <summary>The comment that held the suggestion.</summary>
    public Comment Comment { get; }

    /// <summary>The line that was replaced.</summary>
    public int LineNumber { get; }
}

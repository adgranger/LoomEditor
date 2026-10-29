namespace LoomEditor.Core.Comments;

/// <summary>What changed in a <see cref="CommentThread"/>.</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
public enum CommentThreadChangeKind
{
    /// <summary>A comment was added.</summary>
    CommentAdded,
    /// <summary>A comment was removed.</summary>
    CommentRemoved,
    /// <summary>A comment was replaced.</summary>
    CommentReplaced,
    /// <summary>The state changed.</summary>
    StateChanged,
    /// <summary>The title changed.</summary>
    TitleChanged,
    /// <summary>The line number changed.</summary>
    LineChanged,
}

/// <summary>Arguments of <see cref="CommentThread.Changed"/>.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
public sealed class CommentThreadChangedEventArgs(CommentThreadChangeKind kind, Comment? comment) : EventArgs
{
    /// <summary>What changed.</summary>
    public CommentThreadChangeKind Kind { get; } = kind;

    /// <summary>The added, removed or new comment; <see langword="null"/> for the other kinds.</summary>
    public Comment? Comment { get; } = comment;
}

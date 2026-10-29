namespace LoomEditor.View.Comments;

/// <summary>The outcome of <see cref="CommentThreadsController.ApplySuggestion"/>.</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
internal enum ApplySuggestionResult
{
    /// <summary>The line was replaced (one undo step).</summary>
    Applied,

    /// <summary>The editor is read-only.</summary>
    ReadOnly,

    /// <summary>The line changed since the suggestion was rendered.</summary>
    Stale,

    /// <summary>The suggestion equals the current text.</summary>
    NoOp,

    /// <summary>The thread, comment or suggestion does not exist.</summary>
    NotFound,
}

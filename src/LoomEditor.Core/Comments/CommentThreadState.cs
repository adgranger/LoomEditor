namespace LoomEditor.Core.Comments;

/// <summary>Whether a thread is still being discussed.</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
public enum CommentThreadState
{
    /// <summary>The thread is open.</summary>
    Open,
    /// <summary>The thread is resolved.</summary>
    Resolved,
}

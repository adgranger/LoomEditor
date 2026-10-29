namespace LoomEditor.View.Comments;

/// <summary>What the host must do after the user cancelled a comment box.</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
internal enum CancelOutcome
{
    /// <summary>Close the composer; the thread keeps its comments.</summary>
    Closed,

    /// <summary>The thread has no comment: remove it.</summary>
    RemoveThread,
}

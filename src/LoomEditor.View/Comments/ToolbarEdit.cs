namespace LoomEditor.View.Comments;

/// <summary>The result of a toolbar action: the whole new text and the new selection.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Text">The new text.</param>
/// <param name="Selection">The new selection, in <paramref name="Text"/>.</param>
internal readonly record struct ToolbarEdit(string Text, ToolbarSelection Selection);

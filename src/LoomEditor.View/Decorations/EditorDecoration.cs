namespace LoomEditor.View.Decorations;

/// <summary>A decoration to put on a range of the text (Monaco: <c>IModelDeltaDecoration</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
/// <param name="Range">The decorated range; a range outside the model is clamped to it.</param>
/// <param name="Options">How the range is drawn; reuse one instance for many decorations.</param>
public readonly record struct EditorDecoration(Range Range, EditorDecorationOptions Options);

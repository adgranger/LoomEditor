namespace LoomEditor.View.DiffEditor;

/// <summary>A character-level change inside a <see cref="DiffChangeBlock"/>.</summary>
/// <param name="OriginalRange">The replaced text, in the original text.</param>
/// <param name="ModifiedRange">The new text, in the modified text.</param>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
public readonly record struct DiffInnerChange(Range OriginalRange, Range ModifiedRange);

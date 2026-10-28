using LoomEditor.Core.Decorations;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Features;

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>An <see cref="IDiffDecorationTarget"/> over the decorations collection of an editor.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class EditorDecorationTarget(EditorDecorationsCollection collection) : IDiffDecorationTarget
{
    /// <inheritdoc />
    public void SetDecorations(IReadOnlyList<ModelDeltaDecoration> decorations) => collection.Set(decorations);

    /// <summary>Removes the decorations.</summary>
    public void Clear() => collection.Clear();
}

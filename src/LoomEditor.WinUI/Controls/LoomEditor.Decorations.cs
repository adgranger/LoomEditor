using LoomEditor.View.Decorations;

namespace LoomEditor.WinUI.Controls;

/// <remarks>Threading: UI thread only.</remarks>
public sealed partial class LoomEditor
{
    /// <summary>
    /// Creates a collection of decorations owned by the caller (Monaco: <c>createDecorationsCollection</c>). The collection lives with this editor's model and
    /// stays usable after <see cref="Text"/> or <see cref="ResetText"/>, but loading new text removes every decoration (Monaco: <c>setValue</c>), so the host sets them again.
    /// </summary>
    /// <param name="decorations">The initial decorations, or <see langword="null"/> for an empty collection.</param>
    /// <returns>The new collection.</returns>
    /// <exception cref="ArgumentException">A decoration has no options.</exception>
    /// <remarks>Threading: UI thread only.</remarks>
    public DecorationsCollection CreateDecorationsCollection(IReadOnlyList<EditorDecoration>? decorations = null)
    {
        var collection = new DecorationsCollection(_context.CreateDecorationsCollection());
        if (decorations is not null)
        {
            collection.Set(decorations);
        }

        return collection;
    }
}

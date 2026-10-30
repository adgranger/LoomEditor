using LoomEditor.Core.Decorations;
using LoomEditor.View.Features;

namespace LoomEditor.View.Decorations;

/// <summary>
/// A set of host decorations that the host sets, appends and clears as a whole (Monaco: <c>IEditorDecorationsCollection</c>;
/// named <c>DecorationsCollection</c> so it does not clash with the internal port).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Phase 13: the name is decided (Monaco: IEditorDecorationsCollection).")]
public sealed class DecorationsCollection
{
    private readonly EditorDecorationsCollection _inner;

    internal DecorationsCollection(EditorDecorationsCollection inner) => _inner = inner;

    /// <summary>The number of decorations in the collection (Monaco: <c>length</c>).</summary>
    public int Count => _inner.Count;

    /// <summary>Replaces every decoration of the collection (Monaco: <c>set</c>).</summary>
    /// <param name="decorations">The new decorations.</param>
    /// <exception cref="ArgumentNullException"><paramref name="decorations"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A decoration has no options.</exception>
    public void Set(IReadOnlyList<EditorDecoration> decorations) => _inner.Set(ToModel(decorations));

    /// <summary>Adds decorations to the collection (Monaco: <c>append</c>).</summary>
    /// <param name="decorations">The decorations to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="decorations"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A decoration has no options.</exception>
    public void Append(IReadOnlyList<EditorDecoration> decorations) => _inner.Append(ToModel(decorations));

    /// <summary>Removes every decoration of the collection (Monaco: <c>clear</c>).</summary>
    public void Clear() => _inner.Clear();

    /// <summary>The current range of the decoration at <paramref name="index"/>, or <see langword="null"/> for a bad index or a removed decoration (Monaco: <c>getRange</c>).</summary>
    public Range? GetRange(int index) => _inner.GetRange(index);

    /// <summary>The current ranges of the decorations, in the order they were added (Monaco: <c>getRanges</c>).</summary>
    public IReadOnlyList<Range> GetRanges() => _inner.GetRanges();

    private static ModelDeltaDecoration[] ToModel(IReadOnlyList<EditorDecoration> decorations)
    {
        ArgumentNullException.ThrowIfNull(decorations);
        var result = new ModelDeltaDecoration[decorations.Count];
        for (int i = 0; i < result.Length; i++)
        {
            EditorDecoration decoration = decorations[i];
            if (decoration.Options is null)
            {
                throw new ArgumentException($"The decoration at index {i} has no options.", nameof(decorations));
            }

            result[i] = new ModelDeltaDecoration(decoration.Range, decoration.Options.ToModelOptions());
        }

        return result;
    }
}

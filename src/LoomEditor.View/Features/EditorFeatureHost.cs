namespace LoomEditor.View.Features;

/// <summary>
/// Creates and attaches a list of features to one editor, and disposes them all together.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class EditorFeatureHost : IDisposable
{
    private readonly List<IEditorFeature> _features = [];

    /// <summary>Creates every feature from <paramref name="factories"/> and attaches it to <paramref name="context"/>.</summary>
    /// <exception cref="InvalidOperationException">Two features have the same <see cref="IEditorFeature.Id"/>.</exception>
    public EditorFeatureHost(IEditorContext context, IEnumerable<Func<IEditorFeature>> factories)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factories);

        foreach (Func<IEditorFeature> factory in factories)
        {
            IEditorFeature feature = factory();
            if (_features.Exists(existing => existing.Id == feature.Id))
            {
                feature.Dispose();
                Dispose();
                throw new InvalidOperationException($"The feature '{feature.Id}' is listed twice.");
            }

            _features.Add(feature);
            feature.Attach(context);
        }
    }

    /// <summary>The attached features, in attach order.</summary>
    public IReadOnlyList<IEditorFeature> Features => _features;

    /// <summary>Disposes every feature, in reverse attach order.</summary>
    public void Dispose()
    {
        for (int i = _features.Count - 1; i >= 0; i--)
        {
            _features[i].Dispose();
        }

        _features.Clear();
    }
}

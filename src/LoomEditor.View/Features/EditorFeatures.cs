using LoomEditor.View.Features.SelectAll;
using LoomEditor.View.Features.WordOperations;

namespace LoomEditor.View.Features;

/// <summary>
/// THE list of features enabled in an editor. To add a feature: create its folder under <c>Features/</c>,
/// then add one line here. See docs/how-to/add-a-feature.md.
/// </summary>
/// <remarks>
/// Features are created through factories (<c>() => new XxxFeature()</c>) so that every editor gets its own
/// instances. No reflection and no assembly scanning: what you read here is exactly what runs.
/// </remarks>
public static class EditorFeatures
{
    /// <summary>The features enabled by default, in the order they are attached.</summary>
    public static IReadOnlyList<Func<IEditorFeature>> Default { get; } =
    [
        () => new SelectAllFeature(),
        () => new WordOperationsFeature(),
    ];
}

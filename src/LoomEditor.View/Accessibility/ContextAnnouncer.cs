using LoomEditor.View.Features;

namespace LoomEditor.View.Accessibility;

/// <summary>
/// Forwards to the announcer of an <see cref="IEditorContext"/>, read at announcement time because the control assigns it
/// after the features were attached.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ContextAnnouncer(IEditorContext context) : IEditorAnnouncer
{
    /// <inheritdoc />
    public void Announce(string message) => context.GetAnnouncer().Announce(message);
}

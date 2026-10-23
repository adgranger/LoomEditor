namespace LoomEditor.View.Accessibility;

/// <summary>Announces a message to assistive technology (Monaco: <c>aria.alert</c> / <c>aria.status</c>, one channel here).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IEditorAnnouncer
{
    /// <summary>Announces <paramref name="message"/>; implementations with a side effect ignore null or empty messages.</summary>
    void Announce(string message);
}

/// <summary>The announcer that does nothing: the default when no control is attached.</summary>
/// <remarks>Threading: stateless; thread-safe.</remarks>
internal sealed class NullEditorAnnouncer : IEditorAnnouncer
{
    /// <summary>The shared instance.</summary>
    public static readonly NullEditorAnnouncer Instance = new();

    /// <inheritdoc />
    public void Announce(string message)
    {
    }
}

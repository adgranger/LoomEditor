namespace LoomEditor.View.Hover;

/// <summary>The hover options (Monaco <c>EditorOption.hover</c> defaults). Not ported: <c>above</c>, <c>showLongLineWarning</c>.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record EditorHoverOptions
{
    /// <summary>The Monaco defaults: enabled, 300 ms, sticky, 300 ms.</summary>
    public static readonly EditorHoverOptions Default = new();

    /// <summary>Whether hovers are shown.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>The time the mouse must rest before the hover shows.</summary>
    public TimeSpan Delay { get; init; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Whether the mouse can move onto the popup.</summary>
    public bool Sticky { get; init; } = true;

    /// <summary>The delay before a sticky popup hides.</summary>
    public TimeSpan HidingDelay { get; init; } = TimeSpan.FromMilliseconds(300);
}

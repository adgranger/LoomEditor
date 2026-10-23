namespace LoomEditor.View.Layout;

/// <summary>The screen reader mode of the editor (Monaco: <c>accessibilitySupport</c> and <c>env.accessibilitySupport</c>).</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed record EditorAccessibilityOptions
{
    /// <summary>The option value (Monaco: <c>accessibilitySupport</c>; <see cref="AccessibilitySupport.Unknown"/> is <c>'auto'</c>).</summary>
    public AccessibilitySupport Setting { get; init; } = AccessibilitySupport.Unknown;

    /// <summary>What the environment reports (Monaco: <c>env.accessibilitySupport</c>).</summary>
    public AccessibilitySupport Detected { get; init; } = AccessibilitySupport.Unknown;

    /// <summary>Monaco: <c>EditorAccessibilitySupport.compute</c>: an unknown setting takes the detected value.</summary>
    public AccessibilitySupport Effective => Setting == AccessibilitySupport.Unknown ? Detected : Setting;

    /// <summary>True when the editor runs in screen reader mode.</summary>
    public bool IsEnabled => Effective == AccessibilitySupport.Enabled;
}

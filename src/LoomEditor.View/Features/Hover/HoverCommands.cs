namespace LoomEditor.View.Features.Hover;

/// <summary>Ids of the commands exposed by <see cref="HoverFeature"/>. Same ids as Monaco.</summary>
internal static class HoverCommands
{
    /// <summary>Shows the hover at the cursor (Ctrl+K Ctrl+I).</summary>
    public const string ShowHover = "editor.action.showHover";

    /// <summary>Hides the hover (Escape while it is visible).</summary>
    public const string HideHover = "editor.action.hideHover";
}

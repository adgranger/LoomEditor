namespace LoomEditor.View.Features.DiffNavigation;

/// <summary>Ids of the commands exposed by <see cref="DiffNavigationFeature"/>.</summary>
internal static class DiffNavigationCommands
{
    /// <summary>Goes to the next change (VS Code workbench: <c>workbench.action.compareEditor.nextChange</c>).</summary>
    public const string NextChange = "workbench.action.compareEditor.nextChange";

    /// <summary>Goes to the previous change (VS Code workbench: <c>workbench.action.compareEditor.previousChange</c>).</summary>
    public const string PreviousChange = "workbench.action.compareEditor.previousChange";
}

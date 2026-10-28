namespace LoomEditor.View.Features.DiffNavigation;

/// <summary>Ids of the commands exposed by <see cref="DiffNavigationFeature"/>.</summary>
internal static class DiffNavigationCommands
{
    /// <summary>Goes to the next change (VS Code workbench: <c>workbench.action.compareEditor.nextChange</c>).</summary>
    public const string NextChange = "workbench.action.compareEditor.nextChange";

    /// <summary>Goes to the previous change (VS Code workbench: <c>workbench.action.compareEditor.previousChange</c>).</summary>
    public const string PreviousChange = "workbench.action.compareEditor.previousChange";

    /// <summary>Opens the accessible diff viewer or goes to its next group (Monaco: <c>editor.action.accessibleDiffViewer.next</c>, <c>F7</c>).</summary>
    public const string AccessibleViewerNext = "editor.action.accessibleDiffViewer.next";

    /// <summary>Opens the accessible diff viewer on its previous group (Monaco: <c>editor.action.accessibleDiffViewer.prev</c>, <c>Shift+F7</c>).</summary>
    public const string AccessibleViewerPrevious = "editor.action.accessibleDiffViewer.prev";
}

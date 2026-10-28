namespace LoomEditor.View.Features.SelectionAnchor;

/// <summary>Ids of the commands exposed by <see cref="SelectionAnchorFeature"/>. Same ids as in Monaco.</summary>
public static class SelectionAnchorCommands
{
    /// <summary>Puts the anchor at the caret (<c>Ctrl+K Ctrl+B</c>).</summary>
    public const string SetSelectionAnchor = "editor.action.setSelectionAnchor";

    /// <summary>Moves the caret to the anchor (no key).</summary>
    public const string GoToSelectionAnchor = "editor.action.goToSelectionAnchor";

    /// <summary>Selects from the anchor to the caret and removes the anchor (<c>Ctrl+K Ctrl+K</c>).</summary>
    public const string SelectFromAnchorToCursor = "editor.action.selectFromAnchorToCursor";

    /// <summary>Removes the anchor (<c>Escape</c>).</summary>
    public const string CancelSelectionAnchor = "editor.action.cancelSelectionAnchor";
}

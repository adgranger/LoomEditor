namespace LoomEditor.View.Features.DragAndDrop;

/// <summary>Ids used by <see cref="DragAndDropFeature"/>. The feature registers no command (the mouse drives it).</summary>
internal static class DragAndDropCommands
{
    /// <summary>The source given to the cursor edit and the reveal of a drop. Same id as Monaco's contribution.</summary>
    public const string Source = "editor.contrib.dragAndDrop";
}

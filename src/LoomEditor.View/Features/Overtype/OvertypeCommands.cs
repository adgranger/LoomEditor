namespace LoomEditor.View.Features.Overtype;

/// <summary>Ids of the commands exposed by <see cref="OvertypeFeature"/>. Bound to Insert by default.</summary>
public static class OvertypeCommands
{
    /// <summary>Flips <see cref="EditorContext.InputMode"/> between insert and overtype. No Monaco id: the workbench owns it.</summary>
    public const string ToggleOvertype = "editor.action.toggleOvertype";
}

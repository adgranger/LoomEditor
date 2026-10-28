namespace LoomEditor.View.Features.Overtype;

/// <summary>Ids of the commands exposed by <see cref="OvertypeFeature"/>. No default key (P10-12 adds Insert).</summary>
public static class OvertypeCommands
{
    /// <summary>Flips <see cref="EditorContext.InputMode"/> between insert and overtype. No Monaco id: the workbench owns it.</summary>
    public const string ToggleOvertype = "editor.action.toggleOvertype";
}

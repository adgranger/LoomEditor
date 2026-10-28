namespace LoomEditor.View.Features.SmartSelect;

/// <summary>Ids of the commands exposed by <see cref="SmartSelectFeature"/>. Same ids as in Monaco.</summary>
public static class SmartSelectCommands
{
    /// <summary>Grows each selection to the next enclosing range (<c>Shift+Alt+Right</c>).</summary>
    public const string Expand = "editor.action.smartSelect.expand";

    /// <summary>Goes back to the previous range of each selection (<c>Shift+Alt+Left</c>).</summary>
    public const string Shrink = "editor.action.smartSelect.shrink";
}

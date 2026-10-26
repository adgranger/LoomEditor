using LoomEditor.View.Features.GotoDefinition;

namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomEditor
{
    /// <summary>
    /// Opens a definition that is in another document (go to definition). Without it, such a definition is only announced.
    /// </summary>
    public ILocationOpener? LocationOpener
    {
        get => _context.LocationOpener;
        set => _context.LocationOpener = value;
    }
}

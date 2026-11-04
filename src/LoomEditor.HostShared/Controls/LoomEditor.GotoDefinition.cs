using LoomEditor.View.Features.GotoDefinition;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif

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

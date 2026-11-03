using Avalonia.Controls;
using LoomEditor.Avalonia.Controls.Overlay;

namespace LoomEditor.Avalonia.Controls;

// The optional PART_OverlayCanvas and PART_ZoneWidgetCanvas of the template; without a part its widgets have no UI.
/// <remarks>Threading: UI thread only.</remarks>
public sealed partial class LoomEditor
{
    private OverlayHost? _overlay;

    // The overlay for the widgets that float above the text, for the widget cards that follow.
    internal OverlayHost? Overlay => _overlay;

    private void AttachOverlays(INameScope nameScope)
    {
        _overlay = nameScope.Find<Canvas>(OverlayCanvasPartName) is { } overlay ? new OverlayHost(overlay) : null;
        _hostZoneLayer.Attach(nameScope.Find<Canvas>(ZoneWidgetCanvasPartName) is { } zones ? new OverlayHost(zones) : null);
    }
}

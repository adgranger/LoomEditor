using LoomEditor.Themes;
using LoomEditor.WinUI.Controls.Hover;
using LoomEditor.WinUI.Controls.Overlay;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls;

// Hover: the optional PART_OverlayCanvas in the template hosts the hover popup. Without the part the hover has no UI.
public sealed partial class LoomEditor
{
    private OverlayHost? _overlay;
    private HoverPresenter? _hoverPresenter;

    private void InitializeHover(Canvas? overlayCanvas)
    {
        _overlay = overlayCanvas is null ? null : new OverlayHost(overlayCanvas);
        if (_overlay is null)
        {
            _hoverPresenter = null;
            return;
        }

        _hoverPresenter = new HoverPresenter(_overlay, this);
        _hoverPresenter.ApplyTheme(EffectiveTheme);
        _context.HoverPresenter = _hoverPresenter;
    }

    private void ApplyHoverTheme(EditorTheme theme) => _hoverPresenter?.ApplyTheme(theme);
}

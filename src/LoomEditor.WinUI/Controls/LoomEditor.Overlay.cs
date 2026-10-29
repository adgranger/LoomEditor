using LoomEditor.Themes;
using LoomEditor.WinUI.Controls.Hover;
using LoomEditor.WinUI.Controls.Overlay;
using LoomEditor.WinUI.Controls.Suggest;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls;

// Overlay widgets: the optional PART_OverlayCanvas in the template hosts the hover popup and the completion list. Without the part they have no UI.
public sealed partial class LoomEditor
{
    private OverlayHost? _overlay;
    private HoverPresenter? _hoverPresenter;
    private SuggestPresenter? _suggestPresenter;

    private void InitializeOverlayWidgets(Canvas? overlayCanvas)
    {
        _overlay = overlayCanvas is null ? null : new OverlayHost(overlayCanvas);
        if (_overlay is null)
        {
            _hoverPresenter = null;
            _suggestPresenter = null;
            return;
        }

        _hoverPresenter = new HoverPresenter(_overlay, this);
        _hoverPresenter.ApplyTheme(EffectiveTheme);
        _context.HoverPresenter = _hoverPresenter;

        _suggestPresenter = new SuggestPresenter(_overlay, this);
        _suggestPresenter.ApplyTheme(EffectiveTheme);
        _context.SuggestPresenter = _suggestPresenter;
    }

    private void ApplyOverlayWidgetsTheme(EditorTheme theme)
    {
        _hoverPresenter?.ApplyTheme(theme);
        _suggestPresenter?.ApplyTheme(theme);
        _commentZoneHost?.ApplyTheme(theme);
    }
}

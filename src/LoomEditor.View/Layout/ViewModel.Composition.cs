using LoomEditor.View.Controller;

namespace LoomEditor.View.Layout;

// The IME underlines. Monaco keeps them as model decorations (`textFormatDecoration`); there is no decorations API in
// phase 3 (ADR 0006), so they are view model state drawn by the renderer.
internal sealed partial class ViewModel
{
    private IReadOnlyList<CompositionFormat> _compositionFormats = [];

    /// <summary>The underlines of the text being composed; empty outside a composition.</summary>
    public IReadOnlyList<CompositionFormat> CompositionFormats => _compositionFormats;

    /// <summary>Raised when <see cref="CompositionFormats"/> changed, so that the view redraws.</summary>
    public event EventHandler? CompositionFormatsChanged;

    /// <summary>Replaces the underlines of the composed text.</summary>
    public void SetCompositionFormats(IReadOnlyList<CompositionFormat> formats)
    {
        ArgumentNullException.ThrowIfNull(formats);
        if (formats.Count == 0 && _compositionFormats.Count == 0)
        {
            return;
        }

        _compositionFormats = formats;
        CompositionFormatsChanged?.Invoke(this, EventArgs.Empty);
    }
}

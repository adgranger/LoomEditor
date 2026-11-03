using LoomEditor.View.Controller;
#if LOOM_WINUI
using LoomEditor.WinUI.Controls.DiffEditor;
using LoomEditor.WinUI.Controls.Overlay;
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
using LoomEditor.Avalonia.Controls.DiffEditor;
using LoomEditor.Avalonia.Controls.Overlay;
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// The gutter controls of a side of the diff editor (block action buttons, line checkboxes), created by the diff editor on the zone-widget layer.
public sealed partial class LoomEditor
{
    private int _glyphMarginLaneCount;
    private OverlayHost? _gutterLayer;
    private Func<OverlayHost, Func<double, double, MouseTarget>, DiffGutterControllers>? _diffGutterFactory;
    private DiffGutterControllers? _diffGutter;

    /// <summary>The lanes of the glyph margin the diff editor uses, each one line height wide; 0 (the default) turns the glyph margin off.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal int GlyphMarginLaneCount
    {
        get => _glyphMarginLaneCount;
        set
        {
            if (value == _glyphMarginLaneCount)
            {
                return;
            }

            _glyphMarginLaneCount = value;
            _viewModel.GlyphMarginLaneCount = Math.Max(1, value);
            ApplyHostLayoutOptions();
        }
    }

    /// <summary>
    /// The diff editor's gutter controls of this editor (block action buttons, line checkboxes); null without a factory or without the template part
    /// <c>PART_ZoneWidgetCanvas</c>.
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal DiffGutterControllers? DiffGutter => _diffGutter;

    /// <summary>
    /// Sets how the diff editor creates its gutter controls on the gutter layer: now, and again when the template gives a new layer. Null removes them.
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal void SetDiffGutterFactory(Func<OverlayHost, Func<double, double, MouseTarget>, DiffGutterControllers>? factory)
    {
        _diffGutterFactory = factory;
        CreateDiffGutter();
    }

    /// <summary>The model ranges of the lines in the viewport (partly visible ones included), without the hidden areas.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal List<Core.Text.Range> GetVisibleModelRanges() => _viewModel.GetModelVisibleRanges();

    private void SetGutterLayer(OverlayHost? layer)
    {
        _gutterLayer = layer;
        CreateDiffGutter();
    }

    private void CreateDiffGutter()
    {
        _diffGutter?.Dispose();
        _diffGutter = _gutterLayer is { } layer && _diffGutterFactory is { } factory ? factory(layer, CreateMouseTarget) : null;
        _diffGutter?.ApplyTheme(EffectiveTheme);
    }
}

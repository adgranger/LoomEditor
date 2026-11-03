using Avalonia.Threading;
using LoomEditor.Avalonia.Controls.DiffEditor;
using LoomEditor.Avalonia.Controls.Overlay;
using LoomEditor.View.Controller;

namespace LoomEditor.Avalonia.Controls;

// The gutter controls of a side of the diff editor (block action buttons, line checkboxes), created by the diff editor on the zone-widget layer.
/// <remarks>Threading: UI thread only.</remarks>
public sealed partial class LoomEditor
{
    private Func<OverlayHost, Func<double, double, MouseTarget>, DiffGutterControllers>? _diffGutterFactory;
    private DiffGutterControllers? _diffGutter;
    private OverlayHost? _gutterLayer;
    private int _glyphMarginLaneCount;
    private bool _diffGutterArrangePending;

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

    // Called after each draw: Avalonia refuses a layout change during the render pass, so the buttons are placed right after it.
    private void ArrangeDiffGutterAfterDraw()
    {
        if (_diffGutterArrangePending || _diffGutter is null)
        {
            return;
        }

        _diffGutterArrangePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _diffGutterArrangePending = false;
            _diffGutter?.Arrange();
        });
    }
}

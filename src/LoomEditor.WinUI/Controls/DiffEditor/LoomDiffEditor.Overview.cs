// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/features/overviewRulerFeature.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Rendering;
using LoomEditor.Themes;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;
using LoomEditor.WinUI.Controls.DiffEditor;
using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls;

// The overview strip: two rulers (removed lines of the original, added lines of the modified one) and the viewport slider.
// Behavior differences: the managers are drawn in DIPs; the editors' own overview rulers (find matches, markers) are off in a diff editor.
public sealed partial class LoomDiffEditor
{
    private const double OneOverviewWidth = DiffOverviewRuler.StripWidth / 2;
    private const double ScrollbarWidth = 14;

    private static readonly ThemeColor DefaultSliderColor = new(0x66, 0x79, 0x79, 0x79);

    private DiffOverviewRuler? _overview;
    private OverviewZoneManager? _originalZones;
    private OverviewZoneManager? _modifiedZones;
    private OverviewRulerPainter? _originalPainter;
    private OverviewRulerPainter? _modifiedPainter;
    private bool _overviewZonesDirty = true;
    private ScrollbarState? _dragState;
    private double _dragStartY;

    private void ConnectOverview(DiffOverviewRuler overview)
    {
        _overview = overview;
        _originalZones = new OverviewZoneManager(line => _original!.GetVerticalOffsetForViewLine(line));
        _modifiedZones = new OverviewZoneManager(line => _modified!.GetVerticalOffsetForViewLine(line));
        _originalPainter = new OverviewRulerPainter(_originalZones);
        _modifiedPainter = new OverviewRulerPainter(_modifiedZones);
        overview.DrawContent = DrawOverview;
        overview.Pressed += OnOverviewPressed;
        overview.Dragged += OnOverviewDragged;
        overview.WheelChanged += OnOverviewWheel;
        foreach (LoomEditor editor in new[] { _original!, _modified! })
        {
            editor.ViewZones.ZonesChanged += OnOverviewInputChanged;
            editor.HostLayoutChanged += OnOverviewInputChanged;
            editor.HostScrollChanged += OnOverviewScrollChanged;
        }
    }

    private void DisconnectOverview()
    {
        if (_overview is null)
        {
            return;
        }

        _overview.DrawContent = null;
        _overview.Pressed -= OnOverviewPressed;
        _overview.Dragged -= OnOverviewDragged;
        _overview.WheelChanged -= OnOverviewWheel;
        foreach (LoomEditor? editor in new[] { _original, _modified })
        {
            if (editor is not null)
            {
                editor.ViewZones.ZonesChanged -= OnOverviewInputChanged;
                editor.HostLayoutChanged -= OnOverviewInputChanged;
                editor.HostScrollChanged -= OnOverviewScrollChanged;
            }
        }

        _overview = null;
    }

    // The diff, the zones, the hidden areas (they change the layout), the size or the theme changed: the zones are rebuilt.
    private void InvalidateOverview()
    {
        _overviewZonesDirty = true;
        _overview?.Invalidate();
    }

    private void OnOverviewInputChanged(object? sender, EventArgs e) => InvalidateOverview();

    private void OnOverviewScrollChanged(object? sender, HostScrollEventArgs e)
    {
        if (e.ScrollHeightChanged)
        {
            _overviewZonesDirty = true;
        }

        _overview?.Invalidate();
    }

    // Monaco: createZones, for the diff of the last published state.
    private void RebuildOverviewZones()
    {
        IReadOnlyList<DiffMapping> mappings = _viewModel?.Diff?.Mappings ?? [];
        (ThemeColor insert, ThemeColor remove) = DiffOverviewZones.GetColors(Theme);
        _originalZones!.SetZones(CreateOverviewZones(_original!, mappings.Select(m => m.LineRangeMapping.Original).ToList(), remove));
        _modifiedZones!.SetZones(CreateOverviewZones(_modified!, mappings.Select(m => m.LineRangeMapping.Modified).ToList(), insert));
        _overviewZonesDirty = false;
    }

    private static IReadOnlyList<OverviewRulerZone> CreateOverviewZones(LoomEditor editor, IReadOnlyList<Core.Text.LineRange> ranges, ThemeColor color) =>
        DiffOverviewZones.Create(
            ranges,
            line => editor.CoordinatesConverter.ConvertModelPositionToViewPosition(new Core.Text.Position(line, 1)).LineNumber,
            color);

    private ScrollbarState CreateSliderState() =>
        new(0, ScrollbarWidth, 0, _modified!.LayoutInfo.Height, _modified.GetScrollHeight(), _modified.ScrollTop);

    private void DrawOverview(IRenderSurface surface, float width, float height)
    {
        if (_viewModel is null || _original is null || _modified is null || _overview is null)
        {
            return;
        }

        if (_overviewZonesDirty)
        {
            RebuildOverviewZones();
        }

        double ratio = _overview.PixelRatio;
        Configure(_originalZones!, _original, height, ratio);
        Configure(_modifiedZones!, _modified, height, ratio);
        _originalPainter!.Draw(surface, 0, 0, (float)OneOverviewWidth, height);
        _modifiedPainter!.Draw(surface, (float)OneOverviewWidth, 0, (float)OneOverviewWidth, height);

        // Monaco: the `diffViewport` element, over both rulers.
        ScrollbarState slider = CreateSliderState();
        string colorId = _overview.IsPointerOver ? "scrollbarSlider.hoverBackground" : "scrollbarSlider.background";
        surface.FillRectangle(0, (float)slider.GetSliderPosition(), (float)DiffOverviewRuler.StripWidth, (float)slider.GetSliderSize(), Theme.GetColor(colorId) ?? DefaultSliderColor);
    }

    private static void Configure(OverviewZoneManager manager, LoomEditor editor, float height, double pixelRatio)
    {
        manager.SetLineHeight((int)editor.LineHeight);
        manager.SetOuterHeight((int)editor.GetScrollHeight());
        manager.SetDomWidth((int)OneOverviewWidth);
        manager.SetDomHeight((int)height);
        manager.SetPixelRatio(pixelRatio);
    }

    // Monaco: `delegateVerticalScrollbarPointerDown`: a press on the track centers the slider on the pointer, a press on the slider does not
    // move it; a drag then moves the slider with the pointer.
    private void OnOverviewPressed(object? sender, double y)
    {
        ScrollbarState state = CreateSliderState();
        if (y < state.GetSliderPosition() || y > state.GetSliderPosition() + state.GetSliderSize())
        {
            _modified!.SetScrollPositionImmediate(null, state.GetDesiredScrollPositionFromOffset(y));
            state = CreateSliderState();
        }

        _dragState = state;
        _dragStartY = y;
    }

    private void OnOverviewDragged(object? sender, double y)
    {
        if (_dragState is { } state)
        {
            _modified!.SetScrollPositionImmediate(null, state.GetDesiredScrollPositionFromDelta(y - _dragStartY));
        }
    }

    private void OnOverviewWheel(object? sender, PointerRoutedEventArgs e) => _modified?.ScrollFromWheel(e);
}

using LoomEditor.Themes;
using LoomEditor.View.DiffEditor;
using LoomEditor.WinUI.Controls.DiffEditor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls;

// The sash and the widths of the two editors. Monaco: `diffEditorSash.ts` and the layout of `diffEditorWidget.ts`.
public sealed partial class LoomDiffEditor
{
    /// <summary>The width reserved on the right of the modified editor for the overview ruler (Monaco: <c>ENTIRE_DIFF_OVERVIEW_WIDTH</c>).</summary>
    private const double OverviewRulerWidth = 30;

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _sashLayout.Width = e.NewSize.Width;
        InvalidateOverview();
        _options.SetWidth(e.NewSize.Width);
        ApplyLayout();
    }

    private void OnOptionsChanged(object? sender, DiffEditorOptionsChangedEventArgs e)
    {
        IReadOnlySet<string> names = e.ChangedOptions;
        if (names.Contains(nameof(DiffEditorOptions.RenderIndicators)) || names.Contains(nameof(DiffEditorOptions.ShowEmptyDecorations)))
        {
            UpdateDecorations();
        }

        _sashLayout.EnableSplitViewResizing = _options.EnableSplitViewResizing;
        _sashLayout.SplitViewDefaultRatio = _options.SplitViewDefaultRatio;
        ApplyLayout();
        UpdateViewZones();
    }

    private void OnSashMoved(object? sender, double sashLeft)
    {
        _sashLayout.SetSashLeft(sashLeft);
        ApplyLayout();
    }

    private void OnSashReset(object? sender, EventArgs e)
    {
        _sashLayout.ResetSash();
        ApplyLayout();
    }

    // Monaco: the zones of diffEditorViewZones.ts; side by side they pad both editors, inline the modified one gets the deleted lines.
    private void UpdateViewZones()
    {
        bool wanted = _viewModel is not null && _original is not null && _modified is not null;
        bool inline = !_options.RenderSideBySideEffective;
        if (!wanted || (_viewZones is not null && _viewZonesInline != inline))
        {
            _viewZones?.Dispose();
            _viewZones = null;
        }

        if (!wanted)
        {
            return;
        }

        _hideUnchangedRegions?.Invalidate();

        if (_viewZones is null)
        {
            _viewZonesInline = inline;
            _viewZones = new DiffEditorViewZones(_original!, _modified!, _viewModel!, DispatcherQueue, inline, () => _options.RenderIndicators);
        }
        else
        {
            _viewZones.Invalidate();
        }
    }

    private void ApplyLayout()
    {
        if (_original is not { } original || _modified is not { } modified || _sash is not { } sash)
        {
            return;
        }

        // The original part and the sash are collapsed in the inline view.
        bool sideBySide = _options.RenderSideBySideEffective;
        double left = sideBySide ? _sashLayout.SashLeft : 0;
        Visibility visibility = sideBySide ? Visibility.Visible : Visibility.Collapsed;
        original.Visibility = visibility;
        original.Width = left;
        sash.Visibility = visibility;
        sash.IsSplitEnabled = _options.EnableSplitViewResizing;
        sash.Margin = new Thickness(left - DiffEditorSash.HitWidth / 2, 0, 0, 0);
        sash.LineBrush = DiffBorderBrush(Theme);
        if (_overview is not null)
        {
            _overview.Visibility = _options.RenderOverviewRuler ? Visibility.Visible : Visibility.Collapsed;
        }

        modified.Margin = new Thickness(sideBySide ? left + 1 : 0, 0, _options.RenderOverviewRuler ? OverviewRulerWidth : 0, 0);
    }

    // Monaco: `diffEditor.border`; no line when the theme has no value for it.
    private static SolidColorBrush? DiffBorderBrush(EditorTheme theme) =>
        theme.GetColor("diffEditor.border") is { } color
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B))
            : null;
}

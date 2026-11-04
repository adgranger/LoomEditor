using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LoomEditor.Avalonia.Controls.DiffEditor;
using LoomEditor.Themes;

namespace LoomEditor.Avalonia.Controls;

// The sash and the widths of the two editors. Monaco: `diffEditorSash.ts` and the layout of `diffEditorWidget.ts`.
public sealed partial class LoomDiffEditor
{
    /// <summary>The width reserved on the right of the modified editor for the overview ruler (Monaco: <c>ENTIRE_DIFF_OVERVIEW_WIDTH</c>).</summary>
    private const double OverviewRulerWidth = 30;

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _sashLayout.Width = e.NewSize.Width;
        InvalidateOverview();
        _options.SetWidth(e.NewSize.Width);
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (_original is not { } original || _modified is not { } modified || _sash is not { } sash)
        {
            return;
        }

        // The original part and the sash are hidden in the inline view.
        bool sideBySide = _options.RenderSideBySideEffective;
        double left = sideBySide ? _sashLayout.SashLeft : 0;
        original.IsVisible = sideBySide;
        original.Width = left;
        sash.IsVisible = sideBySide;
        sash.IsSplitEnabled = _options.EnableSplitViewResizing;
        sash.Margin = new Thickness(left - (DiffEditorSash.HitWidth / 2), 0, 0, 0);
        sash.LineBrush = DiffBorderBrush(Theme);
        if (_overview is not null)
        {
            _overview.IsVisible = _options.RenderOverviewRuler;
        }

        modified.Margin = new Thickness(sideBySide ? left + 1 : 0, 0, _options.RenderOverviewRuler ? OverviewRulerWidth : 0, 0);
        UpdateGutterLanes();
    }

    // Monaco: `diffEditor.border`; no line when the theme has no value for it.
    private static SolidColorBrush? DiffBorderBrush(EditorTheme theme) =>
        theme.GetColor("diffEditor.border") is { } color ? new SolidColorBrush(global::Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B)) : null;
}

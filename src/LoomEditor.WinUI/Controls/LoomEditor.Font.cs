using LoomEditor.Rendering.Win2D;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Font family, font size, tab size and DPI. Monaco: `ViewLayout.onConfigurationChanged` and
// `ViewLines.onConfigurationChanged`. One method, `RecreateTextMeasurer`, handles every case.
public sealed partial class LoomEditor
{
    private const int DefaultTabSize = 4;

    /// <summary>Identifies the <see cref="TabSize"/> dependency property.</summary>
    public static readonly DependencyProperty TabSizeProperty = DependencyProperty.Register(
        nameof(TabSize), typeof(int), typeof(LoomEditor), new PropertyMetadata(DefaultTabSize, OnTabSizeChanged));

    private double _rasterizationScale = 1;
    private XamlRoot? _observedXamlRoot;

    /// <summary>The width of a tab stop, in spaces (Monaco: the <c>tabSize</c> model option). Defaults to 4.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1; the previous value is kept.</exception>
    public int TabSize
    {
        get => (int)GetValue(TabSizeProperty);
        set => SetValue(TabSizeProperty, value);
    }

    private void InitializeFont()
    {
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => RecreateTextMeasurer());
        RegisterPropertyChangedCallback(FontSizeProperty, (_, _) => RecreateTextMeasurer());
    }

    private static void OnTabSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        int value = (int)e.NewValue;
        if (value < 1)
        {
            editor.SetValue(TabSizeProperty, e.OldValue);
            throw new ArgumentOutOfRangeException(nameof(e), value, "The tab size must be at least 1.");
        }

        editor.RecreateTextMeasurer();
    }

    private void ObserveXamlRoot()
    {
        if (XamlRoot is not { } root || ReferenceEquals(root, _observedXamlRoot))
        {
            return;
        }

        StopObservingXamlRoot();
        _observedXamlRoot = root;
        _rasterizationScale = root.RasterizationScale;
        root.Changed += OnXamlRootChanged;
    }

    private void StopObservingXamlRoot()
    {
        if (_observedXamlRoot is not null)
        {
            _observedXamlRoot.Changed -= OnXamlRootChanged;
            _observedXamlRoot = null;
        }
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        if (sender.RasterizationScale != _rasterizationScale)
        {
            _rasterizationScale = sender.RasterizationScale;
            RecreateTextMeasurer();
        }
    }

    private Win2DTextMeasurer CreateTextMeasurer(CanvasControl canvas) =>
        new(canvas, FontFamily.Source, FontSize, TabSize, _rasterizationScale);

    // Replaces the measurer with one that matches the current font, tab size and DPI. Also used after a device loss.
    private void RecreateTextMeasurer()
    {
        if (_canvas is null || _measurer is null || _renderer is null || _viewModel is null)
        {
            // Nothing is created yet: CreateResources reads the current values.
            return;
        }

        // Keep the first visible line: it is scrolled to the top again once the line height is known.
        int firstLine = _viewModel.ViewLayout.GetLineNumberAtVerticalOffset((int)_viewModel.ViewLayout.GetCurrentScrollTop());

        Win2DTextMeasurer oldMeasurer = _measurer;
        _measurer = CreateTextMeasurer(_canvas);
        _viewModel.SetFontInfo(_measurer.FontInfo);
        _renderer.OnFontChanged(_measurer);
        // The renderer has disposed the layouts: only now the old measurer can go.
        oldMeasurer.Dispose();

        ScrollToLine(firstLine);
        UpdateScrollBars();
        _canvas.Invalidate();
    }

    private void ScrollToLine(int lineNumber)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.ViewLayout.SetScrollPosition(
            new View.Layout.NewScrollPosition(null, _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(lineNumber)),
            View.Layout.ScrollType.Immediate);
    }
}

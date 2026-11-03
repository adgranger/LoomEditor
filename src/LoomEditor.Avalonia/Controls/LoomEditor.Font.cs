using Avalonia;
using Avalonia.Controls;
using LoomEditor.Avalonia.Rendering;
using LoomEditor.Core.Model;

namespace LoomEditor.Avalonia.Controls;

// Font family, font size, tab size and DPI. Monaco: `ViewLayout.onConfigurationChanged` and
// `ViewLines.onConfigurationChanged`. One method, `RecreateTextMeasurer`, handles every case.
public sealed partial class LoomEditor
{
    private const int DefaultTabSize = 4;

    /// <summary>Identifies the <see cref="TabSize"/> styled property.</summary>
    public static readonly StyledProperty<int> TabSizeProperty =
        AvaloniaProperty.Register<LoomEditor, int>(nameof(TabSize), DefaultTabSize);

    private double _renderScaling = 1;

    /// <summary>The width of a tab stop, in spaces (Monaco: the <c>tabSize</c> model option). Defaults to 4.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1; the previous value is kept.</exception>
    public int TabSize
    {
        get => GetValue(TabSizeProperty);
        set => SetValue(TabSizeProperty, value);
    }

    private static void InitializeFontProperties()
    {
        TabSizeProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnTabSizeChanged(e));
        FontFamilyProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.RecreateTextMeasurer());
        FontSizeProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.RecreateTextMeasurer());
        FontWeightProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.RecreateTextMeasurer());
    }

    private void InitializeFont()
    {
        _context.Model.OptionsChanged += OnModelOptionsChanged;
        _context.Zoom.Changed += OnZoomChanged;
    }

    private void OnTabSizeChanged(AvaloniaPropertyChangedEventArgs e)
    {
        int value = (int)e.NewValue!;
        if (value < 1)
        {
            SetCurrentValue(TabSizeProperty, (int)e.OldValue!);
            throw new ArgumentOutOfRangeException(nameof(e), value, "The tab size must be at least 1.");
        }

        // The model owns the tab size: OnModelOptionsChanged recreates the text measurer.
        _context.Model.UpdateOptions(new TextModelUpdateOptions(TabSize: value));
    }

    private void OnModelOptionsChanged(object? sender, ModelOptionsChangedEventArgs e)
    {
        if (e.TabSize)
        {
            RecreateTextMeasurer();
        }
    }

    private void ObserveTopLevel()
    {
        if (TopLevel.GetTopLevel(this) is not { } top || ReferenceEquals(top, _observedTopLevel))
        {
            return;
        }

        StopObservingTopLevel();
        _observedTopLevel = top;
        _renderScaling = top.RenderScaling;
        top.ScalingChanged += OnTopLevelScalingChanged;
    }

    private void StopObservingTopLevel()
    {
        if (_observedTopLevel is not null)
        {
            _observedTopLevel.ScalingChanged -= OnTopLevelScalingChanged;
            _observedTopLevel = null;
        }
    }

    private void OnTopLevelScalingChanged(object? sender, EventArgs e)
    {
        if (sender is TopLevel top && top.RenderScaling != _renderScaling)
        {
            _renderScaling = top.RenderScaling;
            RecreateTextMeasurer();
        }
    }

    private AvaloniaTextMeasurer CreateTextMeasurer() =>
        new(
            FontFamily.ToString(), FontSize, _context.Model.Options.TabSize, _renderScaling, CurrentFontOptions, _context.Zoom.Level);

    // Replaces the measurer with one that matches the current font, tab size and DPI.
    private void RecreateTextMeasurer()
    {
        if (_canvas is null || _measurer is null || _renderer is null)
        {
            // Nothing is created yet: CreateResources reads the current values.
            return;
        }

        // Keep the first visible line: it is scrolled to the top again once the line height is known.
        int firstLine = _viewModel.ViewLayout.GetLineNumberAtVerticalOffset((int)_viewModel.ViewLayout.GetCurrentScrollTop());

        AvaloniaTextMeasurer oldMeasurer = _measurer;
        _measurer = CreateTextMeasurer();
        _viewModel.SetFontInfo(_measurer.FontInfo);
        _renderer.OnFontChanged(_measurer);
        // The renderer has disposed the layouts: only now the old measurer can go.
        oldMeasurer.Dispose();

        ScrollToLine(firstLine);
        UpdateScrollBars();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void ScrollToLine(int lineNumber)
    {
        _viewModel.ViewLayout.SetScrollPosition(
            new View.Layout.NewScrollPosition(null, _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(lineNumber)),
            View.Layout.ScrollType.Immediate);
    }
}

using LoomEditor.WinUI.Controls.Comments;
using LoomEditor.WinUI.Controls.Overlay;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls;

/// <remarks>Threading: UI thread only (not checked).</remarks>
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="CommentGutterButtonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty CommentGutterButtonEnabledProperty = DependencyProperty.Register(
        nameof(CommentGutterButtonEnabled), typeof(bool), typeof(LoomEditor), new PropertyMetadata(false, OnCommentGutterButtonEnabledChanged));

    private Canvas? _zoneWidgetCanvas;

    /// <summary>The zone-widget layer status line, for the Gallery's manual test.</summary>
    internal string CommentZonesDiagnostics => _commentZoneHost?.Diagnostics ?? "no zone-widget layer";

    /// <summary>
    /// Whether hovering the line numbers shows a "+" button that raises <see cref="AddCommentRequested"/> for that line. Defaults to
    /// <see langword="false"/>. Needs the template part <c>PART_ZoneWidgetCanvas</c>.
    /// </summary>
    public bool CommentGutterButtonEnabled
    {
        get => (bool)GetValue(CommentGutterButtonEnabledProperty);
        set => SetValue(CommentGutterButtonEnabledProperty, value);
    }

    // The optional PART_ZoneWidgetCanvas hosts a box over each comment thread's zone; without the part the zones are only gaps.
    private void InitializeCommentZoneHost(Canvas? canvas)
    {
        _commentZoneHost?.Dispose();
        _commentZoneHost = null;
        _commentGutter?.Dispose();
        _commentGutter = null;
        if (_zoneWidgetCanvas is not null)
        {
            _zoneWidgetCanvas.SizeChanged -= OnZoneWidgetCanvasSizeChanged;
        }

        _zoneWidgetCanvas = canvas;
        if (canvas is null)
        {
            _hostZoneLayer.Attach(null);
            SetGutterLayer(null);
            return;
        }

        canvas.SizeChanged += OnZoneWidgetCanvasSizeChanged;
        var layer = new OverlayHost(canvas);
        _hostZoneLayer.Attach(layer);
        _commentZoneHost = new CommentZoneHost(this, layer, _commentThreads);
        _commentZoneHost.ApplyTheme(EffectiveTheme, _systemHighContrastTheme is not null);
        _commentGutter = new CommentGutterController(this, layer, _commentThreads, CreateMouseTarget) { Enabled = CommentGutterButtonEnabled };
        _commentGutter.ApplyTheme(EffectiveTheme);
        SetGutterLayer(layer);
    }

    private static void OnCommentGutterButtonEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (((LoomEditor)sender)._commentGutter is { } gutter)
        {
            gutter.Enabled = (bool)e.NewValue;
        }
    }

    // A Canvas does not clip its children: without this a box scrolled half out would draw outside the editor.
    private void OnZoneWidgetCanvasSizeChanged(object sender, SizeChangedEventArgs e) =>
        ((Canvas)sender).Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
}

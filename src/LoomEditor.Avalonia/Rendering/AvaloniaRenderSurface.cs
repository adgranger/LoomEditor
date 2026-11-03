using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using LoomEditor.Rendering;
using LoomEditor.Themes;

namespace LoomEditor.Avalonia.Rendering;

/// <summary>
/// <see cref="IRenderSurface"/> implemented on an Avalonia <see cref="DrawingContext"/> (Skia).
/// </summary>
/// <remarks>
/// Create one per frame, inside <c>Render</c>; it does not own the context.
/// Threading: only valid during the render call, on the UI thread.
/// </remarks>
internal sealed class AvaloniaRenderSurface : IRenderSurface
{
    private readonly DrawingContext _context;
    private readonly AvaloniaTextMeasurer? _measurer;
    private readonly Rect _bounds;
    private readonly AvaloniaBitmapCache _bitmaps;
    private readonly AvaloniaPaintCache _paints;

    /// <summary>Wraps a drawing context.</summary>
    /// <param name="context">The context of the current render call.</param>
    /// <param name="bounds">The area <see cref="Clear"/> fills.</param>
    /// <param name="measurer">The measurer that made the layouts; <see langword="null"/> for a surface that only fills shapes.</param>
    /// <param name="bitmaps">Keeps the uploaded images between frames; without it every image is uploaded again each frame.</param>
    /// <param name="paints">Keeps the brushes and pens between frames.</param>
    public AvaloniaRenderSurface(
        DrawingContext context, Rect bounds, AvaloniaTextMeasurer? measurer, AvaloniaBitmapCache? bitmaps = null, AvaloniaPaintCache? paints = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _bounds = bounds;
        _measurer = measurer;
        _bitmaps = bitmaps ?? new AvaloniaBitmapCache();
        _paints = paints ?? new AvaloniaPaintCache();
    }

    /// <inheritdoc />
    public void Clear(ThemeColor color) => _context.DrawRectangle(_paints.GetBrush(color), null, _bounds);

    /// <inheritdoc />
    public void FillRectangle(float x, float y, float width, float height, ThemeColor color) =>
        _context.DrawRectangle(_paints.GetBrush(color), null, new Rect(x, y, width, height));

    /// <inheritdoc />
    public void DrawLine(float x1, float y1, float x2, float y2, ThemeColor color, float strokeWidth) =>
        _context.DrawLine(_paints.GetPen(color, strokeWidth), new Point(x1, y1), new Point(x2, y2));

    /// <inheritdoc />
    public void DrawRectangle(float x, float y, float width, float height, ThemeColor color, float strokeWidth) =>
        _context.DrawRectangle(null, _paints.GetPen(color, strokeWidth), new Rect(x, y, width, height));

    /// <inheritdoc />
    public void DrawLineLayout(ILineLayout layout, float x, float lineTop, ThemeColor color)
    {
        ArgumentNullException.ThrowIfNull(layout);

        if (layout is not AvaloniaLineLayout avaloniaLayout)
        {
            throw new ArgumentException("The layout must come from an AvaloniaTextMeasurer.", nameof(layout));
        }

        double glyphTop = (_measurer ?? throw new InvalidOperationException("This surface was created without a text measurer.")).GlyphTopOffset;
        _measurer.DefaultForeground.Color = global::Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        avaloniaLayout.TextLayout.Draw(_context, new Point(x, lineTop + glyphTop));
    }

    /// <inheritdoc />
    public void DrawRgbaImage(byte[] rgba, int pixelWidth, int pixelHeight, float x, float y, float width, float height, float opacity, int version)
    {
        ArgumentNullException.ThrowIfNull(rgba);

        global::Avalonia.Media.Imaging.WriteableBitmap bitmap = _bitmaps.Get(rgba, pixelWidth, pixelHeight, version);
        using (_context.PushOpacity(opacity))
        using (_context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.LowQuality }))
        {
            _context.DrawImage(bitmap, new Rect(0, 0, pixelWidth, pixelHeight), new Rect(x, y, width, height));
        }
    }
}

/// <summary>Brushes and pens by color (and width), so a frame allocates none.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AvaloniaPaintCache
{
    private readonly Dictionary<ThemeColor, ImmutableSolidColorBrush> _brushes = [];
    private readonly Dictionary<(ThemeColor, float), ImmutablePen> _pens = [];

    /// <summary>The solid brush of <paramref name="color"/>.</summary>
    public ImmutableSolidColorBrush GetBrush(ThemeColor color)
    {
        if (!_brushes.TryGetValue(color, out ImmutableSolidColorBrush? brush))
        {
            brush = new ImmutableSolidColorBrush(global::Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            _brushes[color] = brush;
        }

        return brush;
    }

    /// <summary>The pen of <paramref name="color"/> and <paramref name="width"/>.</summary>
    public ImmutablePen GetPen(ThemeColor color, float width)
    {
        if (!_pens.TryGetValue((color, width), out ImmutablePen? pen))
        {
            pen = new ImmutablePen(GetBrush(color), width);
            _pens[(color, width)] = pen;
        }

        return pen;
    }
}

using LoomEditor.Themes;
using Microsoft.Graphics.Canvas;

namespace LoomEditor.Rendering.Win2D;

/// <summary>
/// <see cref="IRenderSurface"/> implemented with a Win2D drawing session (Direct2D + DirectWrite).
/// </summary>
/// <remarks>
/// Create one per frame, inside the <c>CanvasControl.Draw</c> handler; it does not own the session.
/// Threading: only valid during the draw call, on the thread that received it.
/// </remarks>
internal sealed class Win2DRenderSurface : IRenderSurface
{
    private readonly CanvasDrawingSession _session;
    private readonly Win2DTextMeasurer? _measurer;
    private readonly Win2DBitmapCache _bitmaps;

    /// <summary>Wraps a drawing session.</summary>
    /// <param name="session">The session of the current draw call.</param>
    /// <param name="measurer">The measurer that made the layouts; <see langword="null"/> for a surface that only fills shapes.</param>
    /// <param name="bitmaps">Keeps the uploaded images between frames; without it every image is uploaded again each frame.</param>
    public Win2DRenderSurface(CanvasDrawingSession session, Win2DTextMeasurer? measurer, Win2DBitmapCache? bitmaps = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
        _measurer = measurer;
        _bitmaps = bitmaps ?? new Win2DBitmapCache();
    }

    /// <inheritdoc />
    public void Clear(ThemeColor color) => _session.Clear(ToWindowsColor(color));

    /// <inheritdoc />
    public void FillRectangle(float x, float y, float width, float height, ThemeColor color) =>
        _session.FillRectangle(x, y, width, height, ToWindowsColor(color));

    /// <inheritdoc />
    public void DrawLine(float x1, float y1, float x2, float y2, ThemeColor color, float strokeWidth) =>
        _session.DrawLine(x1, y1, x2, y2, ToWindowsColor(color), strokeWidth);

    /// <inheritdoc />
    public void DrawRectangle(float x, float y, float width, float height, ThemeColor color, float strokeWidth) =>
        _session.DrawRectangle(x, y, width, height, ToWindowsColor(color), strokeWidth);

    /// <inheritdoc />
    public void DrawLineLayout(ILineLayout layout, float x, float lineTop, ThemeColor color)
    {
        ArgumentNullException.ThrowIfNull(layout);

        if (layout is not Win2DLineLayout win2DLayout)
        {
            throw new ArgumentException("The layout must come from a Win2DTextMeasurer.", nameof(layout));
        }

        _session.DrawTextLayout(win2DLayout.TextLayout, x, lineTop + (_measurer ?? throw new InvalidOperationException("This surface was created without a text measurer.")).GlyphTopOffset, ToWindowsColor(color));
    }

    /// <inheritdoc />
    public void DrawRgbaImage(byte[] rgba, int pixelWidth, int pixelHeight, float x, float y, float width, float height, float opacity, int version)
    {
        ArgumentNullException.ThrowIfNull(rgba);

        CanvasBitmap bitmap = _bitmaps.Get(_session.Device, rgba, pixelWidth, pixelHeight, version);
        _session.DrawImage(bitmap, new Windows.Foundation.Rect(x, y, width, height), bitmap.Bounds, opacity, CanvasImageInterpolation.Linear);
    }

    private static Windows.UI.Color ToWindowsColor(ThemeColor color) =>
        Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
}

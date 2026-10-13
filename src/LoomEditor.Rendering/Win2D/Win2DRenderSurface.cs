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
public sealed class Win2DRenderSurface : IRenderSurface
{
    private readonly CanvasDrawingSession _session;
    private readonly Win2DFont _font;

    /// <summary>Wraps a drawing session.</summary>
    public Win2DRenderSurface(CanvasDrawingSession session, Win2DFont font)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(font);

        _session = session;
        _font = font;
    }

    /// <inheritdoc />
    public FontMetrics FontMetrics => _font.Metrics;

    /// <inheritdoc />
    public void Clear(ThemeColor color) => _session.Clear(ToWindowsColor(color));

    /// <inheritdoc />
    public void FillRectangle(float x, float y, float width, float height, ThemeColor color) =>
        _session.FillRectangle(x, y, width, height, ToWindowsColor(color));

    /// <inheritdoc />
    public void DrawText(string text, float x, float y, ThemeColor color) =>
        _session.DrawText(text, x, y, ToWindowsColor(color), _font.TextFormat);

    private static Windows.UI.Color ToWindowsColor(ThemeColor color) =>
        Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
}

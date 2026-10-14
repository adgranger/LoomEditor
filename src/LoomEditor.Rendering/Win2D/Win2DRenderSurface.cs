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
    private readonly Win2DTextMeasurer _measurer;

    /// <summary>Wraps a drawing session.</summary>
    public Win2DRenderSurface(CanvasDrawingSession session, Win2DTextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(measurer);

        _session = session;
        _measurer = measurer;
    }

    /// <inheritdoc />
    public void Clear(ThemeColor color) => _session.Clear(ToWindowsColor(color));

    /// <inheritdoc />
    public void FillRectangle(float x, float y, float width, float height, ThemeColor color) =>
        _session.FillRectangle(x, y, width, height, ToWindowsColor(color));

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

        _session.DrawTextLayout(win2DLayout.TextLayout, x, lineTop + _measurer.GlyphTopOffset, ToWindowsColor(color));
    }

    private static Windows.UI.Color ToWindowsColor(ThemeColor color) =>
        Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
}

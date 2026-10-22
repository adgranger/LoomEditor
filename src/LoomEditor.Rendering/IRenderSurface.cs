using LoomEditor.Themes;

namespace LoomEditor.Rendering;

/// <summary>
/// Something the editor can paint on. The only drawing API used outside of the <c>Win2D</c> folder.
/// </summary>
/// <remarks>
/// <para>
/// Keeping drawing behind this interface means the rendering backend (Win2D today, maybe raw
/// Direct2D/DirectWrite later) can change without touching the code that decides <em>what</em> to draw.
/// See docs/adr/0002-rendering-backend.md.
/// </para>
/// <para>Coordinates are in device-independent pixels (DIPs), origin at the top-left corner.</para>
/// <para>Threading: a surface is only valid during one draw call, on the thread that called it.</para>
/// </remarks>
internal interface IRenderSurface
{
    /// <summary>Fills the whole surface with <paramref name="color"/>.</summary>
    void Clear(ThemeColor color);

    /// <summary>Fills a rectangle.</summary>
    void FillRectangle(float x, float y, float width, float height, ThemeColor color);

    /// <summary>Draws a straight line segment.</summary>
    void DrawLine(float x1, float y1, float x2, float y2, ThemeColor color, float strokeWidth);

    /// <summary>Draws the outline of a rectangle.</summary>
    void DrawRectangle(float x, float y, float width, float height, ThemeColor color, float strokeWidth);

    /// <summary>
    /// Draws a laid-out line. <paramref name="lineTop"/> is the top of the line box (<c>FontInfo.LineHeight</c> high):
    /// the glyphs are centered in it, half of the extra space above and half below.
    /// </summary>
    void DrawLineLayout(ILineLayout layout, float x, float lineTop, ThemeColor color);
}

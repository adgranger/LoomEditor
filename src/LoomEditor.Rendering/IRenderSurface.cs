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
public interface IRenderSurface
{
    /// <summary>Size of one character cell and of one line of text, for the editor font.</summary>
    FontMetrics FontMetrics { get; }

    /// <summary>Fills the whole surface with <paramref name="color"/>.</summary>
    void Clear(ThemeColor color);

    /// <summary>Fills a rectangle.</summary>
    void FillRectangle(float x, float y, float width, float height, ThemeColor color);

    /// <summary>Draws one line of text with its top-left corner at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    void DrawText(string text, float x, float y, ThemeColor color);
}

/// <summary>
/// Measurements of the editor font, in DIPs.
/// </summary>
/// <remarks>
/// Phase 0 assumes a monospaced font: every character is <see cref="CharacterWidth"/> wide.
/// Phase 2 replaces this with real text layout (proportional fonts, tabs, wide characters).
/// </remarks>
/// <param name="CharacterWidth">Width of one character.</param>
/// <param name="LineHeight">Height of one line.</param>
public readonly record struct FontMetrics(float CharacterWidth, float LineHeight);

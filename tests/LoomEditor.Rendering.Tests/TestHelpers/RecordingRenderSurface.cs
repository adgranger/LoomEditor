using LoomEditor.Themes;

namespace LoomEditor.Rendering.Tests.TestHelpers;

/// <summary>An <see cref="IRenderSurface"/> that records every call instead of drawing.</summary>
/// <remarks>Threading: not thread-safe; one test, one thread.</remarks>
internal sealed class RecordingRenderSurface(FontMetrics fontMetrics) : IRenderSurface
{
    public FontMetrics FontMetrics { get; } = fontMetrics;

    public List<DrawCall> Calls { get; } = [];

    public void Clear(ThemeColor color) => Calls.Add(new ClearCall(color));

    public void FillRectangle(float x, float y, float width, float height, ThemeColor color) =>
        Calls.Add(new FillRectangleCall(x, y, width, height, color));

    public void DrawText(string text, float x, float y, ThemeColor color) => Calls.Add(new DrawTextCall(text, x, y, color));
}

internal abstract record DrawCall;

internal sealed record ClearCall(ThemeColor Color) : DrawCall;

internal sealed record FillRectangleCall(float X, float Y, float Width, float Height, ThemeColor Color) : DrawCall;

internal sealed record DrawTextCall(string Text, float X, float Y, ThemeColor Color) : DrawCall;

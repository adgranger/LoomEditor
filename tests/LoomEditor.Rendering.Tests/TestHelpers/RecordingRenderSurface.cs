using LoomEditor.Themes;

namespace LoomEditor.Rendering.Tests.TestHelpers;

/// <summary>An <see cref="IRenderSurface"/> that records every call instead of drawing.</summary>
/// <remarks>Threading: not thread-safe; one test, one thread.</remarks>
internal sealed class RecordingRenderSurface : IRenderSurface
{
    public List<DrawCall> Calls { get; } = [];

    public void Clear(ThemeColor color) => Calls.Add(new ClearCall(color));

    public void FillRectangle(float x, float y, float width, float height, ThemeColor color) =>
        Calls.Add(new FillRectangleCall(x, y, width, height, color));

    public void DrawRectangle(float x, float y, float width, float height, ThemeColor color, float strokeWidth) =>
        Calls.Add(new DrawRectangleCall(x, y, width, height, color, strokeWidth));

    public void DrawLineLayout(ILineLayout layout, float x, float lineTop, ThemeColor color) =>
        Calls.Add(new DrawLineLayoutCall(layout.Text, x, lineTop, color));
}

internal abstract record DrawCall;

internal sealed record ClearCall(ThemeColor Color) : DrawCall;

internal sealed record FillRectangleCall(float X, float Y, float Width, float Height, ThemeColor Color) : DrawCall;

internal sealed record DrawRectangleCall(float X, float Y, float Width, float Height, ThemeColor Color, float StrokeWidth)
    : DrawCall;

/// <summary>A <c>DrawLineLayout</c> call; the layout is recorded by its text.</summary>
internal sealed record DrawLineLayoutCall(string Text, float X, float LineTop, ThemeColor Color) : DrawCall;

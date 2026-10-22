namespace LoomEditor.View.Minimap;

/// <summary>An RGBA pixel buffer, row-major. Monaco: the DOM <c>ImageData</c>.</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class MinimapImageData(int width, int height)
{
    /// <summary>The width in pixels.</summary>
    public int Width { get; } = width;

    /// <summary>The height in pixels.</summary>
    public int Height { get; } = height;

    /// <summary>The pixels, 4 bytes (R, G, B, A) each.</summary>
    public byte[] Data { get; } = new byte[width * height * MinimapCharSheet.RgbaChannelsCnt];
}

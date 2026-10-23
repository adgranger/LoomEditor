namespace LoomEditor.View.Minimap;

/// <summary>
/// Pixel format conversion for the minimap: the graphics API gives BGRA premultiplied bytes, Monaco's
/// <c>getImageData</c> gives RGBA non-premultiplied bytes, and the minimap buffers must be premultiplied again to be drawn.
/// </summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal static class MinimapSampleConversion
{
    /// <summary>Converts BGRA premultiplied pixels to RGBA non-premultiplied ones (<c>c * 255 / a</c>, rounded; 0 when <c>a == 0</c>).</summary>
    public static byte[] ConvertBgraPremultipliedToRgba(ReadOnlySpan<byte> bgra)
    {
        byte[] rgba = new byte[bgra.Length];
        for (int i = 0; i + 3 < bgra.Length; i += 4)
        {
            int a = bgra[i + 3];
            if (a == 0)
            {
                continue;
            }

            rgba[i] = Unpremultiply(bgra[i + 2], a);
            rgba[i + 1] = Unpremultiply(bgra[i + 1], a);
            rgba[i + 2] = Unpremultiply(bgra[i], a);
            rgba[i + 3] = (byte)a;
        }

        return rgba;
    }

    /// <summary>Converts RGBA straight-alpha pixels to RGBA premultiplied ones (<c>c * a / 255</c>, rounded), the form Win2D draws.</summary>
    public static byte[] PremultiplyRgba(ReadOnlySpan<byte> rgba)
    {
        byte[] result = new byte[rgba.Length];
        for (int i = 0; i + 3 < rgba.Length; i += 4)
        {
            int a = rgba[i + 3];
            result[i] = (byte)((rgba[i] * a + 127) / 255);
            result[i + 1] = (byte)((rgba[i + 1] * a + 127) / 255);
            result[i + 2] = (byte)((rgba[i + 2] * a + 127) / 255);
            result[i + 3] = (byte)a;
        }

        return result;
    }

    private static byte Unpremultiply(int channel, int alpha) => (byte)Math.Min(255, (channel * 255 + alpha / 2) / alpha);
}

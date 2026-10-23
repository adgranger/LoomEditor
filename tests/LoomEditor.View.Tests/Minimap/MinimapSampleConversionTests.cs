using LoomEditor.View.Minimap;

namespace LoomEditor.View.Tests.Minimap;

public class MinimapSampleConversionTests
{
    [Fact]
    public void ConvertBgraPremultipliedToRgba_Unpremultiplies()
    {
        // B, G, R, A: opaque, half transparent (premultiplied 64 of 128), fully transparent.
        byte[] bgra = [10, 20, 30, 255, 64, 32, 128, 128, 7, 7, 7, 0];

        byte[] rgba = MinimapSampleConversion.ConvertBgraPremultipliedToRgba(bgra);

        Assert.Equal(new byte[] { 30, 20, 10, 255, 255, 64, 128, 128, 0, 0, 0, 0 }, rgba);
    }

    [Fact]
    public void Premultiply_Converts()
    {
        // R, G, B, A: opaque, half transparent, fully transparent.
        byte[] rgba = [30, 20, 10, 255, 255, 64, 128, 128, 7, 7, 7, 0];

        byte[] premultiplied = MinimapSampleConversion.PremultiplyRgba(rgba);

        Assert.Equal(new byte[] { 30, 20, 10, 255, 128, 32, 64, 128, 0, 0, 0, 0 }, premultiplied);
    }
}

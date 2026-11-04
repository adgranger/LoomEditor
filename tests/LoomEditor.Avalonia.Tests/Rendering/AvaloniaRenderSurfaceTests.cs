using LoomEditor.Avalonia.Rendering;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Themes;

namespace LoomEditor.Avalonia.Tests.Rendering;

// The twin of the Win2D surface: no upstream test, all tests are new.
public class AvaloniaRenderSurfaceTests
{
    private static readonly ThemeColor White = ThemeColor.FromRgb(255, 255, 255);
    private static readonly ThemeColor Red = ThemeColor.FromRgb(255, 0, 0);
    private static readonly (byte B, byte G, byte R, byte A) WhitePixel = (255, 255, 255, 255);

    [Fact]
    public async Task FillRectangle_PaintsExactPixels()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using var frame = new FrameBitmap(40, 40);
            frame.Draw(null, surface =>
            {
                surface.Clear(White);
                surface.FillRectangle(5, 5, 10, 10, Red);
            });

            Assert.Equal(((byte)0, (byte)0, (byte)255, (byte)255), frame.PixelAt(5, 5));
            Assert.Equal(((byte)0, (byte)0, (byte)255, (byte)255), frame.PixelAt(14, 14));
            Assert.Equal(WhitePixel, frame.PixelAt(4, 5));
            Assert.Equal(WhitePixel, frame.PixelAt(15, 14));
            Assert.Equal(WhitePixel, frame.PixelAt(10, 15));
        });
    }

    [Fact]
    public async Task DrawLine_OneDip_Visible()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using var frame = new FrameBitmap(40, 40);
            frame.Draw(null, surface =>
            {
                surface.Clear(White);
                surface.DrawLine(5, 10.5f, 35, 10.5f, Red, 1);
                surface.DrawRectangle(5.5f, 20.5f, 20, 10, Red, 1);
            });

            Assert.True(frame.HasInk(5, 10, 35, 11, WhitePixel));
            Assert.False(frame.HasInk(5, 13, 35, 18, WhitePixel));
            Assert.True(frame.HasInk(5, 20, 26, 21, WhitePixel));
            Assert.False(frame.HasInk(8, 23, 22, 28, WhitePixel));
        });
    }

    [Fact]
    public async Task DrawRgbaImage_SameVersion_UploadsOnce()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using var frame = new FrameBitmap(20, 20);
            byte[] rgba = Solid(4, 4, 255, 0, 0);

            for (int i = 0; i < 3; i++)
            {
                frame.Draw(null, surface => surface.DrawRgbaImage(rgba, 4, 4, 0, 0, 20, 20, 1, version: 1));
            }

            Assert.Equal(1, frame.Bitmaps.UploadCount);
            Assert.Equal(((byte)0, (byte)0, (byte)255, (byte)255), frame.PixelAt(10, 10));
        });
    }

    [Fact]
    public async Task DrawRgbaImage_NewVersion_Reuploads()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using var frame = new FrameBitmap(20, 20);
            byte[] rgba = Solid(4, 4, 255, 0, 0);
            frame.Draw(null, surface => surface.DrawRgbaImage(rgba, 4, 4, 0, 0, 20, 20, 1, version: 1));

            for (int i = 0; i < rgba.Length; i += 4)
            {
                (rgba[i], rgba[i + 2]) = (0, 255);
            }

            frame.Draw(null, surface => surface.DrawRgbaImage(rgba, 4, 4, 0, 0, 20, 20, 1, version: 2));

            Assert.Equal(2, frame.Bitmaps.UploadCount);
            Assert.Equal(((byte)255, (byte)0, (byte)0, (byte)255), frame.PixelAt(10, 10));
        });
    }

    [Fact]
    public async Task MinimapSampler_ProducesNonEmptySheet()
    {
        await HeadlessSession.RunAsync(() =>
        {
            byte[] sheet = AvaloniaMinimapFontSampler.CreateSampleData(TestFont.Family);

            int width = View.Minimap.MinimapCharSheet.CharCount * View.Minimap.MinimapCharSheet.SampledCharWidth;
            Assert.Equal(width * View.Minimap.MinimapCharSheet.SampledCharHeight * 4, sheet.Length);

            // Characters 'A'..'Z' (and '#') have ink; the space has none.
            foreach (int code in (ReadOnlySpan<int>)['A', 'M', 'x', '#', '@'])
            {
                Assert.True(CellHasAlpha(sheet, width, code - 32), $"no ink for '{(char)code}'");
            }

            Assert.False(CellHasAlpha(sheet, width, 0));
        });
    }

    private static bool CellHasAlpha(byte[] sheet, int width, int cell)
    {
        for (int y = 0; y < View.Minimap.MinimapCharSheet.SampledCharHeight; y++)
        {
            for (int x = 0; x < View.Minimap.MinimapCharSheet.SampledCharWidth; x++)
            {
                if (sheet[((y * width) + (cell * View.Minimap.MinimapCharSheet.SampledCharWidth) + x) * 4 + 3] != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static byte[] Solid(int width, int height, byte r, byte g, byte b)
    {
        byte[] rgba = new byte[width * height * 4];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            (rgba[i], rgba[i + 1], rgba[i + 2], rgba[i + 3]) = (r, g, b, 255);
        }

        return rgba;
    }
}

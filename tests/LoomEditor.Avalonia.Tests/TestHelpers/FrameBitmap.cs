using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using LoomEditor.Avalonia.Rendering;

namespace LoomEditor.Avalonia.Tests.TestHelpers;

/// <summary>A headless bitmap an <see cref="AvaloniaRenderSurface"/> draws on, with its pixels read back as BGRA.</summary>
/// <remarks>Threading: UI thread of the headless session only.</remarks>
internal sealed class FrameBitmap : IDisposable
{
    private readonly RenderTargetBitmap _bitmap;

    public FrameBitmap(int width, int height)
    {
        Width = width;
        Height = height;
        _bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
    }

    public int Width { get; }

    public int Height { get; }

    public AvaloniaPaintCache Paints { get; } = new();

    public AvaloniaBitmapCache Bitmaps { get; } = new();

    /// <summary>Draws one frame: <paramref name="draw"/> gets a surface over the whole bitmap.</summary>
    public void Draw(AvaloniaTextMeasurer? measurer, Action<AvaloniaRenderSurface> draw)
    {
        using DrawingContext context = _bitmap.CreateDrawingContext();
        draw(new AvaloniaRenderSurface(context, new Rect(0, 0, Width, Height), measurer, Bitmaps, Paints));
    }

    /// <summary>The pixel at (<paramref name="x"/>, <paramref name="y"/>) as (B, G, R, A), premultiplied.</summary>
    public (byte B, byte G, byte R, byte A) PixelAt(int x, int y)
    {
        byte[] pixels = ReadPixels();
        int i = ((y * Width) + x) * 4;
        return (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
    }

    /// <summary>True when a pixel of the rectangle differs from <paramref name="background"/> (B, G, R, A).</summary>
    public bool HasInk(int left, int top, int right, int bottom, (byte B, byte G, byte R, byte A) background)
    {
        byte[] pixels = ReadPixels();
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                int i = ((y * Width) + x) * 4;
                if ((pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) != background)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Dispose() => _bitmap.Dispose();

    private byte[] ReadPixels()
    {
        byte[] pixels = new byte[Width * Height * 4];
        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            _bitmap.CopyPixels(new PixelRect(0, 0, Width, Height), handle.AddrOfPinnedObject(), pixels.Length, Width * 4);
        }
        finally
        {
            handle.Free();
        }

        // The native format of the headless bitmap is RGBA on some platforms (macOS arm64): the helpers read BGRA.
        if (_bitmap.Format == PixelFormat.Rgba8888)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
            }
        }

        return pixels;
    }
}

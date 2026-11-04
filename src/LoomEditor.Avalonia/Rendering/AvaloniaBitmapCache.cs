using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using LoomEditor.View.Minimap;

namespace LoomEditor.Avalonia.Rendering;

/// <summary>
/// Keeps the <see cref="WriteableBitmap"/> made from a pixel buffer until the buffer changes, so the minimap is uploaded
/// only when it was repainted.
/// </summary>
/// <remarks>
/// shortcut: bitmaps are released by the garbage collector when their buffer is, not disposed explicitly; dispose them
/// here if bitmap memory ever shows up in a profile.
/// Threading: UI thread only.
/// </remarks>
internal sealed class AvaloniaBitmapCache
{
    private readonly ConditionalWeakTable<byte[], Entry> _entries = [];

    /// <summary>How many times a bitmap was filled since this cache was created.</summary>
    public int UploadCount { get; private set; }

    /// <summary>Returns the bitmap of <paramref name="rgba"/> (straight alpha), filling it when the content changed.</summary>
    public WriteableBitmap Get(byte[] rgba, int pixelWidth, int pixelHeight, int version)
    {
        ArgumentNullException.ThrowIfNull(rgba);

        var size = new PixelSize(pixelWidth, pixelHeight);
        _entries.TryGetValue(rgba, out Entry? entry);
        if (entry is not null && entry.Version == version && entry.Bitmap.PixelSize == size)
        {
            return entry.Bitmap;
        }

        WriteableBitmap bitmap = entry is not null && entry.Bitmap.PixelSize == size
            ? entry.Bitmap
            : new WriteableBitmap(size, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

        // Avalonia draws premultiplied alpha; the buffer is RGBA, the bitmap BGRA.
        byte[] pixels = MinimapSampleConversion.PremultiplyRgba(rgba);
        for (int i = 0; i + 3 < pixels.Length; i += 4)
        {
            (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
        }

        using (ILockedFramebuffer frame = bitmap.Lock())
        {
            int rowLength = pixelWidth * 4;
            for (int row = 0; row < pixelHeight; row++)
            {
                Marshal.Copy(pixels, row * rowLength, frame.Address + row * frame.RowBytes, rowLength);
            }
        }

        UploadCount++;
        _entries.AddOrUpdate(rgba, new Entry(bitmap, version));
        return bitmap;
    }

    private sealed record Entry(WriteableBitmap Bitmap, int Version);
}

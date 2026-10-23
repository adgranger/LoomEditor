using System.Runtime.CompilerServices;
using LoomEditor.View.Minimap;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.DirectX;

namespace LoomEditor.Rendering.Win2D;

/// <summary>
/// Keeps the <see cref="CanvasBitmap"/> made from a pixel buffer until the buffer changes, so the minimap is uploaded
/// to the GPU only when it was repainted.
/// </summary>
/// <remarks>
/// shortcut: bitmaps are released by the garbage collector when their buffer is, not disposed explicitly; dispose them
/// here if bitmap memory ever shows up in a profile.
/// Threading: UI thread only.
/// </remarks>
internal sealed class Win2DBitmapCache
{
    private readonly ConditionalWeakTable<byte[], Entry> _entries = [];

    /// <summary>Returns the bitmap of <paramref name="rgba"/> (straight alpha), creating it when the content or the device changed.</summary>
    public CanvasBitmap Get(CanvasDevice device, byte[] rgba, int pixelWidth, int pixelHeight, int version)
    {
        if (_entries.TryGetValue(rgba, out Entry? entry) && entry.Version == version && ReferenceEquals(entry.Device, device))
        {
            return entry.Bitmap;
        }

        // Win2D draws premultiplied alpha.
        CanvasBitmap bitmap = CanvasBitmap.CreateFromBytes(
            device, MinimapSampleConversion.PremultiplyRgba(rgba), pixelWidth, pixelHeight,
            DirectXPixelFormat.R8G8B8A8UIntNormalized, 96, CanvasAlphaMode.Premultiplied);
        _entries.AddOrUpdate(rgba, new Entry(bitmap, device, version));
        return bitmap;
    }

    private sealed record Entry(CanvasBitmap Bitmap, CanvasDevice Device, int Version);
}

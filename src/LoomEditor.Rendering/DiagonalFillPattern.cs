using LoomEditor.Themes;

namespace LoomEditor.Rendering;

/// <summary>
/// The hatching of the view zones that stand for "no line here" (Monaco: <c>.diagonal-fill</c>), as cached RGBA buffers.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DiagonalFillPattern
{
    /// <summary>The number of buffers kept; the least recently used one is dropped beyond it.</summary>
    public const int Capacity = 8;

    private const int TileSize = 8;

    private readonly List<Entry> _entries = [];
    private int _versionCounter;

    /// <summary>The buffer for a size and a color; the same instance until it is evicted.</summary>
    public Entry Get(int width, int height, ThemeColor color)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry.Width == width && entry.Height == height && entry.Color == color)
            {
                // Most recently used goes last.
                _entries.RemoveAt(i);
                _entries.Add(entry);
                return entry;
            }
        }

        if (_entries.Count == Capacity)
        {
            _entries.RemoveAt(0);
        }

        var created = new Entry(width, height, color, Create(width, height, color), ++_versionCounter);
        _entries.Add(created);
        return created;
    }

    // CSS `linear-gradient(-45deg, C 12.5%, transparent 12.5%, transparent 50%, C 50%, C 62.5%, transparent 62.5%)` on an
    // 8x8 tile. At -45 degrees the gradient of a square tile spans x + y from 0 to 16, so t = (x + y) / 16 and the stripes
    // (t < 0.125 and 0.5 <= t < 0.625) are x + y in {0, 1} and {8, 9}, i.e. (x + y) mod 8 in {0, 1}: 2 px wide, one every 8.
    // Pixels of equal x + y run from bottom-left to top-right, which gives the "/" hatch of Monaco.
    private static byte[] Create(int width, int height, ThemeColor color)
    {
        var rgba = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int phase = (x + y) % TileSize;
                if (phase is 0 or 1)
                {
                    int offset = ((y * width) + x) * 4;
                    rgba[offset] = color.R;
                    rgba[offset + 1] = color.G;
                    rgba[offset + 2] = color.B;
                    rgba[offset + 3] = color.A;
                }
            }
        }

        return rgba;
    }

    /// <summary>A cached buffer.</summary>
    /// <param name="Width">Width in pixels.</param>
    /// <param name="Height">Height in pixels.</param>
    /// <param name="Color">The stripe color.</param>
    /// <param name="Rgba">The pixels (straight alpha).</param>
    /// <param name="Version">Unique per buffer, for <see cref="IRenderSurface.DrawRgbaImage"/>.</param>
    public sealed record Entry(int Width, int Height, ThemeColor Color, byte[] Rgba, int Version);
}

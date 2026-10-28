// Ported from microsoft/vscode: src/vs/editor/common/viewModel/overviewZoneManager.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;

namespace LoomEditor.View.Layout;

/// <summary>A band of an overview ruler, in canvas pixels, with the id of its color (Monaco: <c>ColorZone</c>).</summary>
/// <param name="From">The first pixel.</param>
/// <param name="To">The last pixel.</param>
/// <param name="ColorId">The id of the color in <see cref="OverviewZoneManager.GetId2Color"/>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record ColorZone(int From, int To, int ColorId)
{
    /// <summary>Orders by color id, then <see cref="From"/>, then <see cref="To"/>.</summary>
    public static int Compare(ColorZone a, ColorZone b)
    {
        if (a.ColorId == b.ColorId)
        {
            return a.From == b.From ? a.To - b.To : a.From - b.From;
        }

        return a.ColorId - b.ColorId;
    }
}

/// <summary>A zone of an overview ruler: a line range and a color (Monaco: <c>OverviewRulerZone</c>).</summary>
/// <remarks>Threading: UI thread only; the resolved color zone is cached in the instance.</remarks>
internal sealed class OverviewRulerZone
{
    /// <summary>Creates a zone.</summary>
    /// <param name="startLineNumber">The first line.</param>
    /// <param name="endLineNumber">The last line.</param>
    /// <param name="heightInLines">When 0, the height is computed from <paramref name="endLineNumber"/>.</param>
    /// <param name="color">The color of the band.</param>
    public OverviewRulerZone(int startLineNumber, int endLineNumber, int heightInLines, ThemeColor color)
    {
        StartLineNumber = startLineNumber;
        EndLineNumber = endLineNumber;
        HeightInLines = heightInLines;
        Color = color;
    }

    /// <summary>The first line.</summary>
    public int StartLineNumber { get; }

    /// <summary>The last line.</summary>
    public int EndLineNumber { get; }

    /// <summary>When 0, the height is computed from <see cref="EndLineNumber"/>.</summary>
    public int HeightInLines { get; }

    /// <summary>The color of the band.</summary>
    public ThemeColor Color { get; }

    /// <summary>The band computed by the last <see cref="OverviewZoneManager.ResolveColorZones"/>.</summary>
    public ColorZone? ColorZone { get; set; }

    /// <summary>
    /// Orders by color, start line, height in lines, end line. Deliberate difference: Monaco compares the color strings; this compares the
    /// packed ARGB value. Any total order gives the same drawing because the color zones are sorted by color id afterwards.
    /// </summary>
    public static int Compare(OverviewRulerZone a, OverviewRulerZone b)
    {
        if (a.Color == b.Color)
        {
            if (a.StartLineNumber == b.StartLineNumber)
            {
                return a.HeightInLines == b.HeightInLines ? a.EndLineNumber - b.EndLineNumber : a.HeightInLines - b.HeightInLines;
            }

            return a.StartLineNumber - b.StartLineNumber;
        }

        return Pack(a.Color) < Pack(b.Color) ? -1 : 1;
    }

    private static uint Pack(ThemeColor c) => ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
}

/// <summary>Turns overview ruler zones into sorted color bands for a canvas of any height (Monaco: <c>OverviewZoneManager</c>).</summary>
/// <remarks>Threading: UI thread only; not thread-safe.</remarks>
internal sealed class OverviewZoneManager
{
    private const int MinimumHeight = 4;

    private readonly Func<int, int> _getVerticalOffsetForLine;
    private readonly Dictionary<ThemeColor, int> _color2Id = [];
    private readonly List<ThemeColor> _id2Color = [default]; // index 0 is unused: color ids start at 1
    private List<OverviewRulerZone> _zones = [];
    private bool _colorZonesInvalid;
    private int _lineHeight;
    private int _domWidth;
    private int _domHeight;
    private int _outerHeight;
    private double _pixelRatio = 1;
    private int _lastAssignedId;

    /// <summary>Creates a manager.</summary>
    /// <param name="getVerticalOffsetForLine">The top of a line, in pixels (a view layout callback).</param>
    public OverviewZoneManager(Func<int, int> getVerticalOffsetForLine)
    {
        _getVerticalOffsetForLine = getVerticalOffsetForLine;
    }

    /// <summary>The width of the canvas, in pixels.</summary>
    public int CanvasWidth => (int)(_domWidth * _pixelRatio);

    /// <summary>The height of the canvas, in pixels.</summary>
    public int CanvasHeight => (int)(_domHeight * _pixelRatio);

    /// <summary>The color of each color id (index 0 is unused).</summary>
    public IReadOnlyList<ThemeColor> GetId2Color() => _id2Color;

    /// <summary>Replaces the zones; a sorted copy is kept.</summary>
    public void SetZones(IEnumerable<OverviewRulerZone> newZones)
    {
        _zones = [.. newZones];
        _zones.Sort(OverviewRulerZone.Compare);
    }

    /// <summary>Sets the line height; false when unchanged.</summary>
    public bool SetLineHeight(int lineHeight) => Set(ref _lineHeight, lineHeight);

    /// <summary>Sets the DPI scale.</summary>
    public void SetPixelRatio(double pixelRatio)
    {
        _pixelRatio = pixelRatio;
        _colorZonesInvalid = true;
    }

    /// <summary>Sets the width of the ruler, in DIPs; false when unchanged.</summary>
    public bool SetDomWidth(int width) => Set(ref _domWidth, width);

    /// <summary>Sets the height of the ruler, in DIPs; false when unchanged.</summary>
    public bool SetDomHeight(int height) => Set(ref _domHeight, height);

    /// <summary>Sets the height of the whole scrollable content, in pixels; false when unchanged.</summary>
    public bool SetOuterHeight(int outerHeight) => Set(ref _outerHeight, outerHeight);

    private bool Set(ref int field, int value)
    {
        if (field == value)
        {
            return false;
        }

        field = value;
        _colorZonesInvalid = true;
        return true;
    }

    /// <summary>The bands of all the zones, sorted by <see cref="ColorZone.Compare"/>; cached per zone until something invalidates them.</summary>
    public IReadOnlyList<ColorZone> ResolveColorZones()
    {
        bool colorZonesInvalid = _colorZonesInvalid;
        int lineHeight = _lineHeight;
        int totalHeight = CanvasHeight;
        double heightRatio = (double)totalHeight / _outerHeight;
        int halfMinimumHeight = (int)Math.Floor(MinimumHeight * _pixelRatio / 2);

        List<ColorZone> all = [];
        foreach (OverviewRulerZone zone in _zones)
        {
            if (!colorZonesInvalid && zone.ColorZone is { } cached)
            {
                all.Add(cached);
                continue;
            }

            int offset1 = _getVerticalOffsetForLine(zone.StartLineNumber);
            int offset2 = zone.HeightInLines == 0
                ? _getVerticalOffsetForLine(zone.EndLineNumber) + lineHeight
                : offset1 + (zone.HeightInLines * lineHeight);

            int y1 = (int)Math.Floor(heightRatio * offset1);
            int y2 = (int)Math.Floor(heightRatio * offset2);

            int ycenter = (int)Math.Floor((y1 + y2) / 2.0);
            int halfHeight = y2 - ycenter;
            if (halfHeight < halfMinimumHeight)
            {
                halfHeight = halfMinimumHeight;
            }

            if (ycenter - halfHeight < 0)
            {
                ycenter = halfHeight;
            }

            if (ycenter + halfHeight > totalHeight)
            {
                ycenter = totalHeight - halfHeight;
            }

            if (!_color2Id.TryGetValue(zone.Color, out int colorId))
            {
                colorId = ++_lastAssignedId;
                _color2Id[zone.Color] = colorId;
                _id2Color.Add(zone.Color);
            }

            var colorZone = new ColorZone(ycenter - halfHeight, ycenter + halfHeight, colorId);
            zone.ColorZone = colorZone;
            all.Add(colorZone);
        }

        _colorZonesInvalid = false;
        all.Sort(ColorZone.Compare);
        return all;
    }
}

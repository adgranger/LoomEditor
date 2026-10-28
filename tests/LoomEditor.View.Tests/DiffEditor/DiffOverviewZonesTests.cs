using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

// New tests: Monaco's overview ruler feature is tested through the whole widget.
public class DiffOverviewZonesTests
{
    private static readonly ThemeColor Red = ThemeColor.FromRgb(255, 0, 0);

    [Fact]
    public void Create_SkipsEmptyRanges()
    {
        var zones = DiffOverviewZones.Create([new LineRange(3, 3), new LineRange(5, 7)], line => line, Red);

        Assert.Equal(5, Assert.Single(zones).StartLineNumber);
    }

    [Fact]
    public void Create_MapsModelToViewLines()
    {
        var zones = DiffOverviewZones.Create([new LineRange(5, 7)], line => line * 2, Red);

        var zone = Assert.Single(zones);
        Assert.Equal((10, 14), (zone.StartLineNumber, zone.EndLineNumber));
    }

    [Fact]
    public void Create_HeightInLinesIsViewLineDelta()
    {
        // A wrapped line takes two view lines: the model range [5, 7) spans three view lines.
        var zones = DiffOverviewZones.Create([new LineRange(5, 7)], line => line == 7 ? 9 : 6, Red);

        Assert.Equal(3, Assert.Single(zones).HeightInLines);
    }

    [Fact]
    public void GetColors_ThemeValues_Used_AlphaDoubled()
    {
        var theme = new Dictionary<string, ThemeColor>
        {
            ["diffEditor.insertedTextBackground"] = new(51, 1, 2, 3),
            ["diffEditorOverview.removedForeground"] = new(40, 4, 5, 6),
        };

        var (insert, remove) = DiffOverviewZones.GetColors(id => theme.TryGetValue(id, out ThemeColor c) ? c : null);

        Assert.Equal(new ThemeColor(102, 1, 2, 3), insert);
        // The overview color is used as it is.
        Assert.Equal(new ThemeColor(40, 4, 5, 6), remove);
    }

    [Fact]
    public void GetColors_Defaults_WhenThemeHasNone()
    {
        var (insert, remove) = DiffOverviewZones.GetColors(_ => null);

        Assert.Equal(new ThemeColor(102, 155, 185, 85), insert);
        Assert.Equal(new ThemeColor(102, 255, 0, 0), remove);
    }

    [Fact]
    public void GetColors_AlphaCappedAtOne()
    {
        var (insert, _) = DiffOverviewZones.GetColors(id => id == "diffEditor.insertedTextBackground" ? new ThemeColor(200, 1, 2, 3) : null);

        Assert.Equal(255, insert.A);
    }
}

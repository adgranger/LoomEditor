using LoomEditor.Avalonia.Rendering;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Tokens;
using LoomEditor.Rendering;

namespace LoomEditor.Avalonia.Tests.Rendering;

// The twin of the Win2D measurer: no upstream test, all tests are new.
public class AvaloniaTextMeasurerTests
{
    [Fact]
    public async Task FontInfo_TestFont_MatchesMeasuredSpaceWidth()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer();
            using ILineLayout space = measurer.CreateLineLayout(" ", LineStyle.Plain);

            Assert.Equal(TestFont.Family, measurer.FontInfo.FontFamily);
            Assert.True(measurer.FontInfo.IsMonospace);
            Assert.True(measurer.FontInfo.SpaceWidth > 0);
            Assert.Equal(measurer.FontInfo.SpaceWidth, space.Width, 3);
            Assert.Equal(19, measurer.FontInfo.LineHeight);
        });
    }

    [Fact]
    public async Task Bold_UsesWeight700AtLeast()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer();
            using var regular = (AvaloniaLineLayout)measurer.CreateLineLayout("m", LineStyle.Plain);
            using var bold = (AvaloniaLineLayout)measurer.CreateLineLayout("m", TestFont.Style(1, FontStyle.Bold));

            Assert.True(bold.Width >= regular.Width, $"bold {bold.Width} < regular {regular.Width}");
            Assert.True((int)bold.TextLayout.TextLines[0].TextRuns[0].Properties!.Typeface.Weight >= 700);
        });
    }

    [Fact]
    public async Task UnknownFamilies_FallBackToAnInstalledFamily()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using var measurer = new AvaloniaTextMeasurer("No Such Font, 'Nor This One'", 14, 4, 1, View.Layout.EditorFontOptions.Default);

            Assert.DoesNotContain("No Such Font", measurer.FontInfo.FontFamily, StringComparison.Ordinal);
            Assert.NotEmpty(measurer.FontInfo.FontFamily);
            Assert.True(measurer.FontInfo.SpaceWidth > 0);
        });
    }
}

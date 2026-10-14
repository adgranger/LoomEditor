using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no common test for fontMeasurements.ts (it needs a browser): all tests are new.
public class FontMeasurementsTests
{
    private static readonly BareFontInfo Bare = BareFontInfo.Create("Consolas", "normal", 14, 0, 0, 1);

    [Fact]
    public void Read_MonospaceReader_IsMonospace()
    {
        FontInfo info = FontMeasurements.Read(Bare, FakeCharacterWidthReader.Monospace(), 1);

        Assert.True(info.IsMonospace);
        Assert.Equal(8.4, info.TypicalHalfwidthCharacterWidth);
        Assert.Equal(16.8, info.TypicalFullwidthCharacterWidth);
        Assert.Equal(8.4, info.MaxDigitWidth);
        Assert.Equal(8.4, info.SpaceWidth);
        Assert.Equal(19, info.LineHeight);
    }

    [Fact]
    public void Read_ProportionalReader_IsNotMonospaceAndMaxDigitIsWidest()
    {
        var reader = new FakeCharacterWidthReader((c, _) => c switch
        {
            "i" => 4,
            "m" => 12,
            "7" => 9.5,
            "ｍ" => 16,
            _ => 8,
        });

        FontInfo info = FontMeasurements.Read(Bare, reader, 1);

        Assert.False(info.IsMonospace);
        Assert.Equal(9.5, info.MaxDigitWidth);
    }

    [Fact]
    public void Read_TinyWidth_FallsBackToFive()
    {
        var reader = new FakeCharacterWidthReader((c, _) => c == "n" ? 1 : 8.4);

        FontInfo info = FontMeasurements.Read(Bare, reader, 1);

        Assert.Equal(5, info.TypicalHalfwidthCharacterWidth);
        Assert.Equal(8.4, info.SpaceWidth);
    }

    [Fact]
    public void Read_HalfwidthArrowWiderThanArrow_CannotUseHalfwidthArrow()
    {
        var reader = new FakeCharacterWidthReader((c, _) => c == "￫" ? 20 : 8.4);

        FontInfo info = FontMeasurements.Read(Bare, reader, 1);

        Assert.False(info.CanUseHalfwidthRightwardsArrow);
    }

    [Fact]
    public void Read_MonospaceReader_CanUseHalfwidthArrow()
    {
        FontInfo info = FontMeasurements.Read(Bare, FakeCharacterWidthReader.Monospace(), 1);

        Assert.True(info.CanUseHalfwidthRightwardsArrow);
    }

    [Fact]
    public void Read_MeasuresMonacoCharactersInOrder()
    {
        var reader = FakeCharacterWidthReader.Monospace();

        FontMeasurements.Read(Bare, reader, 1);

        Assert.Equal(("n", CharacterStyle.Regular), reader.Requests[0]);
        Assert.Equal(("ｍ", CharacterStyle.Regular), reader.Requests[1]);
        Assert.Equal((" ", CharacterStyle.Regular), reader.Requests[2]);
        Assert.Equal(("|", CharacterStyle.Regular), reader.Requests[17]);
        Assert.Equal(("|", CharacterStyle.Italic), reader.Requests[18]);
        Assert.Equal(("|", CharacterStyle.Bold), reader.Requests[19]);
        Assert.Equal(17 + 8 * 3, reader.Requests.Count);
    }

    [Fact]
    public void Equals_SameMeasurements_AreEqual()
    {
        FontInfo a = FontMeasurements.Read(Bare, FakeCharacterWidthReader.Monospace(), 1);
        FontInfo b = FontMeasurements.Read(Bare, FakeCharacterWidthReader.Monospace(), 1);

        Assert.Equal(a, b);
    }
}

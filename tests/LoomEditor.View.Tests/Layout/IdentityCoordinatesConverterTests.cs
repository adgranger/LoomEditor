using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no test for IdentityCoordinatesConverter: all tests are new.
public class IdentityCoordinatesConverterTests
{
    private static IdentityCoordinatesConverter Create(string text = "abc\ndef\nxy") => new(new TextModel(text));

    [Fact]
    public void ConvertViewPositionToModelPosition_OutsideTheText_IsClampedToTheLastPosition()
    {
        var converter = Create();

        Assert.Equal(new Position(3, 3), converter.ConvertViewPositionToModelPosition(new Position(10, 50)));
        Assert.Equal(new Position(2, 4), converter.ConvertModelPositionToViewPosition(new Position(2, 50)));
    }

    [Fact]
    public void ConvertRanges_OutsideTheText_AreClamped()
    {
        var converter = Create();

        Assert.Equal(new Range(1, 2, 3, 3), converter.ConvertViewRangeToModelRange(new Range(1, 2, 9, 9)));
        Assert.Equal(new Range(1, 2, 3, 3), converter.ConvertModelRangeToViewRange(new Range(1, 2, 9, 9)));
    }

    [Fact]
    public void ValidateViewPosition_IgnoresTheViewPosition()
    {
        var converter = Create();

        Position result = converter.ValidateViewPosition(new Position(1, 1), new Position(2, 2));

        Assert.Equal(new Position(2, 2), result);
    }

    [Fact]
    public void ValidateViewRange_ValidatesTheExpectedModelRange()
    {
        var converter = Create();

        Range result = converter.ValidateViewRange(new Range(1, 1, 1, 1), new Range(2, 1, 2, 99));

        Assert.Equal(new Range(2, 1, 2, 4), result);
    }

    [Fact]
    public void ModelPositionIsVisible_LineAfterTheLastLine_IsFalse()
    {
        var converter = Create();

        Assert.True(converter.ModelPositionIsVisible(new Position(3, 1)));
        Assert.False(converter.ModelPositionIsVisible(new Position(4, 1)));
    }

    [Fact]
    public void ModelRangeIsVisible_RangeEndingAfterTheLastLine_IsFalse()
    {
        var converter = Create();

        Assert.True(converter.ModelRangeIsVisible(new Range(1, 1, 3, 1)));
        Assert.False(converter.ModelRangeIsVisible(new Range(1, 1, 4, 1)));
    }

    [Fact]
    public void LineCounts_EveryModelLineIsOneViewLine()
    {
        var converter = Create();

        Assert.Equal(1, converter.GetModelLineViewLineCount(2));
        Assert.Equal(2, converter.GetViewLineNumberOfModelPosition(2, 3));
    }
}

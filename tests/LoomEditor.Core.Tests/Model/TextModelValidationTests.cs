using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Ported from Monaco <c>textModel.test.ts</c>, suite <c>Editor Model - TextModel</c>: position and range validation,
/// <c>modifyPosition</c> and the out-of-bounds assertions of <c>getValueLengthInRange</c>.
/// </summary>
/// <remarks>
/// Not applicable in C# (<see cref="int"/> cannot be NaN or a float): <c>validatePosition handle NaN.</c>,
/// <c>issue #71480: validatePosition handle floats</c>, <c>issue #71480: validateRange handle floats</c>.
/// Monaco builds <c>new Position(0, 0)</c>, which <see cref="Position"/> forbids: those tests use the <see cref="int"/> overloads.
/// </remarks>
public class TextModelValidationTests
{
    private static void AssertPosition(TextModel m, int line, int column, int expectedLine, int expectedColumn)
    {
        Assert.Equal(new Position(expectedLine, expectedColumn), m.ValidatePosition(line, column));
    }

    private static void AssertRange(TextModel m, (int, int, int, int) input, (int, int, int, int) expected)
    {
        Assert.Equal(
            new Range(expected.Item1, expected.Item2, expected.Item3, expected.Item4),
            m.ValidateRange(input.Item1, input.Item2, input.Item3, input.Item4));
    }

    // Monaco: 'validatePosition'
    [Fact]
    public void ValidatePosition_OutOfBounds_ReturnsNearestValidPosition()
    {
        TextModel m = TestTextModels.Create("line one\nline two");

        AssertPosition(m, 0, 0, 1, 1);
        AssertPosition(m, 0, 1, 1, 1);

        AssertPosition(m, 1, 1, 1, 1);
        AssertPosition(m, 1, 2, 1, 2);
        AssertPosition(m, 1, 30, 1, 9);

        AssertPosition(m, 2, 0, 2, 1);
        AssertPosition(m, 2, 1, 2, 1);
        AssertPosition(m, 2, 2, 2, 2);
        AssertPosition(m, 2, 30, 2, 9);

        AssertPosition(m, 3, 0, 2, 9);
        AssertPosition(m, 3, 1, 2, 9);
        AssertPosition(m, 3, 30, 2, 9);

        AssertPosition(m, 30, 30, 2, 9);

        // Monaco: new Position(-123.123, -0.5) and Number.MIN_VALUE (adapted to int)
        AssertPosition(m, -123, 0, 1, 1);
        AssertPosition(m, int.MinValue, int.MinValue, 1, 1);

        // Monaco: Number.MAX_VALUE and new Position(123.23, 47.5) (adapted to int)
        AssertPosition(m, int.MaxValue, int.MaxValue, 2, 9);
        AssertPosition(m, 123, 47, 2, 9);
    }

    // Monaco: 'validatePosition around high-low surrogate pairs 1'
    [Fact]
    public void ValidatePosition_SurrogatePairs1()
    {
        TextModel m = TestTextModels.Create("a📚b");

        AssertPosition(m, 0, 0, 1, 1);
        AssertPosition(m, 0, 1, 1, 1);
        AssertPosition(m, 0, 7, 1, 1);

        AssertPosition(m, 1, 1, 1, 1);
        AssertPosition(m, 1, 2, 1, 2);
        AssertPosition(m, 1, 3, 1, 2);
        AssertPosition(m, 1, 4, 1, 4);
        AssertPosition(m, 1, 5, 1, 5);
        AssertPosition(m, 1, 30, 1, 5);

        AssertPosition(m, 2, 0, 1, 5);
        AssertPosition(m, 2, 1, 1, 5);
        AssertPosition(m, 2, 2, 1, 5);
        AssertPosition(m, 2, 30, 1, 5);

        AssertPosition(m, -123, 0, 1, 1);
        AssertPosition(m, int.MinValue, int.MinValue, 1, 1);

        AssertPosition(m, int.MaxValue, int.MaxValue, 1, 5);
        AssertPosition(m, 123, 47, 1, 5);
    }

    // Monaco: 'validatePosition around high-low surrogate pairs 2'
    [Fact]
    public void ValidatePosition_SurrogatePairs2()
    {
        TextModel m = TestTextModels.Create("a📚📚b");

        AssertPosition(m, 1, 1, 1, 1);
        AssertPosition(m, 1, 2, 1, 2);
        AssertPosition(m, 1, 3, 1, 2);
        AssertPosition(m, 1, 4, 1, 4);
        AssertPosition(m, 1, 5, 1, 4);
        AssertPosition(m, 1, 6, 1, 6);
        AssertPosition(m, 1, 7, 1, 7);
    }

    // Monaco: 'validateRange around high-low surrogate pairs 1'
    [Fact]
    public void ValidateRange_SurrogatePairs1()
    {
        TextModel m = TestTextModels.Create("a📚b");

        AssertRange(m, (0, 0, 0, 1), (1, 1, 1, 1));
        AssertRange(m, (0, 0, 0, 7), (1, 1, 1, 1));

        AssertRange(m, (1, 1, 1, 1), (1, 1, 1, 1));
        AssertRange(m, (1, 1, 1, 2), (1, 1, 1, 2));
        AssertRange(m, (1, 1, 1, 3), (1, 1, 1, 4));
        AssertRange(m, (1, 1, 1, 4), (1, 1, 1, 4));
        AssertRange(m, (1, 1, 1, 5), (1, 1, 1, 5));

        AssertRange(m, (1, 2, 1, 2), (1, 2, 1, 2));
        AssertRange(m, (1, 2, 1, 3), (1, 2, 1, 4));
        AssertRange(m, (1, 2, 1, 4), (1, 2, 1, 4));
        AssertRange(m, (1, 2, 1, 5), (1, 2, 1, 5));

        AssertRange(m, (1, 3, 1, 3), (1, 2, 1, 2));
        AssertRange(m, (1, 3, 1, 4), (1, 2, 1, 4));
        AssertRange(m, (1, 3, 1, 5), (1, 2, 1, 5));

        AssertRange(m, (1, 4, 1, 4), (1, 4, 1, 4));
        AssertRange(m, (1, 4, 1, 5), (1, 4, 1, 5));

        AssertRange(m, (1, 5, 1, 5), (1, 5, 1, 5));
    }

    // Monaco: 'validateRange around high-low surrogate pairs 2'
    [Fact]
    public void ValidateRange_SurrogatePairs2()
    {
        TextModel m = TestTextModels.Create("a📚📚b");

        AssertRange(m, (0, 0, 0, 1), (1, 1, 1, 1));
        AssertRange(m, (0, 0, 0, 7), (1, 1, 1, 1));

        AssertRange(m, (1, 1, 1, 1), (1, 1, 1, 1));
        AssertRange(m, (1, 1, 1, 2), (1, 1, 1, 2));
        AssertRange(m, (1, 1, 1, 3), (1, 1, 1, 4));
        AssertRange(m, (1, 1, 1, 4), (1, 1, 1, 4));
        AssertRange(m, (1, 1, 1, 5), (1, 1, 1, 6));
        AssertRange(m, (1, 1, 1, 6), (1, 1, 1, 6));
        AssertRange(m, (1, 1, 1, 7), (1, 1, 1, 7));

        AssertRange(m, (1, 2, 1, 2), (1, 2, 1, 2));
        AssertRange(m, (1, 2, 1, 3), (1, 2, 1, 4));
        AssertRange(m, (1, 2, 1, 4), (1, 2, 1, 4));
        AssertRange(m, (1, 2, 1, 5), (1, 2, 1, 6));
        AssertRange(m, (1, 2, 1, 6), (1, 2, 1, 6));
        AssertRange(m, (1, 2, 1, 7), (1, 2, 1, 7));

        AssertRange(m, (1, 3, 1, 3), (1, 2, 1, 2));
        AssertRange(m, (1, 3, 1, 4), (1, 2, 1, 4));
        AssertRange(m, (1, 3, 1, 5), (1, 2, 1, 6));
        AssertRange(m, (1, 3, 1, 6), (1, 2, 1, 6));
        AssertRange(m, (1, 3, 1, 7), (1, 2, 1, 7));

        AssertRange(m, (1, 4, 1, 4), (1, 4, 1, 4));
        AssertRange(m, (1, 4, 1, 5), (1, 4, 1, 6));
        AssertRange(m, (1, 4, 1, 6), (1, 4, 1, 6));
        AssertRange(m, (1, 4, 1, 7), (1, 4, 1, 7));

        AssertRange(m, (1, 5, 1, 5), (1, 4, 1, 4));
        AssertRange(m, (1, 5, 1, 6), (1, 4, 1, 6));
        AssertRange(m, (1, 5, 1, 7), (1, 4, 1, 7));

        AssertRange(m, (1, 6, 1, 6), (1, 6, 1, 6));
        AssertRange(m, (1, 6, 1, 7), (1, 6, 1, 7));

        AssertRange(m, (1, 7, 1, 7), (1, 7, 1, 7));
    }

    // Monaco: 'modifyPosition'
    [Fact]
    public void ModifyPosition_ClampsToTheText()
    {
        TextModel m = TestTextModels.Create("line one\nline two");

        Assert.Equal(new Position(1, 1), m.ModifyPosition(new Position(1, 1), 0));
        Assert.Equal(new Position(1, 1), m.ModifyPosition(m.ValidatePosition(0, 0), 0));
        Assert.Equal(new Position(2, 9), m.ModifyPosition(m.ValidatePosition(30, 1), 0));

        Assert.Equal(new Position(2, 9), m.ModifyPosition(new Position(1, 1), 17));
        Assert.Equal(new Position(1, 2), m.ModifyPosition(new Position(1, 1), 1));
        Assert.Equal(new Position(1, 4), m.ModifyPosition(new Position(1, 1), 3));
        Assert.Equal(new Position(2, 3), m.ModifyPosition(new Position(1, 2), 10));
        Assert.Equal(new Position(2, 9), m.ModifyPosition(new Position(1, 5), 13));
        Assert.Equal(new Position(2, 9), m.ModifyPosition(new Position(1, 2), 16));

        Assert.Equal(new Position(1, 1), m.ModifyPosition(new Position(2, 9), -17));
        Assert.Equal(new Position(1, 1), m.ModifyPosition(new Position(1, 2), -1));
        Assert.Equal(new Position(1, 1), m.ModifyPosition(new Position(1, 4), -3));
        Assert.Equal(new Position(1, 2), m.ModifyPosition(new Position(2, 3), -10));
        Assert.Equal(new Position(1, 5), m.ModifyPosition(new Position(2, 9), -13));
        Assert.Equal(new Position(1, 2), m.ModifyPosition(new Position(2, 9), -16));

        Assert.Equal(new Position(2, 9), m.ModifyPosition(new Position(1, 2), 17));
        Assert.Equal(new Position(2, 9), m.ModifyPosition(new Position(1, 2), 100));

        Assert.Equal(new Position(1, 1), m.ModifyPosition(new Position(1, 2), -2));
        Assert.Equal(new Position(1, 1), m.ModifyPosition(new Position(1, 2), -100));
        Assert.Equal(new Position(1, 1), m.ModifyPosition(new Position(2, 2), -100));
        Assert.Equal(new Position(1, 1), m.ModifyPosition(new Position(2, 9), -18));
    }

    // Monaco: 'getValueLengthInRange' (out-of-bounds assertions)
    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    public void GetValueLengthInRange_OutOfBoundsRanges(string eol)
    {
        TextModel m = TestTextModels.Create($"My First Line{eol}My Second Line{eol}My Third Line");

        Assert.Equal($"y First Line{eol}My Second Line".Length, m.GetValueLengthInRange(new Range(1, 2, 2, 1000)));
        Assert.Equal($"y First Line{eol}My Second Line{eol}My Third Line".Length, m.GetValueLengthInRange(new Range(1, 2, 3, 1000)));
        Assert.Equal($"My First Line{eol}My Second Line{eol}My Third Line".Length, m.GetValueLengthInRange(new Range(1, 1, 1000, 1000)));
    }

    // Monaco: 'getValueLengthInRange different EOL' (out-of-bounds assertions)
    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    public void GetValueLengthInRange_DifferentEol_OutOfBoundsRanges(string eol)
    {
        TextModel m = TestTextModels.Create($"My First Line{eol}My Second Line{eol}My Third Line");
        var all = new Range(1, 1, 1000, 1000);

        Assert.Equal($"My First Line{eol}My Second Line{eol}My Third Line".Length, m.GetValueLengthInRange(all, EndOfLinePreference.TextDefined));
        Assert.Equal("My First Line\r\nMy Second Line\r\nMy Third Line".Length, m.GetValueLengthInRange(all, EndOfLinePreference.CRLF));
        Assert.Equal("My First Line\nMy Second Line\nMy Third Line".Length, m.GetValueLengthInRange(all, EndOfLinePreference.LF));
    }

    // Monaco: '#50471. getValueInRange with invalid range' (adapted: NaN does not exist, use out-of-bounds values)
    [Fact]
    public void GetValueInRange_InvalidRange_IsValidated()
    {
        TextModel m = TestTextModels.Create("My First Line\r\nMy Second Line\r\nMy Third Line");

        // Monaco's NaN column becomes 1 ("My"); an oversized column becomes the end of the line, and the reversed range is swapped.
        Assert.Equal(" First Line", m.GetValueInRange(m.ValidateRange(1, int.MaxValue, 1, 3)));
        Assert.Equal("My", m.GetValueInRange(m.ValidateRange(1, 0, 1, 3)));
        Assert.Equal("", m.GetValueInRange(m.ValidateRange(0, 0, 0, 0)));
        Assert.Equal("", m.GetValueInRange(m.ValidateRange(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)));
    }

    [Fact]
    public void GetValueInRange_RangeInsideSurrogatePair_IsExpanded()
    {
        TextModel m = TestTextModels.Create("a📚b");

        Assert.Equal("📚", m.GetValueInRange(new Range(1, 3, 1, 4)));
        Assert.Equal("", m.GetValueInRange(new Range(1, 3, 1, 3)));
    }

    [Fact]
    public void IsValidRange_ChecksBoundsAndSurrogatePairs()
    {
        TextModel m = TestTextModels.Create("a📚b\nxy");

        Assert.True(m.IsValidRange(new Range(1, 1, 2, 3)));
        Assert.False(m.IsValidRange(new Range(1, 1, 2, 4)));
        Assert.False(m.IsValidRange(new Range(1, 1, 3, 1)));
        Assert.False(m.IsValidRange(new Range(1, 3, 1, 4)));
        Assert.False(m.IsValidRange(new Range(1, 1, 1, 3)));
    }

    [Fact]
    public void ValidateRange_ValidRange_ReturnsItUnchanged()
    {
        TextModel m = TestTextModels.Create("line one\nline two");
        var range = new Range(1, 2, 2, 5);

        Assert.Equal(range, m.ValidateRange(range));
        Assert.Equal(new Range(1, 1, 2, 9), m.ValidateRange(new Range(1, 1, 50, 50)));
    }

    [Fact]
    public void ValidateRange_ReversedInput_IsSwapped()
    {
        TextModel m = TestTextModels.Create("line one\nline two");

        Assert.Equal(new Range(1, 3, 2, 4), m.ValidateRange(2, 4, 1, 3));
    }
}

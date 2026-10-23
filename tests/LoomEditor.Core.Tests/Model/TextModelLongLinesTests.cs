using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;

namespace LoomEditor.Core.Tests.Model;

public class TextModelLongLinesTests
{
    private static string Lines(int count, int length) => string.Join('\n', Enumerable.Repeat(new string('a', length), count));

    [Fact]
    public void IsDominatedByLongLines_EmptyModel_ReturnsFalse()
    {
        Assert.False(TestTextModels.Create("").IsDominatedByLongLines());
    }

    [Fact]
    public void IsDominatedByLongLines_ShortLinesOnly_ReturnsFalse()
    {
        Assert.False(TestTextModels.Create("one\ntwo\nthree").IsDominatedByLongLines());
    }

    [Fact]
    public void IsDominatedByLongLines_OneLongLineAndLittleElse_ReturnsTrue()
    {
        Assert.True(TestTextModels.Create(Lines(1, 10_000) + "\n" + Lines(5, 5)).IsDominatedByLongLines());
    }

    [Fact]
    public void IsDominatedByLongLines_LineOfExactlyTheBoundary_CountsAsLong()
    {
        Assert.True(TestTextModels.Create(Lines(1, 10_000)).IsDominatedByLongLines());
        Assert.False(TestTextModels.Create(Lines(1, 9_999)).IsDominatedByLongLines());
    }

    [Fact]
    public void IsDominatedByLongLines_LongAndShortCharactersEqual_ReturnsFalse()
    {
        Assert.False(TestTextModels.Create(Lines(1, 10_000) + "\n" + Lines(10, 1_000)).IsDominatedByLongLines());
    }

    [Fact]
    public void IsDominatedByLongLines_ManyShortCharsOutweighOneLongLine_ReturnsFalse()
    {
        Assert.False(TestTextModels.Create(Lines(1, 10_000) + "\n" + Lines(20, 1_000)).IsDominatedByLongLines());
    }

    [Fact]
    public void IsDominatedByLongLines_TooLargeForTokenization_ReturnsFalse()
    {
        // 300 000 line breaks exceed the line count threshold, while the long line would otherwise dominate.
        TextModel model = TestTextModels.Create(Lines(1, 10_000) + new string('\n', 300_000));

        Assert.True(model.IsTooLargeForTokenization);
        Assert.False(model.IsDominatedByLongLines());
    }
}

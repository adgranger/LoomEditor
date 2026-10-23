using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class MonospaceLineBreaksComputerHelpersTests
{
    [Fact]
    public void Classifier_BreakBeforeAndAfterCharacters_AreClassified()
    {
        var classifier = new WrappingCharacterClassifier("({", ".,");

        Assert.Equal(CharacterClass.BreakBefore, classifier.Get('('));
        Assert.Equal(CharacterClass.BreakAfter, classifier.Get(','));
        Assert.Equal(CharacterClass.None, classifier.Get('a'));
    }

    [Fact]
    public void Classifier_CjkRanges_AreIdeographic()
    {
        var classifier = new WrappingCharacterClassifier("", "");

        Assert.Equal(CharacterClass.BreakIdeographic, classifier.Get(0x3040));
        Assert.Equal(CharacterClass.BreakIdeographic, classifier.Get(0x4E00));
        Assert.Equal(CharacterClass.BreakIdeographic, classifier.Get(0x9FFF));
        Assert.Equal(CharacterClass.None, classifier.Get(0xA000));
    }

    [Theory]
    [InlineData((int)CharacterClass.BreakAfter, (int)CharacterClass.None, 'a', false, true)]
    [InlineData((int)CharacterClass.BreakAfter, (int)CharacterClass.BreakAfter, 'a', false, false)]
    [InlineData((int)CharacterClass.None, (int)CharacterClass.BreakBefore, 'a', false, true)]
    [InlineData((int)CharacterClass.BreakBefore, (int)CharacterClass.BreakBefore, 'a', false, false)]
    [InlineData((int)CharacterClass.BreakIdeographic, (int)CharacterClass.None, 'a', false, true)]
    [InlineData((int)CharacterClass.BreakIdeographic, (int)CharacterClass.None, 'a', true, false)]
    [InlineData((int)CharacterClass.None, (int)CharacterClass.BreakIdeographic, 'a', false, true)]
    [InlineData((int)CharacterClass.None, (int)CharacterClass.BreakIdeographic, 'a', true, false)]
    [InlineData((int)CharacterClass.BreakAfter, (int)CharacterClass.None, ' ', false, false)]
    public void CanBreak_KinsokuShori_Cases(int prev, int current, char charCode, bool keepAll, bool expected) =>
        Assert.Equal(expected, MonospaceLineBreaksComputer.CanBreak('x', (CharacterClass)prev, charCode, (CharacterClass)current, keepAll));

    [Fact]
    public void ComputeCharWidth_Tab_FullWidth_Control_Normal()
    {
        Assert.Equal(3, MonospaceLineBreaksComputer.ComputeCharWidth('\t', 1, 4, 1.5, false));
        Assert.Equal(1.5, MonospaceLineBreaksComputer.ComputeCharWidth('中', 0, 4, 1.5, false));
        Assert.Equal(2, MonospaceLineBreaksComputer.ComputeCharWidth('中', 0, 4, 1.5, true));
        Assert.Equal(1.5, MonospaceLineBreaksComputer.ComputeCharWidth(1, 0, 4, 1.5, false));
        Assert.Equal(1, MonospaceLineBreaksComputer.ComputeCharWidth('a', 0, 4, 1.5, false));
    }

    [Fact]
    public void ComputeCharPixelWidth_TwoCells_UsesTwoHalfwidths()
    {
        FontInfo font = FontInfo.Default with { TypicalHalfwidthCharacterWidth = 10, TypicalFullwidthCharacterWidth = 30 };

        Assert.Equal(20, MonospaceLineBreaksComputer.ComputeCharPixelWidth('中', 0, 4, font, true));
        Assert.Equal(30, MonospaceLineBreaksComputer.ComputeCharPixelWidth('中', 0, 4, font, false));
        Assert.Equal(30, MonospaceLineBreaksComputer.ComputeCharPixelWidth('\t', 1, 4, font, false));
        Assert.Equal(10, MonospaceLineBreaksComputer.ComputeCharPixelWidth('a', 0, 4, font, false));
    }

    [Fact]
    public void IsEscapedLineBreakAtPosition_OddBackslashes_True_EvenFalse()
    {
        Assert.True(MonospaceLineBreaksComputer.IsEscapedLineBreakAtPosition("x\\n", 3));
        Assert.False(MonospaceLineBreaksComputer.IsEscapedLineBreakAtPosition("x\\\\n", 4));
        Assert.False(MonospaceLineBreaksComputer.IsEscapedLineBreakAtPosition("xn", 2));
    }

    [Theory]
    [InlineData((int)WrappingIndent.None, 0)]
    [InlineData((int)WrappingIndent.Same, 6)]
    [InlineData((int)WrappingIndent.Indent, 8)]
    [InlineData((int)WrappingIndent.DeepIndent, 12)]
    public void ComputeWrappedTextIndentLength_None_Same_Indent_DeepIndent(int indent, int expected) =>
        Assert.Equal(expected, MonospaceLineBreaksComputer.ComputeWrappedTextIndentLength("\t  x", 4, 80, 2, (WrappingIndent)indent));

    [Fact]
    public void ComputeWrappedTextIndentLength_IndentTooWide_FallsBackToZero() =>
        Assert.Equal(0, MonospaceLineBreaksComputer.ComputeWrappedTextIndentLength("\t  x", 4, 7, 2, WrappingIndent.Same));
}

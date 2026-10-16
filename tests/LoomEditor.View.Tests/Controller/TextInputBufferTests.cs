using LoomEditor.View.Controller;

namespace LoomEditor.View.Tests.Controller;

public class TextInputBufferTests
{
    [Fact]
    public void Accept_OrdinaryCharacter_ReturnsIt()
    {
        Assert.Equal("a", new TextInputBuffer().Accept('a'));
        Assert.Equal("€", new TextInputBuffer().Accept('€'));
        Assert.Equal(" ", new TextInputBuffer().Accept(' '));
    }

    [Fact]
    public void Accept_CarriageReturn_ReturnsANewLine()
    {
        Assert.Equal("\n", new TextInputBuffer().Accept('\r'));
    }

    [Theory]
    [InlineData('\t')]
    [InlineData('\b')]
    [InlineData('\u0001')]
    [InlineData('\n')]
    [InlineData('\u007F')]
    public void Accept_ControlCharacter_ReturnsNull(char ch)
    {
        Assert.Null(new TextInputBuffer().Accept(ch));
    }

    [Fact]
    public void Accept_SurrogatePair_ReturnsThePairAfterTheLowSurrogate()
    {
        var buffer = new TextInputBuffer();

        Assert.Null(buffer.Accept('\uD83D'));
        Assert.Equal("😀", buffer.Accept('\uDE00'));
    }

    [Fact]
    public void Accept_LoneLowSurrogate_ReturnsNull()
    {
        Assert.Null(new TextInputBuffer().Accept('\uDE00'));
    }

    [Fact]
    public void Accept_HighSurrogateFollowedBySomethingElse_DropsTheSurrogateAndKeepsTheCharacter()
    {
        var buffer = new TextInputBuffer();

        Assert.Null(buffer.Accept('\uD83D'));
        Assert.Equal("a", buffer.Accept('a'));
        Assert.Null(buffer.Accept('\uDE00'));
    }

    [Fact]
    public void Accept_TwoHighSurrogates_KeepsTheLastOne()
    {
        var buffer = new TextInputBuffer();

        Assert.Null(buffer.Accept('\uD83D'));
        Assert.Null(buffer.Accept('\uD83D'));
        Assert.Equal("😀", buffer.Accept('\uDE00'));
    }
}

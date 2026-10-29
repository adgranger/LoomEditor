using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class AvatarInitialsTests
{
    [Fact]
    public void Get_TwoWords_TwoLetters() => Assert.Equal("AG", AvatarInitials.Get("Adrien Granger"));

    [Fact]
    public void Get_ThreeWords_FirstTwo() => Assert.Equal("AG", AvatarInitials.Get("Adrien  Granger Jr"));

    [Fact]
    public void Get_OneWord_OneLetter() => Assert.Equal("A", AvatarInitials.Get("Adrien"));

    [Theory]
    [InlineData("")]
    [InlineData("  \t ")]
    public void Get_Blank_QuestionMark(string name) => Assert.Equal("?", AvatarInitials.Get(name));

    [Fact]
    public void Get_LowerCase_UpperCased() => Assert.Equal("AG", AvatarInitials.Get("adrien granger"));

    [Fact]
    public void Get_SurrogatePair_NotSplit() => Assert.Equal("\U0001D4D0B", AvatarInitials.Get("\U0001D4D0x Bob"));

    [Fact]
    public void PaletteIndex_SameName_SameIndex() =>
        Assert.Equal(AvatarInitials.PaletteIndex("Adrien", 8), AvatarInitials.PaletteIndex("Adrien", 8));

    [Fact]
    public void PaletteIndex_InRange()
    {
        for (int i = 0; i < 100; i++)
        {
            Assert.InRange(AvatarInitials.PaletteIndex("author " + i, 7), 0, 6);
        }
    }

    [Fact]
    public void PaletteIndex_ZeroSize_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => AvatarInitials.PaletteIndex("a", 0));
}

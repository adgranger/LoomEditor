using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no unit test for the substitution (it is part of the HTML of the line): all tests are new.
public class ControlCharactersTests
{
    [Fact]
    public void Substitute_NoControlCharacters_ReturnsSameInstance()
    {
        string text = "plain\ttext é";

        Assert.Same(text, ControlCharacters.Substitute(text, renderControlCharacters: true));
    }

    [Fact]
    public void Substitute_Nul_GivesControlPicture() =>
        Assert.Equal("\u2400", ControlCharacters.Substitute("\0", true));

    [Fact]
    public void Substitute_Us_GivesU241F() =>
        Assert.Equal("\u241F", ControlCharacters.Substitute("\u001F", true));

    [Fact]
    public void Substitute_Del_GivesU2421() =>
        Assert.Equal("\u2421", ControlCharacters.Substitute("\u007F", true));

    [Fact]
    public void Substitute_Tab_Unchanged() =>
        Assert.Equal("\t", ControlCharacters.Substitute("\t", true));

    [Theory]
    [InlineData("\uFEFF")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    [InlineData("\u0085")]
    public void Substitute_Bom_GivesReplacementEvenWhenOff(string c) =>
        Assert.Equal("\uFFFD", ControlCharacters.Substitute(c, renderControlCharacters: false));

    [Fact]
    public void Substitute_OptionOff_LeavesControlCharacters()
    {
        string text = "a\u0001b\u007F";

        Assert.Same(text, ControlCharacters.Substitute(text, renderControlCharacters: false));
    }

    [Fact]
    public void Substitute_SameLength()
    {
        string text = "a\u0001b\u0007c\u007Fd\uFEFFe\t";

        string result = ControlCharacters.Substitute(text, true);

        Assert.Equal(text.Length, result.Length);
        Assert.Equal("a\u2401b\u2407c\u2421d\uFFFDe\t", result);
        Assert.True(ControlCharacters.IsControlPicture('\u2401'));
        Assert.False(ControlCharacters.IsControlPicture('\uFFFD'));
    }
}

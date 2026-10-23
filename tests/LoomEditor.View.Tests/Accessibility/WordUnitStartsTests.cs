using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class WordUnitStartsTests
{
    private static void Run(string text, Action<AutomationUnitStarts> test) => WithDocument(text, d => test(new WordUnitStarts(d)));

    private static void Next(string text, Position from, Position? expected) =>
        Run(text, u => Assert.Equal(expected, u.NextStartStrict(from)));

    [Fact]
    public void Starts_FooDotBar() => Run("foo.bar", u =>
    {
        Assert.Equal(Pos(1, 4), u.NextStartStrict(Pos(1, 1)));
        Assert.Equal(Pos(1, 5), u.NextStartStrict(Pos(1, 4)));
        Assert.Null(u.NextStartStrict(Pos(1, 5)));
    });

    [Fact]
    public void Starts_TrailingWhitespaceBelongsToTheWord() => Run("foo  bar", u =>
    {
        Assert.Equal(Pos(1, 6), u.NextStartStrict(Pos(1, 1)));
        Assert.Equal(Pos(1, 1), u.StartAtOrBefore(Pos(1, 5)));
    });

    [Fact]
    public void Starts_LeadingWhitespaceIsAWord() => Next("  x", Pos(1, 1), Pos(1, 3));

    [Fact]
    public void Starts_WhitespaceOnlyLine_IsOneWord() => Next("   ", Pos(1, 1), null);

    [Fact]
    public void Starts_SeparatorRun_IsOneWord() => Run("a//b", u =>
    {
        Assert.Equal(Pos(1, 2), u.NextStartStrict(Pos(1, 1)));
        Assert.Equal(Pos(1, 4), u.NextStartStrict(Pos(1, 2)));
    });

    [Fact]
    public void Starts_EmptyLine_IsAWord() => Run("a\n\nb", u =>
    {
        Assert.Equal(Pos(2, 1), u.NextStartStrict(Pos(1, 1)));
        Assert.Equal(Pos(3, 1), u.NextStartStrict(Pos(2, 1)));
    });

    [Fact]
    public void S_AtTheEndOfALine_IsTheLastWord() => Run("ab cd", u => Assert.Equal(Pos(1, 4), u.StartAtOrBefore(Pos(1, 6))));

    [Fact]
    public void P_AtLineStart_IsTheLastWordOfThePreviousLine() => Run("ab cd\nx", u => Assert.Equal(Pos(1, 4), u.PreviousStart(Pos(2, 1))));

    [Fact]
    public void P_AtDocumentStart_IsNull() => Run("ab cd", u => Assert.Null(u.PreviousStart(Pos(1, 1))));

    [Fact]
    public void N_InTheLastWord_IsNull() => Run("ab cd", u => Assert.Null(u.NextStartStrict(Pos(1, 4))));

    [Fact]
    public void NS_InTheLastWord_IsDocumentEnd() => Run("ab cd", u => Assert.Equal(Pos(1, 6), u.NextStart(Pos(1, 4))));

    [Fact]
    public void Tab_IsWhitespace() => Next("a\tb", Pos(1, 1), Pos(1, 3));

    [Fact]
    public void Digits_AreRegular() => Next("abc123 x", Pos(1, 1), Pos(1, 8));

    [Fact]
    public void Underscore_IsRegular() => Next("a_b c", Pos(1, 1), Pos(1, 5));

    [Fact]
    public void CjkRun_IsOneWord() => Next("日本語 x", Pos(1, 1), Pos(1, 5));

    [Fact]
    public void EmptyDocument_StartIsDocumentStart_NextIsNull() => Run("", u =>
    {
        Assert.Equal(Pos(1, 1), u.StartAtOrBefore(Pos(1, 1)));
        Assert.Null(u.NextStartStrict(Pos(1, 1)));
    });
}

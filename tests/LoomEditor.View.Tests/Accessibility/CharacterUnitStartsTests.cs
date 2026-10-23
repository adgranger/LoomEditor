using LoomEditor.View.Accessibility;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class CharacterUnitStartsTests
{
    private static void Run(string text, Action<AutomationUnitStarts> test) =>
        WithDocument(text, d => test(new CharacterUnitStarts(d)));

    [Fact]
    public void S_InsideALine_IsThePositionItself() => Run("abc", u => Assert.Equal(Pos(1, 2), u.StartAtOrBefore(Pos(1, 2))));

    [Fact]
    public void N_InsideALine_IsTheNextColumn() => Run("abc", u => Assert.Equal(Pos(1, 3), u.NextStartStrict(Pos(1, 2))));

    [Fact]
    public void P_InsideALine_IsThePreviousColumn() => Run("abc", u => Assert.Equal(Pos(1, 1), u.PreviousStart(Pos(1, 2))));

    [Fact]
    public void N_LastCharacterOfALine_IsTheLineBreak() => Run("ab\ncd", u => Assert.Equal(Pos(1, 3), u.NextStartStrict(Pos(1, 2))));

    [Fact]
    public void N_AtTheLineBreak_IsTheNextLineStart() => Run("ab\ncd", u => Assert.Equal(Pos(2, 1), u.NextStartStrict(Pos(1, 3))));

    [Fact]
    public void P_AtLineStart_IsThePreviousLineBreak() => Run("ab\ncd", u => Assert.Equal(Pos(1, 3), u.PreviousStart(Pos(2, 1))));

    [Fact]
    public void S_AtTheEndOfTheLastLine_IsTheLastCharacter() => Run("ab\ncd", u => Assert.Equal(Pos(2, 2), u.StartAtOrBefore(Pos(2, 3))));

    [Fact]
    public void N_AtTheEndOfTheLastLine_IsNull() => Run("ab\ncd", u => Assert.Null(u.NextStartStrict(Pos(2, 3))));

    [Fact]
    public void NS_AtTheEndOfTheLastLine_IsDocumentEnd() => Run("ab\ncd", u => Assert.Equal(Pos(2, 3), u.NextStart(Pos(2, 3))));

    [Fact]
    public void P_AtDocumentStart_IsNull() => Run("ab", u => Assert.Null(u.PreviousStart(Pos(1, 1))));

    [Fact]
    public void S_AtDocumentStart_IsDocumentStart() => Run("ab", u => Assert.Equal(Pos(1, 1), u.StartAtOrBefore(Pos(1, 1))));

    [Fact]
    public void EmptyDocument_StartIsDocumentStart_NextIsNull() =>
        Run("", u =>
        {
            Assert.Equal(Pos(1, 1), u.StartAtOrBefore(Pos(1, 1)));
            Assert.Null(u.NextStartStrict(Pos(1, 1)));
        });

    [Fact]
    public void EmptyLineInTheMiddle_IsOneCharacter() =>
        Run("a\n\nb", u =>
        {
            Assert.Equal(Pos(2, 1), u.NextStartStrict(Pos(1, 2)));
            Assert.Equal(Pos(3, 1), u.NextStartStrict(Pos(2, 1)));
        });

    [Fact]
    public void TrailingEmptyLastLine() =>
        Run("a\n", u =>
        {
            Assert.Null(u.NextStartStrict(Pos(1, 2)));
            Assert.Equal(Pos(1, 2), u.StartAtOrBefore(Pos(2, 1)));
        });

    [Fact]
    public void SurrogatePair_IsOneCharacter() =>
        Run("a😀b", u =>
        {
            Assert.Equal(Pos(1, 4), u.NextStartStrict(Pos(1, 2)));
            Assert.Equal(Pos(1, 2), u.PreviousStart(Pos(1, 4)));
            Assert.Equal(Pos(1, 2), u.StartAtOrBefore(Pos(1, 3)));
        });

    [Fact]
    public void Grapheme_CombiningMark_IsOneCharacter() => Run("éx", u => Assert.Equal(Pos(1, 3), u.NextStartStrict(Pos(1, 1))));

    [Fact]
    public void Crlf_Model_HasNoSeparateCarriageReturn() =>
        Run("a\r\nb", u =>
        {
            Assert.Equal(Pos(1, 2), u.NextStartStrict(Pos(1, 1)));
            Assert.Equal(Pos(2, 1), u.NextStartStrict(Pos(1, 2)));
        });
}

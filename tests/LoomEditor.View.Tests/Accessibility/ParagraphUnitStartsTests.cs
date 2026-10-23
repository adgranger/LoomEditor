using LoomEditor.View.Accessibility;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class ParagraphUnitStartsTests
{
    private const string Text = "abc\ndef\nghi";

    private static void Run(string text, Action<AutomationUnitStarts> test, TestCodeEditorOptions? options = null) =>
        WithDocument(text, d => test(new ParagraphUnitStarts(d)), options);

    [Fact]
    public void S_IsTheLineStart() => Run(Text, u => Assert.Equal(Pos(2, 1), u.StartAtOrBefore(Pos(2, 3))));

    [Fact]
    public void P_MidLine_IsTheLineStart() => Run(Text, u => Assert.Equal(Pos(2, 1), u.PreviousStart(Pos(2, 3))));

    [Fact]
    public void P_AtLineStart_IsThePreviousLineStart() => Run(Text, u => Assert.Equal(Pos(1, 1), u.PreviousStart(Pos(2, 1))));

    [Fact]
    public void P_AtDocumentStart_IsNull() => Run(Text, u => Assert.Null(u.PreviousStart(Pos(1, 1))));

    [Fact]
    public void N_IsTheNextLineStart() => Run(Text, u => Assert.Equal(Pos(3, 1), u.NextStartStrict(Pos(2, 2))));

    [Fact]
    public void N_OnTheLastLine_IsNull() => Run(Text, u => Assert.Null(u.NextStartStrict(Pos(3, 2))));

    [Fact]
    public void NS_OnTheLastLine_IsDocumentEnd() => Run(Text, u => Assert.Equal(Pos(3, 4), u.NextStart(Pos(3, 2))));

    [Fact]
    public void WrappedLine_IsOneParagraph() =>
        Run("aaaa bbbb cccc dddd eeee ffff\nx", u => Assert.Equal(Pos(2, 1), u.NextStartStrict(Pos(1, 25))),
            new TestCodeEditorOptions(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 }));
}

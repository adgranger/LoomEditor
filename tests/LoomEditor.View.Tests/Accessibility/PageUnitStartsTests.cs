using LoomEditor.View.Accessibility;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class PageUnitStartsTests
{
    private const string Text = "1\n2\n3\n4\n5\n6\n7";

    private static void Run(string text, Action<PageUnitStarts> test, int outerHeight = 3 * 19, TestCodeEditorOptions? options = null) =>
        WithDocument(text, d => test(new PageUnitStarts(d)), (options ?? new TestCodeEditorOptions()) with { OuterHeight = outerHeight });

    private static void RunThreeLines(string text, Action<PageUnitStarts> test, TestCodeEditorOptions? options = null) =>
        Run(text, u =>
        {
            var range = u.Document.ViewModel.GetCompletelyVisibleViewRange();
            Assert.Equal((1, 3), (range.StartLineNumber, range.EndLineNumber));
            test(u);
        }, 3 * 19, options);

    [Fact]
    public void S_FirstPage_IsDocumentStart() => RunThreeLines(Text, u => Assert.Equal(Pos(1, 1), u.StartAtOrBefore(Pos(2, 1))));

    [Fact]
    public void S_FifthLine_IsTheStartOfPageTwo() => RunThreeLines(Text, u => Assert.Equal(Pos(4, 1), u.StartAtOrBefore(Pos(5, 1))));

    [Fact]
    public void N_FirstPage_IsLineFour() => RunThreeLines(Text, u => Assert.Equal(Pos(4, 1), u.NextStartStrict(Pos(1, 1))));

    [Fact]
    public void N_LastPage_IsNull() => RunThreeLines(Text, u => Assert.Null(u.NextStartStrict(Pos(7, 1))));

    [Fact]
    public void P_PageTwo_IsPageOneStart() => RunThreeLines(Text, u => Assert.Equal(Pos(1, 1), u.PreviousStart(Pos(4, 1))));

    [Fact]
    public void P_AtDocumentStart_IsNull() => RunThreeLines(Text, u => Assert.Null(u.PreviousStart(Pos(1, 1))));

    [Fact]
    public void NoViewport_PagesAreOneLine() => Run(Text, u => Assert.Equal(Pos(3, 1), u.NextStartStrict(Pos(2, 1))), 0);

    [Fact]
    public void PagesFollowViewLinesNotModelLines() =>
        RunThreeLines("aaaa bbbb cccc dddd eeee ffff gggg hhhh", u =>
        {
            Assert.Equal("aaaa bbbb ", u.Document.ViewModel.GetLineContent(1));
            Assert.Equal(Pos(1, 31), u.NextStartStrict(Pos(1, 1)));
            Assert.Equal(Pos(1, 1), u.StartAtOrBefore(Pos(1, 25)));
        }, new TestCodeEditorOptions(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 }));
}

using LoomEditor.View.Accessibility;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class LineUnitStartsTests
{
    private const string Wrapped = "aaaa bbbb cccc";

    private static readonly TestCodeEditorOptions WrapOptions =
        new(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 });

    private static void Run(string text, Action<LineUnitStarts> test, TestCodeEditorOptions? options = null) =>
        WithDocument(text, d => test(new LineUnitStarts(d)), options);

    private static void RunWrapped(string text, Action<LineUnitStarts> test) =>
        Run(text, u =>
        {
            Assert.Equal("aaaa bbbb ", u.Document.ViewModel.GetLineContent(1));
            Assert.Equal("cccc", u.Document.ViewModel.GetLineContent(2));
            test(u);
        }, WrapOptions);

    [Fact]
    public void S_NoWrap_IsTheModelLineStart() => Run("abc\ndef", u => Assert.Equal(Pos(2, 1), u.StartAtOrBefore(Pos(2, 3))));

    [Fact]
    public void N_NoWrap_IsTheNextModelLineStart() => Run("abc\ndef", u => Assert.Equal(Pos(2, 1), u.NextStartStrict(Pos(1, 2))));

    [Fact]
    public void S_SecondViewLine_IsTheBreakPosition() => RunWrapped(Wrapped, u => Assert.Equal(Pos(1, 11), u.StartAtOrBefore(Pos(1, 12))));

    [Fact]
    public void N_FirstViewLine_IsTheBreakPosition() => RunWrapped(Wrapped, u => Assert.Equal(Pos(1, 11), u.NextStartStrict(Pos(1, 3))));

    [Fact]
    public void P_SecondViewLine_Positions() => RunWrapped(Wrapped, u =>
    {
        Assert.Equal(Pos(1, 11), u.PreviousStart(Pos(1, 12)));
        Assert.Equal(Pos(1, 1), u.PreviousStart(Pos(1, 11)));
    });

    [Fact]
    public void N_LastViewLine_IsNull() => RunWrapped(Wrapped, u => Assert.Null(u.NextStartStrict(Pos(1, 12))));

    [Fact]
    public void P_AtDocumentStart_IsNull() => Run("abc", u => Assert.Null(u.PreviousStart(Pos(1, 1))));

    [Fact]
    public void NS_LastViewLine_IsDocumentEnd() => RunWrapped(Wrapped, u => Assert.Equal(Pos(1, 15), u.NextStart(Pos(1, 12))));

    [Fact]
    public void WrappedLineThenNextModelLine() => RunWrapped("aaaa bbbb cccc\nx", u => Assert.Equal(Pos(2, 1), u.NextStartStrict(Pos(1, 11))));

    [Fact]
    public void FoldedLines_BelongToThePreviousViewLine() =>
        Run("a\nb\nc", u =>
        {
            u.Document.ViewModel.SetHiddenAreas([new Range(2, 1, 3, 1)]);
            Assert.Equal(Pos(1, 1), u.StartAtOrBefore(Pos(3, 1)));
            Assert.Null(u.NextStartStrict(Pos(1, 1)));
        });

    [Fact]
    public void FoldedLines_NextVisibleLineFollows() =>
        Run("a\nb\nc\nd", u =>
        {
            u.Document.ViewModel.SetHiddenAreas([new Range(2, 1, 3, 1)]);
            Assert.Equal(Pos(4, 1), u.NextStartStrict(Pos(1, 1)));
        });

    [Fact]
    public void EmptyDocument_StartIsDocumentStart_NextIsNull() => Run("", u =>
    {
        Assert.Equal(Pos(1, 1), u.StartAtOrBefore(Pos(1, 1)));
        Assert.Null(u.NextStartStrict(Pos(1, 1)));
    });

    [Fact]
    public void LargeDocument_BinarySearchIsCorrect() =>
        Run(string.Join('\n', Enumerable.Repeat("x", 10_000)), u =>
        {
            Assert.Equal(Pos(5000, 1), u.StartAtOrBefore(Pos(5000, 1)));
            Assert.Equal(Pos(5001, 1), u.NextStartStrict(Pos(5000, 1)));
            Assert.Equal(Pos(4999, 1), u.PreviousStart(Pos(5000, 1)));
        });
}

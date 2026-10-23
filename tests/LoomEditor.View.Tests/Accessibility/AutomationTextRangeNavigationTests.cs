using LoomEditor.View.Accessibility;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AutomationTextRangeNavigationTests
{
    private const string T1 = "one two\nthree";
    private const string Wrapped = "aaaa bbbb cccc";
    private const string Pages = "1\n2\n3\n4\n5\n6\n7";

    private const AutomationTextUnit Word = AutomationTextUnit.Word;
    private const AutomationTextEndpoint Start = AutomationTextEndpoint.Start;
    private const AutomationTextEndpoint End = AutomationTextEndpoint.End;

    private static readonly TestCodeEditorOptions WrapOptions =
        new(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 });

    private static void RunWrapped(Action<AutomationTextDocument> test) =>
        WithDocument(Wrapped, d =>
        {
            Assert.Equal("aaaa bbbb ", d.ViewModel.GetLineContent(1));
            Assert.Equal("cccc", d.ViewModel.GetLineContent(2));
            test(d);
        }, WrapOptions);

    private static void RunPages(Action<AutomationTextDocument> test) =>
        WithDocument(Pages, test, new TestCodeEditorOptions() with { OuterHeight = 3 * 19 });

    private static AutomationTextRange R(AutomationTextDocument d, int sl, int sc, int el, int ec) => new(d, Rng(sl, sc, el, ec));

    private static void Expand(string text, AutomationTextUnit unit, Range from, Range expected, string? expectedText = null) =>
        WithDocument(text, d =>
        {
            var r = new AutomationTextRange(d, from);
            r.ExpandToEnclosingUnit(unit);
            Assert.Equal(expected, r.Range);
            if (expectedText is not null)
            {
                Assert.Equal(expectedText, r.GetText());
            }
        });

    private static void MoveTo(string text, Range from, AutomationTextUnit unit, int count, int expectedMoved, Range expected) =>
        WithDocument(text, d =>
        {
            var r = new AutomationTextRange(d, from);
            Assert.Equal(expectedMoved, r.Move(unit, count));
            Assert.Equal(expected, r.Range);
        });

    private static void MoveEnd(AutomationTextEndpoint endpoint, Range from, AutomationTextUnit unit, int count, int expectedMoved, Range expected) =>
        WithDocument(T1, d =>
        {
            var r = new AutomationTextRange(d, from);
            Assert.Equal(expectedMoved, r.MoveEndpointByUnit(endpoint, unit, count));
            Assert.Equal(expected, r.Range);
        });

    private static void ByRange(AutomationTextEndpoint endpoint, Range a, Range b, AutomationTextEndpoint bEndpoint, Range expected) =>
        WithDocument(T1, d =>
        {
            var ra = new AutomationTextRange(d, a);
            ra.MoveEndpointByRange(endpoint, new AutomationTextRange(d, b), bEndpoint);
            Assert.Equal(expected, ra.Range);
        });

    [Fact]
    public void Expand_Word_Degenerate() => Expand(T1, Word, Rng(1, 2, 1, 2), Rng(1, 1, 1, 5), "one ");

    [Fact]
    public void Expand_Word_AtLineEnd_IncludesTheLineBreak() => Expand(T1, Word, Rng(1, 8, 1, 8), Rng(1, 5, 2, 1), "two\n");

    [Fact]
    public void Expand_Word_AtDocumentEnd_IsTheLastWord() => Expand(T1, Word, Rng(2, 6, 2, 6), Rng(2, 1, 2, 6), "three");

    [Fact]
    public void Expand_Word_LongerRange_IsShortenedToTheFirstWord() => Expand(T1, Word, Rng(1, 2, 2, 3), Rng(1, 1, 1, 5));

    [Fact]
    public void Expand_Character() => Expand(T1, AutomationTextUnit.Character, Rng(1, 2, 1, 2), Rng(1, 2, 1, 3), "n");

    [Fact]
    public void Expand_Character_AtLineEnd_IsTheLineBreak() => Expand(T1, AutomationTextUnit.Character, Rng(1, 8, 1, 8), Rng(1, 8, 2, 1), "\n");

    [Fact]
    public void Expand_Paragraph() => Expand(T1, AutomationTextUnit.Paragraph, Rng(1, 3, 1, 3), Rng(1, 1, 2, 1), "one two\n");

    [Fact]
    public void Expand_Document() => Expand(T1, AutomationTextUnit.Document, Rng(2, 3, 2, 3), Rng(1, 1, 2, 6));

    [Fact]
    public void Expand_Line_NoWrap_EqualsParagraph() => Expand(T1, AutomationTextUnit.Line, Rng(1, 3, 1, 3), Rng(1, 1, 2, 1));

    [Fact]
    public void Expand_Line_Wrapped() => RunWrapped(d =>
    {
        var r = R(d, 1, 12, 1, 12);
        r.ExpandToEnclosingUnit(AutomationTextUnit.Line);
        Assert.Equal(Rng(1, 11, 1, 15), r.Range);
        Assert.Equal("cccc", r.GetText());
    });

    [Fact]
    public void Expand_Page() => RunPages(d =>
    {
        var r = R(d, 2, 1, 2, 1);
        r.ExpandToEnclosingUnit(AutomationTextUnit.Page);
        Assert.Equal(Rng(1, 1, 4, 1), r.Range);
    });

    [Fact]
    public void Expand_EmptyDocument_StaysEmpty() => Expand("", Word, Rng(1, 1, 1, 1), Rng(1, 1, 1, 1));

    [Fact]
    public void Expand_ExactUnit_NoChange() => Expand(T1, Word, Rng(1, 1, 1, 5), Rng(1, 1, 1, 5));

    [Fact]
    public void Move_WordForward() => MoveTo(T1, Rng(1, 1, 1, 1), Word, 1, 1, Rng(1, 5, 2, 1));

    [Fact]
    public void Move_WordForwardPastTheEnd() => MoveTo(T1, Rng(1, 1, 1, 1), Word, 5, 2, Rng(2, 1, 2, 6));

    [Fact]
    public void Move_WordAtTheLastWord_ReturnsZero() => MoveTo(T1, Rng(2, 1, 2, 6), Word, 1, 0, Rng(2, 1, 2, 6));

    [Fact]
    public void Move_WordBackward() => MoveTo(T1, Rng(2, 1, 2, 6), Word, -1, -1, Rng(1, 5, 2, 1));

    [Fact]
    public void Move_WordBackwardPastTheStart() => MoveTo(T1, Rng(1, 5, 2, 1), Word, -5, -1, Rng(1, 1, 1, 5));

    [Fact]
    public void Move_CharacterAcrossTheLineBreak() => WithDocument(T1, d =>
    {
        var r = R(d, 1, 7, 1, 8);
        Assert.Equal(1, r.Move(AutomationTextUnit.Character, 1));
        Assert.Equal(Rng(1, 8, 2, 1), r.Range);
        Assert.Equal(1, r.Move(AutomationTextUnit.Character, 1));
        Assert.Equal(Rng(2, 1, 2, 2), r.Range);
    });

    [Fact]
    public void Move_Zero_OnlyExpands() => MoveTo(T1, Rng(1, 2, 1, 2), Word, 0, 0, Rng(1, 1, 1, 5));

    [Fact]
    public void Move_Document_ReturnsZeroAndSelectsEverything() =>
        MoveTo(T1, Rng(1, 3, 1, 3), AutomationTextUnit.Document, 1, 0, Rng(1, 1, 2, 6));

    [Fact]
    public void Move_Line_Wrapped_GoesByViewLine() => RunWrapped(d =>
    {
        var r = R(d, 1, 1, 1, 1);
        Assert.Equal(1, r.Move(AutomationTextUnit.Line, 1));
        Assert.Equal(Rng(1, 11, 1, 15), r.Range);
    });

    [Fact]
    public void Move_Paragraph_GoesByModelLine() =>
        MoveTo("a\nb\nc", Rng(1, 1, 1, 1), AutomationTextUnit.Paragraph, 1, 1, Rng(2, 1, 3, 1));

    [Fact]
    public void Move_Page() => RunPages(d =>
    {
        var r = R(d, 1, 1, 1, 1);
        Assert.Equal(1, r.Move(AutomationTextUnit.Page, 1));
        Assert.Equal(Rng(4, 1, 7, 1), r.Range);
    });

    [Fact]
    public void Move_ExpandsANonDegenerateRangeFirst() => MoveTo(T1, Rng(1, 2, 2, 3), Word, 1, 1, Rng(1, 5, 2, 1));

    [Fact]
    public void Move_SurrogatePair_IsOneCharacter() => WithDocument("a\U0001F600b", d =>
    {
        var r = R(d, 1, 1, 1, 1);
        Assert.Equal(1, r.Move(AutomationTextUnit.Character, 1));
        Assert.Equal(Rng(1, 2, 1, 4), r.Range);
        Assert.Equal(1, r.Move(AutomationTextUnit.Character, 1));
        Assert.Equal(Rng(1, 4, 1, 5), r.Range);
    });

    [Fact]
    public void End_Word_Forward() => MoveEnd(End, Rng(1, 1, 1, 5), Word, 1, 1, Rng(1, 1, 2, 1));

    [Fact]
    public void End_Word_PastTheEnd() => MoveEnd(End, Rng(1, 1, 1, 5), Word, 5, 2, Rng(1, 1, 2, 6));

    [Fact]
    public void Start_Word_Forward_CollapsesWhenCrossing() => MoveEnd(Start, Rng(1, 1, 1, 5), Word, 2, 2, Rng(2, 1, 2, 1));

    [Fact]
    public void Start_Word_Forward_ToTheEnd_IsDegenerate() => MoveEnd(Start, Rng(1, 1, 1, 5), Word, 1, 1, Rng(1, 5, 1, 5));

    [Fact]
    public void End_Word_Backward() => MoveEnd(End, Rng(1, 1, 1, 5), Word, -1, -1, Rng(1, 1, 1, 1));

    [Fact]
    public void End_Word_Backward_CrossingCollapsesToTheEnd() => MoveEnd(End, Rng(1, 5, 2, 1), Word, -2, -2, Rng(1, 1, 1, 1));

    [Fact]
    public void Start_Character_BackwardAtDocumentStart_ReturnsZero() =>
        MoveEnd(Start, Rng(1, 1, 1, 5), AutomationTextUnit.Character, -1, 0, Rng(1, 1, 1, 5));

    [Fact]
    public void End_Document_Forward_GoesToTheDocumentEnd() =>
        MoveEnd(End, Rng(1, 1, 1, 1), AutomationTextUnit.Document, 1, 1, Rng(1, 1, 2, 6));

    [Fact]
    public void End_Document_Backward_GoesToTheDocumentStart() =>
        MoveEnd(End, Rng(1, 1, 2, 6), AutomationTextUnit.Document, -1, -1, Rng(1, 1, 1, 1));

    [Fact]
    public void Count_Zero_ReturnsZeroAndChangesNothing() => MoveEnd(End, Rng(1, 2, 1, 3), Word, 0, 0, Rng(1, 2, 1, 3));

    [Fact]
    public void MidWordEndpoint_ForwardGoesToTheNextStart() => MoveEnd(End, Rng(1, 1, 1, 3), Word, 1, 1, Rng(1, 1, 1, 5));

    [Fact]
    public void End_ToTheStartOfAnotherRange() => ByRange(End, Rng(1, 1, 1, 5), Rng(2, 1, 2, 6), Start, Rng(1, 1, 2, 1));

    [Fact]
    public void Start_ToTheEndOfAnotherRange_CollapsesA() => ByRange(Start, Rng(1, 1, 1, 5), Rng(2, 1, 2, 6), End, Rng(2, 6, 2, 6));

    [Fact]
    public void End_ToTheEndOfAnotherRange() => ByRange(End, Rng(1, 1, 1, 5), Rng(2, 1, 2, 6), End, Rng(1, 1, 2, 6));

    [Fact]
    public void End_BeforeStart_CollapsesToTheEnd() => ByRange(End, Rng(1, 5, 2, 1), Rng(1, 1, 1, 3), End, Rng(1, 3, 1, 3));

    [Fact]
    public void OtherDocument_Throws() => WithDocument(T1, d1 => WithDocument(T1, d2 =>
        Assert.Throws<ArgumentException>(() => R(d1, 1, 1, 1, 2).MoveEndpointByRange(End, R(d2, 1, 1, 1, 2), End))));

    [Fact]
    public void Arguments_AreValidated() => WithDocument(T1, d =>
    {
        var r = R(d, 1, 1, 1, 2);
        Assert.Throws<ArgumentNullException>(() => r.MoveEndpointByRange(End, null!, End));
        Assert.Throws<ArgumentOutOfRangeException>(() => r.Move((AutomationTextUnit)99, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => r.ExpandToEnclosingUnit((AutomationTextUnit)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => r.MoveEndpointByUnit((AutomationTextEndpoint)9, Word, 1));
    });

    [Fact]
    public void Move_ModelChangedBetweenCalls_DoesNotThrow() => WithDocument(T1, d =>
    {
        var r = R(d, 2, 1, 2, 6);
        d.Model.SetValue("ab");
        r.ExpandToEnclosingUnit(Word);
        Assert.Equal(Rng(1, 1, 1, 3), r.Range);
        r.Set(Rng(5, 5, 5, 5));
        Assert.Equal(0, r.Move(Word, 1));
    });
}

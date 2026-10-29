using LoomEditor.Core.Model;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Snippet;

namespace LoomEditor.View.Tests.Features.Snippet;

public partial class SnippetSessionTests
{
    // Monaco: 'normalize whitespace'
    [Fact]
    public void NormalizeWhitespace()
    {
        void AssertNormalized(Position position, string input, string expected)
        {
            TextmateSnippet snippet = new SnippetParser().Parse(input);
            SnippetSession.AdjustWhitespace(Model, position, true, snippet);
            Assert.Equal(expected, snippet.ToTextmateString());
        }

        AssertNormalized(new Position(1, 1), "foo", "foo");
        AssertNormalized(new Position(1, 1), "foo\rbar", "foo\nbar");
        AssertNormalized(new Position(1, 1), "foo\rbar", "foo\nbar");
        AssertNormalized(new Position(2, 5), "foo\r\tbar", "foo\n        bar");
        AssertNormalized(new Position(2, 3), "foo\r\tbar", "foo\n    bar");
        AssertNormalized(new Position(2, 5), "foo\r\tbar\nfoo", "foo\n        bar\n    foo");

        // Indentation issue with choice elements that span multiple lines #46266
        AssertNormalized(new Position(2, 5), "a\nb${1|foo,\nbar|}", "a\n    b${1|foo,\nbar|}");
    }

    // Monaco: 'adjust selection (overwrite[Before|After])'
    [Fact]
    public void AdjustSelection()
    {
        Assert.Equal(new Range(1, 1, 1, 2), SnippetSession.AdjustSelection(Model, new Selection(1, 2, 1, 2), 1, 0).Range);
        Assert.Equal(new Range(1, 1, 1, 2), SnippetSession.AdjustSelection(Model, new Selection(1, 2, 1, 2), 1111, 0).Range);
        Assert.Equal(new Range(1, 2, 1, 12), SnippetSession.AdjustSelection(Model, new Selection(1, 2, 1, 2), 0, 10).Range);
        Assert.Equal(new Range(1, 2, 1, 17), SnippetSession.AdjustSelection(Model, new Selection(1, 2, 1, 2), 0, 10111).Range);
    }

    // Monaco: 'Snippet variable text isn't whitespace normalised, #31124'
    [Fact]
    public void Issue31124_VariableTextIsNotWhitespaceNormalized()
    {
        SetValue(string.Join('\n', "start", "\t\t-one", "\t\t-two", "end"));
        Model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: false));
        Context.SetSelections([new Selection(2, 2, 3, 7)]);

        Insert("<div>\n\t$TM_SELECTED_TEXT\n</div>$0");

        Assert.Equal(string.Join('\n', "start", "\t<div>", "\t\t\t-one", "\t\t\t-two", "\t</div>", "end"), Model.GetValue());

        SetValue(string.Join('\n', "start", "\t\t-one", "\t-two", "end"));
        Model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: false));
        Context.SetSelections([new Selection(2, 2, 3, 7)]);

        Insert("<div>\n\t$TM_SELECTED_TEXT\n</div>$0");

        Assert.Equal(string.Join('\n', "start", "\t<div>", "\t\t\t-one", "\t\t-two", "\t</div>", "end"), Model.GetValue());
    }

    // Monaco: 'Selecting text from left to right, and choosing item messes up code, #31199'
    [Fact]
    public void Issue31199_SelectionLeftToRight()
    {
        SetValue("console.log");

        Selection actual = SnippetSession.AdjustSelection(Model, new Selection(1, 12, 1, 9), 3, 0);
        Assert.Equal(new Selection(1, 9, 1, 6), actual);

        actual = SnippetSession.AdjustSelection(Model, new Selection(1, 9, 1, 12), 3, 0);
        Assert.Equal(new Selection(1, 9, 1, 12), actual);

        Context.SetSelections([new Selection(1, 9, 1, 12)]);
        Insert("far", new SnippetSessionInsertOptions { OverwriteBefore = 3 });

        Assert.Equal("console.far", Model.GetValue());
    }

    // Monaco: 'Tabs don't get replaced with spaces in snippet transformations #103818'
    [Fact]
    public void Issue103818_TabsAreNotReplacedWithSpacesInTransformations()
    {
        SetValue("\n{\n  \n}");
        Model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: true, IndentSize: 2));
        Context.SetSelections([new Selection(1, 1, 1, 1), new Selection(3, 6, 3, 6)]);

        SnippetSession session = Insert(string.Join('\n', "function animate () {", "\tvar ${1:a} = 12;", "\tconsole.log(${1/(.*)/\n\t\t$1\n\t/})", "}"));

        Assert.Equal(
            string.Join('\n', "function animate () {", "  var a = 12;", "  console.log(a)", "}", "{", "  function animate () {", "    var a = 12;", "    console.log(a)", "  }", "}"),
            Model.GetValue());

        Type("bbb");
        session.Next();

        Assert.Equal(
            string.Join('\n', "function animate () {", "  var bbb = 12;", "  console.log(", "    bbb", "  )", "}", "{", "  function animate () {", "    var bbb = 12;", "    console.log(", "      bbb", "    )", "  }", "}"),
            Model.GetValue());
    }

    [Fact]
    public void Insert_AtEveryCursor_OneUndoStep()
    {
        Insert("x${1:y}$0");
        Assert.Equal("xyfunction foo() {\n    xyconsole.log(a);\n}", Model.GetValue());

        Model.Undo();

        Assert.Equal(Text, Model.GetValue());
    }

    [Fact]
    public void Insert_ReplacesSelection()
    {
        SetValue("foo bar");
        Context.SetSelections([new Selection(1, 5, 1, 8)]);

        Insert("baz$0");

        Assert.Equal("foo baz", Model.GetValue());
        AssertSelections(new Selection(1, 8, 1, 8));
    }

    [Fact]
    public void Insert_OverwriteBeforeAfter()
    {
        SetValue("abcdef");
        Context.SetSelections([new Selection(1, 4, 1, 4)]);

        Insert("X$0", new SnippetSessionInsertOptions { OverwriteBefore = 2, OverwriteAfter = 1 });

        Assert.Equal("aXef", Model.GetValue());
        AssertSelections(new Selection(1, 3, 1, 3));
    }

    [Fact]
    public void Insert_AdjustWhitespace_False_KeepsRawTemplate()
    {
        SetValue("\tx");
        Context.SetSelections([new Selection(1, 2, 1, 2)]);

        Insert("a\n  b$0", new SnippetSessionInsertOptions { AdjustWhitespace = false });

        Assert.Equal("\ta\n  bx", Model.GetValue());
    }

    [Fact]
    public void Insert_ResolvesSelectionVariable()
    {
        SetValue("foo");
        Context.SetSelections([new Selection(1, 1, 1, 4)]);

        Insert("[$SELECTION]$0");

        Assert.Equal("[foo]", Model.GetValue());
    }

    [Fact]
    public void Insert_NoPlaceholders_EndsAtFinalPosition()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        SnippetSession session = Insert("abc");

        AssertSelections(new Selection(1, 4, 1, 4));
        Assert.False(session.HasPlaceholder);
    }

    [Fact]
    public void Insert_WithFinalTabstop_CaretThere()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        SnippetSession session = Insert("a$0bc");

        AssertSelections(new Selection(1, 2, 1, 2));
        Assert.True(session.IsAtLastPlaceholder);
    }

    [Fact]
    public void Next_Prev_WalkPlaceholders()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("${1:a}-${2:b}$0");
        AssertSelections(new Selection(1, 1, 1, 2));
        Assert.True(session.IsAtFirstPlaceholder);

        session.Next();
        AssertSelections(new Selection(1, 3, 1, 4));
        Assert.False(session.IsAtFirstPlaceholder);

        session.Next();
        AssertSelections(new Selection(1, 4, 1, 4));
        Assert.True(session.IsAtLastPlaceholder);

        session.Prev();
        session.Prev();
        AssertSelections(new Selection(1, 1, 1, 2));
    }

    [Fact]
    public void Merge_InsideActivePlaceholder_Nested()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("a${1:b}c$0");

        session.Merge("[${1:x}${2:y}]$0");

        Assert.Equal("a[xy]c", Model.GetValue());
        AssertSelections(new Selection(1, 3, 1, 4));
        Assert.Single(session.TemplateMerges);
        session.Next();
        AssertSelections(new Selection(1, 4, 1, 5));
        session.Next();
        AssertSelections(new Selection(1, 6, 1, 6));
        Assert.False(session.IsAtLastPlaceholder);
        session.Next();
        AssertSelections(new Selection(1, 7, 1, 7));
        Assert.True(session.IsAtLastPlaceholder);
    }

    [Fact]
    public void IsSelectionWithinPlaceholders_OutsideSelection_False()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("a${1:b}c$0");
        Assert.True(session.IsSelectionWithinPlaceholders());

        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Assert.False(session.IsSelectionWithinPlaceholders());
    }

    [Fact]
    public void GetEnclosingRange_CoversAllSnippets()
    {
        SnippetSession session = Insert("a${1:b}c${2:d}$0");

        Assert.Equal(new Range(1, 2, 2, 9), session.GetEnclosingRange());
    }

    [Fact]
    public void Dispose_RemovesAllDecorations()
    {
        SnippetSession session = Insert("${1:a}$0");
        Assert.Equal(4, DecorationCount());

        session.Dispose();

        Assert.Equal(0, DecorationCount());
    }
}

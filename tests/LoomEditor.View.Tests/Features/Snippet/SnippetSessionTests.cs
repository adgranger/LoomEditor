using LoomEditor.Core.Model;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Snippet;

namespace LoomEditor.View.Tests.Features.Snippet;

// Monaco: suite 'SnippetSession' (snippetSession.test.ts). The tests that only move between placeholders are in OneSnippetTests;
// the tests of createEditsAndSnippetsFromEdits and of the OvertypingCapturer are not ported (later).
public partial class SnippetSessionTests : IDisposable
{
    private const string Text = "function foo() {\n    console.log(a);\n}";

    private readonly List<SnippetSession> _sessions = [];

    public SnippetSessionTests()
    {
        Context = new EditorContext();
        Context.SetText(Text);
        Context.SetSelections([new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5)]);
    }

    private EditorContext Context { get; }

    private TextModel Model => Context.Model;

    public void Dispose()
    {
        foreach (SnippetSession session in _sessions)
        {
            session.Dispose();
        }

        Context.Dispose();
        GC.SuppressFinalize(this);
    }

    private SnippetSession Session(string template, SnippetSessionInsertOptions? options = null, ISnippetVariableHost? host = null)
    {
        var session = new SnippetSession(Context, template, options, host ?? ISnippetVariableHost.None, TimeProvider.System, new Random(1));
        _sessions.Add(session);
        return session;
    }

    private SnippetSession Insert(string template, SnippetSessionInsertOptions? options = null)
    {
        SnippetSession session = Session(template, options);
        session.Insert();
        return session;
    }

    private void Type(string text) => Context.ViewModel.Type(text, "test");

    private void SetValue(string value) => Model.SetValue(value);

    private void AssertSelections(params Selection[] expected)
    {
        IReadOnlyList<Selection> actual = Context.Selections;
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(actual[i] == expected[i], $"actual={actual[i]} <> expected={expected[i]}");
        }
    }

    private int DecorationCount() =>
        Enumerable.Range(1, Model.LineCount)
            .Sum(line => Context.GetLineDecorations(line).Count(decoration => decoration.Options.ClassName is "snippet-placeholder" or "finish-snippet-placeholder"));

    // Monaco: 'text edits & selection'
    [Fact]
    public void TextEditsAndSelection()
    {
        SnippetSession session = Insert("foo${1:bar}foo$0");
        Assert.Equal("foobarfoofunction foo() {\n    foobarfooconsole.log(a);\n}", Model.GetValue());

        AssertSelections(new Selection(1, 4, 1, 7), new Selection(2, 8, 2, 11));
        session.Next();
        AssertSelections(new Selection(1, 10, 1, 10), new Selection(2, 14, 2, 14));
    }

    // Monaco: 'text edit with reversed selection'
    [Fact]
    public void TextEditWithReversedSelection()
    {
        SnippetSession session = Session("${1:bar}$0");
        Context.SetSelections([new Selection(2, 5, 2, 5), new Selection(1, 1, 1, 1)]);

        session.Insert();

        Assert.Equal("barfunction foo() {\n    barconsole.log(a);\n}", Model.GetValue());
        AssertSelections(new Selection(2, 5, 2, 8), new Selection(1, 1, 1, 4));
    }

    // Monaco: 'snippets, just text'
    [Fact]
    public void JustText()
    {
        Insert("foobar");

        Assert.Equal("foobarfunction foo() {\n    foobarconsole.log(a);\n}", Model.GetValue());
        AssertSelections(new Selection(1, 7, 1, 7), new Selection(2, 11, 2, 11));
    }

    // Monaco: 'snippets, selections and new text with newlines'
    [Fact]
    public void SelectionsAndNewTextWithNewlines()
    {
        SnippetSession session = Insert("foo\n\t${1:bar}\n$0");

        Assert.Equal("foo\n    bar\nfunction foo() {\n    foo\n        bar\n    console.log(a);\n}", Model.GetValue());
        AssertSelections(new Selection(2, 5, 2, 8), new Selection(5, 9, 5, 12));

        session.Next();
        AssertSelections(new Selection(3, 1, 3, 1), new Selection(6, 5, 6, 5));
    }

    // Monaco: 'snippets, newline NO whitespace adjust'
    [Fact]
    public void NewlineNoWhitespaceAdjust()
    {
        Context.SetSelections([new Selection(2, 5, 2, 5)]);

        Insert("abc\n    foo\n        bar\n$0", new SnippetSessionInsertOptions { AdjustWhitespace = false });

        Assert.Equal("function foo() {\n    abc\n    foo\n        bar\nconsole.log(a);\n}", Model.GetValue());
    }

    // Monaco: 'snippets, insert shorter snippet into non-empty selection'
    [Fact]
    public void InsertShorterSnippetIntoNonEmptySelection()
    {
        SetValue("foo_bar_foo");
        Context.SetSelections([new Selection(1, 1, 1, 4), new Selection(1, 9, 1, 12)]);

        Insert("x$0");

        Assert.Equal("x_bar_x", Model.GetValue());
        AssertSelections(new Selection(1, 2, 1, 2), new Selection(1, 8, 1, 8));
    }

    // Monaco: 'snippets, insert longer snippet into non-empty selection'
    [Fact]
    public void InsertLongerSnippetIntoNonEmptySelection()
    {
        SetValue("foo_bar_foo");
        Context.SetSelections([new Selection(1, 1, 1, 4), new Selection(1, 9, 1, 12)]);

        Insert("LONGER$0");

        Assert.Equal("LONGER_bar_LONGER", Model.GetValue());
        AssertSelections(new Selection(1, 7, 1, 7), new Selection(1, 18, 1, 18));
    }

    // Monaco: 'snippets, selections and snippet ranges'
    [Fact]
    public void SelectionsAndSnippetRanges()
    {
        SnippetSession session = Insert("${1:foo}farboo${2:bar}$0");
        Assert.Equal("foofarboobarfunction foo() {\n    foofarboobarconsole.log(a);\n}", Model.GetValue());
        AssertSelections(new Selection(1, 1, 1, 4), new Selection(2, 5, 2, 8));

        Assert.True(session.IsSelectionWithinPlaceholders());

        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Assert.False(session.IsSelectionWithinPlaceholders());

        Context.SetSelections([new Selection(1, 6, 1, 6), new Selection(2, 10, 2, 10)]);
        Assert.False(session.IsSelectionWithinPlaceholders()); // in snippet, outside placeholder

        Context.SetSelections([new Selection(1, 6, 1, 6), new Selection(2, 10, 2, 10), new Selection(1, 1, 1, 1)]);
        Assert.False(session.IsSelectionWithinPlaceholders()); // in snippet, outside placeholder

        Context.SetSelections([new Selection(1, 6, 1, 6), new Selection(2, 10, 2, 10), new Selection(2, 20, 2, 21)]);
        Assert.False(session.IsSelectionWithinPlaceholders());

        // Reset the selection to the placeholder.
        session.Next();
        Assert.True(session.IsSelectionWithinPlaceholders());
        AssertSelections(new Selection(1, 10, 1, 13), new Selection(2, 14, 2, 17));

        // Reset the selection to the placeholder.
        session.Next();
        Assert.True(session.IsSelectionWithinPlaceholders());
        Assert.True(session.IsAtLastPlaceholder);
        AssertSelections(new Selection(1, 13, 1, 13), new Selection(2, 17, 2, 17));
    }

    // Monaco: 'snippets, nested sessions'
    [Fact]
    public void NestedSessions()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        SnippetSession first = Insert("foo${2:bar}foo$0");
        Assert.Equal("foobarfoo", Model.GetValue());
        AssertSelections(new Selection(1, 4, 1, 7));

        SnippetSession second = Insert("ba${1:zzzz}$0");
        Assert.Equal("foobazzzzfoo", Model.GetValue());
        AssertSelections(new Selection(1, 6, 1, 10));

        second.Next();
        Assert.True(second.IsAtLastPlaceholder);
        AssertSelections(new Selection(1, 10, 1, 10));

        first.Next();
        Assert.True(first.IsAtLastPlaceholder);
        AssertSelections(new Selection(1, 13, 1, 13));
    }

    // Monaco: 'snippets, typing at final tabstop'
    [Fact]
    public void TypingAtFinalTabstop()
    {
        SnippetSession session = Insert("farboo$0");
        Assert.True(session.IsAtLastPlaceholder);
        Assert.False(session.IsSelectionWithinPlaceholders());

        Type("XXX");

        Assert.False(session.IsSelectionWithinPlaceholders());
    }

    // Monaco: 'snippets, typing at beginning'
    [Fact]
    public void TypingAtBeginning()
    {
        Context.SetSelections([new Selection(1, 2, 1, 2)]);
        SnippetSession session = Insert("farboo$0");

        Context.SetSelections([new Selection(1, 2, 1, 2)]);
        Assert.False(session.IsSelectionWithinPlaceholders());
        Assert.True(session.IsAtLastPlaceholder);

        Type("XXX");
        Assert.Equal("fXXXfarboounction foo() {", Model.GetLineContent(1));
        Assert.False(session.IsSelectionWithinPlaceholders());

        session.Next();
        AssertSelections(new Selection(1, 11, 1, 11));
    }

    // Monaco: 'snippets, snippet with variables'
    [Fact]
    public void SnippetWithVariables()
    {
        Insert("@line=$TM_LINE_NUMBER$0");

        Assert.Equal("@line=1function foo() {\n    @line=2console.log(a);\n}", Model.GetValue());
        AssertSelections(new Selection(1, 8, 1, 8), new Selection(2, 12, 2, 12));
    }

    // Monaco: 'snippets, merge does not throw when placeholder occurrences collapse to same position'
    [Fact]
    public void MergeDoesNotThrowWhenPlaceholderOccurrencesCollapse()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("$1$1$0");

        session.Merge("${1:nested}$0");

        Assert.Equal("nested", Model.GetValue());
    }

    // Monaco: 'snippets, transform'
    [Fact]
    public void Transform()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("${1/foo/bar/}$0");
        AssertSelections(new Selection(1, 1, 1, 1));

        Type("foo");
        session.Next();

        Assert.Equal("bar", Model.GetValue());
        Assert.True(session.IsAtLastPlaceholder);
        AssertSelections(new Selection(1, 4, 1, 4));
    }

    // Monaco: 'snippets, multi placeholder same index one transform'
    [Fact]
    public void MultiPlaceholderSameIndexOneTransform()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("$1 baz ${1/foo/bar/}$0");
        AssertSelections(new Selection(1, 1, 1, 1), new Selection(1, 6, 1, 6));

        Type("foo");
        session.Next();

        Assert.Equal("foo baz bar", Model.GetValue());
        Assert.True(session.IsAtLastPlaceholder);
        AssertSelections(new Selection(1, 12, 1, 12));
    }

    // Monaco: 'snippets, transform example'
    [Fact]
    public void TransformExample()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("${1:name} : ${2:type}${3/\\s:=(.*)/${1:+ :=}${1}/};\n$0");

        AssertSelections(new Selection(1, 1, 1, 5));
        Type("clk");
        session.Next();

        AssertSelections(new Selection(1, 7, 1, 11));
        Type("std_logic");
        session.Next();

        AssertSelections(new Selection(1, 16, 1, 16));
        session.Next();

        Assert.Equal("clk : std_logic;\n", Model.GetValue());
        Assert.True(session.IsAtLastPlaceholder);
        AssertSelections(new Selection(2, 1, 2, 1));
    }

    // Monaco: 'snippets, transform with indent'
    [Fact]
    public void TransformWithIndent()
    {
        string snippet = string.Join('\n', "private readonly ${1} = new Emitter<$2>();", "readonly ${1/^_(.*)/$1/}: Event<$2> = this.$1.event;", "$0");
        string expected = string.Join('\n', "{", "\tprivate readonly _prop = new Emitter<string>();", "\treadonly prop: Event<string> = this._prop.event;", "\t", "}");

        SetValue(string.Join('\n', "{", "\t", "}"));
        Model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: false));
        Context.SetSelections([new Selection(2, 2, 2, 2)]);

        SnippetSession session = Insert(snippet);

        AssertSelections(new Selection(2, 19, 2, 19), new Selection(3, 11, 3, 11), new Selection(3, 28, 3, 28));
        Type("_prop");
        session.Next();

        AssertSelections(new Selection(2, 39, 2, 39), new Selection(3, 23, 3, 23));
        Type("string");
        session.Next();

        Assert.Equal(expected, Model.GetValue());
        Assert.True(session.IsAtLastPlaceholder);
        AssertSelections(new Selection(4, 2, 4, 2));
    }

    // Monaco: 'snippets, transform example hit if'
    [Fact]
    public void TransformExampleHitIf()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("${1:name} : ${2:type}${3/\\s:=(.*)/${1:+ :=}${1}/};\n$0");

        AssertSelections(new Selection(1, 1, 1, 5));
        Type("clk");
        session.Next();

        AssertSelections(new Selection(1, 7, 1, 11));
        Type("std_logic");
        session.Next();

        AssertSelections(new Selection(1, 16, 1, 16));
        Type(" := '1'");
        session.Next();

        Assert.Equal("clk : std_logic := '1';\n", Model.GetValue());
        Assert.True(session.IsAtLastPlaceholder);
        AssertSelections(new Selection(2, 1, 2, 1));
    }

    // Monaco: 'Snippet tab stop selection issue #96545, snippets, transform adjacent to previous placeholder'
    [Fact]
    public void Issue96545_TransformAdjacentToPreviousPlaceholder()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("${1:{}${2:fff}${1/{/}/}");

        AssertSelections(new Selection(1, 1, 1, 2), new Selection(1, 5, 1, 6));
        session.Next();

        Assert.Equal("{fff}", Model.GetValue());
        AssertSelections(new Selection(1, 2, 1, 5));
        Type("ggg");
        session.Next();

        Assert.Equal("{ggg}", Model.GetValue());
        Assert.True(session.IsAtLastPlaceholder);
        AssertSelections(new Selection(1, 6, 1, 6));
    }

    // Monaco: 'Snippet tab stop selection issue #96545'
    [Fact]
    public void Issue96545()
    {
        SetValue(string.Empty);
        SnippetSession session = Insert("${1:{}${2:fff}${1/[\\{]/}/}$0");
        Assert.Equal("{fff{", Model.GetValue());

        AssertSelections(new Selection(1, 1, 1, 2), new Selection(1, 5, 1, 6));
        session.Next();
        AssertSelections(new Selection(1, 2, 1, 5));
    }

    // Monaco: 'Snippet placeholder index incorrect after using 2+ snippets in a row that each end with a placeholder, #30769'
    [Fact]
    public void Issue30769_SnippetsInARowThatEndWithAPlaceholder()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        SnippetSession session = Insert("test ${1:replaceme}");

        Type("1");
        Type("\n");
        Assert.Equal("test 1\n", Model.GetValue());

        string expected = "test 1\n";
        for (int i = 2; i <= 4; i++)
        {
            session.Merge("test ${1:replaceme}");
            Type(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Type("\n");
            expected += $"test {i}\n";
            Assert.Equal(expected, Model.GetValue());
        }
    }
}

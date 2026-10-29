using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Snippet;

namespace LoomEditor.View.Tests.Features.Snippet;

// Monaco: suite 'SnippetController2' (snippetController2.test.ts). Not ported (later): the suite 'createEditsAndSnippetsFromEdits' and the
// other tests that use apply(edits) (EOL-Sequence ... #167386, "Surround With" ... #169319, $TM_SELECTED_TEXT ... #206121, apply with
// multiple cursors ... $CURSOR_NUMBER, undo restores original selection after apply #170041), and the skipped test #112362.
public partial class SnippetControllerTests : IDisposable
{
    public SnippetControllerTests()
    {
        Context = new EditorContext();
        _feature = new SnippetFeature();
        _feature.Attach(Context);
        Context.SetText("if\n    $state\nfi");
        Context.SetSelections([new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5)]);
        Assert.Equal("\n", Model.Eol);
    }

    private readonly SnippetFeature _feature;

    private EditorContext Context { get; }

    private TextModel Model => Context.Model;

    private SnippetController Ctrl => Context.SnippetController!;

    public void Dispose()
    {
        _feature.Dispose();
        Context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Insert(string template, SnippetInsertOptions? options = null) => Ctrl.Insert(template, options);

    private void SetValue(string value) => Model.SetValue(value);

    private void Type(string text) => Context.ViewModel.Type(text, "test");

    private void AssertSelections(params Selection[] expected)
    {
        IReadOnlyList<Selection> actual = Context.Selections;
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(actual[i] == expected[i], $"actual={actual[i]} <> expected={expected[i]}");
        }
    }

    private void AssertContextKeys(bool inSnippet, bool hasPrev, bool hasNext)
    {
        Assert.Equal(inSnippet, Context.InSnippetMode);
        Assert.Equal(hasPrev, Context.HasPrevTabstop);
        Assert.Equal(hasNext, Context.HasNextTabstop);
    }

    // Monaco: 'creation'
    [Fact]
    public void Creation()
    {
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'insert, insert -> abort'
    [Fact]
    public void InsertAbort()
    {
        Insert("foo${1:bar}foo$0");
        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 4, 1, 7), new Selection(2, 8, 2, 11));

        Ctrl.Cancel();
        AssertContextKeys(false, false, false);
        AssertSelections(new Selection(1, 4, 1, 7), new Selection(2, 8, 2, 11));
    }

    // Monaco: 'insert, insert -> tab, tab, done'
    [Fact]
    public void InsertTabTabDone()
    {
        Insert("${1:one}${2:two}$0");
        AssertContextKeys(true, false, true);

        Ctrl.Next();
        AssertContextKeys(true, true, true);

        Ctrl.Next();
        AssertContextKeys(false, false, false);

        Type("\t");
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'insert, insert -> cursor moves out (left/right)'
    [Fact]
    public void CursorMovesOutLeftRight()
    {
        Insert("foo${1:bar}foo$0");
        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 4, 1, 7), new Selection(2, 8, 2, 11));

        Context.SetSelections([new Selection(1, 12, 1, 12), new Selection(2, 16, 2, 16)]);
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'insert, insert -> cursor moves out (up/down)'
    [Fact]
    public void CursorMovesOutUpDown()
    {
        Insert("foo${1:bar}foo$0");
        AssertContextKeys(true, false, true);

        Context.SetSelections([new Selection(2, 4, 2, 7), new Selection(3, 8, 3, 11)]);
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'insert, insert -> cursors collapse'
    [Fact]
    public void CursorsCollapse()
    {
        Insert("foo${1:bar}foo$0");
        Assert.True(Context.InSnippetMode);
        AssertSelections(new Selection(1, 4, 1, 7), new Selection(2, 8, 2, 11));

        Context.SetSelections([new Selection(1, 4, 1, 7)]);
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'insert, insert plain text -> no snippet mode'
    [Fact]
    public void InsertPlainTextNoSnippetMode()
    {
        Insert("foobar");
        AssertContextKeys(false, false, false);
        AssertSelections(new Selection(1, 7, 1, 7), new Selection(2, 11, 2, 11));
    }

    // Monaco: 'insert, delete snippet text'
    [Fact]
    public void DeleteSnippetText()
    {
        Insert("${1:foobar}$0");
        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 1, 1, 7), new Selection(2, 5, 2, 11));

        Context.ViewModel.Cut("test");
        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5));

        Type("abc");
        AssertContextKeys(true, false, true);

        Ctrl.Next();
        AssertContextKeys(false, false, false);

        Context.Commands.Execute("tab");
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'insert, nested trivial snippet'
    [Fact]
    public void NestedTrivialSnippet()
    {
        Insert("${1:foo}bar$0");
        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 1, 1, 4), new Selection(2, 5, 2, 8));

        Insert("FOO$0");
        AssertSelections(new Selection(1, 4, 1, 4), new Selection(2, 8, 2, 8));
        AssertContextKeys(true, false, true);

        Ctrl.Next();
        AssertSelections(new Selection(1, 7, 1, 7), new Selection(2, 11, 2, 11));
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'insert, nested snippet'
    [Fact]
    public void NestedSnippet()
    {
        Insert("${1:foobar}$0");
        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 1, 1, 7), new Selection(2, 5, 2, 11));

        Insert("far$1boo$0");
        AssertSelections(new Selection(1, 4, 1, 4), new Selection(2, 8, 2, 8));
        AssertContextKeys(true, false, true);

        Ctrl.Next();
        AssertSelections(new Selection(1, 7, 1, 7), new Selection(2, 11, 2, 11));
        AssertContextKeys(true, true, true);

        Ctrl.Next();
        AssertSelections(new Selection(1, 7, 1, 7), new Selection(2, 11, 2, 11));
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'insert, nested plain text'
    [Fact]
    public void NestedPlainText()
    {
        Insert("${1:foobar}$0");
        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 1, 1, 7), new Selection(2, 5, 2, 11));

        Insert("farboo");
        AssertSelections(new Selection(1, 7, 1, 7), new Selection(2, 11, 2, 11));
        AssertContextKeys(true, false, true);

        Ctrl.Next();
        AssertSelections(new Selection(1, 7, 1, 7), new Selection(2, 11, 2, 11));
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'Nested snippets without final placeholder jumps to next outer placeholder, #27898'
    [Fact]
    public void Issue27898_NestedWithoutFinalPlaceholderJumpsToNextOuter()
    {
        Insert("for(const ${1:element} of ${2:array}) {$0}");
        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 11, 1, 18), new Selection(2, 15, 2, 22));

        Ctrl.Next();
        AssertContextKeys(true, true, true);
        AssertSelections(new Selection(1, 22, 1, 27), new Selection(2, 26, 2, 31));

        Insert("document");
        AssertContextKeys(true, true, true);
        AssertSelections(new Selection(1, 30, 1, 30), new Selection(2, 34, 2, 34));

        Ctrl.Next();
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'Inconsistent tab stop behaviour with recursive snippets and tab / shift tab, #27543'
    [Fact]
    public void Issue27543_RecursiveSnippetsTabShiftTab()
    {
        Insert("1_calize(${1:nl}, '${2:value}')$0");

        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 10, 1, 12), new Selection(2, 14, 2, 16));

        Insert("2_calize(${1:nl}, '${2:value}')$0");

        AssertSelections(new Selection(1, 19, 1, 21), new Selection(2, 23, 2, 25));

        Ctrl.Next(); // inner `value`
        AssertSelections(new Selection(1, 24, 1, 29), new Selection(2, 28, 2, 33));

        Ctrl.Next(); // inner `$0`
        AssertSelections(new Selection(1, 31, 1, 31), new Selection(2, 35, 2, 35));

        Ctrl.Next(); // outer `value`
        AssertSelections(new Selection(1, 34, 1, 39), new Selection(2, 38, 2, 43));

        Ctrl.Prev(); // inner `$0`
        AssertSelections(new Selection(1, 31, 1, 31), new Selection(2, 35, 2, 35));
    }

    // Monaco: 'Snippet tabstop selecting content of previously entered variable only works when separated by space, #23728'
    [Fact]
    public void Issue23728_TabstopSelectingContentOfPreviouslyEnteredVariable()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Insert("import ${2:${1:module}} from '${1:module}'$0");

        AssertContextKeys(true, false, true);
        AssertSelections(new Selection(1, 8, 1, 14), new Selection(1, 21, 1, 27));

        Insert("foo");
        AssertSelections(new Selection(1, 11, 1, 11), new Selection(1, 21, 1, 21));

        Ctrl.Next(); // ${2:...}
        AssertSelections(new Selection(1, 8, 1, 11));
    }

    // Monaco: 'HTML Snippets Combine, #32211'
    [Fact]
    public void Issue32211_HtmlSnippetsCombine()
    {
        SetValue(string.Empty);
        Model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: false, TabSize: 4, TrimAutoWhitespace: false));
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Insert("\n\t\t\t<!DOCTYPE html>\n\t\t\t<html lang=\"en\">\n\t\t\t<head>\n\t\t\t\t<meta charset=\"UTF-8\">\n\t\t\t\t<meta name=\"viewport\" content=\"width=${2:device-width}, initial-scale=${3:1.0}\">\n\t\t\t\t<meta http-equiv=\"X-UA-Compatible\" content=\"${5:ie=edge}\">\n\t\t\t\t<title>${7:Document}</title>\n\t\t\t</head>\n\t\t\t<body>\n\t\t\t\t${8}\n\t\t\t</body>\n\t\t\t</html>\n\t\t");
        Ctrl.Next();
        Ctrl.Next();
        Ctrl.Next();
        Ctrl.Next();
        AssertSelections(new Selection(11, 5, 11, 5));

        Insert("<input type=\"${2:text}\">");
        AssertSelections(new Selection(11, 18, 11, 22));
    }

    // Monaco: 'Problems with nested snippet insertion #39594'
    [Fact]
    public void Issue39594_NestedSnippetInsertion()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Insert("$1 = ConvertTo-Json $1");
        AssertSelections(new Selection(1, 1, 1, 1), new Selection(1, 19, 1, 19));

        Context.SetSelections([new Selection(1, 19, 1, 19)]);

        // Snippet mode should stop because $1 has two occurrences and we only have one selection left.
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'Problems with nested snippet insertion #39594 (part2)'
    [Fact]
    public void Issue39594_NestedSnippetInsertion_Part2()
    {
        // Ensure the selection-change-to-cancel logic is not too aggressive.
        SetValue("a-\naaa-");
        Context.SetSelections([new Selection(2, 5, 2, 5), new Selection(1, 3, 1, 3)]);

        Insert("log($1);$0");
        AssertSelections(new Selection(2, 9, 2, 9), new Selection(1, 7, 1, 7));
        AssertContextKeys(true, false, true);
    }

    // Monaco: '“Nested” snippets terminating abruptly in VSCode 1.19.2. #42012'
    [Fact]
    public void Issue42012_NestedSnippetsTerminatingAbruptly()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Insert("var ${2:${1:name}} = ${1:name} + 1;${0}");

        AssertSelections(new Selection(1, 5, 1, 9), new Selection(1, 12, 1, 16));
        AssertContextKeys(true, false, true);

        Ctrl.Next();
        AssertContextKeys(true, true, true);
    }

    // Monaco: 'Placeholders order #58267'
    [Fact]
    public void Issue58267_PlaceholdersOrder()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Insert("\\pth{$1}$0");

        AssertSelections(new Selection(1, 6, 1, 6));
        AssertContextKeys(true, false, true);

        Insert("\\itv{${1:left}}{${2:right}}{${3:left_value}}{${4:right_value}}$0");
        AssertSelections(new Selection(1, 11, 1, 15));

        Ctrl.Next();
        AssertSelections(new Selection(1, 17, 1, 22));

        Ctrl.Next();
        AssertSelections(new Selection(1, 24, 1, 34));

        Ctrl.Next();
        AssertSelections(new Selection(1, 36, 1, 47));

        Ctrl.Next();
        AssertSelections(new Selection(1, 48, 1, 48));

        Ctrl.Next();
        AssertSelections(new Selection(1, 49, 1, 49));
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'Must tab through deleted tab stops in snippets #31619'
    [Fact]
    public void Issue31619_MustTabThroughDeletedTabStops()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Insert("foo${1:a${2:bar}baz}end$0");
        AssertSelections(new Selection(1, 4, 1, 11));

        Context.ViewModel.Cut("test");
        AssertSelections(new Selection(1, 4, 1, 4));

        Ctrl.Next();
        AssertSelections(new Selection(1, 7, 1, 7));
        AssertContextKeys(false, false, false);
    }
}

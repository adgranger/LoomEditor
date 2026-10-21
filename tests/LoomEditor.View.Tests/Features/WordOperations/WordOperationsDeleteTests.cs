// Ported from microsoft/vscode: src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts (the delete tests)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features.WordOperations;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.WordTestUtils;

namespace LoomEditor.View.Tests.Features.WordOperations;

public class WordOperationsDeleteTests
{
    private static readonly DeleteWordLeftCommand DeleteWordLeft = new(WordOperationsCommands.DeleteWordLeft, true, WordNavigationType.WordStart);
    private static readonly DeleteWordLeftCommand DeleteWordStartLeft = new(WordOperationsCommands.DeleteWordStartLeft, false, WordNavigationType.WordStart);
    private static readonly DeleteWordLeftCommand DeleteWordEndLeft = new(WordOperationsCommands.DeleteWordEndLeft, false, WordNavigationType.WordEnd);
    private static readonly DeleteWordRightCommand DeleteWordRight = new(WordOperationsCommands.DeleteWordRight, true, WordNavigationType.WordEnd);
    private static readonly DeleteWordRightCommand DeleteWordStartRight = new(WordOperationsCommands.DeleteWordStartRight, false, WordNavigationType.WordStart);
    private static readonly DeleteWordRightCommand DeleteWordEndRight = new(WordOperationsCommands.DeleteWordEndRight, false, WordNavigationType.WordEnd);

    private static readonly string[] SampleLines = ["    \tMy First Line\t ", "\tMy Second Line", "    Third Line🐶", string.Empty, "1"];

    private static void SetPosition(TestCodeEditor editor, int lineNumber, int column) =>
        editor.ViewModel.SetSelections("test", [new Selection(lineNumber, column, lineNumber, column)]);

    private static Position GetPosition(TestCodeEditor editor) => editor.ViewModel.GetPosition();

    private static string Line(TestCodeEditor editor, int lineNumber) => editor.Model.GetLineContent(lineNumber);

    private static void DeleteInsideWord(TestCodeEditor editor, bool onlyWord = false) =>
        LoomEditor.View.Features.WordOperations.DeleteInsideWord.Run(editor.ViewModel, onlyWord);

    // Monaco: 'deleteWordLeft for non-empty selection'
    [Fact]
    public void DeleteWordLeft_ForNonEmptySelection() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        editor.ViewModel.SetSelections("test", [new Selection(3, 7, 3, 9)]);
        DeleteWordLeft.RunEditorCommand(editor.ViewModel);
        Assert.Equal("    Thd Line🐶", Line(editor, 3));
        Assert.Equal(new Position(3, 7), GetPosition(editor));
    });

    // Monaco: 'deleteWordLeft for cursor at beginning of document'
    [Fact]
    public void DeleteWordLeft_ForCursorAtBeginningOfDocument() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        SetPosition(editor, 1, 1);
        DeleteWordLeft.RunEditorCommand(editor.ViewModel);
        Assert.Equal("    \tMy First Line\t ", Line(editor, 1));
        Assert.Equal(new Position(1, 1), GetPosition(editor));
    });

    // Monaco: 'deleteWordLeft for cursor at end of whitespace'
    [Fact]
    public void DeleteWordLeft_ForCursorAtEndOfWhitespace() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        SetPosition(editor, 3, 11);
        DeleteWordLeft.RunEditorCommand(editor.ViewModel);
        Assert.Equal("    Line🐶", Line(editor, 3));
        Assert.Equal(new Position(3, 5), GetPosition(editor));
    });

    // Monaco: 'deleteWordLeft for cursor just behind a word'
    [Fact]
    public void DeleteWordLeft_ForCursorJustBehindAWord() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        SetPosition(editor, 2, 11);
        DeleteWordLeft.RunEditorCommand(editor.ViewModel);
        Assert.Equal("\tMy  Line", Line(editor, 2));
        Assert.Equal(new Position(2, 5), GetPosition(editor));
    });

    // Monaco: 'deleteWordLeft for cursor inside of a word'
    [Fact]
    public void DeleteWordLeft_ForCursorInsideOfAWord() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        SetPosition(editor, 1, 12);
        DeleteWordLeft.RunEditorCommand(editor.ViewModel);
        Assert.Equal("    \tMy st Line\t ", Line(editor, 1));
        Assert.Equal(new Position(1, 9), GetPosition(editor));
    });

    // Monaco: 'deleteWordRight for non-empty selection'
    [Fact]
    public void DeleteWordRight_ForNonEmptySelection() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        editor.ViewModel.SetSelections("test", [new Selection(3, 7, 3, 9)]);
        DeleteWordRight.RunEditorCommand(editor.ViewModel);
        Assert.Equal("    Thd Line🐶", Line(editor, 3));
        Assert.Equal(new Position(3, 7), GetPosition(editor));
    });

    // Monaco: 'deleteWordRight for cursor at end of document'
    [Fact]
    public void DeleteWordRight_ForCursorAtEndOfDocument() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        SetPosition(editor, 5, 3);
        DeleteWordRight.RunEditorCommand(editor.ViewModel);
        Assert.Equal("1", Line(editor, 5));
        Assert.Equal(new Position(5, 2), GetPosition(editor));
    });

    // Monaco: 'deleteWordRight for cursor at beggining of whitespace'
    [Fact]
    public void DeleteWordRight_ForCursorAtBeginningOfWhitespace() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        SetPosition(editor, 3, 1);
        DeleteWordRight.RunEditorCommand(editor.ViewModel);
        Assert.Equal("Third Line🐶", Line(editor, 3));
        Assert.Equal(new Position(3, 1), GetPosition(editor));
    });

    // Monaco: 'deleteWordRight for cursor just before a word'
    [Fact]
    public void DeleteWordRight_ForCursorJustBeforeAWord() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        SetPosition(editor, 2, 5);
        DeleteWordRight.RunEditorCommand(editor.ViewModel);
        Assert.Equal("\tMy  Line", Line(editor, 2));
        Assert.Equal(new Position(2, 5), GetPosition(editor));
    });

    // Monaco: 'deleteWordRight for cursor inside of a word'
    [Fact]
    public void DeleteWordRight_ForCursorInsideOfAWord() => TestCodeEditor.WithTestCodeEditor(SampleLines, editor =>
    {
        SetPosition(editor, 1, 11);
        DeleteWordRight.RunEditorCommand(editor.ViewModel);
        Assert.Equal("    \tMy Fi Line\t ", Line(editor, 1));
        Assert.Equal(new Position(1, 11), GetPosition(editor));
    });

    // The shared body of the pipe tests that delete until the text is empty.
    private static void AssertDeleteStops(string expected, Position initialPosition, Action<TestCodeEditor> action, Func<TestCodeEditor, Position>? record = null)
    {
        (string text, _) = DeserializePipePositions(expected);
        List<Position> actualStops = TestRepeatedActionAndExtractPositions(
            text,
            initialPosition,
            action,
            record ?? GetPosition,
            ed => ed.Model.GetValue().Length == 0);
        Assert.Equal(expected, SerializePipePositions(text, actualStops));
    }

    // Monaco: 'deleteWordLeft - issue #832'
    [Fact]
    public void DeleteWordLeft_Issue832() => AssertDeleteStops(
        "|   |/* |Just |some |text |a|+= |3 |+|5 |*/|  ",
        new Position(1000, 10000),
        ed => DeleteWordLeft.RunEditorCommand(ed.ViewModel));

    // Monaco: 'deleteWordStartLeft'
    [Fact]
    public void DeleteWordStartLeft_Test() => AssertDeleteStops(
        "|   |/* |Just |some |text |a|+= |3 |+|5 |*/  ",
        new Position(1000, 10000),
        ed => DeleteWordStartLeft.RunEditorCommand(ed.ViewModel));

    // Monaco: 'deleteWordEndLeft'
    [Fact]
    public void DeleteWordEndLeft_Test() => AssertDeleteStops(
        "|   /*| Just| some| text| a|+=| 3| +|5| */|  ",
        new Position(1000, 10000),
        ed => DeleteWordEndLeft.RunEditorCommand(ed.ViewModel));

    // Monaco: 'deleteWordLeft - issue #24947'
    [Fact]
    public void DeleteWordLeft_Issue24947()
    {
        TestCodeEditor.WithTestCodeEditor(["{", "}"], editor =>
        {
            SetPosition(editor, 2, 1);
            DeleteWordLeft.RunEditorCommand(editor.ViewModel);
            Assert.Equal("{}", Line(editor, 1));
        });

        TestCodeEditor.WithTestCodeEditor(["{", "}"], editor =>
        {
            SetPosition(editor, 2, 1);
            DeleteWordStartLeft.RunEditorCommand(editor.ViewModel);
            Assert.Equal("{}", Line(editor, 1));
        });

        TestCodeEditor.WithTestCodeEditor(["{", "}"], editor =>
        {
            SetPosition(editor, 2, 1);
            DeleteWordEndLeft.RunEditorCommand(editor.ViewModel);
            Assert.Equal("{}", Line(editor, 1));
        });
    }

    // Monaco: 'deleteWordRight - issue #832'
    [Fact]
    public void DeleteWordRight_Issue832()
    {
        const string expected = "   |/*| |Just| |some| |text| |a|+=| |3| |+|5|-|3| |*/|  |";
        (string text, _) = DeserializePipePositions(expected);
        AssertDeleteStops(expected, new Position(1, 1), ed => DeleteWordRight.RunEditorCommand(ed.ViewModel), ed => new Position(1, text.Length - ed.Model.GetValue().Length + 1));
    }

    // Monaco: 'deleteWordRight - issue #3882'
    [Fact]
    public void DeleteWordRight_Issue3882() => TestCodeEditor.WithTestCodeEditor(["public void Add( int x,", "                 int y )"], editor =>
    {
        SetPosition(editor, 1, 24);
        DeleteWordRight.RunEditorCommand(editor.ViewModel);
        Assert.Equal("public void Add( int x,int y )", Line(editor, 1));
    });

    // Monaco: 'deleteWordStartRight - issue #3882'
    [Fact]
    public void DeleteWordStartRight_Issue3882() => TestCodeEditor.WithTestCodeEditor(["public void Add( int x,", "                 int y )"], editor =>
    {
        SetPosition(editor, 1, 24);
        DeleteWordStartRight.RunEditorCommand(editor.ViewModel);
        Assert.Equal("public void Add( int x,int y )", Line(editor, 1));
    });

    // Monaco: 'deleteWordEndRight - issue #3882'
    [Fact]
    public void DeleteWordEndRight_Issue3882() => TestCodeEditor.WithTestCodeEditor(["public void Add( int x,", "                 int y )"], editor =>
    {
        SetPosition(editor, 1, 24);
        DeleteWordEndRight.RunEditorCommand(editor.ViewModel);
        Assert.Equal("public void Add( int x,int y )", Line(editor, 1));
    });

    // Monaco: 'deleteWordStartRight'
    [Fact]
    public void DeleteWordStartRight_Test()
    {
        const string expected = "   |/* |Just |some |text |a|+= |3 |+|5|-|3 |*/  |";
        (string text, _) = DeserializePipePositions(expected);
        AssertDeleteStops(expected, new Position(1, 1), ed => DeleteWordStartRight.RunEditorCommand(ed.ViewModel), ed => new Position(1, text.Length - ed.Model.GetValue().Length + 1));
    }

    // Monaco: 'deleteWordEndRight'
    [Fact]
    public void DeleteWordEndRight_Test()
    {
        const string expected = "   /*| Just| some| text| a|+=| 3| +|5|-|3| */|  |";
        (string text, _) = DeserializePipePositions(expected);
        AssertDeleteStops(expected, new Position(1, 1), ed => DeleteWordEndRight.RunEditorCommand(ed.ViewModel), ed => new Position(1, text.Length - ed.Model.GetValue().Length + 1));
    }

    // Monaco: 'deleteWordRight - issue #3882 (1): Ctrl+Delete removing entire line when used at the end of line'
    [Fact]
    public void DeleteWordRight_Issue3882_1() => TestCodeEditor.WithTestCodeEditor(["A line with text.", "   And another one"], editor =>
    {
        SetPosition(editor, 1, 18);
        DeleteWordRight.RunEditorCommand(editor.ViewModel);
        Assert.Equal("A line with text.And another one", Line(editor, 1));
    });

    // Monaco: 'deleteWordLeft - issue #3882 (2): Ctrl+Delete removing entire line when used at the end of line'
    [Fact]
    public void DeleteWordLeft_Issue3882_2() => TestCodeEditor.WithTestCodeEditor(["A line with text.", "   And another one"], editor =>
    {
        SetPosition(editor, 2, 1);
        DeleteWordLeft.RunEditorCommand(editor.ViewModel);
        Assert.Equal("A line with text.   And another one", Line(editor, 1));
    });

    // Monaco: 'deleteInsideWord - empty line'
    [Fact]
    public void DeleteInsideWord_EmptyLine() => TestCodeEditor.WithTestCodeEditor(["Line1", string.Empty, "Line2"], editor =>
    {
        SetPosition(editor, 2, 1);
        DeleteInsideWord(editor);
        Assert.Equal("Line1\nLine2", editor.Model.GetValue());
    });

    // Monaco: 'deleteInsideWord - in whitespace 1'
    [Fact]
    public void DeleteInsideWord_InWhitespace1() => TestCodeEditor.WithTestCodeEditor(["Just  some text."], editor =>
    {
        SetPosition(editor, 1, 6);
        DeleteInsideWord(editor);
        Assert.Equal("Justsome text.", editor.Model.GetValue());
    });

    // Monaco: 'deleteInsideWord - in whitespace 2'
    [Fact]
    public void DeleteInsideWord_InWhitespace2() => TestCodeEditor.WithTestCodeEditor(["Just     some text."], editor =>
    {
        SetPosition(editor, 1, 6);
        DeleteInsideWord(editor);
        Assert.Equal("Justsome text.", editor.Model.GetValue());
    });

    private static void AssertSuccessiveDeletes(TestCodeEditor editor, params string[] expectedValues)
    {
        foreach (string expected in expectedValues)
        {
            DeleteInsideWord(editor);
            Assert.Equal(expected, editor.Model.GetValue());
        }
    }

    // Monaco: 'deleteInsideWord - in whitespace 3'
    [Fact]
    public void DeleteInsideWord_InWhitespace3() => TestCodeEditor.WithTestCodeEditor(["Just     \"some text."], editor =>
    {
        SetPosition(editor, 1, 6);
        AssertSuccessiveDeletes(editor, "Just\"some text.", "\"some text.", "some text.", "text.", ".", string.Empty, string.Empty);
    });

    // Monaco: 'deleteInsideWord - in non-words'
    [Fact]
    public void DeleteInsideWord_InNonWords() => TestCodeEditor.WithTestCodeEditor(["x=3+4+5+6"], editor =>
    {
        SetPosition(editor, 1, 7);
        AssertSuccessiveDeletes(editor, "x=3+45+6", "x=3++6", "x=36", "x=", "x", string.Empty, string.Empty);
    });

    // Monaco: 'deleteInsideWord - in words 1'
    [Fact]
    public void DeleteInsideWord_InWords1() => TestCodeEditor.WithTestCodeEditor(["This is interesting"], editor =>
    {
        SetPosition(editor, 1, 7);
        AssertSuccessiveDeletes(editor, "This interesting", "This", string.Empty, string.Empty);
    });

    // Monaco: 'deleteInsideWord - in words 2'
    [Fact]
    public void DeleteInsideWord_InWords2() => TestCodeEditor.WithTestCodeEditor(["This  is  interesting"], editor =>
    {
        SetPosition(editor, 1, 7);
        AssertSuccessiveDeletes(editor, "This  interesting", "This", string.Empty, string.Empty);
    });

    // Monaco: 'deleteInsideWord - onlyWord: does not delete whitespace before last word'
    [Fact]
    public void DeleteInsideWord_OnlyWord_DoesNotDeleteWhitespaceBeforeLastWord() => TestCodeEditor.WithTestCodeEditor(["hello world"], editor =>
    {
        SetPosition(editor, 1, 9);
        DeleteInsideWord(editor, onlyWord: true);
        Assert.Equal("hello ", editor.Model.GetValue());
    });

    // Monaco: 'deleteInsideWord - onlyWord: deletes just the word (leaves double spaces)'
    // Monaco: 'deleteWordLeft - issue #91855: Matching (quote, bracket, paren) doesn't get deleted when hitting Ctrl+Backspace'
    [Fact]
    public void DeleteWordLeft_Issue91855MatchingQuoteBracketParenGetsDeletedWhenHittingCtrlBackspace()
    {
        using var services = new TestEditorServices();
        const string LanguageId = "myTestMode";
        services.RegisterLanguage(LanguageId);
        services.Add(services.LanguageConfigurationRegistry.Register(LanguageId, new LanguageConfiguration
        {
            AutoClosingPairs = [new AutoClosingPairConditional("\"", "\"")],
        }));
        TextModel model = services.CreateTextModel("a \"\"", LanguageId);

        TestCodeEditor.WithTestCodeEditor(
            model,
            editor =>
            {
                SetPosition(editor, 1, 4);
                DeleteWordLeft.RunEditorCommand(editor.ViewModel);
                Assert.Equal("a ", model.GetLineContent(1));
            },
            new TestCodeEditorOptions(CursorOptions: new EditorCursorOptions { AutoClosingDelete = EditorAutoClosingEditStrategy.Always }));
    }

    [Fact]
    public void DeleteInsideWord_OnlyWord_DeletesJustTheWord() => TestCodeEditor.WithTestCodeEditor(["This is interesting"], editor =>
    {
        SetPosition(editor, 1, 7);
        DeleteInsideWord(editor, onlyWord: true);
        Assert.Equal("This  interesting", editor.Model.GetValue());
    });
}

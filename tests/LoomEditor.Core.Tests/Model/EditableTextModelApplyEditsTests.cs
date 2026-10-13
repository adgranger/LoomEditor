using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tests.Model.TestHelpers;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Ported from Monaco <c>editableTextModel.test.ts</c>: suite <c>EditorModel - EditableTextModel.applyEdits</c>
/// (46 tests).
/// </summary>
public class EditableTextModelApplyEditsTests
{
    private static TextEdit EditOp(int startLineNumber, int startColumn, int endLineNumber, int endColumn, params string[] text) =>
        TextEdit.Replace(new Range(startLineNumber, startColumn, endLineNumber, endColumn), string.Join('\n', text));

    // Monaco: 'high-low surrogates 1'
    [Fact]
    public void HighLowSurrogates1() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["📚some", "very nice", "text"],
            [EditOp(1, 2, 1, 2, "a")],
            ["a📚some", "very nice", "text"], inputEditsAreInvalid: true);

    // Monaco: 'high-low surrogates 2'
    [Fact]
    public void HighLowSurrogates2() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["📚some", "very nice", "text"],
            [EditOp(1, 2, 1, 3, "a")],
            ["asome", "very nice", "text"], inputEditsAreInvalid: true);

    // Monaco: 'high-low surrogates 3'
    [Fact]
    public void HighLowSurrogates3() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["📚some", "very nice", "text"],
            [EditOp(1, 1, 1, 2, "a")],
            ["asome", "very nice", "text"], inputEditsAreInvalid: true);

    // Monaco: 'high-low surrogates 4'
    [Fact]
    public void HighLowSurrogates4() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["📚some", "very nice", "text"],
            [EditOp(1, 1, 1, 3, "a")],
            ["asome", "very nice", "text"], inputEditsAreInvalid: true);

    // Monaco: 'Bug 19872: Undo is funky'
    [Fact]
    public void Bug19872UndoIsFunky() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["something", " A", "", " B", "something else"],
            [EditOp(2, 1, 2, 2, ""), EditOp(3, 1, 4, 2, "")],
            ["something", "A", "B", "something else"]);

    // Monaco: 'Bug 19872: Undo is funky (2)'
    [Fact]
    public void Bug19872UndoIsFunky2() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["something", "A", "B", "something else"],
            [EditOp(2, 1, 2, 1, " "), EditOp(3, 1, 3, 1, "", " ")],
            ["something", " A", "", " B", "something else"]);

    // Monaco: 'insert empty text'
    [Fact]
    public void InsertEmptyText() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 1, 1, 1, "")],
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'last op is no-op'
    [Fact]
    public void LastOpIsNoOp() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 1, 1, 2, ""), EditOp(4, 1, 4, 1, "")],
            ["y First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'insert text without newline 1'
    [Fact]
    public void InsertTextWithoutNewline1() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 1, 1, 1, "foo ")],
            ["foo My First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'insert text without newline 2'
    [Fact]
    public void InsertTextWithoutNewline2() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 3, 1, 3, " foo")],
            ["My foo First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'insert one newline'
    [Fact]
    public void InsertOneNewline() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 4, 1, 4, "", "")],
            ["My ", "First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'insert text with one newline'
    [Fact]
    public void InsertTextWithOneNewline() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 3, 1, 3, " new line", "No longer")],
            ["My new line", "No longer First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'insert text with two newlines'
    [Fact]
    public void InsertTextWithTwoNewlines() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 3, 1, 3, " new line", "One more line in the middle", "No longer")],
            ["My new line", "One more line in the middle", "No longer First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'insert text with many newlines'
    [Fact]
    public void InsertTextWithManyNewlines() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 3, 1, 3, "", "", "", "", "")],
            ["My", "", "", "", " First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'insert multiple newlines'
    [Fact]
    public void InsertMultipleNewlines() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 3, 1, 3, "", "", "", "", ""), EditOp(3, 15, 3, 15, "a", "b")],
            ["My", "", "", "", " First Line", "\t\tMy Second Line", "    Third Linea", "b", "", "1"]);

    // Monaco: 'delete empty text'
    [Fact]
    public void DeleteEmptyText() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 1, 1, 1, "")],
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'delete text from one line'
    [Fact]
    public void DeleteTextFromOneLine() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 1, 1, 2, "")],
            ["y First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'delete text from one line 2'
    [Fact]
    public void DeleteTextFromOneLine2() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 1, 1, 3, "a")],
            ["a First Line", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'delete all text from a line'
    [Fact]
    public void DeleteAllTextFromALine() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 1, 1, 14, "")],
            ["", "\t\tMy Second Line", "    Third Line", "", "1"]);

    // Monaco: 'delete text from two lines'
    [Fact]
    public void DeleteTextFromTwoLines() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 4, 2, 6, "")],
            ["My Second Line", "    Third Line", "", "1"]);

    // Monaco: 'delete text from many lines'
    [Fact]
    public void DeleteTextFromManyLines() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 4, 3, 5, "")],
            ["My Third Line", "", "1"]);

    // Monaco: 'delete everything'
    [Fact]
    public void DeleteEverything() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 1, 5, 2, "")],
            [""]);

    // Monaco: 'two unrelated edits'
    [Fact]
    public void TwoUnrelatedEdits() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "123"],
            [EditOp(2, 1, 2, 3, "\t"), EditOp(3, 1, 3, 5, "")],
            ["My First Line", "\tMy Second Line", "Third Line", "", "123"]);

    // Monaco: 'two edits on one line'
    [Fact]
    public void TwoEditsOnOneLine() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["\t\tfirst\t    ", "\t\tsecond line", "\tthird line", "fourth line", "\t\t<!@#fifth#@!>\t\t"],
            [EditOp(5, 3, 5, 7, ""), EditOp(5, 12, 5, 16, "")],
            ["\t\tfirst\t    ", "\t\tsecond line", "\tthird line", "fourth line", "\t\tfifth\t\t"]);

    // Monaco: 'many edits'
    [Fact]
    public void ManyEdits() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["{\"x\" : 1}"],
            [EditOp(1, 2, 1, 2, "\n  "), EditOp(1, 5, 1, 6, ""), EditOp(1, 9, 1, 9, "\n")],
            ["{", "  \"x\": 1", "}"]);

    // Monaco: 'many edits reversed'
    [Fact]
    public void ManyEditsReversed() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["{", "  \"x\": 1", "}"],
            [EditOp(1, 2, 2, 3, ""), EditOp(2, 6, 2, 6, " "), EditOp(2, 9, 3, 1, "")],
            ["{\"x\" : 1}"]);

    // Monaco: 'replacing newlines 1'
    [Fact]
    public void ReplacingNewlines1() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["{", "\"a\": true,", "", "\"b\": true", "}"],
            [EditOp(1, 2, 2, 1, "", "\t"), EditOp(2, 11, 4, 1, "", "\t")],
            ["{", "\t\"a\": true,", "\t\"b\": true", "}"]);

    // Monaco: 'replacing newlines 2'
    [Fact]
    public void ReplacingNewlines2() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["some text", "some more text", "now comes an empty line", "", "after empty line", "and the last line"],
            [EditOp(1, 5, 3, 1, " text", "some more text", "some more text"), EditOp(3, 2, 4, 1, "o more lines", "asd", "asd", "asd"), EditOp(5, 1, 5, 6, "zzzzzzzz"), EditOp(5, 11, 6, 16, "1", "2", "3", "4")],
            ["some text", "some more text", "some more textno more lines", "asd", "asd", "asd", "zzzzzzzz empt1", "2", "3", "4ne"]);

    // Monaco: 'advanced 1'
    [Fact]
    public void Advanced1() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            [" {       \"d\": [", "             null", "        ] /*comment*/", "        ,\"e\": /*comment*/ [null] }"],
            [EditOp(1, 1, 1, 2, ""), EditOp(1, 3, 1, 10, "", "  "), EditOp(1, 16, 2, 14, "", "    "), EditOp(2, 18, 3, 9, "", "  "), EditOp(3, 22, 4, 9, ""), EditOp(4, 10, 4, 10, "", "  "), EditOp(4, 28, 4, 28, "", "    "), EditOp(4, 32, 4, 32, "", "  "), EditOp(4, 33, 4, 34, "", "")],
            ["{", "  \"d\": [", "    null", "  ] /*comment*/,", "  \"e\": /*comment*/ [", "    null", "  ]", "}"]);

    // Monaco: 'advanced simplified'
    [Fact]
    public void AdvancedSimplified() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["   abc", " ,def"],
            [EditOp(1, 1, 1, 4, ""), EditOp(1, 7, 2, 2, ""), EditOp(2, 3, 2, 3, "", "")],
            ["abc,", "def"]);

    // Monaco: 'issue #144'
    [Fact]
    public void Issue144() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["package caddy", "", "func main() {", "\tfmt.Println(\"Hello World! :)\")", "}", ""],
            [EditOp(1, 1, 6, 1, "package caddy", "", "import \"fmt\"", "", "func main() {", "\tfmt.Println(\"Hello World! :)\")", "}", "")],
            ["package caddy", "", "import \"fmt\"", "", "func main() {", "\tfmt.Println(\"Hello World! :)\")", "}", ""]);

    // Monaco: 'issue #2586 Replacing selected end-of-line with newline locks up the document'
    [Fact]
    public void Issue2586ReplacingSelectedEndOfLineWithNewlineLocksUpTheDocument() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["something", "interesting"],
            [EditOp(1, 10, 2, 1, "", "")],
            ["something", "interesting"]);

    // Monaco: 'issue #3980'
    [Fact]
    public void Issue3980() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["class A {", "    someProperty = false;", "    someMethod() {", "    this.someMethod();", "    }", "}"],
            [EditOp(1, 8, 1, 9, "", ""), EditOp(3, 17, 3, 18, "", ""), EditOp(3, 18, 3, 18, "    "), EditOp(4, 5, 4, 5, "    ")],
            ["class A", "{", "    someProperty = false;", "    someMethod()", "    {", "        this.someMethod();", "    }", "}"]);

    // Monaco: 'touching edits: two inserts at the same position'
    [Fact]
    public void TouchingEditsTwoInsertsAtTheSamePosition() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["hello world"],
            [EditOp(1, 1, 1, 1, "a"), EditOp(1, 1, 1, 1, "b")],
            ["abhello world"]);

    // Monaco: 'touching edits: insert and replace touching'
    [Fact]
    public void TouchingEditsInsertAndReplaceTouching() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["hello world"],
            [EditOp(1, 1, 1, 1, "b"), EditOp(1, 1, 1, 3, "ab")],
            ["babllo world"]);

    // Monaco: 'touching edits: two touching replaces'
    [Fact]
    public void TouchingEditsTwoTouchingReplaces() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["hello world"],
            [EditOp(1, 1, 1, 2, "H"), EditOp(1, 2, 1, 3, "E")],
            ["HEllo world"]);

    // Monaco: 'touching edits: two touching deletes'
    [Fact]
    public void TouchingEditsTwoTouchingDeletes() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["hello world"],
            [EditOp(1, 1, 1, 2, ""), EditOp(1, 2, 1, 3, "")],
            ["llo world"]);

    // Monaco: 'touching edits: insert and replace'
    [Fact]
    public void TouchingEditsInsertAndReplace() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["hello world"],
            [EditOp(1, 1, 1, 1, "H"), EditOp(1, 1, 1, 3, "e")],
            ["Hello world"]);

    // Monaco: 'touching edits: replace and insert'
    [Fact]
    public void TouchingEditsReplaceAndInsert() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["hello world"],
            [EditOp(1, 1, 1, 3, "H"), EditOp(1, 3, 1, 3, "e")],
            ["Hello world"]);

    private static void TestApplyEditsFails(string[] original, TextEdit[] edits)
    {
        var model = new TextModel(string.Join('\n', original));

        bool hasThrown = false;
        try
        {
            model.ApplyEdits(edits);
        }
        catch (Exception)
        {
            hasThrown = true;
        }

        Assert.True(hasThrown, "expected model.ApplyEdits to fail.");
    }

    // Monaco: 'overlapping edits: two overlapping replaces'
    [Fact]
    public void OverlappingEdits_TwoOverlappingReplaces() =>
        TestApplyEditsFails(["hello world"], [EditOp(1, 1, 1, 2, "b"), EditOp(1, 1, 1, 3, "ab")]);

    // Monaco: 'overlapping edits: two overlapping deletes'
    [Fact]
    public void OverlappingEdits_TwoOverlappingDeletes() =>
        TestApplyEditsFails(["hello world"], [EditOp(1, 1, 1, 2, ""), EditOp(1, 1, 1, 3, "")]);

    // Monaco: 'change while emitting events 1'
    [Fact]
    public void ChangeWhileEmittingEvents1() => ChangeWhileEmittingEvents();

    // Monaco: 'change while emitting events 2'
    [Fact]
    public void ChangeWhileEmittingEvents2() => ChangeWhileEmittingEvents();

    private static void ChangeWhileEmittingEvents()
    {
        EditableTextModelAssert.AssertSyncedModels(
            "Hello",
            (model, assertMirrorModels) =>
            {
                model.ApplyEdits([TextEdit.Replace(new Range(1, 6, 1, 6), " world!")]);

                assertMirrorModels();
            },
            model =>
            {
                bool isFirstTime = true;
                model.ContentChanged += (_, _) =>
                {
                    if (!isFirstTime)
                    {
                        return;
                    }

                    isFirstTime = false;

                    model.ApplyEdits([TextEdit.Replace(new Range(1, 13, 1, 13), " How are you?")]);
                };
            });
    }

    // Monaco: 'issue #1580: Changes in line endings are not correctly reflected in the extension host, leading to invalid offsets sent to external refactoring tools'
    [Fact]
    public void Issue1580_EolChangeIsReflectedInTheMirror()
    {
        var model = new TextModel("Hello\nWorld!");
        Assert.Equal("\n", model.Eol);

        var mirror = new MirrorText(model);

        model.SetEol(EndOfLineSequence.CRLF);

        Assert.Equal(model.GetValue(), mirror.Text);
        Assert.Equal(model.VersionId, mirror.VersionId);
    }

    // Monaco: 'issue #47733: Undo mangles unicode characters'
    [Fact]
    public void Issue47733_UndoDoesNotMangleUnicodeCharacters()
    {
        var model = new TextModel("'👁'");

        model.ApplyEdits([
            TextEdit.Replace(new Range(1, 1, 1, 1), "\""),
            TextEdit.Replace(new Range(1, 2, 1, 2), "\""),
        ]);

        Assert.Equal("\"'\"👁'", model.GetValue(EndOfLinePreference.LF));

        Assert.Equal(new Range(1, 3, 1, 4), model.ValidateRange(new Range(1, 3, 1, 4)));

        model.ApplyEdits([
            TextEdit.Delete(new Range(1, 1, 1, 2)),
            TextEdit.Delete(new Range(1, 3, 1, 4)),
        ]);

        Assert.Equal("'👁'", model.GetValue(EndOfLinePreference.LF));
    }

    // Monaco: 'issue #48741: Broken undo stack with move lines up with multiple cursors'
    [Fact]
    public void Issue48741_BrokenUndoStackWithMoveLinesUpWithMultipleCursors()
    {
        var model = new TextModel(string.Join('\n', "line1", "line2", "line3", ""));

        IReadOnlyList<TextEdit> undoEdits = model.ApplyEdits(
            [
                TextEdit.Replace(new Range(4, 1, 4, 1), "line3"),
                TextEdit.Delete(new Range(3, 1, 3, 6)),
                TextEdit.Delete(new Range(2, 1, 3, 1)),
                TextEdit.Replace(new Range(3, 6, 3, 6), "\nline2"),
            ],
            computeUndoEdits: true);

        model.ApplyEdits(undoEdits);

        Assert.Equal("line1\nline2\nline3\n", model.GetValue());
    }
}

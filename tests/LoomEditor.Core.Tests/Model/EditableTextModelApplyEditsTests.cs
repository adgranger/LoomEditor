using LoomEditor.Core.Text;
using LoomEditor.Core.Tests.Model.TestHelpers;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Ported from Monaco <c>editableTextModel.test.ts</c>: suite <c>EditorModel - EditableTextModel.applyEdits</c>
/// (first 23 tests; the rest comes with P1-25).
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
}

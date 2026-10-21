using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.LinesOperations;

// Monaco: InsertLineBefore / InsertLineAfter, indent lines and delete lines tests of 'Editor Contrib - Line Operations'
public partial class LinesOperationsTests
{
    private static readonly string[] ThreeLines = ["First line", "Second line", "Third line"];

    private static void TestInsertLine(bool before, int lineNumber, int column, Selection expectedSelection, string[] expectedLines) =>
        TestCodeEditor.WithTestCodeEditor(ThreeLines, editor =>
        {
            editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(lineNumber, column))]);
            if (before)
            {
                LinesOperationsActions.InsertLineBefore(editor.ViewModel);
            }
            else
            {
                LinesOperationsActions.InsertLineAfter(editor.ViewModel);
            }

            Assert.Equal(expectedSelection, editor.ViewModel.GetSelection());
            Assert.Equal(expectedLines, editor.Model.GetLinesContent());
        });

    // Monaco: 'InsertLineBeforeAction'
    [Fact]
    public void InsertLineBefore_AtEachLine()
    {
        TestInsertLine(true, 1, 3, new Selection(1, 1, 1, 1), [string.Empty, "First line", "Second line", "Third line"]);
        TestInsertLine(true, 2, 3, new Selection(2, 1, 2, 1), ["First line", string.Empty, "Second line", "Third line"]);
        TestInsertLine(true, 3, 3, new Selection(3, 1, 3, 1), ["First line", "Second line", string.Empty, "Third line"]);
    }

    // Monaco: 'InsertLineAfterAction'
    [Fact]
    public void InsertLineAfter_AtEachLine()
    {
        TestInsertLine(false, 1, 3, new Selection(2, 1, 2, 1), ["First line", string.Empty, "Second line", "Third line"]);
        TestInsertLine(false, 2, 3, new Selection(3, 1, 3, 1), ["First line", "Second line", string.Empty, "Third line"]);
        TestInsertLine(false, 3, 3, new Selection(4, 1, 4, 1), ["First line", "Second line", "Third line", string.Empty]);
    }

    // Monaco: 'Bug 18276:[editor] Indentation broken when selection is empty'
    [Fact]
    public void Bug18276_IndentationBrokenWhenSelectionIsEmpty()
    {
        TextModel model = TestCodeEditor.CreateTextModel("function baz() {", new TextModelOptions(insertSpaces: false));
        TestCodeEditor.WithTestCodeEditor(model, editor =>
        {
            editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(1, 2))]);

            LinesOperationsActions.IndentLines(editor.ViewModel);
            Assert.Equal("\tfunction baz() {", model.GetLineContent(1));
            Assert.Equal(new Selection(1, 3, 1, 3), editor.ViewModel.GetSelection());

            CoreEditingCommands.Tab.RunCoreEditingCommand(editor.ViewModel);
            Assert.Equal("\tf\tunction baz() {", model.GetLineContent(1));
        });
    }

    // Monaco: 'issue #80736: Indenting while the cursor is at the start of a line of text causes the added spaces or tab to be selected'
    [Fact]
    public void Issue80736_IndentingAtTheStartOfALineDoesNotSelectTheIndentation()
    {
        TextModel model = TestCodeEditor.CreateTextModel("Some text", new TextModelOptions(insertSpaces: false));
        TestCodeEditor.WithTestCodeEditor(model, editor =>
        {
            editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(1, 1))]);

            LinesOperationsActions.IndentLines(editor.ViewModel);
            Assert.Equal("\tSome text", model.GetLineContent(1));
            Assert.Equal(new Selection(1, 2, 1, 2), editor.ViewModel.GetSelection());
        });
    }

    // Monaco: 'Indenting on empty line should move cursor'
    [Fact]
    public void IndentingOnEmptyLine_ShouldMoveCursor()
    {
        TextModel model = TestCodeEditor.CreateTextModel(string.Empty);
        TestCodeEditor.WithTestCodeEditor(
            model,
            editor =>
            {
                editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(1, 1))]);

                LinesOperationsActions.IndentLines(editor.ViewModel);
                Assert.Equal("    ", model.GetLineContent(1));
                Assert.Equal(new Selection(1, 5, 1, 5), editor.ViewModel.GetSelection());
            },
            new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { UseTabStops = false }));
    }

    [Fact]
    public void OutdentLines_IndentedLine_RemovesOneLevel() =>
        TestCodeEditor.WithTestCodeEditor(["    a"], editor =>
        {
            LinesOperationsActions.OutdentLines(editor.ViewModel);
            Assert.Equal("a", editor.Model.GetValue());
        });

    // Monaco: 'issue #62112: Delete line does not work properly when multiple cursors are on line'
    [Fact]
    public void Issue62112_DeleteLineWithMultipleCursorsOnLine() =>
        TestCodeEditor.WithTestCodeEditor(["a", "foo boo", "too", "c"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(2, 4, 2, 4), new Selection(2, 8, 2, 8), new Selection(3, 4, 3, 4)]);
            LinesOperationsActions.DeleteLines(editor.ViewModel);

            Assert.Equal("a\nc", editor.Model.GetValue());
        });

    private static readonly string[] FiveLines = ["first", "second line", "third line", "fourth line", "fifth"];

    // Monaco: testDeleteLinesCommand
    private static void TestDeleteLinesCommand(string[] initialText, Selection[] initialSelections, string[] resultingText, Selection[] resultingSelections) =>
        TestCodeEditor.WithTestCodeEditor(initialText, editor =>
        {
            editor.ViewModel.SetSelections(null, initialSelections);
            LinesOperationsActions.DeleteLines(editor.ViewModel);
            Assert.Equal(string.Join('\n', resultingText), editor.Model.GetValue());
            Assert.Equal(resultingSelections, editor.ViewModel.GetSelections());
        });

    // Monaco: 'empty selection in middle of lines'
    [Fact]
    public void DeleteLines_EmptySelectionInMiddleOfLines() =>
        TestDeleteLinesCommand(FiveLines, [new Selection(2, 3, 2, 3)], ["first", "third line", "fourth line", "fifth"], [new Selection(2, 3, 2, 3)]);

    // Monaco: 'empty selection at top of lines'
    [Fact]
    public void DeleteLines_EmptySelectionAtTopOfLines() =>
        TestDeleteLinesCommand(FiveLines, [new Selection(1, 5, 1, 5)], ["second line", "third line", "fourth line", "fifth"], [new Selection(1, 5, 1, 5)]);

    // Monaco: 'empty selection at end of lines'
    [Fact]
    public void DeleteLines_EmptySelectionAtEndOfLines() =>
        TestDeleteLinesCommand(FiveLines, [new Selection(5, 2, 5, 2)], ["first", "second line", "third line", "fourth line"], [new Selection(4, 2, 4, 2)]);

    // Monaco: 'with selection in middle of lines'
    [Fact]
    public void DeleteLines_WithSelectionInMiddleOfLines() =>
        TestDeleteLinesCommand(FiveLines, [new Selection(3, 3, 2, 2)], ["first", "fourth line", "fifth"], [new Selection(2, 2, 2, 2)]);

    // Monaco: 'with selection at top of lines'
    [Fact]
    public void DeleteLines_WithSelectionAtTopOfLines() =>
        TestDeleteLinesCommand(FiveLines, [new Selection(1, 4, 1, 5)], ["second line", "third line", "fourth line", "fifth"], [new Selection(1, 5, 1, 5)]);

    // Monaco: 'with selection at end of lines'
    [Fact]
    public void DeleteLines_WithSelectionAtEndOfLines() =>
        TestDeleteLinesCommand(FiveLines, [new Selection(5, 1, 5, 2)], ["first", "second line", "third line", "fourth line"], [new Selection(4, 2, 4, 2)]);

    // Monaco: 'with full line selection in middle of lines'
    [Fact]
    public void DeleteLines_WithFullLineSelectionInMiddleOfLines() =>
        TestDeleteLinesCommand(FiveLines, [new Selection(4, 1, 2, 1)], ["first", "fourth line", "fifth"], [new Selection(2, 1, 2, 1)]);

    // Monaco: 'with full line selection at top of lines'
    [Fact]
    public void DeleteLines_WithFullLineSelectionAtTopOfLines() =>
        TestDeleteLinesCommand(FiveLines, [new Selection(2, 1, 1, 5)], ["second line", "third line", "fourth line", "fifth"], [new Selection(1, 5, 1, 5)]);

    // Monaco: 'with full line selection at end of lines'
    [Fact]
    public void DeleteLines_WithFullLineSelectionAtEndOfLines() =>
        TestDeleteLinesCommand(FiveLines, [new Selection(4, 1, 5, 2)], ["first", "second line", "third line"], [new Selection(3, 2, 3, 2)]);

    // Monaco: 'multicursor 1'
    [Fact]
    public void DeleteLines_Multicursor1() =>
        TestDeleteLinesCommand(
            [
                "class P {",
                string.Empty,
                "    getA() {",
                "        if (true) {",
                "            return \"a\";",
                "        }",
                "    }",
                string.Empty,
                "    getB() {",
                "        if (true) {",
                "            return \"b\";",
                "        }",
                "    }",
                string.Empty,
                "    getC() {",
                "        if (true) {",
                "            return \"c\";",
                "        }",
                "    }",
                "}",
            ],
            [new Selection(4, 1, 5, 1), new Selection(10, 1, 11, 1), new Selection(16, 1, 17, 1)],
            [
                "class P {",
                string.Empty,
                "    getA() {",
                "            return \"a\";",
                "        }",
                "    }",
                string.Empty,
                "    getB() {",
                "            return \"b\";",
                "        }",
                "    }",
                string.Empty,
                "    getC() {",
                "            return \"c\";",
                "        }",
                "    }",
                "}",
            ],
            [new Selection(4, 1, 4, 1), new Selection(9, 1, 9, 1), new Selection(14, 1, 14, 1)]);

    [Fact]
    public void Keybindings_DeleteIndentAndInsertKeys_ResolveToTheirCommands()
    {
        var focused = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true, Writable: true);
        var dispatcher = new KeybindingDispatcher(KeybindingResolver.Default);

        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.KeyK), focused, out string? deleteLines));
        Assert.Equal(LinesOperationsCommands.DeleteLines, deleteLines);
        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl, KeyCode.BracketRight), focused, out string? indentLines));
        Assert.Equal(LinesOperationsCommands.IndentLines, indentLines);
        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl, KeyCode.Enter), focused, out string? insertAfter));
        Assert.Equal(LinesOperationsCommands.InsertLineAfter, insertAfter);

        var readOnly = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true, Writable: false);
        Assert.Null(KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.KeyK), readOnly));
    }
}

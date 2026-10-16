// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller', Tab and Shift+Tab tests)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// `Undo(editor)` and `Redo(editor)` run TextModel.Undo / Redo (the core commands are P3-37).
public class TabTests
{
    private static readonly TestCodeEditorOptions NoSpaces = new(ModelOptions: new TextModelOptions(insertSpaces: false, indentSize: 4));

    private static void Tab(TestCodeEditor editor) => CoreEditingCommands.Tab.RunCoreEditingCommand(editor.ViewModel);

    private static void Outdent(TestCodeEditor editor) => CoreEditingCommands.Outdent.RunCoreEditingCommand(editor.ViewModel);

    // Monaco: 'issue microsoft/monaco-editor#443: Indentation of a single row deletes selected text in some cases'
    [Fact]
    public void IssueMonacoEditor443IndentationOfASingleRowDeletesSelectedTextInSomeCases()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "Hello world!", "another line"),
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 1, 1, 13)]);

                // Check that indenting maintains the selection start at column 1
                Tab(editor);
                Assert.Equal(new Selection(1, 1, 1, 14), editor.ViewModel.GetSelection());
            },
            NoSpaces);
    }

    // Monaco: 'Bug 9121: Auto indent + undo + redo is funky'
    [Fact]
    public void Bug9121AutoIndentUndoRedoIsFunky()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Empty,
            editor =>
            {
                TextModel model = editor.Model;
                var viewModel = editor.ViewModel;

                viewModel.Type("\n", "keyboard");
                Assert.Equal("\n", model.GetValue(EndOfLinePreference.LF));

                Tab(editor);
                Assert.Equal("\n\t", model.GetValue(EndOfLinePreference.LF));

                viewModel.Type("\n", "keyboard");
                Assert.Equal("\n\t\n\t", model.GetValue(EndOfLinePreference.LF));

                viewModel.Type("x");
                Assert.Equal("\n\t\n\tx", model.GetValue(EndOfLinePreference.LF));

                MoveLeft(viewModel);
                Assert.Equal("\n\t\n\tx", model.GetValue(EndOfLinePreference.LF));

                DeleteLeft(editor);
                Assert.Equal("\n\t\nx", model.GetValue(EndOfLinePreference.LF));

                DeleteLeft(editor);
                Assert.Equal("\n\tx", model.GetValue(EndOfLinePreference.LF));

                DeleteLeft(editor);
                Assert.Equal("\nx", model.GetValue(EndOfLinePreference.LF));

                DeleteLeft(editor);
                Assert.Equal("x", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("\nx", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("\n\t\nx", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("\n\t\n\tx", model.GetValue(EndOfLinePreference.LF));

                Redo(editor);
                Assert.Equal("\n\t\nx", model.GetValue(EndOfLinePreference.LF));

                Redo(editor);
                Assert.Equal("\nx", model.GetValue(EndOfLinePreference.LF));

                Redo(editor);
                Assert.Equal("x", model.GetValue(EndOfLinePreference.LF));
            },
            new TestCodeEditorOptions(ModelOptions: new TextModelOptions(insertSpaces: false, indentSize: 4, trimAutoWhitespace: false)));
    }

    // Monaco: 'Bug #18293:[regression][editor] Can't outdent whitespace line'
    [Fact]
    public void Bug18293RegressionEditorCantOutdentWhitespaceLine()
    {
        TestCodeEditor.WithTestCodeEditor(
            "      ",
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 7, false);
                AssertCursor(editor.ViewModel, new Selection(1, 7, 1, 7));

                Outdent(editor);
                Assert.Equal("    ", editor.Model.GetLineContent(1));
                AssertCursor(editor.ViewModel, new Selection(1, 5, 1, 5));
            });
    }

    // Monaco: 'issue #95591: Unindenting moves cursor to beginning of line'
    [Fact]
    public void Issue95591UnindentingMovesCursorToBeginningOfLine()
    {
        TestCodeEditor.WithTestCodeEditor(
            "        ",
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 9, false);
                AssertCursor(editor.ViewModel, new Selection(1, 9, 1, 9));

                Outdent(editor);
                Assert.Equal("    ", editor.Model.GetLineContent(1));
                AssertCursor(editor.ViewModel, new Selection(1, 5, 1, 5));
            },
            new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { UseTabStops = false }));
    }

    // Monaco: 'Bug #16657: [editor] Tab on empty line of zero indentation moves cursor to position (1,1)'
    [Fact]
    public void Bug16657TabOnEmptyLineOfZeroIndentationMovesCursorToPosition11()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "function baz() {", "\tfunction hello() { // something here", "\t", string.Empty, "\t}", "}", string.Empty),
            editor =>
            {
                MoveTo(editor.ViewModel, 7, 1, false);
                AssertCursor(editor.ViewModel, new Selection(7, 1, 7, 1));

                Tab(editor);
                Assert.Equal("\t", editor.Model.GetLineContent(7));
                AssertCursor(editor.ViewModel, new Selection(7, 2, 7, 2));
            },
            NoSpaces);
    }

    // Monaco: 'issue #3071: Investigate why undo stack gets corrupted'
    [Fact]
    public void Issue3071InvestigateWhyUndoStackGetsCorrupted()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "some lines", "and more lines", "just some text"),
            editor =>
            {
                TextModel model = editor.Model;
                var viewModel = editor.ViewModel;

                MoveTo(viewModel, 1, 1, false);
                MoveTo(viewModel, 3, 4, true);

                var isFirst = true;
                void OnContentChanged(object? sender, ModelContentChangedEventArgs e)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        viewModel.Type("\t", "keyboard");
                    }
                }

                model.ContentChanged += OnContentChanged;

                Tab(editor);
                Assert.Equal("\t just some text", model.GetValue());

                Undo(editor);
                Assert.Equal(string.Join('\n', "    some lines", "    and more lines", "    just some text"), model.GetValue());

                Undo(editor);
                Assert.Equal(string.Join('\n', "some lines", "and more lines", "just some text"), model.GetValue());

                Undo(editor);
                Assert.Equal(string.Join('\n', "some lines", "and more lines", "just some text"), model.GetValue());

                model.ContentChanged -= OnContentChanged;
            });
    }

    // Monaco: 'issue #3463: pressing tab adds spaces, but not as many as for a tab'
    [Fact]
    public void Issue3463PressingTabAddsSpacesButNotAsManyAsForATab()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "function a() {", "\tvar a = {", "\t\tx: 3", "\t};", "}"),
            editor =>
            {
                MoveTo(editor.ViewModel, 3, 2, false);
                Tab(editor);
                Assert.Equal("\t    \tx: 3", editor.Model.GetLineContent(3));
            });
    }

    // Monaco: 'issue #4312: trying to type a tab character over a sequence of spaces results in unexpected behaviour'
    [Fact]
    public void Issue4312TryingToTypeATabCharacterOverASequenceOfSpacesResultsInUnexpectedBehaviour()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "var foo = 123;       // this is a comment", "var bar = 4;       // another comment"),
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 15, false);
                MoveTo(editor.ViewModel, 1, 22, true);
                Tab(editor);
                Assert.Equal("var foo = 123;\t// this is a comment", editor.Model.GetLineContent(1));
            },
            NoSpaces);
    }

    // Monaco: 'Cursor honors insertSpaces configuration on tab'
    [Fact]
    public void CursorHonorsInsertSpacesConfigurationOnTab()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "    \tMy First Line\t ", "My Second Line123", "    Third Line", string.Empty, "1"),
            editor =>
            {
                TextModel model = editor.Model;

                void TabAt(int column, string expected)
                {
                    Assert.Equal("My Second Line123", model.GetLineContent(2));
                    MoveTo(editor.ViewModel, 2, column);
                    Tab(editor);
                    Assert.Equal(expected, model.GetLineContent(2));
                }

                // Tab on column 1
                MoveTo(editor.ViewModel, 2, 1);
                Tab(editor);
                Assert.Equal("             My Second Line123", model.GetLineContent(2));
                Undo(editor);

                // Tab on column 2
                TabAt(2, "M            y Second Line123");
                Undo(editor);

                // Tab on column 3
                TabAt(3, "My            Second Line123");
                Undo(editor);

                // Tab on column 4
                TabAt(4, "My           Second Line123");
                Undo(editor);

                // Tab on column 5
                TabAt(5, "My S         econd Line123");
                Undo(editor);

                // Tab on column 5
                TabAt(5, "My S         econd Line123");
                Undo(editor);

                // Tab on column 13
                TabAt(13, "My Second Li ne123");
                Undo(editor);

                // Tab on column 14
                TabAt(14, "My Second Lin             e123");
            },
            new TestCodeEditorOptions(ModelOptions: new TextModelOptions(tabSize: 13, indentSize: 13)));
    }

    // Monaco: 'PR #5423: Auto indent + undo + redo is funky'
    [Fact]
    public void Pr5423AutoIndentUndoRedoIsFunky()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Empty,
            editor =>
            {
                TextModel model = editor.Model;
                var viewModel = editor.ViewModel;

                viewModel.Type("\n", "keyboard");
                Assert.Equal("\n", model.GetValue(EndOfLinePreference.LF));

                Tab(editor);
                Assert.Equal("\n\t", model.GetValue(EndOfLinePreference.LF));

                viewModel.Type("y", "keyboard");
                Assert.Equal("\n\ty", model.GetValue(EndOfLinePreference.LF));

                viewModel.Type("\n", "keyboard");
                Assert.Equal("\n\ty\n\t", model.GetValue(EndOfLinePreference.LF));

                viewModel.Type("x");
                Assert.Equal("\n\ty\n\tx", model.GetValue(EndOfLinePreference.LF));

                MoveLeft(viewModel);
                Assert.Equal("\n\ty\n\tx", model.GetValue(EndOfLinePreference.LF));

                DeleteLeft(editor);
                Assert.Equal("\n\ty\nx", model.GetValue(EndOfLinePreference.LF));

                DeleteLeft(editor);
                Assert.Equal("\n\tyx", model.GetValue(EndOfLinePreference.LF));

                DeleteLeft(editor);
                Assert.Equal("\n\tx", model.GetValue(EndOfLinePreference.LF));

                DeleteLeft(editor);
                Assert.Equal("\nx", model.GetValue(EndOfLinePreference.LF));

                DeleteLeft(editor);
                Assert.Equal("x", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("\nx", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("\n\ty\nx", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("\n\ty\n\tx", model.GetValue(EndOfLinePreference.LF));

                Redo(editor);
                Assert.Equal("\n\ty\nx", model.GetValue(EndOfLinePreference.LF));

                Redo(editor);
                Assert.Equal("\nx", model.GetValue(EndOfLinePreference.LF));

                Redo(editor);
                Assert.Equal("x", model.GetValue(EndOfLinePreference.LF));
            },
            NoSpaces);
    }

    // Monaco: 'removeAutoWhitespace on: removes only whitespace the cursor added 2' (the test registers a language without any
    // configuration: a plain model behaves the same)
    [Fact]
    public void RemoveAutoWhitespaceOnRemovesOnlyWhitespaceTheCursorAdded2()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "    if (a) {", "        ", string.Empty, string.Empty, "    }"),
            editor =>
            {
                TextModel model = editor.Model;

                MoveTo(editor.ViewModel, 3, 1);
                Tab(editor);
                Assert.Equal("    if (a) {", model.GetLineContent(1));
                Assert.Equal("        ", model.GetLineContent(2));
                Assert.Equal("    ", model.GetLineContent(3));
                Assert.Equal(string.Empty, model.GetLineContent(4));
                Assert.Equal("    }", model.GetLineContent(5));

                MoveTo(editor.ViewModel, 4, 1);
                Tab(editor);
                Assert.Equal("    if (a) {", model.GetLineContent(1));
                Assert.Equal("        ", model.GetLineContent(2));
                Assert.Equal(string.Empty, model.GetLineContent(3));
                Assert.Equal("    ", model.GetLineContent(4));
                Assert.Equal("    }", model.GetLineContent(5));

                MoveTo(editor.ViewModel, 5, model.GetLineMaxColumn(5));
                editor.ViewModel.Type("something", "keyboard");
                Assert.Equal("    if (a) {", model.GetLineContent(1));
                Assert.Equal("        ", model.GetLineContent(2));
                Assert.Equal(string.Empty, model.GetLineContent(3));
                Assert.Equal(string.Empty, model.GetLineContent(4));
                Assert.Equal("    }something", model.GetLineContent(5));
            });
    }

    // Monaco: 'removeAutoWhitespace on: test 1'
    [Fact]
    public void RemoveAutoWhitespaceOnTest1()
    {
        TestCodeEditor.WithTestCodeEditor(
            "    some  line abc  ",
            editor =>
            {
                TextModel model = editor.Model;
                var viewModel = editor.ViewModel;

                // Move cursor to the end, verify that we do not trim whitespaces if line has values
                MoveTo(viewModel, 1, model.GetLineContent(1).Length + 1);
                viewModel.Type("\n", "keyboard");
                Assert.Equal("    some  line abc  ", model.GetLineContent(1));
                Assert.Equal("    ", model.GetLineContent(2));

                // Try to enter again, we should trimmed previous line
                viewModel.Type("\n", "keyboard");
                Assert.Equal("    some  line abc  ", model.GetLineContent(1));
                Assert.Equal(string.Empty, model.GetLineContent(2));
                Assert.Equal("    ", model.GetLineContent(3));

                // More whitespaces
                Tab(editor);
                Assert.Equal("    some  line abc  ", model.GetLineContent(1));
                Assert.Equal(string.Empty, model.GetLineContent(2));
                Assert.Equal("        ", model.GetLineContent(3));

                // Enter and verify that trimmed again
                viewModel.Type("\n", "keyboard");
                Assert.Equal("    some  line abc  ", model.GetLineContent(1));
                Assert.Equal(string.Empty, model.GetLineContent(2));
                Assert.Equal(string.Empty, model.GetLineContent(3));
                Assert.Equal("        ", model.GetLineContent(4));

                // Trimmed if we will keep only text
                MoveTo(viewModel, 1, 5);
                viewModel.Type("\n", "keyboard");
                Assert.Equal("    ", model.GetLineContent(1));
                Assert.Equal("    some  line abc  ", model.GetLineContent(2));
                Assert.Equal(string.Empty, model.GetLineContent(3));
                Assert.Equal(string.Empty, model.GetLineContent(4));
                Assert.Equal(string.Empty, model.GetLineContent(5));

                // Trimmed if we will keep only text by selection
                MoveTo(viewModel, 2, 5);
                MoveTo(viewModel, 3, 1, true);
                viewModel.Type("\n", "keyboard");
                Assert.Equal("    ", model.GetLineContent(1));
                Assert.Equal("    ", model.GetLineContent(2));
                Assert.Equal("    ", model.GetLineContent(3));
                Assert.Equal(string.Empty, model.GetLineContent(4));
                Assert.Equal(string.Empty, model.GetLineContent(5));
            });
    }
}

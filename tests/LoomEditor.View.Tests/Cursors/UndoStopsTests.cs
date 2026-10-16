// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Undo stops')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// `Undo(editor)` runs TextModel.Undo: the core command `undo` is P3-37.
public class UndoStopsTests
{
    private static readonly TestCodeEditorOptions NoSpaces = new(ModelOptions: new TextModelOptions(insertSpaces: false, indentSize: 4));

    // Monaco: 'there is an undo stop between typing and deleting left'
    [Fact]
    public void ThereIsAnUndoStopBetweenTypingAndDeletingLeft()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "A  line", "Another line"),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.SetSelections("test", [new Selection(1, 3, 1, 3)]);
                viewModel.Type("first", "keyboard");
                Assert.Equal("A first line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 8, 1, 8));

                DeleteLeft(editor);
                DeleteLeft(editor);
                Assert.Equal("A fir line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 6, 1, 6));

                Undo(editor);
                Assert.Equal("A first line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 8, 1, 8));

                Undo(editor);
                Assert.Equal("A  line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 3, 1, 3));
            });
    }

    // Monaco: 'there is an undo stop between typing and deleting right'
    [Fact]
    public void ThereIsAnUndoStopBetweenTypingAndDeletingRight()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "A  line", "Another line"),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.SetSelections("test", [new Selection(1, 3, 1, 3)]);
                viewModel.Type("first", "keyboard");
                Assert.Equal("A first line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 8, 1, 8));

                DeleteRight(editor);
                DeleteRight(editor);
                Assert.Equal("A firstine", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 8, 1, 8));

                Undo(editor);
                Assert.Equal("A first line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 8, 1, 8));

                Undo(editor);
                Assert.Equal("A  line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 3, 1, 3));
            });
    }

    // Monaco: 'there is an undo stop between deleting left and typing'
    [Fact]
    public void ThereIsAnUndoStopBetweenDeletingLeftAndTyping()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "A  line", "Another line"),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.SetSelections("test", [new Selection(2, 8, 2, 8)]);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                Assert.Equal(" line", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 1, 2, 1));

                viewModel.Type("Second", "keyboard");
                Assert.Equal("Second line", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 7, 2, 7));

                Undo(editor);
                Assert.Equal(" line", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 1, 2, 1));

                Undo(editor);
                Assert.Equal("Another line", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 8, 2, 8));
            });
    }

    // Monaco: 'there is an undo stop between deleting left and deleting right'
    [Fact]
    public void ThereIsAnUndoStopBetweenDeletingLeftAndDeletingRight()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "A  line", "Another line"),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.SetSelections("test", [new Selection(2, 8, 2, 8)]);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                Assert.Equal(" line", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 1, 2, 1));

                DeleteRight(editor);
                DeleteRight(editor);
                DeleteRight(editor);
                DeleteRight(editor);
                DeleteRight(editor);
                Assert.Equal("", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 1, 2, 1));

                Undo(editor);
                Assert.Equal(" line", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 1, 2, 1));

                Undo(editor);
                Assert.Equal("Another line", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 8, 2, 8));
            });
    }

    // Monaco: 'there is an undo stop between deleting right and typing'
    [Fact]
    public void ThereIsAnUndoStopBetweenDeletingRightAndTyping()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "A  line", "Another line"),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.SetSelections("test", [new Selection(2, 9, 2, 9)]);
                DeleteRight(editor);
                DeleteRight(editor);
                DeleteRight(editor);
                DeleteRight(editor);
                Assert.Equal("Another ", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 9, 2, 9));

                viewModel.Type("text", "keyboard");
                Assert.Equal("Another text", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 13, 2, 13));

                Undo(editor);
                Assert.Equal("Another ", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 9, 2, 9));

                Undo(editor);
                Assert.Equal("Another line", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 9, 2, 9));
            });
    }

    // Monaco: 'there is an undo stop between deleting right and deleting left'
    [Fact]
    public void ThereIsAnUndoStopBetweenDeletingRightAndDeletingLeft()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "A  line", "Another line"),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.SetSelections("test", [new Selection(2, 9, 2, 9)]);
                DeleteRight(editor);
                DeleteRight(editor);
                DeleteRight(editor);
                DeleteRight(editor);
                Assert.Equal("Another ", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 9, 2, 9));

                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                DeleteLeft(editor);
                Assert.Equal("An", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 3, 2, 3));

                Undo(editor);
                Assert.Equal("Another ", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 9, 2, 9));

                Undo(editor);
                Assert.Equal("Another line", model.GetLineContent(2));
                AssertCursor(viewModel, new Selection(2, 9, 2, 9));
            });
    }

    // Monaco: 'inserts undo stop when typing space'
    [Fact]
    public void InsertsUndoStopWhenTypingSpace()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "A  line", "Another line"),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.SetSelections("test", [new Selection(1, 3, 1, 3)]);
                viewModel.Type("first and interesting", "keyboard");
                Assert.Equal("A first and interesting line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 24, 1, 24));

                Undo(editor);
                Assert.Equal("A first and line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 12, 1, 12));

                Undo(editor);
                Assert.Equal("A first line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 8, 1, 8));

                Undo(editor);
                Assert.Equal("A  line", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 3, 1, 3));
            });
    }

    // Monaco: 'can undo typing and EOL change in one undo stop'
    [Fact]
    public void CanUndoTypingAndEOLChangeInOneUndoStop()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "A  line", "Another line"),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.SetSelections("test", [new Selection(1, 3, 1, 3)]);
                viewModel.Type("first", "keyboard");
                Assert.Equal("A first line\nAnother line", model.GetValue());
                AssertCursor(viewModel, new Selection(1, 8, 1, 8));

                model.PushEol(EndOfLineSequence.CRLF);
                Assert.Equal("A first line\r\nAnother line", model.GetValue());
                AssertCursor(viewModel, new Selection(1, 8, 1, 8));

                Undo(editor);
                Assert.Equal("A  line\nAnother line", model.GetValue());
                AssertCursor(viewModel, new Selection(1, 3, 1, 3));
            });
    }

    // Monaco: 'issue #93585: Undo multi cursor edit corrupts document'
    [Fact]
    public void Issue93585UndoMultiCursorEditCorruptsDocument()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "hello world", "hello world"),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.SetSelections("test", [new Selection(2, 7, 2, 12), new Selection(1, 7, 1, 12)]);
                viewModel.Type("no", "keyboard");
                Assert.Equal("hello no\nhello no", model.GetValue());

                Undo(editor);
                Assert.Equal("hello world\nhello world", model.GetValue());
            });
    }

    // Monaco: 'there is a single undo stop for consecutive whitespaces'
    [Fact]
    public void ThereIsASingleUndoStopForConsecutiveWhitespaces()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', ""),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.Type("a", "keyboard");
                viewModel.Type("b", "keyboard");
                viewModel.Type(" ", "keyboard");
                viewModel.Type(" ", "keyboard");
                viewModel.Type("c", "keyboard");
                viewModel.Type("d", "keyboard");

                Assert.Equal("ab  cd", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("ab  ", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("ab", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("", model.GetValue(EndOfLinePreference.LF));
            },
            NoSpaces);
    }

    // Monaco: 'there is no undo stop after a single whitespace'
    [Fact]
    public void ThereIsNoUndoStopAfterASingleWhitespace()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', ""),
            editor =>
            {
                TextModel model = editor.Model;
                ViewModel viewModel = editor.ViewModel;

                viewModel.Type("a", "keyboard");
                viewModel.Type("b", "keyboard");
                viewModel.Type(" ", "keyboard");
                viewModel.Type("c", "keyboard");
                viewModel.Type("d", "keyboard");

                Assert.Equal("ab cd", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("ab", model.GetValue(EndOfLinePreference.LF));

                Undo(editor);
                Assert.Equal("", model.GetValue(EndOfLinePreference.LF));
            },
            NoSpaces);
    }
}

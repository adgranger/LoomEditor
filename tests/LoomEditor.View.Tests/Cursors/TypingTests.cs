// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller', typing and Enter tests)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// Not ported here (they use the Tab command, P3-34): 'removeAutoWhitespace on: removes only whitespace the cursor added 2'
// and 'removeAutoWhitespace on: test 1'. 'Undo stops' is in UndoStopsTests. `Undo(editor)` runs TextModel.Undo (P3-37 adds
// the core command).
public class TypingTests
{
    private static TestCodeEditorOptions Model(TextModelOptions options) => new(ModelOptions: options);

    // Monaco: 'issue #23539: Setting model EOL isn't undoable'
    [Fact]
    public void Issue23539SettingModelEolIsntUndoable()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["Hello", "world"],
            editor =>
            {
                TextModel model = editor.Model;

                AssertCursor(editor.ViewModel, new Position(1, 1));
                model.SetEol(EndOfLineSequence.LF);
                Assert.Equal("Hello\nworld", model.GetValue());

                model.PushEol(EndOfLineSequence.CRLF);
                Assert.Equal("Hello\r\nworld", model.GetValue());

                Undo(editor);
                Assert.Equal("Hello\nworld", model.GetValue());
            });
    }

    // Monaco: 'issue #46208: Allow empty selections in the undo/redo stack'
    [Fact]
    public void Issue46208AllowEmptySelectionsInTheUndoRedoStack()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Empty,
            editor =>
            {
                TextModel model = editor.Model;
                var viewModel = editor.ViewModel;

                viewModel.Type("Hello", "keyboard");
                viewModel.Type(" ", "keyboard");
                viewModel.Type("world", "keyboard");
                viewModel.Type(" ", "keyboard");
                Assert.Equal("Hello world ", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 13));

                MoveLeft(viewModel);
                MoveRight(viewModel);

                // Monaco: EditOperation.replaceMove(range, '')
                model.PushEditOperations([], [new IdentifiedEditOperation(new Range(1, 12, 1, 13), string.Empty, ForceMoveMarkers: true)], _ => []);
                Assert.Equal("Hello world", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 12));

                Undo(editor);
                Assert.Equal("Hello world ", model.GetLineContent(1));
                AssertCursor(viewModel, new Selection(1, 13, 1, 13));

                Undo(editor);
                Assert.Equal("Hello world", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 12));

                Undo(editor);
                Assert.Equal("Hello", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 6));

                Undo(editor);
                Assert.Equal(string.Empty, model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 1));

                Redo(editor);
                Assert.Equal("Hello", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 6));

                Redo(editor);
                Assert.Equal("Hello world", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 12));

                Redo(editor);
                Assert.Equal("Hello world ", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 13));

                Redo(editor);
                Assert.Equal("Hello world", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 12));

                Redo(editor);
                Assert.Equal("Hello world", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 12));
            });
    }

    // Monaco: 'issue #12950: Cannot Double Click To Insert Emoji Using OSX Emoji Panel'
    [Fact]
    public void Issue12950CannotDoubleClickToInsertEmojiUsingOsxEmojiPanel()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["some lines", "and more lines", "just some text"],
            editor =>
            {
                MoveTo(editor.ViewModel, 3, 1, false);

                editor.ViewModel.Type("😍", "keyboard");

                Assert.Equal("some lines\nand more lines\n😍just some text", editor.Model.GetValue());
            });
    }

    // Monaco: 'issue #37967: problem replacing consecutive characters'
    [Fact]
    public void Issue37967ProblemReplacingConsecutiveCharacters()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["const a = \"foo\";", "const b = \"\""],
            editor =>
            {
                editor.ViewModel.SetSelections(
                    null,
                    [
                        new Selection(1, 12, 1, 12),
                        new Selection(1, 16, 1, 16),
                        new Selection(2, 12, 2, 12),
                        new Selection(2, 13, 2, 13),
                    ]);

                DeleteLeft(editor);

                AssertCursor(
                    editor.ViewModel,
                    [
                        new Selection(1, 11, 1, 11),
                        new Selection(1, 14, 1, 14),
                        new Selection(2, 11, 2, 11),
                        new Selection(2, 11, 2, 11),
                    ]);

                editor.ViewModel.Type("'", "keyboard");

                Assert.Equal("const a = 'foo';", editor.Model.GetLineContent(1));
                Assert.Equal("const b = ''", editor.Model.GetLineContent(2));
            },
            new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { MultiCursorMergeOverlapping = false }));
    }

    // Monaco: 'issue #15761: Cursor doesn't move in a redo operation'
    [Fact]
    public void Issue15761CursorDoesntMoveInARedoOperation()
    {
        TestCodeEditor.WithTestCodeEditor(
            "hello",
            editor =>
            {
                editor.ViewModel.SetSelections(null, [new Selection(1, 4, 1, 4)]);

                ExecuteEdits(editor, new IdentifiedEditOperation(new Range(1, 1, 1, 1), "*", ForceMoveMarkers: true));
                AssertCursor(editor.ViewModel, [new Selection(1, 5, 1, 5)]);

                Undo(editor);
                AssertCursor(editor.ViewModel, [new Selection(1, 4, 1, 4)]);

                Redo(editor);
                AssertCursor(editor.ViewModel, [new Selection(1, 5, 1, 5)]);
            });
    }

    // Monaco: 'issue #42783: API Calls with Undo Leave Cursor in Wrong Position'
    [Fact]
    public void Issue42783ApiCallsWithUndoLeaveCursorInWrongPosition()
    {
        TestCodeEditor.WithTestCodeEditor(
            "ab",
            editor =>
            {
                editor.ViewModel.SetSelections(null, [new Selection(1, 1, 1, 1)]);

                ExecuteEdits(editor, new IdentifiedEditOperation(new Range(1, 1, 1, 3), string.Empty));
                AssertCursor(editor.ViewModel, [new Selection(1, 1, 1, 1)]);

                Undo(editor);
                AssertCursor(editor.ViewModel, [new Selection(1, 1, 1, 1)]);

                ExecuteEdits(editor, new IdentifiedEditOperation(new Range(1, 1, 1, 2), string.Empty));
                AssertCursor(editor.ViewModel, [new Selection(1, 1, 1, 1)]);
            });
    }

    // Monaco: 'Cursor honors insertSpaces configuration on new line'
    [Fact]
    public void CursorHonorsInsertSpacesConfigurationOnNewLine()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["    \tMy First Line\t ", "\tMy Second Line", "    Third Line", string.Empty, "1"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 21);
                editor.ViewModel.Type("\n", "keyboard");
                Assert.Equal("    \tMy First Line\t ", editor.Model.GetLineContent(1));
                Assert.Equal("        ", editor.Model.GetLineContent(2));
            });
    }

    // Monaco: 'issue #148256: Pressing Enter creates line with bad indent with insertSpaces: true'
    [Fact]
    public void Issue148256PressingEnterCreatesLineWithBadIndentWithInsertSpacesTrue()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["  \t"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 4, false);
                editor.ViewModel.Type("\n", "keyboard");
                Assert.Equal("  \t\n    ", editor.Model.GetValue());
            });
    }

    // Monaco: 'issue #148256: Pressing Enter creates line with bad indent with insertSpaces: false'
    [Fact]
    public void Issue148256PressingEnterCreatesLineWithBadIndentWithInsertSpacesFalse()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["  \t"],
            editor =>
            {
                editor.Model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: false));
                MoveTo(editor.ViewModel, 1, 4, false);
                editor.ViewModel.Type("\n", "keyboard");
                Assert.Equal("  \t\n\t", editor.Model.GetValue());
            });
    }

    // Monaco: 'removeAutoWhitespace off'
    [Fact]
    public void RemoveAutoWhitespaceOff()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["    some  line abc  "],
            editor =>
            {
                TextModel model = editor.Model;

                // Move cursor to the end, verify that we do not trim whitespaces if line has values
                MoveTo(editor.ViewModel, 1, model.GetLineContent(1).Length + 1);
                editor.ViewModel.Type("\n", "keyboard");
                Assert.Equal("    some  line abc  ", model.GetLineContent(1));
                Assert.Equal("    ", model.GetLineContent(2));

                // Try to enter again, we should trimmed previous line
                editor.ViewModel.Type("\n", "keyboard");
                Assert.Equal("    some  line abc  ", model.GetLineContent(1));
                Assert.Equal("    ", model.GetLineContent(2));
                Assert.Equal("    ", model.GetLineContent(3));
            },
            Model(new TextModelOptions(indentSize: 4, trimAutoWhitespace: false)));
    }

    // Monaco: 'removeAutoWhitespace on: removes only whitespace the cursor added 1'
    [Fact]
    public void RemoveAutoWhitespaceOnRemovesOnlyWhitespaceTheCursorAdded1()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["    "],
            editor =>
            {
                TextModel model = editor.Model;

                MoveTo(editor.ViewModel, 1, model.GetLineContent(1).Length + 1);
                editor.ViewModel.Type("\n", "keyboard");
                Assert.Equal("    ", model.GetLineContent(1));
                Assert.Equal("    ", model.GetLineContent(2));

                editor.ViewModel.Type("\n", "keyboard");
                Assert.Equal("    ", model.GetLineContent(1));
                Assert.Equal(string.Empty, model.GetLineContent(2));
                Assert.Equal("    ", model.GetLineContent(3));
            });
    }

    // Monaco: 'issue #90973: Undo brings back model alternative version'
    [Fact]
    public void Issue90973UndoBringsBackModelAlternativeVersion()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Empty,
            editor =>
            {
                TextModel model = editor.Model;

                int beforeVersion = model.VersionId;
                int beforeAltVersion = model.AlternativeVersionId;
                editor.ViewModel.Type("Hello", "keyboard");
                Undo(editor);
                int afterVersion = model.VersionId;
                int afterAltVersion = model.AlternativeVersionId;

                Assert.NotEqual(beforeVersion, afterVersion);
                Assert.Equal(beforeAltVersion, afterAltVersion);
            },
            Model(new TextModelOptions(insertSpaces: false, indentSize: 4)));
    }

    // Monaco: 'onEnter works if there are no indentation rules'
    [Fact]
    public void OnEnterWorksIfThereAreNoIndentationRules()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["<?", "\tif (true) {", "\t\techo $hi;", "\t\techo $bye;", "\t}", "?>"],
            editor =>
            {
                MoveTo(editor.ViewModel, 5, 3, false);
                AssertCursor(editor.ViewModel, new Selection(5, 3, 5, 3));

                editor.ViewModel.Type("\n", "keyboard");
                Assert.Equal("\t", editor.Model.GetLineContent(6));
                AssertCursor(editor.ViewModel, new Selection(6, 2, 6, 2));
                Assert.Equal("\t}", editor.Model.GetLineContent(5));
            },
            Model(new TextModelOptions(insertSpaces: false, indentSize: 4)));
    }

    // Monaco: 'onEnter works if there are no indentation rules 2'
    [Fact]
    public void OnEnterWorksIfThereAreNoIndentationRules2()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["\tif (5)", "\t\treturn 5;", "\t"],
            editor =>
            {
                MoveTo(editor.ViewModel, 3, 2, false);
                AssertCursor(editor.ViewModel, new Selection(3, 2, 3, 2));

                editor.ViewModel.Type("\n", "keyboard");
                AssertCursor(editor.ViewModel, new Selection(4, 2, 4, 2));
                Assert.Equal("\t", editor.Model.GetLineContent(4));
            },
            Model(new TextModelOptions(insertSpaces: false, indentSize: 4)));
    }

    // Monaco: 'issue #111513: Text gets automatically selected when typing at the same location in another editor'
    [Fact]
    public void Issue111513TextGetsAutomaticallySelectedWhenTypingAtTheSameLocationInAnotherEditor()
    {
        var model = new TextModel(string.Join('\n', "just", string.Empty, "some text"));

        TestCodeEditor.WithTestCodeEditor(
            model,
            editor1 =>
            {
                editor1.ViewModel.SetSelections(null, [new Selection(2, 1, 2, 1)]);
                TestCodeEditor.WithTestCodeEditor(
                    model,
                    editor2 =>
                    {
                        editor2.ViewModel.SetSelections(null, [new Selection(2, 1, 2, 1)]);
                        editor2.ViewModel.Type("e", "keyboard");
                        AssertCursor(editor2.ViewModel, new Position(2, 2));
                        AssertCursor(editor1.ViewModel, new Position(2, 2));
                    });
            });
    }

    [Fact]
    public void Type_KeyboardTextWithAnEmojiModifier_TypesOneOperationPerGraphemeInOneUndoStep()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Empty,
            editor =>
            {
                var contentChanges = 0;
                editor.Model.ContentChanged += (_, _) => contentChanges++;

                editor.ViewModel.Type("a👍🏽b", "keyboard");

                Assert.Equal("a👍🏽b", editor.Model.GetValue());
                Assert.Equal(3, contentChanges);

                Undo(editor);
                Assert.Equal(string.Empty, editor.Model.GetValue());
            });
    }

    [Fact]
    public void Type_NotFromTheKeyboard_InsertsTheWholeTextAsOneOperation()
    {
        TestCodeEditor.WithTestCodeEditor(
            "x",
            editor =>
            {
                var contentChanges = 0;
                editor.Model.ContentChanged += (_, _) => contentChanges++;

                editor.ViewModel.Type("a\nb", "test");

                // No Enter handling without the keyboard source: the line break is inserted as it is.
                Assert.Equal("a\nbx", editor.Model.GetValue());
                Assert.Equal(1, contentChanges);
            });
    }

    [Fact]
    public void LineBreakInsert_Cursor_BreaksTheLineAndKeepsTheCursorInPlace()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["  abcd"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 5);

                CoreEditingCommands.LineBreakInsert.RunCoreEditingCommand(editor.ViewModel);

                Assert.Equal("  ab\n  cd", editor.Model.GetValue());
                AssertCursor(editor.ViewModel, new Position(1, 5));
            });
    }

    private static void ExecuteEdits(TestCodeEditor editor, IdentifiedEditOperation edit) =>
        editor.ViewModel.ExecuteEdits("test", [edit], _ => null);
}

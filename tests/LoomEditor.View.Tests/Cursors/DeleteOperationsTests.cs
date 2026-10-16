// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller', delete tests)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// Not ported here: 'Undo stops' (P3-33, it needs typing). CoreEditingCommands.Undo is P3-37: 'Backspace removes
// whitespaces with tab size' calls TextModel.Undo directly.
public class DeleteOperationsTests
{
    private static readonly TestCodeEditorOptions NoTabStops = new(CursorOptions: EditorCursorOptions.Default with { UseTabStops = false });

    private static string Value(TestCodeEditor editor) => editor.Model.GetValue(EndOfLinePreference.LF);

    // Monaco: 'issue #1140: Backspace stops prematurely'
    [Fact]
    public void Issue1140BackspaceStopsPrematurely()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "function baz() {", "  return 1;", "};"),
            editor =>
            {
                MoveTo(editor.ViewModel, 3, 2, false);
                MoveTo(editor.ViewModel, 1, 14, true);
                AssertCursor(editor.ViewModel, new Selection(3, 2, 1, 14));

                DeleteLeft(editor);
                AssertCursor(editor.ViewModel, new Selection(1, 14, 1, 14));
                Assert.Equal(1, editor.Model.LineCount);
                Assert.Equal("function baz(;", editor.Model.GetLineContent(1));
            });
    }

    // Monaco: 'issue #84897: Left delete behavior in some languages is changed'
    [Fact]
    public void Issue84897LeftDeleteBehaviorInSomeLanguagesIsChanged() =>
        AssertThaiDeletes(options: null);

    // Monaco: 'issue #122914: Left delete behavior in some languages is changed (useTabStops: false)'
    [Fact]
    public void Issue122914LeftDeleteBehaviorInSomeLanguagesIsChangedUseTabStopsFalse() =>
        AssertThaiDeletes(NoTabStops);

    private static void AssertThaiDeletes(TestCodeEditorOptions? options)
    {
        TestCodeEditor.WithTestCodeEditor(
            "สวัสดี",
            editor =>
            {
                editor.ViewModel.SetSelections(null, [new Selection(1, 7, 1, 7)]);

                foreach (string expected in new[] { "สวัสด", "สวัส", "สวั", "สว", "ส", string.Empty })
                {
                    DeleteLeft(editor);
                    Assert.Equal(expected, Value(editor));
                }
            },
            options);
    }

    // Monaco: 'issue #99629: Emoji modifiers in text treated separately when using backspace'
    [Fact]
    public void Issue99629EmojiModifiersInTextTreatedSeparatelyWhenUsingBackspace()
    {
        TestCodeEditor.WithTestCodeEditor(
            "👶🏾",
            editor =>
            {
                int len = editor.Model.GetValueLength(EndOfLinePreference.LF);
                editor.ViewModel.SetSelections(null, [new Selection(1, 1 + len, 1, 1 + len)]);

                DeleteLeft(editor);
                Assert.Equal(string.Empty, Value(editor));
            },
            NoTabStops);
    }

    // Monaco: 'issue #99629: Emoji modifiers in text treated separately when using backspace (ZWJ sequence)'
    [Fact]
    public void Issue99629EmojiModifiersInTextTreatedSeparatelyWhenUsingBackspaceZwjSequence()
    {
        TestCodeEditor.WithTestCodeEditor(
            "👨‍👩🏽‍👧‍👦",
            editor =>
            {
                int len = editor.Model.GetValueLength(EndOfLinePreference.LF);
                editor.ViewModel.SetSelections(null, [new Selection(1, 1 + len, 1, 1 + len)]);

                foreach (string expected in new[] { "👨‍👩🏽‍👧", "👨‍👩🏽", "👨", string.Empty })
                {
                    DeleteLeft(editor);
                    Assert.Equal(expected, Value(editor));
                }
            },
            NoTabStops);
    }

    // Monaco: 'UseTabStops is off'
    [Fact]
    public void UseTabStopsIsOff()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "    x", "        a    ", "    "),
            editor =>
            {
                // DeleteLeft removes just one whitespace
                MoveTo(editor.ViewModel, 2, 9);
                DeleteLeft(editor);
                Assert.Equal("       a    ", editor.Model.GetLineContent(2));
            },
            NoTabStops);
    }

    // Monaco: 'Backspace removes whitespaces with tab size'
    [Fact]
    public void BackspaceRemovesWhitespacesWithTabSize()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', " \t \t     x", "        a    ", "    "),
            editor =>
            {
                TextModel model = editor.Model;

                // DeleteLeft does not remove tab size, because some text exists before
                MoveTo(editor.ViewModel, 2, model.GetLineContent(2).Length + 1);
                DeleteLeft(editor);
                Assert.Equal("        a   ", model.GetLineContent(2));

                // DeleteLeft removes tab size = 4
                MoveTo(editor.ViewModel, 2, 9);
                DeleteLeft(editor);
                Assert.Equal("    a   ", model.GetLineContent(2));

                // DeleteLeft removes tab size = 4
                DeleteLeft(editor);
                Assert.Equal("a   ", model.GetLineContent(2));

                // Undo DeleteLeft - get us back to original indentation
                model.Undo(); // CoreEditingCommands.Undo is P3-37
                Assert.Equal("        a   ", model.GetLineContent(2));

                // Nothing is broken when cursor is in (1,1)
                MoveTo(editor.ViewModel, 1, 1);
                DeleteLeft(editor);
                Assert.Equal(" \t \t     x", model.GetLineContent(1));

                // DeleteLeft stops at tab stops even in mixed whitespace case
                MoveTo(editor.ViewModel, 1, 10);
                DeleteLeft(editor);
                Assert.Equal(" \t \t    x", model.GetLineContent(1));

                DeleteLeft(editor);
                Assert.Equal(" \t \tx", model.GetLineContent(1));

                DeleteLeft(editor);
                Assert.Equal(" \tx", model.GetLineContent(1));

                DeleteLeft(editor);
                Assert.Equal("x", model.GetLineContent(1));

                // DeleteLeft on last line
                MoveTo(editor.ViewModel, 3, model.GetLineContent(3).Length + 1);
                DeleteLeft(editor);
                Assert.Equal(string.Empty, model.GetLineContent(3));

                // DeleteLeft with removing new line symbol
                DeleteLeft(editor);
                Assert.Equal("x\n        a   ", Value(editor));

                // In case of selection DeleteLeft only deletes selected text
                MoveTo(editor.ViewModel, 2, 3);
                MoveTo(editor.ViewModel, 2, 4, true);
                DeleteLeft(editor);
                Assert.Equal("       a   ", model.GetLineContent(2));
            },
            new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { UseTabStops = true }));
    }

    [Fact]
    public void DeleteRight_EmptySelection_DeletesOneCharacterAndJoinsLinesAtTheEnd()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "ab", "cd"),
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 2);
                DeleteRight(editor);
                Assert.Equal("a\ncd", Value(editor));

                DeleteRight(editor);
                Assert.Equal("acd", Value(editor));

                // At the end of the file: nothing to delete
                MoveTo(editor.ViewModel, 1, 4);
                DeleteRight(editor);
                Assert.Equal("acd", Value(editor));
            });
    }

    [Fact]
    public void DeleteRight_TrimWhitespaceOnDelete_JoinsLinesWithoutLeadingWhitespace()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "ab", "    cd"),
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 3);
                DeleteRight(editor);
                Assert.Equal("abcd", Value(editor));
            },
            new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { TrimWhitespaceOnDelete = true }));
    }

    [Fact]
    public void DeleteLeft_SeveralCursors_DeletesAtEachCursorInOneStep()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "abc", "def"),
            editor =>
            {
                editor.ViewModel.SetSelections(null, [new Selection(1, 3, 1, 3), new Selection(2, 3, 2, 3)]);
                DeleteLeft(editor);
                Assert.Equal("ac\ndf", Value(editor));
            });
    }
}

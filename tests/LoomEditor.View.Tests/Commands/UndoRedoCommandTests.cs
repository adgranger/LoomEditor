// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (issue #44805)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Commands;

public class UndoRedoCommandTests
{
    // Monaco: 'issue #44805: Should not be able to undo in readonly editor'
    [Fact]
    public void Issue44805ShouldNotBeAbleToUndoInReadonlyEditor()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Empty,
            editor =>
            {
                TextModel model = editor.Model;
                model.PushEditOperations(
                    [new Selection(1, 1, 1, 1)],
                    [new TextEdit(new Range(1, 1, 1, 1), "Hello world!")],
                    _ => [new Selection(1, 1, 1, 1)]);
                Assert.Equal("Hello world!", model.GetValue(EndOfLinePreference.LF));

                CoreEditingCommands.Undo.RunCoreEditingCommand(editor.ViewModel);
                Assert.Equal("Hello world!", model.GetValue(EndOfLinePreference.LF));
            },
            new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { ReadOnly = true }));
    }

    [Fact]
    public void Execute_UndoAndRedo_RestoreTheTextAndTheCursor()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Empty,
            editor =>
            {
                var registry = new CommandRegistry();
                using IDisposable registration = CoreCommandRegistration.Register(registry, editor.ViewModel);

                editor.ViewModel.Type("abc", "keyboard");
                MoveTo(editor.ViewModel, 1, 1);

                Assert.True(registry.Execute("undo"));
                Assert.Equal(string.Empty, editor.Model.GetValue());
                AssertCursor(editor.ViewModel, new Position(1, 1));

                Assert.True(registry.Execute("redo"));
                Assert.Equal("abc", editor.Model.GetValue());
                AssertCursor(editor.ViewModel, new Position(1, 4));
            });
    }

    [Fact]
    public void ReadOnly_TypeAndDeleteLeft_ChangeNothingAndRaiseOneReadOnlyEditAttemptEach()
    {
        TestCodeEditor.WithTestCodeEditor(
            "abc",
            editor =>
            {
                var attempts = 0;
                editor.ViewModel.EventDispatcher.OnEvent += (_, e) =>
                {
                    if (e is ReadOnlyEditAttemptEvent)
                    {
                        attempts++;
                    }
                };

                editor.ViewModel.Type("x", "keyboard");
                Assert.Equal(1, attempts);

                CoreEditingCommands.DeleteLeft.RunCoreEditingCommand(editor.ViewModel);
                Assert.Equal(2, attempts);

                Assert.Equal("abc", editor.Model.GetValue());
            },
            new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { ReadOnly = true }));
    }
}

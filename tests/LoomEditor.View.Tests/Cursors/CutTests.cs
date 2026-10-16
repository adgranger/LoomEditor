// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller', cut tests)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class CutTests
{
    // Monaco: 'bug #16740: [editor] Cut line doesn't quite cut the last line'
    [Fact]
    public void Bug16740EditorCutLineDoesntQuiteCutTheLastLine()
    {
        // Part 1 => there is text on the last line
        TestCodeEditor.WithTestCodeEditor(
            ["asdasd", "qwerty"],
            editor =>
            {
                MoveTo(editor.ViewModel, 2, 1, false);
                AssertCursor(editor.ViewModel, new Selection(2, 1, 2, 1));

                editor.ViewModel.Cut("keyboard");
                Assert.Equal(1, editor.Model.LineCount);
                Assert.Equal("asdasd", editor.Model.GetLineContent(1));
            });

        // Part 2 => there is no text on the last line
        TestCodeEditor.WithTestCodeEditor(
            ["asdasd", string.Empty],
            editor =>
            {
                MoveTo(editor.ViewModel, 2, 1, false);
                AssertCursor(editor.ViewModel, new Selection(2, 1, 2, 1));

                editor.ViewModel.Cut("keyboard");
                Assert.Equal(1, editor.Model.LineCount);
                Assert.Equal("asdasd", editor.Model.GetLineContent(1));

                editor.ViewModel.Cut("keyboard");
                Assert.Equal(1, editor.Model.LineCount);
                Assert.Equal(string.Empty, editor.Model.GetLineContent(1));
            });
    }

    // Monaco: 'issue #128602: When cutting multiple lines (ctrl x), the last line will not be erased'
    [Fact]
    public void Issue128602WhenCuttingMultipleLinesCtrlXTheLastLineWillNotBeErased()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["a1", "a2", "a3"],
            editor =>
            {
                editor.ViewModel.SetSelections(
                    "test",
                    [new Selection(1, 1, 1, 1), new Selection(2, 1, 2, 1), new Selection(3, 1, 3, 1)]);

                editor.ViewModel.Cut("keyboard");
                Assert.Equal(1, editor.Model.LineCount);
                Assert.Equal(string.Empty, editor.Model.GetLineContent(1));
            });
    }

    [Fact]
    public void Cut_Selection_RemovesTheSelectedText()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abcdef"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 2, 1, 4)]);

                editor.ViewModel.Cut("keyboard");

                Assert.Equal("adef", editor.Model.GetLineContent(1));
            });
    }

    [Fact]
    public void Cut_EmptySelectionWithEmptySelectionClipboardOff_DoesNothing()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["a", "b"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 1);

                editor.ViewModel.Cut("keyboard");

                Assert.Equal("a\nb", editor.Model.GetValue());
            },
            new TestCodeEditorOptions(CursorOptions: LoomEditor.View.Cursors.EditorCursorOptions.Default with { EmptySelectionClipboard = false }));
    }
}

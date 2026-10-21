// Ported from microsoft/vscode: src/vs/editor/contrib/find/test/browser/find.test.ts (suite 'Find')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;

public class SelectionSearchStringTests
{
    private static readonly string[] Lines = ["ABC DEF", "0123 456"];

    private static void Select(TestCodeEditor editor, int startLine, int startColumn, int endLine, int endColumn) =>
        editor.ViewModel.SetSelections("test", [new Selection(startLine, startColumn, endLine, endColumn)]);

    // Monaco: 'search string at position'
    [Fact]
    public void Get_CursorInWord_ReturnsWord() => TestCodeEditor.WithTestCodeEditor(Lines, editor =>
    {
        // The cursor is at the very top, of the file, at the first ABC
        Assert.Equal("ABC", SelectionSearchString.Get(editor.ViewModel));

        // Move cursor to the end of ABC
        Select(editor, 1, 3, 1, 3);
        Assert.Equal("ABC", SelectionSearchString.Get(editor.ViewModel));

        // Move cursor to DEF
        Select(editor, 1, 5, 1, 5);
        Assert.Equal("DEF", SelectionSearchString.Get(editor.ViewModel));
    });

    // Monaco: 'search string with selection'
    [Fact]
    public void Get_Selection_ReturnsSelectedText() => TestCodeEditor.WithTestCodeEditor(Lines, editor =>
    {
        Select(editor, 1, 1, 1, 2);
        Assert.Equal("A", SelectionSearchString.Get(editor.ViewModel));

        Select(editor, 1, 2, 1, 4);
        Assert.Equal("BC", SelectionSearchString.Get(editor.ViewModel));

        Select(editor, 1, 2, 1, 7);
        Assert.Equal("BC DE", SelectionSearchString.Get(editor.ViewModel));
    });

    // Monaco: 'search string with multiline selection'
    [Fact]
    public void Get_MultilineSelection_ReturnsNull() => TestCodeEditor.WithTestCodeEditor(Lines, editor =>
    {
        // Select first line and newline
        Select(editor, 1, 1, 2, 1);
        Assert.Null(SelectionSearchString.Get(editor.ViewModel));

        // Select first line and chunk of second
        Select(editor, 1, 1, 2, 4);
        Assert.Null(SelectionSearchString.Get(editor.ViewModel));

        // Select end of first line newline and chunk of second
        Select(editor, 1, 7, 2, 4);
        Assert.Null(SelectionSearchString.Get(editor.ViewModel));
    });
}

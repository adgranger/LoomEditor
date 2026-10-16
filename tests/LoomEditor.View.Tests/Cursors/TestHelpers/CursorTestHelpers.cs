// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (lines 36–125)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Cursors.TestHelpers;

/// <summary>The move helpers of Monaco's <c>cursor.test.ts</c>: they run the core commands, like the keyboard does.</summary>
internal static class CursorTestHelpers
{
    public static void MoveTo(ViewModel viewModel, int lineNumber, int column, bool inSelectionMode = false)
    {
        var args = new MoveCommandArgs(new Position(lineNumber, column));
        if (inSelectionMode)
        {
            CoreNavigationCommands.MoveToSelect.RunCoreEditorCommand(viewModel, args);
        }
        else
        {
            CoreNavigationCommands.MoveTo.RunCoreEditorCommand(viewModel, args);
        }
    }

    public static void MoveLeft(ViewModel viewModel, bool inSelectionMode = false) =>
        Run(viewModel, inSelectionMode, CoreNavigationCommands.CursorLeftSelect, CoreNavigationCommands.CursorLeft);

    public static void MoveRight(ViewModel viewModel, bool inSelectionMode = false) =>
        Run(viewModel, inSelectionMode, CoreNavigationCommands.CursorRightSelect, CoreNavigationCommands.CursorRight);

    public static void MoveDown(ViewModel viewModel, bool inSelectionMode = false) =>
        Run(viewModel, inSelectionMode, CoreNavigationCommands.CursorDownSelect, CoreNavigationCommands.CursorDown);

    public static void MoveUp(ViewModel viewModel, bool inSelectionMode = false) =>
        Run(viewModel, inSelectionMode, CoreNavigationCommands.CursorUpSelect, CoreNavigationCommands.CursorUp);

    public static void MoveToBeginningOfLine(ViewModel viewModel, bool inSelectionMode = false)
    {
        CoreEditorCommand<BaseCommandArgs> command = inSelectionMode ? CoreNavigationCommands.CursorHomeSelect : CoreNavigationCommands.CursorHome;
        command.RunCoreEditorCommand(viewModel, new BaseCommandArgs());
    }

    public static void MoveToEndOfLine(ViewModel viewModel, bool inSelectionMode = false)
    {
        CoreEditorCommand<EndCommandArgs> command = inSelectionMode ? CoreNavigationCommands.CursorEndSelect : CoreNavigationCommands.CursorEnd;
        command.RunCoreEditorCommand(viewModel, new EndCommandArgs());
    }

    public static void MoveToBeginningOfBuffer(ViewModel viewModel, bool inSelectionMode = false)
    {
        CoreEditorCommand<BaseCommandArgs> command = inSelectionMode ? CoreNavigationCommands.CursorTopSelect : CoreNavigationCommands.CursorTop;
        command.RunCoreEditorCommand(viewModel, new BaseCommandArgs());
    }

    public static void MoveToEndOfBuffer(ViewModel viewModel, bool inSelectionMode = false)
    {
        CoreEditorCommand<BaseCommandArgs> command = inSelectionMode ? CoreNavigationCommands.CursorBottomSelect : CoreNavigationCommands.CursorBottom;
        command.RunCoreEditorCommand(viewModel, new BaseCommandArgs());
    }

    // Monaco: editor.runCommand(CoreEditingCommands.DeleteLeft, null)
    public static void DeleteLeft(TestCodeEditor editor) => CoreEditingCommands.DeleteLeft.RunCoreEditingCommand(editor.ViewModel);

    // Monaco: editor.runCommand(CoreEditingCommands.DeleteRight, null)
    public static void DeleteRight(TestCodeEditor editor) => CoreEditingCommands.DeleteRight.RunCoreEditingCommand(editor.ViewModel);

    public static void AssertCursor(ViewModel viewModel, Position position) =>
        AssertCursor(viewModel, [new Selection(position, position)]);

    public static void AssertCursor(ViewModel viewModel, Selection selection) =>
        AssertCursor(viewModel, [selection]);

    public static void AssertCursor(ViewModel viewModel, IReadOnlyList<Selection> selections) =>
        Assert.Equal(selections.Select(s => s.ToString()), viewModel.GetSelections().Select(s => s.ToString()));

    private static void Run(ViewModel viewModel, bool inSelectionMode, CoreEditorCommand<CursorMoveCommandArgs> select, CoreEditorCommand<CursorMoveCommandArgs> plain)
    {
        CoreEditorCommand<CursorMoveCommandArgs> command = inSelectionMode ? select : plain;
        command.RunCoreEditorCommand(viewModel, new CursorMoveCommandArgs());
    }
}

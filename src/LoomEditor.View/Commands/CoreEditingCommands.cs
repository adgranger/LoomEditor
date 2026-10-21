// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Cursors;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Commands;

/// <summary>A core command that edits the text at every cursor (Monaco: <c>CoreEditingCommand</c>).</summary>
/// <remarks>
/// Monaco also passes the editor: <c>editor.pushUndoStop()</c> is <c>viewModel.Model.PushStackElement()</c> and
/// <c>editor.executeCommands(id, commands)</c> is <c>viewModel.ExecuteCommands(commands, id)</c>.
/// Threading: UI thread only; the commands hold no state.
/// </remarks>
internal abstract class CoreEditingCommand(string id)
{
    /// <summary>The Monaco id of the command, for example <c>deleteLeft</c>.</summary>
    public string Id { get; } = id;

    /// <summary>Runs the command on the cursors of <paramref name="viewModel"/>.</summary>
    public abstract void RunCoreEditingCommand(ViewModel viewModel);
}

/// <summary>The core editing commands (Monaco: the namespace <c>CoreEditingCommands</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class CoreEditingCommands
{
    /// <summary>Undoes the last edit of the model; does nothing in a read-only editor (Monaco: <c>undo</c>).</summary>
    public static CoreEditingCommand Undo { get; } = new UndoCommand();

    /// <summary>Redoes the last undone edit; does nothing in a read-only editor (Monaco: <c>redo</c>).</summary>
    public static CoreEditingCommand Redo { get; } = new RedoCommand();

    /// <summary>Shift+Tab (Monaco: <c>outdent</c>).</summary>
    public static CoreEditingCommand Outdent { get; } = new OutdentCommand();

    /// <summary>Tab (Monaco: <c>tab</c>).</summary>
    public static CoreEditingCommand Tab { get; } = new TabCommand();

    /// <summary>Breaks the line at every cursor and leaves the cursors in place (Monaco: <c>lineBreakInsert</c>).</summary>
    public static CoreEditingCommand LineBreakInsert { get; } = new LineBreakInsertCommand();

    /// <summary>Backspace (Monaco: <c>deleteLeft</c>).</summary>
    public static CoreEditingCommand DeleteLeft { get; } = new DeleteLeftCommand();

    /// <summary>Delete (Monaco: <c>deleteRight</c>).</summary>
    public static CoreEditingCommand DeleteRight { get; } = new DeleteRightCommand();

    private sealed class UndoCommand() : CoreEditingCommand("undo")
    {
        // Monaco: UndoCommand.runEditorCommand. The cursor restores its selections from the raw event (it does not use the result).
        public override void RunCoreEditingCommand(ViewModel viewModel)
        {
            if (viewModel.CursorConfig.ReadOnly)
            {
                return;
            }

            viewModel.Model.Undo();
        }
    }

    private sealed class RedoCommand() : CoreEditingCommand("redo")
    {
        public override void RunCoreEditingCommand(ViewModel viewModel)
        {
            if (viewModel.CursorConfig.ReadOnly)
            {
                return;
            }

            viewModel.Model.Redo();
        }
    }

    private sealed class OutdentCommand() : CoreEditingCommand("outdent")
    {
        public override void RunCoreEditingCommand(ViewModel viewModel)
        {
            viewModel.Model.PushStackElement();
            viewModel.ExecuteCommands(TypeOperations.Outdent(viewModel.CursorConfig, viewModel, viewModel.GetSelections()), Id);
            viewModel.Model.PushStackElement();
        }
    }

    private sealed class TabCommand() : CoreEditingCommand("tab")
    {
        public override void RunCoreEditingCommand(ViewModel viewModel)
        {
            viewModel.Model.PushStackElement();
            viewModel.ExecuteCommands(TypeOperations.Tab(viewModel.CursorConfig, viewModel.Model, viewModel.GetSelections()), Id);
            viewModel.Model.PushStackElement();
        }
    }

    private sealed class LineBreakInsertCommand() : CoreEditingCommand("lineBreakInsert")
    {
        public override void RunCoreEditingCommand(ViewModel viewModel)
        {
            viewModel.Model.PushStackElement();
            viewModel.ExecuteCommands(EnterOperation.LineBreakInsert(viewModel.CursorConfig, viewModel.Model, viewModel.GetSelections()), Id);
        }
    }

    private sealed class DeleteLeftCommand() : CoreEditingCommand("deleteLeft")
    {
        public override void RunCoreEditingCommand(ViewModel viewModel)
        {
            (bool shouldPushStackElementBefore, IReadOnlyList<IEditCommand?> commands) = DeleteOperations.DeleteLeft(
                viewModel.GetPrevEditOperationType(),
                viewModel.CursorConfig,
                viewModel,
                viewModel.GetSelections(),
                viewModel.GetCursorAutoClosedCharacters());
            if (shouldPushStackElementBefore)
            {
                viewModel.Model.PushStackElement();
            }

            viewModel.ExecuteCommands(commands, Id);
            viewModel.SetPrevEditOperationType(EditOperationType.DeletingLeft);
        }
    }

    private sealed class DeleteRightCommand() : CoreEditingCommand("deleteRight")
    {
        public override void RunCoreEditingCommand(ViewModel viewModel)
        {
            (bool shouldPushStackElementBefore, IReadOnlyList<IEditCommand?> commands) = DeleteOperations.DeleteRight(
                viewModel.GetPrevEditOperationType(),
                viewModel.CursorConfig,
                viewModel,
                viewModel.GetSelections());
            if (shouldPushStackElementBefore)
            {
                viewModel.Model.PushStackElement();
            }

            viewModel.ExecuteCommands(commands, Id);
            viewModel.SetPrevEditOperationType(EditOperationType.DeletingRight);
        }
    }
}
